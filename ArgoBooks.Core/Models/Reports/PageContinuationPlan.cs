namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Represents a single page in the effective page sequence.
/// Can be either an original template page or a continuation page for an overflowing accounting table.
/// </summary>
public class EffectivePage
{
    /// <summary>
    /// Which template page this corresponds to (1-based).
    /// </summary>
    public int SourcePageNumber { get; set; }

    /// <summary>
    /// Element ID of the overflowing accounting table, or null for original template pages.
    /// </summary>
    public string? ContinuationElementId { get; set; }

    /// <summary>
    /// First row index to render on this page (0-based into the table's Rows list).
    /// </summary>
    public int StartRowIndex { get; set; }

    /// <summary>
    /// Number of rows to render on this page.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Starting data row index for alternating row color continuity.
    /// </summary>
    public int DataRowStartIndex { get; set; }

    /// <summary>
    /// Whether this is the last page for this particular accounting table's data.
    /// Used to determine if footnote should be shown.
    /// </summary>
    public bool IsLastContinuationPage { get; set; }

    /// <summary>
    /// Position of this page in the effective output sequence (1-based).
    /// </summary>
    public int EffectivePageNumber { get; set; }

    /// <summary>
    /// True if this is a continuation page (not an original template page).
    /// </summary>
    public bool IsContinuationPage => ContinuationElementId != null;
}

/// <summary>
/// Runtime plan for page continuation. Computed before rendering begins.
/// Maps the fixed template pages into an effective page sequence that includes
/// auto-generated continuation pages for overflowing accounting tables.
/// </summary>
public class PageContinuationPlan
{
    /// <summary>
    /// The effective page sequence, including both original and continuation pages.
    /// </summary>
    public List<EffectivePage> Pages { get; set; } = [];

    /// <summary>
    /// Cached table data fetched during planning, keyed by element ID.
    /// Prevents re-fetching during rendering.
    /// </summary>
    public Dictionary<string, AccountingTableData> CachedTableData { get; set; } = new();

    /// <summary>
    /// For each overflowing element, how many rows fit on the original (first) page.
    /// Keyed by element ID. Used by the renderer to know where to stop on page 1.
    /// </summary>
    public Dictionary<string, int> FirstPageRowCounts { get; set; } = new();

    /// <summary>
    /// Total number of effective pages.
    /// </summary>
    public int TotalPageCount => Pages.Count;
}
