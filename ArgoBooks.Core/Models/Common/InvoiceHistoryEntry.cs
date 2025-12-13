
namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Represents a history entry for an invoice action.
/// </summary>
public class InvoiceHistoryEntry
{
    /// <summary>
    /// Action performed (e.g., Created, Sent, Viewed, Paid).
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// When the action occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Additional details about the action.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
