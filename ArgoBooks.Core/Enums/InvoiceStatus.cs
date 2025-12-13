namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of an invoice.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Invoice is being prepared.</summary>
    Draft,

    /// <summary>Invoice is ready but not yet sent.</summary>
    Pending,

    /// <summary>Invoice has been sent to customer.</summary>
    Sent,

    /// <summary>Invoice has been viewed by recipient.</summary>
    Viewed,

    /// <summary>Partial payment has been received.</summary>
    Partial,

    /// <summary>Invoice has been fully paid.</summary>
    Paid,

    /// <summary>Invoice is past due date.</summary>
    Overdue,

    /// <summary>Invoice has been cancelled.</summary>
    Cancelled
}
