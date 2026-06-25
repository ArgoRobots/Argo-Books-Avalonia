using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.BankMatching;

/// <summary>Per-line instruction for turning an unmatched statement line into a transaction.</summary>
public class BankLineResolution
{
    public required BankStatementLine Line { get; set; }
    public BookRecordType Type { get; set; }

    /// <summary>Existing category to use, or null when a new one should be created.</summary>
    public string? CategoryId { get; set; }
    public string? NewCategoryName { get; set; }

    /// <summary>Existing supplier (Expense) / customer (Revenue) id, or null.</summary>
    public string? CounterpartyId { get; set; }
    public string? NewCounterpartyName { get; set; }
}
