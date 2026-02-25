using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Represents a single row in an accounting report table.
/// </summary>
public class AccountingRow
{
    /// <summary>
    /// Text label for the row (account name, description, etc.).
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Data values for each column after the label column.
    /// </summary>
    public List<string> Values { get; set; } = [];

    /// <summary>
    /// Indent level for hierarchical display (0 = top level, 1 = sub-account, 2 = sub-sub-account).
    /// </summary>
    public int IndentLevel { get; set; }

    /// <summary>
    /// The type of row, which determines rendering style.
    /// </summary>
    public AccountingRowType RowType { get; set; } = AccountingRowType.DataRow;
}

/// <summary>
/// Complete data for rendering an accounting table, including headers, rows, and metadata.
/// </summary>
public class AccountingTableData
{
    /// <summary>
    /// Report title (e.g., "Income Statement", "Balance Sheet").
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Subtitle with date range (e.g., "For the Year Ending December 31, 2025").
    /// </summary>
    public string Subtitle { get; set; } = "";

    /// <summary>
    /// Column header labels (e.g., ["Account", "Amount"] or ["Customer", "Current", "1-30 Days", ...]).
    /// </summary>
    public List<string> ColumnHeaders { get; set; } = [];

    /// <summary>
    /// All rows in the report.
    /// </summary>
    public List<AccountingRow> Rows { get; set; } = [];

    /// <summary>
    /// Optional footnote displayed at the bottom (e.g., "Cash balance estimated from recorded transactions").
    /// </summary>
    public string? Footnote { get; set; }

    /// <summary>
    /// Proportional width ratios for columns (should sum to 1.0).
    /// First column is typically wider for labels.
    /// </summary>
    public List<double> ColumnWidthRatios { get; set; } = [];
}
