using ArgoBooks.Core.Data;

namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Everything currently waiting in the merchant's Argo Books API queue.
/// Read-only: building a preview never changes the books or the server.
/// </summary>
public record ArgoApiSyncPreview(
    IReadOnlyList<ArgoCustomer> Customers,
    IReadOnlyList<ArgoSupplier> Suppliers,
    IReadOnlyList<ArgoCategory> Categories,
    IReadOnlyList<ArgoProduct> Products,
    IReadOnlyList<ArgoExpense> Expenses,
    IReadOnlyList<ArgoRevenue> Revenue,
    IReadOnlyList<ArgoRefund> Refunds)
{
    public int TotalObjects =>
        Customers.Count + Suppliers.Count + Categories.Count +
        Products.Count + Expenses.Count + Revenue.Count + Refunds.Count;

    public bool HasActivity => TotalObjects > 0;

    /// <summary>Gross revenue waiting, for the "you are about to import" summary.</summary>
    public decimal TotalRevenue => Revenue.Sum(r => ArgoMoney.ToDecimal(r.Amount, r.Currency));

    /// <summary>Total expenses waiting, likewise.</summary>
    public decimal TotalExpenses => Expenses.Sum(e => ArgoMoney.ToDecimal(e.Amount, e.Currency));

    public static ArgoApiSyncPreview Empty() => new([], [], [], [], [], [], []);
}

/// <summary>
/// Orchestrates an Argo Books API sync: pull what developers have pushed, let the
/// merchant look at it, then import the approved objects into the books and tell
/// the server they were taken.
///
/// The claim happens after the local write, not before. Both orders can fail
/// somewhere, and this is the order whose failure is recoverable: a local write
/// with no claim can be undone precisely from memory, whereas a claim with no
/// local write would have told the developer their data landed in books that
/// never received it.
/// </summary>
public class ArgoApiSyncService
{
    private readonly ArgoApiClient _client;

    public ArgoApiSyncService(ArgoApiClient client) => _client = client;

    public async Task<ArgoApiSyncPreview> PreviewAsync(CompanyData data, CancellationToken ct = default)
    {
        var api = data.Settings.Integrations.ArgoApi;
        if (!api.Enabled || string.IsNullOrWhiteSpace(api.DesktopKey))
            return ArgoApiSyncPreview.Empty();

        var key = api.DesktopKey!;

        // Sequential rather than concurrent: the server rate-limits per key, and
        // seven parallel paginated drains is the one client most likely to trip it.
        var categories = await _client.ListPendingAsync<ArgoCategory>(key, "categories", ct: ct);
        var customers = await _client.ListPendingAsync<ArgoCustomer>(key, "customers", ct: ct);
        var suppliers = await _client.ListPendingAsync<ArgoSupplier>(key, "suppliers", ct: ct);
        var products = await _client.ListPendingAsync<ArgoProduct>(key, "products", ct: ct);
        var expenses = await _client.ListPendingAsync<ArgoExpense>(key, "expenses", expandLineItems: true, ct: ct);
        var revenue = await _client.ListPendingAsync<ArgoRevenue>(key, "revenue", expandLineItems: true, ct: ct);
        var refunds = await _client.ListPendingAsync<ArgoRefund>(key, "refunds", ct: ct);

        return new ArgoApiSyncPreview(customers, suppliers, categories, products, expenses, revenue, refunds);
    }

    /// <summary>
    /// Import the preview and claim it server-side. Returns a record of everything
    /// created so the caller can register one undo/redo for the whole import.
    ///
    /// If the claim fails, the local changes are rolled back and the exception is
    /// rethrown, so the merchant is never left with books the server disagrees with.
    /// </summary>
    public async Task<ArgoApiImportCreation> ImportPreviewAsync(
        CompanyData data, ArgoApiSyncPreview preview, CancellationToken ct = default)
    {
        var api = data.Settings.Integrations.ArgoApi;
        var creation = new ArgoApiImportCreation
        {
            PreviousSyncTime = api.LastSyncTime,
            Pre = ArgoApiImportCreation.CounterSnapshot.From(data.IdCounters)
        };

        if (!preview.HasActivity || string.IsNullOrWhiteSpace(api.DesktopKey))
        {
            creation.Post = creation.Pre;
            return creation;
        }

        new ArgoApiImporter().Import(data, preview, creation);

        try
        {
            var batch = await _client.CreateImportBatchAsync(
                api.DesktopKey!, creation.ClaimedObjectIds, creation.LocalRefs, ct);

            creation.BatchId = batch?.Id;
        }
        catch
        {
            // Undo restores the id counters too, so a retry produces the same ids
            // rather than leaving a gap in the sequence.
            creation.Undo(data);
            throw;
        }

        if (creation.BatchId != null)
            api.ImportedBatches.Add(creation.BatchId);

        api.LastSyncTime = DateTime.Now;
        creation.NewSyncTime = api.LastSyncTime;
        creation.Post = ArgoApiImportCreation.CounterSnapshot.From(data.IdCounters);
        data.MarkAsModified();

        return creation;
    }

    /// <summary>
    /// Release a batch the merchant has undone, so the objects return to the queue.
    ///
    /// Deliberately swallows failures: the local undo has already happened and must
    /// not be blocked by the network. The batch id stays in the company file, so a
    /// later sync can notice the disagreement and retry.
    /// </summary>
    public async Task<bool> TryReleaseBatchAsync(CompanyData data, string batchId, CancellationToken ct = default)
    {
        var api = data.Settings.Integrations.ArgoApi;
        if (string.IsNullOrWhiteSpace(api.DesktopKey))
            return false;

        try
        {
            await _client.RevertImportBatchAsync(api.DesktopKey!, batchId, ct);
            return true;
        }
        catch (ArgoApiException)
        {
            return false;
        }
    }
}
