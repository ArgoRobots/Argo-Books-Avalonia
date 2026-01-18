using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a category for organizing products.
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
    /// Description of the category.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this category is for Products or Services.
    /// </summary>
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "Product";

    /// <summary>
    /// Color code for UI display (hex format).
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#4A90D9";

    /// <summary>
    /// Icon emoji for UI display.
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "📦";

}
