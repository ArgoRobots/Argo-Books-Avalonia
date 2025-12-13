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
