namespace ArgoBooks.Core.Models.AI;

/// <summary>
/// Batched request to categorize bank statement lines: pick an existing product (which carries
/// the category) or propose a new product with a category, plus a supplier/customer, for each line.
/// </summary>
public class BankLineCategorizationRequest
{
    public List<BankLineToCategorize> Lines { get; set; } = [];
    public List<ExistingProductInfo> ExistingProducts { get; set; } = [];
    public List<ExistingCategoryInfo> ExistingExpenseCategories { get; set; } = [];
    public List<ExistingCategoryInfo> ExistingRevenueCategories { get; set; } = [];
    public List<ExistingSupplierInfo> ExistingSuppliers { get; set; } = [];
    public List<ExistingSupplierInfo> ExistingCustomers { get; set; } = [];
}

/// <summary>A single statement line to categorize.</summary>
public class BankLineToCategorize
{
    public int Index { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>True = revenue (money in), false = expense (money out).</summary>
    public bool IsRevenue { get; set; }
}

/// <summary>Simplified product info for AI matching.</summary>
public class ExistingProductInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }

    /// <summary>True for revenue (sale) products, false for expense (purchase) products.</summary>
    public bool IsRevenue { get; set; }
}

/// <summary>AI suggestion for a single bank line: a product (match or new + category) and a counterparty.</summary>
public class BankLineSuggestion
{
    public int Index { get; set; }

    /// <summary>Matched existing product id, or null when a new product is proposed.</summary>
    public string? ProductId { get; set; }

    /// <summary>Proposed new product name when <see cref="ProductId"/> is null.</summary>
    public string? NewProductName { get; set; }

    /// <summary>Existing category id for a new product, or null when a new category is proposed.</summary>
    public string? ProductCategoryId { get; set; }

    /// <summary>Proposed new category name for a new product when <see cref="ProductCategoryId"/> is null.</summary>
    public string? NewProductCategoryName { get; set; }

    /// <summary>Matched existing supplier/customer id, or null when a new one is proposed.</summary>
    public string? CounterpartyId { get; set; }

    /// <summary>Proposed new supplier/customer name when <see cref="CounterpartyId"/> is null.</summary>
    public string? NewCounterpartyName { get; set; }
}
