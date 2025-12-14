using System;

namespace ArgoBooks.Utilities;

/// <summary>
/// Provides methods for computing Levenshtein distance and normalized similarity between strings.
/// </summary>
public static class LevenshteinDistance
{
    /// <summary>
    /// Computes the Levenshtein distance between two strings.
    /// The Levenshtein distance is the minimum number of single-character edits
    /// (insertions, deletions, or substitutions) required to change one string into another.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string.</param>
    /// <returns>The Levenshtein distance between the two strings.</returns>
    public static int Compute(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Use two rows instead of full matrix for memory efficiency
        var previousRow = new int[targetLength + 1];
        var currentRow = new int[targetLength + 1];

        // Initialize the first row
        for (var j = 0; j <= targetLength; j++)
            previousRow[j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        currentRow[j - 1] + 1,      // Insertion
                        previousRow[j] + 1),        // Deletion
                    previousRow[j - 1] + cost);     // Substitution
            }

            // Swap rows
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }

    /// <summary>
    /// Computes the normalized similarity between two strings (case-insensitive).
    /// Returns a value between 0 and 1, where 1 means identical strings.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string.</param>
    /// <returns>A similarity score between 0 and 1.</returns>
    public static double NormalizedSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        // Case-insensitive comparison
        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        var distance = Compute(source, target);
        var maxLength = Math.Max(source.Length, target.Length);

        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Checks if the target string contains the source as a substring (case-insensitive).
    /// </summary>
    /// <param name="source">The search term.</param>
    /// <param name="target">The string to search in.</param>
    /// <returns>True if target contains source.</returns>
    public static bool ContainsSubstring(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return true;

        if (string.IsNullOrEmpty(target))
            return false;

        return target.Contains(source, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes a combined search score that prioritizes:
    /// 1. Exact matches (highest)
    /// 2. Prefix matches (high)
    /// 3. Substring matches (medium-high)
    /// 4. Fuzzy matches using normalized similarity (lower)
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="target">The target string to match against.</param>
    /// <param name="fuzzyThreshold">Minimum similarity threshold for fuzzy matches (0-1).</param>
    /// <returns>A score between 0 and 1, or -1 if no match.</returns>
    public static double ComputeSearchScore(string searchTerm, string target, double fuzzyThreshold = 0.4)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return 1.0; // Empty search matches everything

        if (string.IsNullOrEmpty(target))
            return -1;

        var searchLower = searchTerm.ToLowerInvariant();
        var targetLower = target.ToLowerInvariant();

        // Exact match (highest priority)
        if (targetLower == searchLower)
            return 1.0;

        // Prefix match (high priority)
        if (targetLower.StartsWith(searchLower))
            return 0.95;

        // Word start match (e.g., "inv" matches "Inventory" in "Add Inventory")
        var words = targetLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.StartsWith(searchLower))
                return 0.9;
        }

        // Substring match (medium-high priority)
        if (targetLower.Contains(searchLower))
            return 0.8;

        // Fuzzy match using normalized similarity
        var similarity = NormalizedSimilarity(searchLower, targetLower);

        // Also check similarity against individual words
        var maxWordSimilarity = 0.0;
        foreach (var word in words)
        {
            var wordSimilarity = NormalizedSimilarity(searchLower, word);
            maxWordSimilarity = Math.Max(maxWordSimilarity, wordSimilarity);
        }

        var bestSimilarity = Math.Max(similarity, maxWordSimilarity);

        if (bestSimilarity >= fuzzyThreshold)
            return bestSimilarity * 0.7; // Scale down fuzzy matches

        return -1; // No match
    }
}
