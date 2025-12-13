
namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Represents page margins for a report.
/// </summary>
public class PageMargins
{
    /// <summary>
    /// Top margin in points.
    /// </summary>
    [JsonPropertyName("top")]
    public double Top { get; set; } = 40;

    /// <summary>
    /// Right margin in points.
    /// </summary>
    [JsonPropertyName("right")]
    public double Right { get; set; } = 40;

    /// <summary>
    /// Bottom margin in points.
    /// </summary>
    [JsonPropertyName("bottom")]
    public double Bottom { get; set; } = 40;

    /// <summary>
    /// Left margin in points.
    /// </summary>
    [JsonPropertyName("left")]
    public double Left { get; set; } = 40;
}
