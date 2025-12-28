using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Represents an invoice.
/// </summary>
public class Invoice
{
    /// <summary>
    /// Unique identifier (e.g., INV-2024-00001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display invoice number (e.g., #INV-2024-001).
    /// </summary>
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Date the invoice was issued.
    /// </summary>
    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Payment due date.
    /// </summary>
    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Line items on the invoice.
    /// </summary>
    [JsonPropertyName("lineItems")]
    public List<LineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Subtotal before tax.
    /// </summary>
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax rate as a decimal.
    /// </summary>
    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Total tax amount.
    /// </summary>
    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount due.
    /// </summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Amount already paid.
    /// </summary>
    [JsonPropertyName("amountPaid")]
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Remaining balance.
    /// </summary>
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    /// <summary>
    /// Invoice status.
    /// </summary>
    [JsonPropertyName("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Recurring invoice ID if generated from recurring.
    /// </summary>
    [JsonPropertyName("recurringInvoiceId")]
    public string? RecurringInvoiceId { get; set; }

    /// <summary>
    /// ID of the accountant who created or manages this invoice.
    /// </summary>
    [JsonPropertyName("accountantId")]
    public string? AccountantId { get; set; }

    /// <summary>
    /// Payment reminder settings.
    /// </summary>
    [JsonPropertyName("reminderSettings")]
    public ReminderSettings ReminderSettings { get; set; } = new();

    /// <summary>
    /// Invoice history (actions taken).
    /// </summary>
    [JsonPropertyName("history")]
    public List<InvoiceHistoryEntry> History { get; set; } = [];

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the record was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the invoice is overdue.
    /// </summary>
    [JsonIgnore]
    public bool IsOverdue => Status != InvoiceStatus.Paid &&
                             Status != InvoiceStatus.Cancelled &&
                             DateTime.UtcNow.Date > DueDate.Date;
}
