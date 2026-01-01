using ArgoBooks.Core.Enums;

namespace ArgoBooks.Utilities;

/// <summary>
/// Helper class for sorting collections with configurable sort columns and directions.
/// </summary>
public static class SortHelper
{
    /// <summary>
    /// Applies sorting to a list based on a column name and direction using a property selector dictionary.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="items">The list to sort.</param>
    /// <param name="sortColumn">The column name to sort by.</param>
    /// <param name="sortDirection">The sort direction (Ascending, Descending, or None).</param>
    /// <param name="propertySelectors">Dictionary mapping column names to property selector functions.</param>
    /// <param name="defaultSelector">Optional default selector to use when sortColumn is not found or direction is None.</param>
    /// <returns>A sorted list.</returns>
    public static List<T> ApplySort<T>(
        this List<T> items,
        string? sortColumn,
        SortDirection sortDirection,
        Dictionary<string, Func<T, object?>> propertySelectors,
        Func<T, object?>? defaultSelector = null)
    {
        if (items.Count == 0)
            return items;

        // If no sort direction or column not found, apply default or return as-is
        if (sortDirection == SortDirection.None || string.IsNullOrEmpty(sortColumn))
        {
            return defaultSelector != null
                ? items.OrderBy(defaultSelector).ToList()
                : items;
        }

        if (!propertySelectors.TryGetValue(sortColumn, out var selector))
        {
            return defaultSelector != null
                ? items.OrderBy(defaultSelector).ToList()
                : items;
        }

        return sortDirection == SortDirection.Ascending
            ? items.OrderBy(selector).ToList()
            : items.OrderByDescending(selector).ToList();
    }

    /// <summary>
    /// Applies sorting to an IEnumerable based on a column name and direction using a property selector dictionary.
    /// </summary>
    /// <typeparam name="T">The type of items in the enumerable.</typeparam>
    /// <param name="items">The enumerable to sort.</param>
    /// <param name="sortColumn">The column name to sort by.</param>
    /// <param name="sortDirection">The sort direction (Ascending, Descending, or None).</param>
    /// <param name="propertySelectors">Dictionary mapping column names to property selector functions.</param>
    /// <param name="defaultSelector">Optional default selector to use when sortColumn is not found or direction is None.</param>
    /// <returns>A sorted list.</returns>
    public static List<T> ApplySort<T>(
        this IEnumerable<T> items,
        string? sortColumn,
        SortDirection sortDirection,
        Dictionary<string, Func<T, object?>> propertySelectors,
        Func<T, object?>? defaultSelector = null)
    {
        return items.ToList().ApplySort(sortColumn, sortDirection, propertySelectors, defaultSelector);
    }
}
