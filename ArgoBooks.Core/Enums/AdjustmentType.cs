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
