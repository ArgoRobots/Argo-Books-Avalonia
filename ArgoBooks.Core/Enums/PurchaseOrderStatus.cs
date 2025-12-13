namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a purchase order.
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>Purchase order is being prepared.</summary>
    Draft,

    /// <summary>Purchase order has been sent to supplier.</summary>
    Sent,

    /// <summary>Some items have been received.</summary>
    PartiallyReceived,

    /// <summary>All items have been received.</summary>
    FullyReceived,

    /// <summary>Purchase order has been cancelled.</summary>
    Cancelled
}
