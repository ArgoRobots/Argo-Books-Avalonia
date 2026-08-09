namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of inventory stock levels.
/// </summary>
public enum InventoryStatus
{
    /// <summary>Stock is above reorder point.</summary>
    InStock,

    /// <summary>Stock is at or below reorder point.</summary>
    LowStock,

    /// <summary>Stock is zero.</summary>
    OutOfStock,

    /// <summary>Stock is above overstock threshold.</summary>
    Overstock
}

/// <summary>
/// Extension methods for InventoryStatus.
/// </summary>
public static class InventoryStatusExtensions
{
    /// <summary>
    /// Gets the display name for an inventory status.
    /// </summary>
    public static string GetDisplayName(this InventoryStatus status)
    {
        return status switch
        {
            InventoryStatus.InStock => "In Stock",
            InventoryStatus.LowStock => "Low Stock",
            InventoryStatus.OutOfStock => "Out of Stock",
            InventoryStatus.Overstock => "Overstock",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Gets filter options including "All" as the first entry.
    /// </summary>
    public static string[] GetFilterOptions()
    {
        return
        [
            "All",
            InventoryStatus.InStock.GetDisplayName(),
            InventoryStatus.LowStock.GetDisplayName(),
            InventoryStatus.OutOfStock.GetDisplayName(),
            InventoryStatus.Overstock.GetDisplayName()
        ];
    }
}
