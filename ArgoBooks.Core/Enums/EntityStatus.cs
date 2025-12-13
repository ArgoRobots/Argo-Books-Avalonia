namespace ArgoBooks.Core.Enums;

/// <summary>
/// General status for entities (customers, products, suppliers, etc.).
/// </summary>
public enum EntityStatus
{
    /// <summary>Entity is active and available for use.</summary>
    Active,

    /// <summary>Entity is inactive but preserved for history.</summary>
    Inactive,

    /// <summary>Entity is archived.</summary>
    Archived
}
