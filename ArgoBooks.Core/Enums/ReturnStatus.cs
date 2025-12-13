namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a return request.
/// </summary>
public enum ReturnStatus
{
    /// <summary>Return is pending review.</summary>
    Pending,

    /// <summary>Return has been approved.</summary>
    Approved,

    /// <summary>Return has been completed and refunded.</summary>
    Completed,

    /// <summary>Return has been rejected.</summary>
    Rejected
}
