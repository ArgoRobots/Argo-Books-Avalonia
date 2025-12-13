namespace ArgoBooks.Core.Enums;

/// <summary>
/// Reason for lost or damaged inventory.
/// </summary>
public enum LostDamagedReason
{
    /// <summary>Item was damaged.</summary>
    Damaged,

    /// <summary>Item was lost.</summary>
    Lost,

    /// <summary>Item was stolen.</summary>
    Stolen,

    /// <summary>Item expired.</summary>
    Expired,

    /// <summary>Other reason.</summary>
    Other
}
