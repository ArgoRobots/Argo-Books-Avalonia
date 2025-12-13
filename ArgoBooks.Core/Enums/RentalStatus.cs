namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a rental record.
/// </summary>
public enum RentalStatus
{
    /// <summary>Rental is currently active.</summary>
    Active,

    /// <summary>Item has been returned.</summary>
    Returned,

    /// <summary>Rental is past due date.</summary>
    Overdue,

    /// <summary>Rental has been cancelled.</summary>
    Cancelled
}
