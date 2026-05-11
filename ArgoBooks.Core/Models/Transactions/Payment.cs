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
    /// The source of this payment: Manual (entered in Argo Books) or
    /// Online (received via payment portal). Defaults to Manual.
    /// </summary>
    [JsonPropertyName("source")]
    public PaymentSource Source { get; set; } = PaymentSource.Manual;

    /// <summary>
    /// The portal payment ID from the server, used to prevent duplicate syncs.
    /// </summary>
    [JsonPropertyName("portalPaymentId")]
    public string? PortalPaymentId { get; set; }

    /// <summary>
    /// The provider's payment intent / order / capture ID. Used by the refund
    /// flow as the <c>provider_payment_id</c> parameter when calling the server.
    /// </summary>
    [JsonPropertyName("providerPaymentId")]
    public string? ProviderPaymentId { get; set; }

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
    /// Gets the effective amount in USD. For USD payments (including legacy data), returns Amount directly.
    /// For non-USD payments, returns the converted AmountUSD value.
    /// </summary>
    [JsonIgnore]
    public decimal EffectiveAmountUSD =>
        string.Equals(OriginalCurrency, "USD", StringComparison.OrdinalIgnoreCase) ? Amount : AmountUSD;

    #endregion

    #region Refund Support

    /// <summary>
    /// True when this row represents a refund rather than a payment received.
    /// Refund rows store <see cref="Amount"/> as a negative value so existing
    /// aggregations (sum-of-payments) naturally yield net paid.
    /// </summary>
    [JsonPropertyName("isRefund")]
    public bool IsRefund { get; set; }

    /// <summary>
    /// Local Payment.Id of the original payment this refund offsets, when known.
    /// Null for refunds that arrived before the corresponding payment was synced
    /// (rare, but possible if sync is out of order).
    /// </summary>
    [JsonPropertyName("refundedFromPaymentId")]
    public string? RefundedFromPaymentId { get; set; }

    /// <summary>
    /// Server-side refund_requests.id when this refund was initiated through the
    /// Argo Books refund flow. Null for refunds created via the provider's own
    /// dashboard (e.g. Stripe Dashboard) that we received via webhook.
    /// </summary>
    [JsonPropertyName("refundRequestId")]
    public string? RefundRequestId { get; set; }

    /// <summary>
    /// User-supplied reason for the refund, captured at request time.
    /// </summary>
    [JsonPropertyName("refundReason")]
    public string? RefundReason { get; set; }

    #endregion
}
