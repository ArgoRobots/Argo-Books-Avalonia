
namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Represents a line item in a transaction (revenue, invoice, expense order, etc.).
/// </summary>
public class LineItem
{
    /// <summary>
    /// Product ID if linked to a product.
    /// </summary>
    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }

    /// <summary>
    /// Description of the item or service.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of items.
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price or rate per item.
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Tax rate as a decimal (e.g., 0.08 for 8%).
    /// </summary>
    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Discount amount applied to this line item.
    /// </summary>
    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    /// <summary>
    /// Calculated subtotal (quantity * unitPrice - discount).
    /// </summary>
    [JsonPropertyName("subtotal")]
    public decimal Subtotal => (Quantity * UnitPrice) - Discount;

    /// <summary>
    /// Calculated tax amount.
    /// </summary>
    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount => Subtotal * TaxRate;

    /// <summary>
    /// Calculated total amount including tax.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount => Subtotal + TaxAmount;
}
