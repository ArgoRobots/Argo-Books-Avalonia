namespace ArgoBooks.Core.Models.Telemetry;

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
    Backup
}
