using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a category for organizing products, purchases, or rentals.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique identifier (e.g., CAT-SAL-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Category type (Sales, Purchase, Rental).
    /// </summary>
    [JsonPropertyName("type")]
    public CategoryType Type { get; set; }

    /// <summary>
    /// Category name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent category ID for hierarchical categories.
    /// </summary>
    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    /// <summary>
    /// Color code for UI display (hex format).
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#4A90D9";

    /// <summary>
    /// Icon name for UI display.
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "box";

    /// <summary>
    /// Default tax rate for items in this category.
    /// </summary>
    [JsonPropertyName("defaultTaxRate")]
    public decimal DefaultTaxRate { get; set; }

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
