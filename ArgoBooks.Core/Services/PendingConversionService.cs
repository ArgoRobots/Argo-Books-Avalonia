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

    public PendingConversionService(IPlatformService platformService, IErrorLogger? errorLogger = null)
    {
        _platformService = platformService;
        _errorLogger = errorLogger;
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
            // Avoid duplicates
            if (_queue.Any(p => p.TransactionId == entry.TransactionId))
                return;
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

            // Remove entries for transactions that have already been converted
            _queue.RemoveAll(p =>
            {
                var transaction = FindTransaction(companyData, p.TransactionId, p.TransactionType);
                return transaction != null && !transaction.IsPendingConversion;
            });

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
        var exchangeService = ExchangeRateService.Instance;
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
                // Try to get the exchange rate (will fetch from API if missing)
                var rate = await exchangeService.GetExchangeRateAsync(
                    entry.OriginalCurrency, "USD", entry.TransactionDate, fetchIfMissing: true);

                // If the specific date's rate is unavailable and the date is not in the future,
                // try today's rate as a fallback. Future-dated transactions must wait until
                // their date arrives so they get the correct historical rate.
                if (rate <= 0 && entry.TransactionDate.Date != DateTime.Today && entry.TransactionDate.Date < DateTime.Today)
                {
                    rate = await exchangeService.GetExchangeRateAsync(
                        entry.OriginalCurrency, "USD", DateTime.Today, fetchIfMissing: true);
                }

                if (rate <= 0)
                    continue; // Still offline or rate unavailable, skip

                // Find the transaction and apply the conversion
                var transaction = FindTransaction(companyData, entry.TransactionId, entry.TransactionType);
                if (transaction == null)
                {
                    // Transaction was deleted, remove from queue
                    processed.Add(entry);
                    continue;
                }

                // Apply USD conversion
                transaction.TotalUSD = Math.Round(entry.Total * rate, 2);
                transaction.TaxAmountUSD = Math.Round(entry.TaxAmount * rate, 2);
                transaction.ShippingCostUSD = Math.Round(entry.ShippingCost * rate, 2);
                transaction.DiscountUSD = Math.Round(entry.Discount * rate, 2);
                transaction.FeeUSD = Math.Round(entry.Fee * rate, 2);
                transaction.UnitPriceUSD = Math.Round(entry.UnitPrice * rate, 2);
                transaction.IsPendingConversion = false;

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
