namespace ArgoBooks.Utilities;

/// <summary>
/// Helper for formatting pagination text consistently across ViewModels.
/// </summary>
public static class PaginationTextHelper
{
    /// <summary>
    /// Formats pagination text based on current page state.
    /// </summary>
    /// <param name="totalCount">Total number of items.</param>
    /// <param name="currentPage">Current page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="totalPages">Total number of pages.</param>
    /// <param name="singular">Singular form of item name (e.g., "customer").</param>
    /// <param name="plural">Plural form of item name (e.g., "customers"). If null, adds 's' to singular.</param>
    /// <returns>Formatted pagination text.</returns>
    public static string FormatPaginationText(
        int totalCount,
        int currentPage,
        int pageSize,
        int totalPages,
        string singular,
        string? plural = null)
    {
        plural ??= singular + "s";

        if (totalCount == 0)
        {
            return $"0 {plural}";
        }

        // For single page, just show count
        if (totalPages <= 1)
        {
            return totalCount == 1 ? $"1 {singular}" : $"{totalCount} {plural}";
        }

        // For multiple pages, show range
        var start = (currentPage - 1) * pageSize + 1;
        var end = Math.Min(currentPage * pageSize, totalCount);
        return $"{start}-{end} of {totalCount} {plural}";
    }

    /// <summary>
    /// Simplified format for ViewModels without pagination (shows total count only).
    /// </summary>
    public static string FormatSimpleCount(int totalCount, string singular, string? plural = null)
    {
        plural ??= singular + "s";
        return totalCount == 1 ? $"1 {singular}" : $"{totalCount} {plural}";
    }
}
