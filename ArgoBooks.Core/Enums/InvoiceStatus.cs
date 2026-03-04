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

/// <summary>
/// Extension methods for InvoiceStatus.
/// </summary>
public static class InvoiceStatusExtensions
{
    /// <summary>
    /// Gets the modal status options (statuses selectable when creating/editing).
    /// </summary>
    public static string[] GetModalOptions()
    {
        return
        [
            nameof(InvoiceStatus.Draft),
            nameof(InvoiceStatus.Pending),
            nameof(InvoiceStatus.Sent),
            nameof(InvoiceStatus.Partial),
            nameof(InvoiceStatus.Paid),
            nameof(InvoiceStatus.Cancelled)
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
            nameof(InvoiceStatus.Draft),
            nameof(InvoiceStatus.Pending),
            nameof(InvoiceStatus.Sent),
            nameof(InvoiceStatus.Partial),
            nameof(InvoiceStatus.Paid),
            nameof(InvoiceStatus.Overdue),
            nameof(InvoiceStatus.Cancelled)
        ];
    }
}
