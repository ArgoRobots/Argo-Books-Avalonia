namespace ArgoBooks.Core.Models.AI;

/// <summary>
/// Request model for AI receipt analysis.
/// </summary>
public class ReceiptAnalysisRequest
{
    /// <summary>
    /// Supplier name extracted from receipt.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Raw OCR text from receipt.
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// Line item descriptions from receipt.
    /// </summary>
    public List<string> LineItemDescriptions { get; set; } = [];

    /// <summary>
    /// Total amount on receipt.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// List of existing suppliers to match against.
    /// </summary>
    public List<ExistingSupplierInfo> ExistingSuppliers { get; set; } = [];

    /// <summary>
    /// List of existing categories to match against.
    /// </summary>
    public List<ExistingCategoryInfo> ExistingCategories { get; set; } = [];
}

/// <summary>
/// Simplified supplier info for AI matching.
/// </summary>
public class ExistingSupplierInfo
{
    /// <summary>
    /// Supplier ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Supplier name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Simplified category info for AI matching.
/// </summary>
public class ExistingCategoryInfo
{
    /// <summary>
    /// Category ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description.
    /// </summary>
    public string? Description { get; set; }
}
