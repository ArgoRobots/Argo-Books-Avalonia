using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Records everything one Argo Books API import created, plus the id-counter
/// state before and after, so the UI can offer a single undo/redo for the whole
/// import. Mirrors <see cref="StripeImportCreation"/>.
///
/// One difference from the Stripe version matters: this import also claimed
/// objects on the server. <see cref="BatchId"/> carries that claim so the caller
/// can release it when the merchant undoes, otherwise the developer's queue would
/// keep reporting data as imported that is no longer in anyone's books.
/// </summary>
public class ArgoApiImportCreation
{
    public List<Revenue> Revenues { get; } = [];
    public List<Expense> Expenses { get; } = [];
    public List<object> Entities { get; } = []; // Customer / Supplier / Product / Category
    public List<Return> Returns { get; } = [];

    /// <summary>The server-side batch this import claimed, once the claim succeeded.</summary>
    public string? BatchId { get; set; }

    /// <summary>Every API object id this import took, in claim order.</summary>
    public List<string> ClaimedObjectIds { get; } = [];

    /// <summary>API id to the local id it became, sent to the server so developers can trace it.</summary>
    public Dictionary<string, string> LocalRefs { get; } = new(StringComparer.Ordinal);

    public DateTime? PreviousSyncTime { get; set; }
    public DateTime? NewSyncTime { get; set; }

    public CounterSnapshot Pre { get; set; }
    public CounterSnapshot Post { get; set; }

    public int RevenuesCreated => Revenues.Count;
    public int ExpensesCreated => Expenses.Count;

    /// <summary>True when the import actually created something, so an undo is worth recording.</summary>
    public bool AnyCreated =>
        Revenues.Count > 0 || Expenses.Count > 0 || Entities.Count > 0 || Returns.Count > 0;

    public void Undo(CompanyData data)
    {
        foreach (var r in Revenues) data.Revenues.Remove(r);
        foreach (var e in Expenses) data.Expenses.Remove(e);
        foreach (var ent in Entities)
        {
            if (ent is Customer c) data.Customers.Remove(c);
            else if (ent is Supplier s) data.Suppliers.Remove(s);
            else if (ent is Product p) data.Products.Remove(p);
            else if (ent is Category cat) data.Categories.Remove(cat);
        }
        foreach (var ret in Returns) data.Returns.Remove(ret);

        var api = data.Settings.Integrations.ArgoApi;
        if (BatchId != null) api.ImportedBatches.Remove(BatchId);
        api.LastSyncTime = PreviousSyncTime;

        Pre.RestoreTo(data.IdCounters);
        data.MarkAsModified();
    }

    public void Redo(CompanyData data)
    {
        // Entities first: revenues and expenses reference them, and re-adding in
        // the other order would briefly leave dangling ids for anything watching.
        foreach (var ent in Entities)
        {
            if (ent is Customer c && !data.Customers.Contains(c)) data.Customers.Add(c);
            else if (ent is Supplier s && !data.Suppliers.Contains(s)) data.Suppliers.Add(s);
            else if (ent is Product p && !data.Products.Contains(p)) data.Products.Add(p);
            else if (ent is Category cat && !data.Categories.Contains(cat)) data.Categories.Add(cat);
        }
        foreach (var r in Revenues) if (!data.Revenues.Contains(r)) data.Revenues.Add(r);
        foreach (var e in Expenses) if (!data.Expenses.Contains(e)) data.Expenses.Add(e);
        foreach (var ret in Returns) if (!data.Returns.Contains(ret)) data.Returns.Add(ret);

        var api = data.Settings.Integrations.ArgoApi;
        if (BatchId != null && !api.ImportedBatches.Contains(BatchId)) api.ImportedBatches.Add(BatchId);
        api.LastSyncTime = NewSyncTime;

        Post.RestoreTo(data.IdCounters);
        data.MarkAsModified();
    }

    /// <summary>Snapshot of the id counters this import can bump, so undo/redo restores them exactly.</summary>
    public readonly record struct CounterSnapshot(
        int Revenue, int Expense, int Customer, int Supplier, int Product, int Category, int Return)
    {
        public static CounterSnapshot From(IdCounters c) =>
            new(c.Revenue, c.Expense, c.Customer, c.Supplier, c.Product, c.Category, c.Return);

        public void RestoreTo(IdCounters c)
        {
            c.Revenue = Revenue;
            c.Expense = Expense;
            c.Customer = Customer;
            c.Supplier = Supplier;
            c.Product = Product;
            c.Category = Category;
            c.Return = Return;
        }
    }
}
