using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a product or service.
/// </summary>
public class Product
{
    /// <summary>
    /// Unique identifier (e.g., PRD-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Product name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stock Keeping Unit.
    /// </summary>
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Product description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category ID for this product.
    /// </summary>
    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    /// <summary>
    /// Selling price per unit.
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Cost price per unit.
    /// </summary>
    [JsonPropertyName("costPrice")]
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Tax rate as a decimal (e.g., 0.08 for 8%).
    /// </summary>
    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Whether to track inventory for this product.
    /// </summary>
    [JsonPropertyName("trackInventory")]
    public bool TrackInventory { get; set; }

    /// <summary>
    /// Primary supplier ID.
    /// </summary>
    [JsonPropertyName("supplierId")]
    public string? SupplierId { get; set; }

    /// <summary>
    /// URL or path to product image.
    /// </summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Product status.
    /// </summary>
    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; } = EntityStatus.Active;

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
    /// Calculated profit margin.
    /// </summary>
    [JsonIgnore]
    public decimal ProfitMargin => UnitPrice > 0 ? (UnitPrice - CostPrice) / UnitPrice : 0;
}
