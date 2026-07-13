namespace ArgoBooks.Core.Services.Sync;

/// <summary>
/// Small, read-only projection of <see cref="Data.CompanyData"/> for the paired mobile app.
/// This is NOT the raw company file: it is a purpose-built read model containing just the
/// dashboard totals and lightweight row lists for each section the mobile prototype shows.
/// A later task encrypts and uploads this via <see cref="SyncCrypto"/> / <see cref="SyncService"/>.
/// </summary>
public sealed class MobileSnapshot
{
    /// <summary>Dashboard summary totals (money in/out, profit, margin).</summary>
    public DashboardDto Dashboard { get; init; } = new();

    /// <summary>Expense rows: vendor as Title, date as Subtitle, formatted amount.</summary>
    public List<RowDto> Expenses { get; init; } = [];

    /// <summary>Revenue rows: customer as Title, date as Subtitle, formatted amount.</summary>
    public List<RowDto> Revenue { get; init; } = [];

    /// <summary>Invoice rows: invoice number as Title, status as Subtitle, formatted total.</summary>
    public List<RowDto> Invoices { get; init; } = [];

    /// <summary>Customer rows: name as Title, company as Subtitle, formatted outstanding balance.</summary>
    public List<RowDto> Customers { get; init; } = [];

    /// <summary>Supplier rows: name as Title, contact person as Subtitle, formatted total spent.</summary>
    public List<RowDto> Suppliers { get; init; } = [];

    /// <summary>Product rows: name as Title, SKU as Subtitle, stock-on-hand as Amount.</summary>
    public List<RowDto> Products { get; init; } = [];

    /// <summary>When this snapshot was built (UTC).</summary>
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Dashboard summary totals shown at the top of the mobile app.
/// </summary>
public sealed class DashboardDto
{
    /// <summary>Total collected revenue (gross, USD).</summary>
    public decimal MoneyIn { get; init; }

    /// <summary>Total expenses (gross, USD).</summary>
    public decimal MoneyOut { get; init; }

    /// <summary>MoneyIn - MoneyOut.</summary>
    public decimal Profit { get; init; }

    /// <summary>Profit as a fraction of MoneyIn (0 when MoneyIn is 0).</summary>
    public decimal ProfitMargin { get; init; }
}

/// <summary>
/// A single lightweight row for a mobile section list. Values are pre-formatted display
/// strings (not raw numbers) so the phone doesn't need to duplicate currency/locale
/// formatting logic; it just renders Title/Subtitle/Amount as-is.
/// </summary>
public sealed class RowDto
{
    /// <summary>Primary label (e.g. vendor/customer name, invoice number, product name).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Secondary label (e.g. date, status, contact person, SKU).</summary>
    public string Subtitle { get; init; } = string.Empty;

    /// <summary>Pre-formatted amount/value (e.g. "-$40.00", "42 in stock").</summary>
    public string Amount { get; init; } = string.Empty;
}
