namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a stock transfer.
/// </summary>
public enum TransferStatus
{
    /// <summary>Transfer is pending approval or processing.</summary>
    Pending,

    /// <summary>Transfer is in transit between locations.</summary>
    InTransit,

    /// <summary>Transfer has been completed.</summary>
    Completed,

    /// <summary>Transfer has been cancelled.</summary>
    Cancelled
}
