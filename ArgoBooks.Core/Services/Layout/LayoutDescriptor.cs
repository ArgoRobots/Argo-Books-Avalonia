namespace ArgoBooks.Core.Services.Layout;

/// <summary>
/// Describes how to extract one or more clean tables from a <see cref="SheetGrid"/>.
/// This is the contract an LLM layout interpreter must satisfy; it is consumed
/// deterministically by <see cref="GridExtractor"/>. All coordinates are
/// <b>0-based</b> indices into <see cref="SheetGrid.Cells"/>.
/// </summary>
public sealed class LayoutDescriptor
{
    /// <summary>One or more table regions found on the sheet.</summary>
    public List<TableRegion> Tables { get; set; } = [];
}

/// <summary>
/// A single rectangular table region within a sheet, plus the metadata needed to
/// turn it into a clean <c>headers + rows</c> table. All row/column indices are
/// <b>0-based</b> into <see cref="SheetGrid.Cells"/>.
/// </summary>
public sealed class TableRegion
{
    /// <summary>0-based index of the first data row (inclusive).</summary>
    public int FirstDataRow { get; set; }

    /// <summary>0-based index of the last data row (inclusive).</summary>
    public int LastDataRow { get; set; }

    /// <summary>0-based index of the first column in the region (inclusive).</summary>
    public int FirstCol { get; set; }

    /// <summary>0-based index of the last column in the region (inclusive).</summary>
    public int LastCol { get; set; }

    /// <summary>
    /// 0-based indices of the header rows. When multiple, their values are
    /// concatenated top-to-bottom (parent &gt; child) to form each column header.
    /// </summary>
    public List<int> HeaderRows { get; set; } = [];

    /// <summary>
    /// <c>"long"</c> (one record per row) or <c>"wide"</c> (cross-tab that is
    /// transposed into long form by <see cref="GridExtractor"/>).
    /// </summary>
    public string Orientation { get; set; } = "long";

    /// <summary>
    /// 0-based data-row indices to skip (subtotals, notes, blank separators).
    /// </summary>
    public List<int> IgnoreRows { get; set; } = [];

    /// <summary>
    /// For <c>"wide"</c> orientation only: the 0-based row-key columns. Non-key
    /// columns inside the region are the "spread" columns that get transposed.
    /// </summary>
    public List<int>? KeyColumns { get; set; }
}
