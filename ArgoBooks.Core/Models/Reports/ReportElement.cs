using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Represents an element in a report template.
/// </summary>
public class ReportElement
{
    /// <summary>
    /// Element type (Label, Table, Chart, Image, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// X position on the page.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Y position on the page.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Element width.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; set; }

    /// <summary>
    /// Element height.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; set; }

    /// <summary>
    /// Text content (for Label type).
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Font size (for Label type).
    /// </summary>
    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// Font weight (Normal, Bold).
    /// </summary>
    [JsonPropertyName("fontWeight")]
    public string FontWeight { get; set; } = "Normal";

    /// <summary>
    /// Text alignment (Left, Center, Right).
    /// </summary>
    [JsonPropertyName("alignment")]
    public string Alignment { get; set; } = "Left";

    /// <summary>
    /// Data source name (for Table/Chart types).
    /// </summary>
    [JsonPropertyName("dataSource")]
    public string? DataSource { get; set; }

    /// <summary>
    /// Column names (for Table type).
    /// </summary>
    [JsonPropertyName("columns")]
    public List<string>? Columns { get; set; }

    /// <summary>
    /// Chart type (for Chart type).
    /// </summary>
    [JsonPropertyName("chartType")]
    public string? ChartType { get; set; }

    /// <summary>
    /// Z-order for layering.
    /// </summary>
    [JsonPropertyName("zOrder")]
    public int ZOrder { get; set; }

    /// <summary>
    /// Whether the element is visible.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; } = true;
}
