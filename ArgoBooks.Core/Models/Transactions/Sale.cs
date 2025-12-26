using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Represents a sale/revenue transaction.
/// </summary>
public class Sale
{
    /// <summary>
    /// Unique identifier (e.g., SAL-2024-00001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Date and time of the sale.
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Customer ID.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Accountant ID who processed the sale.
    /// </summary>
    [JsonPropertyName("accountantId")]
    public string? AccountantId { get; set; }

    /// <summary>
    /// Category ID for this sale.
    /// </summary>
    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    /// <summary>
    /// Description of the sale.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Line items in this sale.
    /// </summary>
    [JsonPropertyName("lineItems")]
    public List<LineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Quantity sold.
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
    /// Subtotal before tax.
    /// </summary>
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Total tax amount.
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
    /// Total amount including tax.
    /// </summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Gets the effective total, calculating from line items or Amount if Total is 0.
    /// </summary>
    [JsonIgnore]
    public decimal EffectiveTotal
    {
        get
        {
            if (Total > 0) return Total;
            if (Amount > 0) return Amount + TaxAmount + ShippingCost - Discount;
            if (LineItems.Count > 0) return LineItems.Sum(li => li.Amount) + TaxAmount + ShippingCost - Discount;
            return Quantity * UnitPrice + TaxAmount + ShippingCost - Discount;
        }
    }

    /// <summary>
    /// Reference number (e.g., order number).
    /// </summary>
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Payment method used.
    /// </summary>
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Payment status (e.g., Paid, Pending).
    /// </summary>
    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = "Paid";

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
}
