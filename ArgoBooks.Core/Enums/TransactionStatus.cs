namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a transaction (expense or revenue).
/// </summary>
public enum TransactionStatus
{
    Completed,
    Pending,
    PartialReturn,
    Returned,
    Cancelled
}

/// <summary>
/// Extension methods for TransactionStatus.
/// </summary>
public static class TransactionStatusExtensions
{
    /// <summary>
    /// Gets the display name for a transaction status.
    /// </summary>
    public static string GetDisplayName(this TransactionStatus status)
    {
        return status switch
        {
            TransactionStatus.PartialReturn => "Partial Return",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Parses a display name string to a TransactionStatus enum value.
    /// </summary>
    public static TransactionStatus? ParseTransactionStatus(string? name)
    {
        return name switch
        {
            "Completed" => TransactionStatus.Completed,
            "Pending" => TransactionStatus.Pending,
            "Partial Return" => TransactionStatus.PartialReturn,
            "Returned" => TransactionStatus.Returned,
            "Cancelled" => TransactionStatus.Cancelled,
            _ => null
        };
    }

    /// <summary>
    /// Gets filter options including "All" as the first entry.
    /// </summary>
    public static string[] GetFilterOptions()
    {
        return
        [
            "All",
            TransactionStatus.Completed.GetDisplayName(),
            TransactionStatus.Pending.GetDisplayName(),
            TransactionStatus.PartialReturn.GetDisplayName(),
            TransactionStatus.Returned.GetDisplayName(),
            TransactionStatus.Cancelled.GetDisplayName()
        ];
    }
}
