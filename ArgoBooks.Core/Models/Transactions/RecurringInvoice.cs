using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Represents a recurring invoice schedule.
/// </summary>
public class RecurringInvoice
{
    /// <summary>
    /// Unique identifier (e.g., REC-INV-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Invoice amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of recurring charge.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// How often to generate invoices.
    /// </summary>
    [JsonPropertyName("frequency")]
    public Frequency Frequency { get; set; }

    /// <summary>
    /// When the recurring schedule starts.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// When the recurring schedule ends (null for indefinite).
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Date of next invoice generation.
    /// </summary>
    [JsonPropertyName("nextInvoiceDate")]
    public DateTime NextInvoiceDate { get; set; }

    /// <summary>
    /// Payment terms (e.g., Net 30).
    /// </summary>
    [JsonPropertyName("paymentTerms")]
    public string PaymentTerms { get; set; } = "Net 30";

    /// <summary>
    /// Whether to automatically send the invoice.
    /// </summary>
    [JsonPropertyName("autoSend")]
    public bool AutoSend { get; set; }

    /// <summary>
    /// Status of the recurring invoice.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Additional notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When an invoice was last generated.
    /// </summary>
    [JsonPropertyName("lastGeneratedAt")]
    public DateTime? LastGeneratedAt { get; set; }

    /// <summary>
    /// Whether the recurring schedule is active.
    /// </summary>
    [JsonIgnore]
    public bool IsActive => Status == "Active" &&
                           (EndDate == null || DateTime.Today <= EndDate.Value.Date);
}
