using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Represents a custom report template.
/// </summary>
public class ReportTemplate
{
    /// <summary>
    /// Unique identifier (e.g., TPL-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Template name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Page size (Letter, A4, Legal, etc.).
    /// </summary>
    [JsonPropertyName("pageSize")]
    public string PageSize { get; set; } = "Letter";

    /// <summary>
    /// Page orientation (Portrait, Landscape).
    /// </summary>
    [JsonPropertyName("pageOrientation")]
    public string PageOrientation { get; set; } = "Portrait";

    /// <summary>
    /// Page margins.
    /// </summary>
    [JsonPropertyName("pageMargins")]
    public PageMargins PageMargins { get; set; } = new();

    /// <summary>
    /// Whether to show header on each page.
    /// </summary>
    [JsonPropertyName("showHeader")]
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Whether to show footer on each page.
    /// </summary>
    [JsonPropertyName("showFooter")]
    public bool ShowFooter { get; set; } = true;

    /// <summary>
    /// Whether to show page numbers.
    /// </summary>
    [JsonPropertyName("showPageNumbers")]
    public bool ShowPageNumbers { get; set; } = true;

    /// <summary>
    /// Report title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Background color (hex format).
    /// </summary>
    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Report elements.
    /// </summary>
    [JsonPropertyName("elements")]
    public List<ReportElement> Elements { get; set; } = [];

    /// <summary>
    /// Whether this is a built-in template.
    /// </summary>
    [JsonPropertyName("isBuiltIn")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// When the template was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
