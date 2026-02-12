using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Base class for financial transactions (purchases and sales).
/// </summary>
public abstract class Transaction
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Date and time of the transaction.
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Accountant ID who processed the transaction.
    /// </summary>
    [JsonPropertyName("accountantId")]
    public string? AccountantId { get; set; }

    /// <summary>
    /// Description of the transaction.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Line items in this transaction.
    /// </summary>
    [JsonPropertyName("lineItems")]
    public List<LineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Quantity.
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price.
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Amount before tax (Quantity * UnitPrice).
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Tax rate percentage.
    /// </summary>
    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    [JsonPropertyName("shippingCost")]
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    /// <summary>
    /// Fee amount (e.g., service fee, processing fee).
    /// </summary>
    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }

    /// <summary>
    /// Total amount including tax.
    /// </summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Reference number (e.g., invoice number, receipt number).
    /// </summary>
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Payment method used.
    /// </summary>
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Associated receipt ID.
    /// </summary>
    [JsonPropertyName("receiptId")]
    public string? ReceiptId { get; set; }

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

    #region Currency Support

    /// <summary>
    /// The ISO currency code in which this transaction was originally entered (e.g., "USD", "EUR", "CAD").
    /// Defaults to "USD" for backward compatibility with existing data.
    /// </summary>
    [JsonPropertyName("originalCurrency")]
    public string OriginalCurrency { get; set; } = "USD";

    /// <summary>
    /// The total amount converted to USD at the time of entry.
    /// Used as the base for all currency conversions.
    /// If null/0, falls back to Total (assumed to be USD for legacy data).
    /// </summary>
    [JsonPropertyName("totalUSD")]
    public decimal TotalUSD { get; set; }

    /// <summary>
    /// The unit price converted to USD at the time of entry.
    /// </summary>
    [JsonPropertyName("unitPriceUSD")]
    public decimal UnitPriceUSD { get; set; }

    /// <summary>
    /// The shipping cost converted to USD at the time of entry.
    /// </summary>
    [JsonPropertyName("shippingCostUSD")]
    public decimal ShippingCostUSD { get; set; }

    /// <summary>
    /// The tax amount converted to USD at the time of entry.
    /// </summary>
    [JsonPropertyName("taxAmountUSD")]
    public decimal TaxAmountUSD { get; set; }

    /// <summary>
    /// The discount amount converted to USD at the time of entry.
    /// </summary>
    [JsonPropertyName("discountUSD")]
    public decimal DiscountUSD { get; set; }

    /// <summary>
    /// The fee amount converted to USD at the time of entry.
    /// </summary>
    [JsonPropertyName("feeUSD")]
    public decimal FeeUSD { get; set; }

    /// <summary>
    /// Gets the effective total in USD, falling back to Total for legacy data.
    /// </summary>
    [JsonIgnore]
    public decimal EffectiveTotalUSD => TotalUSD > 0 ? TotalUSD : Total;

    /// <summary>
    /// Gets the effective unit price in USD, falling back to UnitPrice for legacy data.
    /// </summary>
    [JsonIgnore]
    public decimal EffectiveUnitPriceUSD => UnitPriceUSD > 0 ? UnitPriceUSD : UnitPrice;

    /// <summary>
    /// Gets the effective shipping cost in USD, falling back to ShippingCost for legacy data.
    /// </summary>
    [JsonIgnore]
    public decimal EffectiveShippingCostUSD => ShippingCostUSD > 0 ? ShippingCostUSD : ShippingCost;

    #endregion
}
