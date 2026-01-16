namespace ArgoBooks.Core.Models.AI;

/// <summary>
/// AI-generated suggestion for supplier and category matching.
/// </summary>
public class SupplierCategorySuggestion
{
    /// <summary>
    /// Matched supplier ID from existing suppliers, or null if no match.
    /// </summary>
    [JsonPropertyName("matchedSupplierId")]
    public string? MatchedSupplierId { get; set; }

    /// <summary>
    /// Matched supplier name.
    /// </summary>
    [JsonPropertyName("matchedSupplierName")]
    public string? MatchedSupplierName { get; set; }

    /// <summary>
    /// Confidence score for supplier match (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("supplierConfidence")]
    public double SupplierConfidence { get; set; }

    /// <summary>
    /// Whether AI suggests creating a new supplier.
    /// </summary>
    [JsonPropertyName("shouldCreateNewSupplier")]
    public bool ShouldCreateNewSupplier { get; set; }

    /// <summary>
    /// Suggested new supplier details if no good match found.
    /// </summary>
    [JsonPropertyName("newSupplier")]
    public NewSupplierSuggestion? NewSupplier { get; set; }

    /// <summary>
    /// Matched category ID from existing categories, or null if no match.
    /// </summary>
    [JsonPropertyName("matchedCategoryId")]
    public string? MatchedCategoryId { get; set; }

    /// <summary>
    /// Matched category name.
    /// </summary>
    [JsonPropertyName("matchedCategoryName")]
    public string? MatchedCategoryName { get; set; }

    /// <summary>
    /// Confidence score for category match (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("categoryConfidence")]
    public double CategoryConfidence { get; set; }

    /// <summary>
    /// Whether AI suggests creating a new category.
    /// </summary>
    [JsonPropertyName("shouldCreateNewCategory")]
    public bool ShouldCreateNewCategory { get; set; }

    /// <summary>
    /// Suggested new category details if no good match found.
    /// </summary>
    [JsonPropertyName("newCategory")]
    public NewCategorySuggestion? NewCategory { get; set; }
}

/// <summary>
/// Suggestion for creating a new supplier.
/// </summary>
public class NewSupplierSuggestion
{
    /// <summary>
    /// Suggested supplier name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the supplier.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// Suggestion for creating a new category.
/// </summary>
public class NewCategorySuggestion
{
    /// <summary>
    /// Suggested category name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Item type (Product or Service).
    /// </summary>
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "Product";
}
