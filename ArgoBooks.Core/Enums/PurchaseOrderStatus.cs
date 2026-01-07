namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a purchase order.
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>Purchase order is being prepared.</summary>
    Draft,

    /// <summary>Purchase order is awaiting approval.</summary>
    Pending,

    /// <summary>Purchase order has been approved.</summary>
    Approved,

    /// <summary>Purchase order has been sent to supplier.</summary>
    Sent,

    /// <summary>Items are on order, awaiting delivery.</summary>
    OnOrder,

    /// <summary>Some items have been received.</summary>
    PartiallyReceived,

    /// <summary>All items have been received.</summary>
    Received,

    /// <summary>All items have been received (alias for Received).</summary>
    FullyReceived = Received,

    /// <summary>Purchase order has been cancelled.</summary>
    Cancelled
}
