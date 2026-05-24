using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.BankMatching;

/// <summary>
/// Lightweight reference to a book record (expense, revenue, invoice or payment),
/// used to surface unmatched book entries and candidate matches in the UI.
/// </summary>
public class BookRecordRef
{
    public BookRecordType Type { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    /// <summary>Signed amount aligned to bank convention: negative = money out, positive = money in.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// True when another in-scope book record has the same amount within a few days,
    /// suggesting a possible duplicate entry.
    /// </summary>
    public bool IsPossibleDuplicate { get; set; }
}
