using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Represents a payment received for an invoice.
/// </summary>
public class Payment
{
    /// <summary>
    /// Unique identifier (e.g., PAY-2024-00001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Invoice ID this payment is for.
    /// </summary>
    [JsonPropertyName("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Date of payment.
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Payment amount.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method used.
    /// </summary>
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Reference/transaction number.
    /// </summary>
    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

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

    #region Portal Support

    /// <summary>
    /// The source of this payment: "Manual" (entered in Argo Books) or "Online" (received via payment portal).
    /// Defaults to "Manual" for backward compatibility.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "Manual";

    /// <summary>
    /// The portal payment ID from the server, used to prevent duplicate syncs.
    /// </summary>
    [JsonPropertyName("portalPaymentId")]
    public string? PortalPaymentId { get; set; }

    #endregion

    #region Currency Support

    /// <summary>
    /// The ISO currency code in which this payment was received (e.g., "USD", "EUR", "CAD").
    /// Defaults to "USD" for backward compatibility with existing data.
    /// </summary>
    [JsonPropertyName("originalCurrency")]
    public string OriginalCurrency { get; set; } = "USD";

    /// <summary>
    /// The payment amount converted to USD at the time of entry.
    /// </summary>
    [JsonPropertyName("amountUSD")]
    public decimal AmountUSD { get; set; }

    /// <summary>
    /// Gets the effective amount in USD, falling back to Amount for legacy data.
    /// </summary>
    [JsonIgnore]
    public decimal EffectiveAmountUSD => AmountUSD > 0 ? AmountUSD : Amount;

    #endregion
}
