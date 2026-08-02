using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Integrations;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Records everything one Stripe sync created (revenues, fee/refund expenses, auto-created
/// customers/products/categories, returns, remembered payouts) plus the cursor/time/counter
/// state before and after, so the UI can offer a single undo/redo for the whole import,
/// mirroring the bank-statement import. Import only appends to the collections, so the created
/// items are captured as the tail of each one.
/// </summary>
public class StripeImportCreation
{
    public List<Revenue> Revenues { get; } = [];
    public List<Expense> Expenses { get; } = [];
    public List<object> Entities { get; } = []; // Customer / Product / Category
    public List<Return> Returns { get; } = [];
    public List<StripePayoutRecord> Payouts { get; } = [];

    public string? PreviousCursor { get; set; }
    public DateTime? PreviousSyncTime { get; set; }
    public string? NewCursor { get; set; }
    public DateTime? NewSyncTime { get; set; }

    public CounterSnapshot Pre { get; set; }
    public CounterSnapshot Post { get; set; }

    public int RevenuesCreated => Revenues.Count;
    public int ExpensesCreated => Expenses.Count;

    /// <summary>True when the sync actually created or remembered anything (so an undo is worth recording).</summary>
    public bool AnyCreated =>
        Revenues.Count > 0 || Expenses.Count > 0 || Entities.Count > 0 || Returns.Count > 0 || Payouts.Count > 0;

    public void Undo(CompanyData data)
    {
        foreach (var r in Revenues) data.Revenues.Remove(r);
        foreach (var e in Expenses) data.Expenses.Remove(e);
        foreach (var ent in Entities)
        {
            if (ent is Customer c) data.Customers.Remove(c);
            else if (ent is Product p) data.Products.Remove(p);
            else if (ent is Category cat) data.Categories.Remove(cat);
        }
        foreach (var ret in Returns) data.Returns.Remove(ret);

        var stripe = data.Settings.Integrations.Stripe;
        foreach (var po in Payouts) stripe.ImportedPayouts.Remove(po);
        stripe.LastSyncCursor = PreviousCursor;
        stripe.LastSyncTime = PreviousSyncTime;

        Pre.RestoreTo(data.IdCounters);
        data.MarkAsModified();
    }

    public void Redo(CompanyData data)
    {
        foreach (var ent in Entities)
        {
            if (ent is Customer c && !data.Customers.Contains(c)) data.Customers.Add(c);
            else if (ent is Product p && !data.Products.Contains(p)) data.Products.Add(p);
            else if (ent is Category cat && !data.Categories.Contains(cat)) data.Categories.Add(cat);
        }
        foreach (var r in Revenues) if (!data.Revenues.Contains(r)) data.Revenues.Add(r);
        foreach (var e in Expenses) if (!data.Expenses.Contains(e)) data.Expenses.Add(e);
        foreach (var ret in Returns) if (!data.Returns.Contains(ret)) data.Returns.Add(ret);

        var stripe = data.Settings.Integrations.Stripe;
        foreach (var po in Payouts) if (!stripe.ImportedPayouts.Contains(po)) stripe.ImportedPayouts.Add(po);
        stripe.LastSyncCursor = NewCursor;
        stripe.LastSyncTime = NewSyncTime;

        Post.RestoreTo(data.IdCounters);
        data.MarkAsModified();
    }

    /// <summary>Snapshot of the id counters the Stripe import can bump, so undo/redo restores them exactly.</summary>
    public readonly record struct CounterSnapshot(int Revenue, int Expense, int Customer, int Product, int Category, int Return)
    {
        public static CounterSnapshot From(IdCounters c) =>
            new(c.Revenue, c.Expense, c.Customer, c.Product, c.Category, c.Return);

        public void RestoreTo(IdCounters c)
        {
            c.Revenue = Revenue;
            c.Expense = Expense;
            c.Customer = Customer;
            c.Product = Product;
            c.Category = Category;
            c.Return = Return;
        }
    }
}
