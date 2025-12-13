using System.Text.Json.Serialization;
using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Inventory;

/// <summary>
/// Represents an adjustment to inventory stock levels.
/// </summary>
public class StockAdjustment
{
    /// <summary>
    /// Unique identifier (e.g., ADJ-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Inventory item ID being adjusted.
    /// </summary>
    [JsonPropertyName("inventoryItemId")]
    public string InventoryItemId { get; set; } = string.Empty;

    /// <summary>
    /// Type of adjustment (Add, Remove, Set).
    /// </summary>
    [JsonPropertyName("adjustmentType")]
    public AdjustmentType AdjustmentType { get; set; }

    /// <summary>
    /// Quantity to adjust by.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Stock level before adjustment.
    /// </summary>
    [JsonPropertyName("previousStock")]
    public int PreviousStock { get; set; }

    /// <summary>
    /// Stock level after adjustment.
    /// </summary>
    [JsonPropertyName("newStock")]
    public int NewStock { get; set; }

    /// <summary>
    /// Reason for adjustment.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Reference number (e.g., PO number, RMA number).
    /// </summary>
    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// User who made the adjustment.
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// When the adjustment was made.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
