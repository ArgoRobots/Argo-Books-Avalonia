namespace ArgoBooks.Core.Enums;

/// <summary>
/// Type of stock adjustment.
/// </summary>
public enum AdjustmentType
{
    /// <summary>Add stock to inventory.</summary>
    Add,

    /// <summary>Remove stock from inventory.</summary>
    Remove,

    /// <summary>Set stock to a specific quantity.</summary>
    Set
}

/// <summary>
/// Extension methods for AdjustmentType.
/// </summary>
public static class AdjustmentTypeExtensions
{
    /// <summary>
    /// Gets all adjustment type names for modal dropdowns.
    /// </summary>
    public static string[] GetAllNames()
    {
        return
        [
            nameof(AdjustmentType.Add),
            nameof(AdjustmentType.Remove),
            nameof(AdjustmentType.Set)
        ];
    }

    /// <summary>
    /// Gets filter options including "All" as the first entry.
    /// </summary>
    public static string[] GetFilterOptions()
    {
        return
        [
            "All",
            nameof(AdjustmentType.Add),
            nameof(AdjustmentType.Remove),
            nameof(AdjustmentType.Set)
        ];
    }
}
