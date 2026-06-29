using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Manages a persistent queue of transactions saved offline that need USD conversion.
/// Uses two-layer persistence: app-data file (immediate) + CompanyData (on save).
/// </summary>
public class PendingConversionService
{
    private const string QueueFileName = "pending_conversions.json";

    private readonly IPlatformService _platformService;
    private readonly IErrorLogger? _errorLogger;
    private readonly ExchangeRateService? _exchangeRateService;
    private readonly List<PendingConversion> _queue = [];
    private readonly Lock _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static PendingConversionService? Instance { get; private set; }

    /// <summary>
    /// Fired after pending conversions are successfully processed.
    /// UI should refresh transaction lists and charts.
    /// </summary>
    public event EventHandler<PendingConversionsProcessedEventArgs>? PendingConversionsProcessed;

    public PendingConversionService(IErrorLogger? errorLogger = null)
        : this(PlatformServiceFactory.GetPlatformService(), errorLogger)
    {
    }

    public PendingConversionService(IPlatformService platformService, IErrorLogger? errorLogger = null, ExchangeRateService? exchangeRateService = null)
    {
        _platformService = platformService;
        _errorLogger = errorLogger;
        _exchangeRateService = exchangeRateService;
        Instance ??= this;
    }

    /// <summary>
    /// Number of pending conversions in the queue.
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (_lock) return _queue.Count;
        }
    }

    /// <summary>
    /// Whether there are any pending conversions.
    /// </summary>
    public bool HasPendingConversions => PendingCount > 0;

    /// <summary>
    /// Checks if a specific transaction is pending conversion.
    /// </summary>
    public bool IsTransactionPending(string transactionId)
    {
        lock (_lock) return _queue.Any(p => p.TransactionId == transactionId);
    }

    /// <summary>
    /// Adds a pending conversion entry and immediately persists to disk.
    /// </summary>
    public async Task AddPendingConversionAsync(PendingConversion entry)
    {
        lock (_lock)
        {
            // Replace any existing entry for this record so a later edit's amounts win. ApplyConversion
            // converts from this snapshot, not the live row, and the self-heal is the guarantee that an
            // offline row eventually gets its correct exact-date USD (Calculations.md Rule 3a) - so the
            // snapshot must reflect the row's CURRENT amounts, not the ones from its first save.
            _queue.RemoveAll(p => p.TransactionId == entry.TransactionId);
            _queue.Add(entry);
        }

        await SaveToDiskAsync();
    }

    /// <summary>
    /// Loads the queue from the app-data directory file.
    /// </summary>
    public async Task LoadAsync()
    {
        if (!_platformService.SupportsFileSystem)
            return;

        var filePath = GetQueueFilePath();
        if (!File.Exists(filePath))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var entries = JsonSerializer.Deserialize<List<PendingConversion>>(json, JsonOptions);
            if (entries != null)
            {
                lock (_lock)
                {
                    _queue.Clear();
                    _queue.AddRange(entries);
                }
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning($"Failed to load pending conversions: {ex.Message}", "PendingConversionService");
        }
    }

    /// <summary>
    /// Reconciles the in-memory queue with the CompanyData's PendingConversions list.
    /// Merges entries from both sources (app-data file may have entries not yet in .argo file and vice versa).
    /// Also removes entries for transactions that have already been converted (IsPendingConversion = false).
    /// </summary>
    public async Task ReconcileWithCompanyDataAsync(CompanyData companyData)
    {
        lock (_lock)
        {
            // Build a set of all known transaction IDs
            var existingIds = new HashSet<string>(_queue.Select(p => p.TransactionId));

            // Add any entries from CompanyData that we don't already have
            foreach (var entry in companyData.PendingConversions)
            {
                if (existingIds.Add(entry.TransactionId))
                {
                    _queue.Add(entry);
                }
            }

            // Remove entries for records that have already been converted
            _queue.RemoveAll(p => IsConverted(companyData, p));

            // Sync back to CompanyData
            companyData.PendingConversions.Clear();
            companyData.PendingConversions.AddRange(_queue);
        }

        await SaveToDiskAsync();
    }

    /// <summary>
    /// Attempts to process all pending conversions by fetching exchange rates.
    /// Only processes entries where rates are available (online).
    /// </summary>
    public async Task ProcessPendingConversionsAsync(CompanyData companyData)
    {
        var exchangeService = _exchangeRateService ?? ExchangeRateService.Instance;
        if (exchangeService == null)
            return;

        List<PendingConversion> toProcess;
        lock (_lock)
        {
            toProcess = [.. _queue];
        }

        if (toProcess.Count == 0)
            return;

        var processed = new List<PendingConversion>();

        foreach (var entry in toProcess)
        {
            try
            {
                // Convert ONLY at the exact transaction-date rate (fetching it if missing). Never
                // fall back to today's or any other date's rate: a row stays pending until its own
                // date's rate is available. See docs/Calculations.md (Rule 3a).
                var rate = await exchangeService.GetExchangeRateAsync(
                    entry.OriginalCurrency, "USD", entry.TransactionDate, fetchIfMissing: true);

                if (rate <= 0)
                    continue; // Exact-date rate unavailable (offline, or future-dated); stay pending

                // Apply the conversion to the matching record (a no-op if it was deleted since it
                // was enqueued); either way the entry is done and leaves the queue.
                ApplyConversion(companyData, entry, rate);
                processed.Add(entry);
            }
            catch (Exception ex)
            {
                _errorLogger?.LogWarning($"Failed to process pending conversion for {entry.TransactionId}: {ex.Message}", "PendingConversionService");
            }
        }

        if (processed.Count > 0)
        {
            lock (_lock)
            {
                foreach (var entry in processed)
                {
                    _queue.RemoveAll(p => p.TransactionId == entry.TransactionId);
                }

                // Sync back to CompanyData
                companyData.PendingConversions.Clear();
                companyData.PendingConversions.AddRange(_queue);
            }

            // A healed Payment's EffectiveAmountUSD changes from 0 to a real value, which shifts the
            // owning invoice's USD balance. Recalculate those invoices so cross-currency outstanding
            // aggregates aren't left stale until the next company open.
            var healedInvoiceIds = processed
                .Where(e => e.TransactionType == "Payment")
                .Select(e => companyData.Payments.FirstOrDefault(p => p.Id == e.TransactionId)?.InvoiceId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();
            foreach (var invoiceId in healedInvoiceIds)
            {
                var invoice = companyData.Invoices.FirstOrDefault(i => i.Id == invoiceId);
                if (invoice != null)
                    InvoiceTotalsService.Recalculate(invoice, companyData.Payments);
            }

            await SaveToDiskAsync();

            // Mark company data as changed so the next save includes the updated USD values
            companyData.MarkAsModified();

            PendingConversionsProcessed?.Invoke(this, new PendingConversionsProcessedEventArgs(processed.Count));
        }
    }

    private static Transaction? FindTransaction(CompanyData companyData, string id, string type)
    {
        return type switch
        {
            "Expense" => companyData.Expenses.FirstOrDefault(e => e.Id == id),
            "Revenue" => companyData.Revenues.FirstOrDefault(r => r.Id == id),
            _ => null
        };
    }

    /// <summary>
    /// Applies the exact-date conversion to the record named by <paramref name="entry"/>, at the
    /// supplied <paramref name="rate"/> (original currency -> USD). Handles Revenue/Expense (every
    /// money field) and Payment/PurchaseOrder (the single amount). No-ops when the record was deleted
    /// since it was enqueued. Rounding matches the import-time conversion so an immediately-converted
    /// row and a later-healed row are identical.
    /// </summary>
    private static void ApplyConversion(CompanyData companyData, PendingConversion entry, decimal rate)
    {
        switch (entry.TransactionType)
        {
            case "Revenue":
            case "Expense":
                var txn = FindTransaction(companyData, entry.TransactionId, entry.TransactionType);
                if (txn == null) return;
                txn.TotalUSD = Math.Round(entry.Total * rate, 2);
                txn.TaxAmountUSD = Math.Round(entry.TaxAmount * rate, 2);
                txn.ShippingCostUSD = Math.Round(entry.ShippingCost * rate, 2);
                txn.DiscountUSD = Math.Round(entry.Discount * rate, 2);
                txn.FeeUSD = Math.Round(entry.Fee * rate, 2);
                txn.UnitPriceUSD = Math.Round(entry.UnitPrice * rate, 2);
                txn.IsPendingConversion = false;
                return;

            case "Payment":
                var payment = companyData.Payments.FirstOrDefault(p => p.Id == entry.TransactionId);
                if (payment == null) return;
                payment.AmountUSD = Math.Round(entry.Total * rate, 2);
                payment.IsPendingConversion = false;
                return;

            case "PurchaseOrder":
                var po = companyData.PurchaseOrders.FirstOrDefault(p => p.Id == entry.TransactionId);
                if (po == null) return;
                po.TotalUSD = Math.Round(entry.Total * rate, 2);
                po.IsPendingConversion = false;
                return;

            case "Invoice":
                var invoice = companyData.Invoices.FirstOrDefault(i => i.Id == entry.TransactionId);
                if (invoice == null) return;
                invoice.TotalUSD = Math.Round(entry.Total * rate, 2);
                invoice.BalanceUSD = Math.Round(entry.Balance * rate, 2);
                invoice.IsPendingConversion = false;
                return;
        }
    }

    /// <summary>
    /// True when the record named by <paramref name="entry"/> still exists and is no longer pending,
    /// so its queue entry can be dropped. A deleted record returns false (kept; the process pass
    /// removes it). Mirrors the type set handled by <see cref="ApplyConversion"/>.
    /// </summary>
    private static bool IsConverted(CompanyData companyData, PendingConversion entry) => entry.TransactionType switch
    {
        "Revenue" => companyData.Revenues.FirstOrDefault(r => r.Id == entry.TransactionId) is { IsPendingConversion: false },
        "Expense" => companyData.Expenses.FirstOrDefault(e => e.Id == entry.TransactionId) is { IsPendingConversion: false },
        "Payment" => companyData.Payments.FirstOrDefault(p => p.Id == entry.TransactionId) is { IsPendingConversion: false },
        "PurchaseOrder" => companyData.PurchaseOrders.FirstOrDefault(p => p.Id == entry.TransactionId) is { IsPendingConversion: false },
        "Invoice" => companyData.Invoices.FirstOrDefault(i => i.Id == entry.TransactionId) is { IsPendingConversion: false },
        _ => false
    };

    private async Task SaveToDiskAsync()
    {
        if (!_platformService.SupportsFileSystem)
            return;

        try
        {
            List<PendingConversion> snapshot;
            lock (_lock)
            {
                snapshot = [.. _queue];
            }

            var filePath = GetQueueFilePath();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _platformService.EnsureDirectoryExists(directory);
            }

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning($"Failed to save pending conversions: {ex.Message}", "PendingConversionService");
        }
    }

    private string GetQueueFilePath()
    {
        return _platformService.CombinePaths(_platformService.GetAppDataPath(), QueueFileName);
    }
}

/// <summary>
/// Event args for when pending conversions are processed.
/// </summary>
public class PendingConversionsProcessedEventArgs(int convertedCount) : EventArgs
{
    /// <summary>
    /// The number of transactions that were successfully converted.
    /// </summary>
    public int ConvertedCount { get; } = convertedCount;
}
