using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.BankMatching;

/// <summary>Per-line instruction for turning a statement line into a transaction.</summary>
public class BankLineResolution
{
    public required BankStatementLine Line { get; set; }
    public BookRecordType Type { get; set; }

    /// <summary>Existing product to attach (carries the category), or null when a new one should be created.</summary>
    public string? ProductId { get; set; }

    /// <summary>Name for a new product to create when <see cref="ProductId"/> is null.</summary>
    public string? NewProductName { get; set; }

    /// <summary>Existing category id for a new product, or null when a new category should be created.</summary>
    public string? ProductCategoryId { get; set; }

    /// <summary>Name for a new category to create for a new product when <see cref="ProductCategoryId"/> is null.</summary>
    public string? NewProductCategoryName { get; set; }

    /// <summary>Existing supplier (Expense) / customer (Revenue) id, or null.</summary>
    public string? CounterpartyId { get; set; }
    public string? NewCounterpartyName { get; set; }
}
