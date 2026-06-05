namespace ArgoBooks.Core.Enums;

/// <summary>
/// Match state of an imported bank statement line.
/// </summary>
public enum BankLineMatchStatus
{
    /// <summary>No book record was matched to this line.</summary>
    Unmatched,

    /// <summary>One or more candidate matches were found but not yet confirmed by the user.</summary>
    Suggested,

    /// <summary>The line is confirmed matched to a book record.</summary>
    Matched,

    /// <summary>The user chose to ignore this line (e.g., internal transfer, bank fee already handled).</summary>
    Ignored
}
