namespace ArgoBooks.Core.Enums;

/// <summary>
/// Why a bank statement line was matched (or suggested) to a book record.
/// Used for display and for ranking candidates.
/// </summary>
public enum MatchReason
{
    /// <summary>Amount matched exactly and the date was on/near the same day.</summary>
    ExactAmountAndDate,

    /// <summary>Amount matched exactly and the description was a close fuzzy match.</summary>
    ExactAmountFuzzyDesc,

    /// <summary>Amount matched within tolerance and the date fell inside the search window.</summary>
    AmountWithinWindow,

    /// <summary>Suggested by the AI assistant for an otherwise unmatched line.</summary>
    AiSuggested
}
