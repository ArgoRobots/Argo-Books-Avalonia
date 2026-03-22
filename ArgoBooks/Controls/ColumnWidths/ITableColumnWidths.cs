namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Interface for table column width managers.
/// </summary>
public interface ITableColumnWidths
{
    /// <summary>
    /// Gets the minimum total width required for all visible columns.
    /// </summary>
    double MinimumTotalWidth { get; }

    /// <summary>
    /// Gets whether the table needs horizontal scrolling.
    /// </summary>
    bool NeedsHorizontalScroll { get; }

    /// <summary>
    /// Sets the available width for the table and recalculates column widths.
    /// </summary>
    void SetAvailableWidth(double width);

    /// <summary>
    /// Sets column visibility and recalculates widths.
    /// </summary>
    void SetColumnVisibility(string columnName, bool isVisible);

    /// <summary>
    /// Resize a column by a delta amount.
    /// Returns the actual delta applied, which may be less than requested due to min/max constraints.
    /// </summary>
    double ResizeColumn(string columnName, double delta);

    /// <summary>
    /// Auto-size a column to fit content.
    /// </summary>
    void AutoSizeColumn(string columnName);

    /// <summary>
    /// Resets all column widths to their default proportional sizes.
    /// </summary>
    void ResetWidths();

    /// <summary>
    /// Gets or sets the X position for the column visibility menu.
    /// </summary>
    double ColumnMenuX { get; set; }

    /// <summary>
    /// Gets or sets the Y position for the column visibility menu.
    /// </summary>
    double ColumnMenuY { get; set; }
}
