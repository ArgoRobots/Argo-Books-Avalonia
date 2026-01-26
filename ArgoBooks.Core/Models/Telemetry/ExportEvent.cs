namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Export operation event.
/// </summary>
public class ExportEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.Export;

    /// <summary>
    /// Type of export performed.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExportType ExportType { get; set; }

    /// <summary>
    /// Duration of the export operation in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Size of the exported file in bytes.
    /// </summary>
    public long FileSize { get; set; }
}

/// <summary>
/// Types of export operations.
/// </summary>
public enum ExportType
{
    /// <summary>
    /// Excel spreadsheet export.
    /// </summary>
    Excel,

    /// <summary>
    /// Google Sheets export.
    /// </summary>
    GoogleSheets,

    /// <summary>
    /// PDF report export.
    /// </summary>
    Pdf,

    /// <summary>
    /// CSV export.
    /// </summary>
    Csv,

    /// <summary>
    /// Company backup export.
    /// </summary>
    Backup,

    /// <summary>
    /// Receipt export.
    /// </summary>
    Receipts,

    /// <summary>
    /// Chart image export.
    /// </summary>
    ChartImage
}
