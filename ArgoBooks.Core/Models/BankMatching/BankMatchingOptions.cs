using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.BankMatching;

/// <summary>
/// Tunable parameters for the bank matching engine.
/// </summary>
public class BankMatchingOptions
{
    /// <summary>How many days a book record's date may differ from the bank line's date.</summary>
    public int DateWindowDays { get; set; } = 5;

    /// <summary>Absolute amount tolerance for a match (0 = exact amount required).</summary>
    public decimal AmountTolerance { get; set; } = 0m;

    /// <summary>Confidence at or above which a single unambiguous candidate is auto-confirmed.</summary>
    public double AutoMatchThreshold { get; set; } = 0.90;

    /// <summary>Confidence at or above which a candidate is surfaced as a suggestion.</summary>
    public double SuggestThreshold { get; set; } = 0.55;

    /// <summary>Minimum confidence gap between the best and second-best candidate for an auto-match.</summary>
    public double AutoMatchAmbiguityGap { get; set; } = 0.15;

    /// <summary>Which book record types to match against.</summary>
    public HashSet<BookRecordType> Scope { get; set; } =
    [
        BookRecordType.Expense,
        BookRecordType.Revenue,
        BookRecordType.Invoice,
        BookRecordType.Payment
    ];
}
