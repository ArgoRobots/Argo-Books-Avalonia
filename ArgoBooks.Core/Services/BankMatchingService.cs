using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Matches imported bank statement lines against recorded book entries (expenses, revenue,
/// invoices, payments). Matching is deterministic and runs locally.
/// </summary>
public class BankMatchingService
{
    // Score weights. amount(0.6) + date(0.3) + description(0.1) = 1.0 maximum.
    private const double AmountExactScore = 0.6;
    private const double AmountToleranceScore = 0.4;
    private const double DateMaxScore = 0.3;
    private const double DescMaxScore = 0.1;

    private static readonly HashSet<string> NoiseTokens =
    [
        "pos", "ach", "debit", "credit", "card", "purchase", "payment", "pmt", "ref",
        "reference", "transaction", "txn", "visa", "mastercard", "amex", "eft", "online",
        "pre", "auth", "authorized", "withdrawal", "deposit", "transfer", "the", "and", "inc", "llc", "ltd"
    ];

    #region Deterministic matching

    /// <summary>
    /// Runs deterministic matching over the lines, auto-confirming unambiguous high-confidence
    /// matches and surfacing the rest as suggestions. Mutates the matched book records' flags.
    /// </summary>
    public BankMatchingResult MatchDeterministic(IReadOnlyList<BankStatementLine> lines, CompanyData data, BankMatchingOptions options)
    {
        var result = new BankMatchingResult { Lines = lines.ToList() };

        // All in-scope book records, aligned to bank sign convention, that aren't already matched.
        var records = BuildRecordRefs(data, options.Scope).Where(r => !IsRecordMatched(data, r)).ToList();

        foreach (var line in result.Lines)
        {
            if (line.MatchStatus is BankLineMatchStatus.Matched or BankLineMatchStatus.Ignored)
                continue;

            var candidates = ScoreCandidates(line, records, options);

            if (candidates.Count == 0)
            {
                line.MatchStatus = BankLineMatchStatus.Unmatched;
                result.UnmatchedLineCount++;
                continue;
            }

            var best = candidates[0];
            var gap = candidates.Count > 1 ? best.Confidence - candidates[1].Confidence : 1.0;

            if (best.Confidence >= options.AutoMatchThreshold && gap >= options.AutoMatchAmbiguityGap)
            {
                best.IsAutoMatch = true;
                ConfirmMatch(line, best, data);
                records.RemoveAll(r => r.Type == best.RecordType && r.Id == best.RecordId);
                result.AutoMatchedCount++;
            }
            else if (best.Confidence >= options.SuggestThreshold)
            {
                line.MatchStatus = BankLineMatchStatus.Suggested;
                result.CandidatesByLineId[line.Id] = candidates.Where(c => c.Confidence >= options.SuggestThreshold).ToList();
                result.SuggestedCount++;
            }
            else
            {
                line.MatchStatus = BankLineMatchStatus.Unmatched;
                result.UnmatchedLineCount++;
            }
        }

        // Reverse view: in-scope book records still unmatched (possibly missing from the statement).
        var stillUnmatched = BuildRecordRefs(data, options.Scope).Where(r => !IsRecordMatched(data, r)).ToList();
        FlagDuplicates(stillUnmatched, options.DateWindowDays);
        result.UnmatchedBookRecords = stillUnmatched;

        return result;
    }

    /// <summary>
    /// Returns ranked match candidates (≥ SuggestThreshold) for a single line against the
    /// currently unmatched, in-scope book records. Pure: does not mutate anything.
    /// </summary>
    public List<BankMatchCandidate> FindCandidates(BankStatementLine line, CompanyData data, BankMatchingOptions options)
    {
        var records = BuildRecordRefs(data, options.Scope).Where(r => !IsRecordMatched(data, r));
        return ScoreCandidates(line, records, options)
            .Where(c => c.Confidence >= options.SuggestThreshold)
            .ToList();
    }

    /// <summary>
    /// Returns all unmatched, in-scope records the user can manually pick to match a line,
    /// filtered to the line's direction (money out -> expenses, money in -> revenue). Not scored.
    /// </summary>
    public List<BankMatchCandidate> GetManualMatchOptions(BankStatementLine line, CompanyData data, BankMatchingOptions options)
    {
        return BuildRecordRefs(data, options.Scope)
            .Where(r => !IsRecordMatched(data, r))
            .Where(r => line.Amount == 0
                        || (line.Amount < 0 && r.Type == BookRecordType.Expense)
                        || (line.Amount > 0 && r.Type != BookRecordType.Expense))
            .OrderByDescending(r => r.Date)
            .Select(r => new BankMatchCandidate
            {
                LineId = line.Id,
                RecordType = r.Type,
                RecordId = r.Id,
                RecordDescription = r.Description,
                RecordDate = r.Date,
                RecordAmount = r.Amount,
                Reason = MatchReason.Manual
            })
            .ToList();
    }

    /// <summary>
    /// Scores every amount-compatible record against a line and returns candidates ranked best-first.
    /// </summary>
    private static List<BankMatchCandidate> ScoreCandidates(BankStatementLine line, IEnumerable<BookRecordRef> records, BankMatchingOptions options)
    {
        var candidates = new List<BankMatchCandidate>();
        foreach (var record in records)
        {
            var amountDiff = Math.Abs(line.Amount - record.Amount);
            double amountScore;
            if (amountDiff == 0m) amountScore = AmountExactScore;
            else if (amountDiff <= options.AmountTolerance) amountScore = AmountToleranceScore;
            else continue; // amount must match (exactly or within tolerance)

            var daysApart = Math.Abs((line.Date.Date - record.Date.Date).TotalDays);
            if (daysApart > options.DateWindowDays && amountScore < AmountExactScore)
                continue; // outside window and not an exact-amount hit
            var dateScore = daysApart <= options.DateWindowDays
                ? DateMaxScore * (1 - daysApart / Math.Max(1, options.DateWindowDays))
                : 0;

            var descScore = DescMaxScore * Similarity(NormalizeDescription(line.Description), NormalizeDescription(record.Description));

            var confidence = amountScore + dateScore + descScore;

            candidates.Add(new BankMatchCandidate
            {
                LineId = line.Id,
                RecordType = record.Type,
                RecordId = record.Id,
                RecordDescription = record.Description,
                RecordDate = record.Date,
                RecordAmount = record.Amount,
                Confidence = Math.Round(confidence, 4),
                Reason = DetermineReason(amountScore, daysApart, descScore)
            });
        }

        // Rank best first; for money-in ties prefer Payment over Invoice to avoid double counting.
        return candidates
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => RecordTypeRank(c.RecordType, line.Amount))
            .ToList();
    }

    private static int RecordTypeRank(BookRecordType type, decimal lineAmount)
    {
        // For money-in lines, rank Payment ahead of Invoice (payments are the actual cash event).
        if (lineAmount > 0)
            return type switch
            {
                BookRecordType.Payment => 0,
                BookRecordType.Revenue => 1,
                BookRecordType.Invoice => 2,
                _ => 3
            };
        return 0;
    }

    private static MatchReason DetermineReason(double amountScore, double daysApart, double descScore)
    {
        var amountExact = amountScore >= AmountExactScore;
        if (amountExact && daysApart <= 1) return MatchReason.ExactAmountAndDate;
        if (amountExact && descScore > 0) return MatchReason.ExactAmountFuzzyDesc;
        return MatchReason.AmountWithinWindow;
    }

    private static void FlagDuplicates(List<BookRecordRef> records, int dateWindowDays)
    {
        // Group by amount first so we only compare records that could possibly be duplicates,
        // then compare within each (typically tiny) group by date proximity.
        foreach (var group in records.GroupBy(r => r.Amount).Where(g => g.Count() > 1))
        {
            var byDate = group.OrderBy(r => r.Date).ToList();
            for (int i = 0; i < byDate.Count; i++)
            {
                for (int j = i + 1; j < byDate.Count; j++)
                {
                    var daysApart = (byDate[j].Date.Date - byDate[i].Date.Date).TotalDays;
                    if (daysApart > dateWindowDays) break; // sorted by date: no later record can be closer
                    byDate[i].IsPossibleDuplicate = true;
                    byDate[j].IsPossibleDuplicate = true;
                }
            }
        }
    }

    #endregion

    #region Confirm / reject / unlink

    /// <summary>
    /// Confirms a candidate as the match for a line, setting the persisted flag on the book record.
    /// </summary>
    public void ConfirmMatch(BankStatementLine line, BankMatchCandidate candidate, CompanyData data)
    {
        // If the line was already matched to a different record, release that record first so it
        // doesn't stay flagged as matched (which would orphan it in the unmatched view).
        if (line.MatchedRecordType is { } prevType && line.MatchedRecordId is { } prevId &&
            (prevType != candidate.RecordType || prevId != candidate.RecordId))
        {
            SetRecordMatchState(data, prevType, prevId, matched: false, null);
        }

        line.MatchStatus = BankLineMatchStatus.Matched;
        line.MatchedRecordType = candidate.RecordType;
        line.MatchedRecordId = candidate.RecordId;
        line.MatchedDate = DateTime.UtcNow;
        line.MatchConfidence = candidate.Confidence;

        SetRecordMatchState(data, candidate.RecordType, candidate.RecordId, matched: true, line.Id);
        data.MarkAsModified();
    }

    /// <summary>Marks a line as unmatched without touching any book record (rejects suggestions).</summary>
    public void RejectMatch(BankStatementLine line)
    {
        line.MatchStatus = BankLineMatchStatus.Unmatched;
        line.MatchedRecordType = null;
        line.MatchedRecordId = null;
        line.MatchedDate = null;
        line.MatchConfidence = 0;
    }

    /// <summary>Unlinks a confirmed match, clearing the flag on the previously matched book record.</summary>
    public void UnlinkMatch(BankStatementLine line, CompanyData data)
    {
        if (line.MatchedRecordType is { } type && line.MatchedRecordId is { } id)
        {
            SetRecordMatchState(data, type, id, matched: false, null);
            data.MarkAsModified();
        }
        RejectMatch(line);
    }

    private static void SetRecordMatchState(CompanyData data, BookRecordType type, string id, bool matched, string? lineId)
    {
        var date = matched ? (DateTime?)DateTime.UtcNow : null;
        switch (type)
        {
            case BookRecordType.Expense:
                Apply(data.Expenses.FirstOrDefault(e => e.Id == id));
                break;
            case BookRecordType.Revenue:
                Apply(data.Revenues.FirstOrDefault(r => r.Id == id));
                break;
            case BookRecordType.Invoice:
                ApplyInvoice(data.Invoices.FirstOrDefault(i => i.Id == id));
                break;
            case BookRecordType.Payment:
                ApplyPayment(data.Payments.FirstOrDefault(p => p.Id == id));
                break;
        }

        void Apply(Transaction? t)
        {
            if (t == null) return;
            t.BankMatched = matched;
            t.BankMatchedDate = date;
            t.BankMatchedLineId = lineId;
        }
        void ApplyInvoice(Invoice? inv)
        {
            if (inv == null) return;
            inv.BankMatched = matched;
            inv.BankMatchedDate = date;
            inv.BankMatchedLineId = lineId;
        }
        void ApplyPayment(Payment? p)
        {
            if (p == null) return;
            p.BankMatched = matched;
            p.BankMatchedDate = date;
            p.BankMatchedLineId = lineId;
        }
    }

    private static bool IsRecordMatched(CompanyData data, BookRecordRef r) => r.Type switch
    {
        BookRecordType.Expense => data.Expenses.FirstOrDefault(e => e.Id == r.Id)?.BankMatched ?? false,
        BookRecordType.Revenue => data.Revenues.FirstOrDefault(x => x.Id == r.Id)?.BankMatched ?? false,
        BookRecordType.Invoice => data.Invoices.FirstOrDefault(i => i.Id == r.Id)?.BankMatched ?? false,
        BookRecordType.Payment => data.Payments.FirstOrDefault(p => p.Id == r.Id)?.BankMatched ?? false,
        _ => false
    };

    #endregion

    #region Record extraction

    private static List<BookRecordRef> BuildRecordRefs(CompanyData data, HashSet<BookRecordType> scope)
    {
        var refs = new List<BookRecordRef>();

        if (scope.Contains(BookRecordType.Expense))
            refs.AddRange(data.Expenses.Select(e => new BookRecordRef
            {
                Type = BookRecordType.Expense, Id = e.Id, Description = e.Description, Date = e.Date, Amount = -e.Total
            }));

        if (scope.Contains(BookRecordType.Revenue))
            refs.AddRange(data.Revenues.Select(r => new BookRecordRef
            {
                Type = BookRecordType.Revenue, Id = r.Id, Description = r.Description, Date = r.Date, Amount = r.Total
            }));

        if (scope.Contains(BookRecordType.Payment))
            refs.AddRange(data.Payments.Select(p => new BookRecordRef
            {
                Type = BookRecordType.Payment,
                Id = p.Id,
                Description = string.IsNullOrWhiteSpace(p.Notes) ? p.ReferenceNumber ?? string.Empty : p.Notes,
                Date = p.Date,
                Amount = p.Amount // refunds are stored negative => money out
            }));

        if (scope.Contains(BookRecordType.Invoice))
            refs.AddRange(data.Invoices.Select(i => new BookRecordRef
            {
                Type = BookRecordType.Invoice, Id = i.Id, Description = i.InvoiceNumber, Date = i.IssueDate, Amount = i.Total
            }));

        return refs;
    }

    #endregion

    #region Text similarity

    /// <summary>
    /// Normalizes a description for fuzzy comparison: lowercase, strip punctuation and common
    /// bank/noise tokens and bare numbers, collapse whitespace.
    /// </summary>
    internal static string NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;

        var sb = new StringBuilder(description.Length);
        foreach (var ch in description.ToLowerInvariant())
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');

        var tokens = sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !NoiseTokens.Contains(t))
            .Where(t => !t.All(char.IsDigit)); // drop bare numbers (card trailers, refs)

        return string.Join(' ', tokens);
    }

    /// <summary>
    /// Token-set (Jaccard) similarity of two normalized strings, in the range 0..1.
    /// </summary>
    internal static double Similarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        var setA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var setB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (setA.Count == 0 || setB.Count == 0) return 0;

        var intersection = setA.Count(setB.Contains);
        var union = setA.Count + setB.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    #endregion
}
