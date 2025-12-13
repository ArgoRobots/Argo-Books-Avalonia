using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Inventory;

/// <summary>
/// Line item for a purchase order.
/// </summary>
public class PurchaseOrderLineItem
{
    /// <summary>
    /// Product ID.
    /// </summary>
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Quantity received so far.
    /// </summary>
    [JsonPropertyName("quantityReceived")]
    public int QuantityReceived { get; set; }

    /// <summary>
    /// Unit cost.
    /// </summary>
    [JsonPropertyName("unitCost")]
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Line total.
    /// </summary>
    [JsonPropertyName("total")]
    public decimal Total => Quantity * UnitCost;

    /// <summary>
    /// Whether this line is fully received.
    /// </summary>
    [JsonIgnore]
    public bool IsFullyReceived => QuantityReceived >= Quantity;
}

/// <summary>
/// Represents a purchase order to a supplier.
/// </summary>
public class PurchaseOrder
{
    /// <summary>
    /// Unique identifier (e.g., PO-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display PO number (e.g., #PO-2024-001).
    /// </summary>
    [JsonPropertyName("poNumber")]
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>
    /// Supplier ID.
    /// </summary>
    [JsonPropertyName("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Date the order was placed.
    /// </summary>
    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Expected delivery date.
    /// </summary>
    [JsonPropertyName("expectedDeliveryDate")]
    public DateTime ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Line items in this order.
    /// </summary>
    [JsonPropertyName("lineItems")]
    public List<PurchaseOrderLineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Subtotal before shipping.
    /// </summary>
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    [JsonPropertyName("shippingCost")]
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Total order amount.
    /// </summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Purchase order status.
    /// </summary>
    [JsonPropertyName("status")]
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    /// <summary>
    /// Additional notes.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// When the order was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the order was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether all items have been received.
    /// </summary>
    [JsonIgnore]
    public bool IsFullyReceived => LineItems.Count > 0 && LineItems.All(li => li.IsFullyReceived);
}
