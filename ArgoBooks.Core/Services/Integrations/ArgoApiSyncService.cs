using ArgoBooks.Core.Data;

namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// An object referenced by something in this preview but imported on an earlier
/// occasion, so no longer pending and therefore absent from the preview itself.
/// <see cref="LocalRef"/> is the id the desktop gave it at that time.
/// </summary>
public record ArgoExternalRef(string Id, string? LocalRef, string? Name, string? Email);

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
    IReadOnlyList<ArgoRefund> Refunds,
    IReadOnlyDictionary<string, ArgoExternalRef> ExternalRefs)
{
    public int TotalObjects =>
        Customers.Count + Suppliers.Count + Categories.Count +
        Products.Count + Expenses.Count + Revenue.Count + Refunds.Count;

    public bool HasActivity => TotalObjects > 0;

    /// <summary>Gross revenue waiting, for the "you are about to import" summary.</summary>
    public decimal TotalRevenue => Revenue.Sum(r => ArgoMoney.ToDecimal(r.Amount, r.Currency));

    /// <summary>Total expenses waiting, likewise.</summary>
    public decimal TotalExpenses => Expenses.Sum(e => ArgoMoney.ToDecimal(e.Amount, e.Currency));

    public static ArgoApiSyncPreview Empty() =>
        new([], [], [], [], [], [], [], new Dictionary<string, ArgoExternalRef>());
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

        var preview = new ArgoApiSyncPreview(
            customers, suppliers, categories, products, expenses, revenue, refunds,
            new Dictionary<string, ArgoExternalRef>());

        var external = await ResolveExternalRefsAsync(key, preview, ct);
        return preview with { ExternalRefs = external };
    }

    /// <summary>
    /// Look up every referenced object that is not itself in this preview.
    ///
    /// A developer creates a customer once and then points a year of orders at
    /// it. That customer is imported with the first batch and is no longer
    /// pending, so nothing after the first batch would find it, and every later
    /// order would import with no customer attached and no error to show for it.
    ///
    /// Only distinct missing ids are fetched, so this is a handful of requests
    /// per import rather than one per row.
    /// </summary>
    private async Task<Dictionary<string, ArgoExternalRef>> ResolveExternalRefsAsync(
        string key, ArgoApiSyncPreview preview, CancellationToken ct)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in preview.Customers) known.Add(c.Id);
        foreach (var x in preview.Suppliers) known.Add(x.Id);
        foreach (var x in preview.Categories) known.Add(x.Id);
        foreach (var x in preview.Products) known.Add(x.Id);
        foreach (var x in preview.Revenue) known.Add(x.Id);

        var wanted = new HashSet<string>(StringComparer.Ordinal);
        void Want(string? id)
        {
            if (!string.IsNullOrEmpty(id) && !known.Contains(id)) wanted.Add(id);
        }

        foreach (var p in preview.Products) Want(p.Category);
        foreach (var e in preview.Expenses)
        {
            Want(e.Supplier);
            Want(e.Category);
            foreach (var li in e.LineItems ?? []) Want(li.Product);
        }
        foreach (var r in preview.Revenue)
        {
            Want(r.Customer);
            Want(r.Category);
            foreach (var li in r.LineItems ?? []) Want(li.Product);
        }
        foreach (var r in preview.Refunds) Want(r.Revenue);

        var resolved = new Dictionary<string, ArgoExternalRef>(StringComparer.Ordinal);
        foreach (var id in wanted)
        {
            var resource = ArgoApiClient.ResourceForId(id);
            if (resource == null) continue;

            var obj = await _client.GetRawObjectAsync(key, resource, id, ct);
            if (obj == null) continue;

            string? Read(string prop) =>
                obj.Value.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
                    ? v.GetString()
                    : null;

            string? localRef = null;
            if (obj.Value.TryGetProperty("import", out var imp)
                && imp.TryGetProperty("local_ref", out var lr)
                && lr.ValueKind == JsonValueKind.String)
            {
                localRef = lr.GetString();
            }

            resolved[id] = new ArgoExternalRef(id, localRef, Read("name"), Read("email"));
        }

        return resolved;
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

        // Cache the rates for the dates about to land, or every row on a day the
        // cache does not already hold shows "Pending" in place of its amount and
        // never recovers, because nothing refetches rates for rows already saved.
        await IntegrationRates.EnsureAsync(
            preview.Expenses.Select(e => (ArgoApiImporter.ParseDate(e.OccurredOn), e.Currency))
                .Concat(preview.Revenue.Select(r => (ArgoApiImporter.ParseDate(r.OccurredOn), r.Currency)))
                .Concat(preview.Refunds.Select(r => (ArgoApiImporter.ParseDate(r.OccurredOn), r.Currency))),
            data.Settings.Localization.Currency,
            ct: ct);

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
