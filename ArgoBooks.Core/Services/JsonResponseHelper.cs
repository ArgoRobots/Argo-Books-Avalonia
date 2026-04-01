namespace ArgoBooks.Core.Services;

/// <summary>
/// Shared utilities for cleaning LLM JSON responses.
/// </summary>
internal static class JsonResponseHelper
{
    /// <summary>
    /// Strips markdown code block fences (```json ... ```) from an LLM response,
    /// returning clean JSON suitable for parsing.
    /// </summary>
    internal static string StripMarkdownCodeBlock(string response)
    {
        var cleaned = response.Trim();

        // Strip leading ```json or ``` fence (with or without newline)
        if (cleaned.StartsWith("```"))
        {
            // Find end of opening fence line
            var fenceEnd = cleaned.IndexOf('\n');
            if (fenceEnd == -1)
            {
                // No newline — strip all backticks from start
                cleaned = cleaned.TrimStart('`').Trim();
            }
            else
            {
                // Check if fence tag has content on the same line (e.g. "```json{")
                var fenceLine = cleaned[..fenceEnd].TrimEnd();
                var afterTag = fenceLine.TrimStart('`').TrimStart();
                if (afterTag.Length > 0 && afterTag.Length <= 10 && !afterTag.StartsWith('{') && !afterTag.StartsWith('['))
                {
                    // Language tag like "json" — skip the whole line
                    cleaned = cleaned[(fenceEnd + 1)..];
                }
                else if (afterTag.StartsWith('{') || afterTag.StartsWith('['))
                {
                    // JSON starts on the fence line — just strip the backticks
                    cleaned = cleaned[fenceLine.IndexOf(afterTag[0])..];
                }
                else
                {
                    cleaned = cleaned[(fenceEnd + 1)..];
                }
            }

            // Strip trailing ``` fence
            var closingFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0)
                cleaned = cleaned[..closingFence].Trim();
        }

        return cleaned;
    }
}
