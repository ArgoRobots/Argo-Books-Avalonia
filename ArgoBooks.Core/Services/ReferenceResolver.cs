using System.Text.RegularExpressions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Conservative name-to-entity-ID resolver.
/// Prefers a normalized-exact match; only returns a fuzzy match when the best candidate
/// exceeds a high similarity threshold AND no other candidate is within 0.05 of it.
/// A wrong link is worse than no link.
/// </summary>
public static class ReferenceResolver
{
    // A candidate must score at least this to be returned as a match.
    // Set at 0.80 to accept clear single-character typos (e.g. "Globx" -> "Globex")
    // while still rejecting unrelated names. Conservative: a wrong link is worse than no link.
    private const double FuzzyAcceptThreshold = 0.80;

    // The winning candidate must beat the runner-up by at least this margin.
    // If the gap is smaller, the result is ambiguous.
    private const double TieBreakMargin = 0.05;

    // Regex used to collapse runs of whitespace to a single space.
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    // Trailing characters that are stripped during normalization.
    private static readonly char[] TrailingPunctuation = ['.', ',', ';', ':'];

    /// <summary>
    /// Builds a lookup dictionary from normalized name -> entity ID.
    /// Normalization: trim, lower-invariant, collapse internal whitespace, strip trailing punctuation.
    /// On duplicate normalized names the first entry wins.
    /// </summary>
    public static Dictionary<string, string> BuildIndex(IEnumerable<(string Id, string Name)> entities)
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (id, name) in entities)
        {
            var key = Normalize(name);
            if (key.Length > 0 && !index.ContainsKey(key))
                index[key] = id;
        }
        return index;
    }

    /// <summary>
    /// Resolves a raw value against the pre-built index.
    /// Returns (MatchedId, IsAmbiguous=false) on a clear win; (null, IsAmbiguous=true) when
    /// multiple candidates tie near the top; (null, false) when nothing is close enough.
    /// </summary>
    public static (string? MatchedId, bool IsAmbiguous) Resolve(
        string value,
        IReadOnlyDictionary<string, string> index)
    {
        var normalized = Normalize(value);

        // 1. Exact normalized match — always wins.
        if (index.TryGetValue(normalized, out var exactId))
            return (exactId, false);

        if (index.Count == 0)
            return (null, false);

        // 2. Composite scoring: score every key and find best + second-best.
        double best = 0, secondBest = 0;
        string? bestKey = null;

        foreach (var key in index.Keys)
        {
            double score = CompositeScore(normalized, key);
            if (score > best)
            {
                secondBest = best;
                best = score;
                bestKey = key;
            }
            else if (score > secondBest)
            {
                secondBest = score;
            }
        }

        // Not close enough to accept at all.
        if (best < FuzzyAcceptThreshold)
            return (null, false);

        // Close enough but tied with another — ambiguous.
        if (best - secondBest < TieBreakMargin)
            return (null, true);

        // Single high-confidence winner.
        return (index[bestKey!], false);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Normalizes a name for comparison: trim, lower-invariant, collapse whitespace,
    /// strip trailing punctuation characters.
    /// </summary>
    internal static string Normalize(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();
        s = WhitespaceRun.Replace(s, " ");
        s = s.TrimEnd(TrailingPunctuation);
        return s;
    }

    /// <summary>
    /// Composite similarity score in [0, 1].
    ///
    /// Two signals, take the maximum:
    ///
    /// (a) Levenshtein ratio using max(lenA, lenB) as denominator — conservative for typo
    ///     correction where both strings are similar in length (e.g. "Globx" vs "Globex").
    ///
    /// (b) Word-coverage score: fraction of query words that fuzzy-match a word in the
    ///     candidate (each word pair scores 1.0 if identical, or a word-level Levenshtein
    ///     ratio otherwise). This correctly surfaces "smith" as fully covered by both
    ///     "smith plumbing" and "smith electrical", triggering the ambiguity rule, while
    ///     "totally different" gets no coverage against "acme ltd" or "globex".
    ///
    /// Using max() means a high-confidence typo match (signal a) OR a clear word-subset
    /// match (signal b) is sufficient — but we never average them down.
    /// </summary>
    private static double CompositeScore(string query, string candidate)
    {
        if (query == candidate) return 1.0;
        if (query.Length == 0 || candidate.Length == 0) return 0.0;

        // (a) Levenshtein ratio: 1 - dist / max(len)
        int dist = LevenshteinDistance(query, candidate);
        double levenRatio = 1.0 - (double)dist / Math.Max(query.Length, candidate.Length);

        // (b) Word-coverage: what fraction of query words are found in the candidate words?
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidateWords = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        double wordCoverage = 0.0;
        if (queryWords.Length > 0)
        {
            double totalScore = 0.0;
            foreach (var qw in queryWords)
            {
                // Find the best-matching candidate word for this query word.
                double bestWordScore = 0.0;
                foreach (var cw in candidateWords)
                {
                    double ws = qw == cw ? 1.0
                        : 1.0 - (double)LevenshteinDistance(qw, cw) / Math.Max(qw.Length, cw.Length);
                    if (ws > bestWordScore) bestWordScore = ws;
                }
                totalScore += bestWordScore;
            }
            wordCoverage = totalScore / queryWords.Length;
        }

        return Math.Max(levenRatio, wordCoverage);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int la = a.Length, lb = b.Length;
        // Use two-row DP to keep memory O(min(la,lb)).
        if (la < lb)
        {
            // Swap so a is always the shorter string.
            (a, b, la, lb) = (b, a, lb, la);
        }

        var prev = new int[lb + 1];
        var curr = new int[lb + 1];

        for (int j = 0; j <= lb; j++) prev[j] = j;

        for (int i = 1; i <= la; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= lb; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(prev[j] + 1, curr[j - 1] + 1),
                    prev[j - 1] + cost);
            }
            Array.Copy(curr, prev, lb + 1);
        }

        return prev[lb];
    }
}
