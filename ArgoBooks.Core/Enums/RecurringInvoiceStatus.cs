namespace ArgoBooks.Core.Enums;

/// <summary>
/// Status of a recurring invoice schedule.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecurringInvoiceStatus
{
    /// <summary>Recurring invoice is actively generating invoices.</summary>
    Active,

    /// <summary>Recurring invoice generation is temporarily paused.</summary>
    Paused,

    /// <summary>Recurring invoice schedule has been completed.</summary>
    Completed,

    /// <summary>Recurring invoice schedule has been cancelled.</summary>
    Cancelled
}
