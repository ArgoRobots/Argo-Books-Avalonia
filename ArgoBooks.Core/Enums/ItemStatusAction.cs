namespace ArgoBooks.Core.Enums;

/// <summary>
/// Actions that can be performed to change an item's status
/// (e.g., marking as lost/damaged, returned, or undoing those statuses).
/// </summary>
public enum ItemStatusAction
{
    /// <summary>Mark item as lost or damaged.</summary>
    LostDamaged,

    /// <summary>Mark item as returned.</summary>
    Returned,

    /// <summary>Undo a lost/damaged status.</summary>
    UndoLostDamaged,

    /// <summary>Undo a returned status.</summary>
    UndoReturned
}
