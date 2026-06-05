namespace ArgoBooks.Core.Models.BankMatching;

/// <summary>
/// Result of running the matching engine over a set of bank statement lines.
/// Transient (not persisted): the persisted state lives on the lines themselves
/// and on the matched book records' BankMatched flags.
/// </summary>
public class BankMatchingResult
{
    /// <summary>All lines, with MatchStatus updated by the engine.</summary>
    public List<BankStatementLine> Lines { get; set; } = [];

    /// <summary>Ranked candidate matches per line id (best first). Empty for matched/unmatched lines.</summary>
    public Dictionary<string, List<BankMatchCandidate>> CandidatesByLineId { get; set; } = [];

    /// <summary>Number of lines auto-matched with high confidence.</summary>
    public int AutoMatchedCount { get; set; }

    /// <summary>Number of lines with one or more suggestions awaiting confirmation.</summary>
    public int SuggestedCount { get; set; }

    /// <summary>Number of lines with no candidate match (possibly missing book entries).</summary>
    public int UnmatchedLineCount { get; set; }

    /// <summary>
    /// In-scope book records not matched to any bank line (possibly missing from the
    /// statement, or duplicate entries).
    /// </summary>
    public List<BookRecordRef> UnmatchedBookRecords { get; set; } = [];
}
