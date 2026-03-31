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
        if (cleaned.StartsWith("```"))
        {
            var startIndex = cleaned.IndexOf('\n') + 1;
            var endIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (endIndex > startIndex)
                cleaned = cleaned[startIndex..endIndex].Trim();
        }
        return cleaned;
    }
}
