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
    /// Gets or sets the X position for the column visibility menu.
    /// </summary>
    double ColumnMenuX { get; set; }

    /// <summary>
    /// Gets or sets the Y position for the column visibility menu.
    /// </summary>
    double ColumnMenuY { get; set; }
}

/// <summary>
/// Interface for ViewModels that have table column widths.
/// </summary>
public interface ITablePageViewModel
{
    /// <summary>
    /// Gets the column widths manager for this page.
    /// </summary>
    ITableColumnWidths ColumnWidths { get; }
}

/// <summary>
/// Interface for ViewModels that have a column visibility menu.
/// </summary>
public interface IColumnMenuViewModel : ITablePageViewModel
{
    /// <summary>
    /// Gets or sets whether the column menu is open.
    /// </summary>
    bool IsColumnMenuOpen { get; set; }

    /// <summary>
    /// Gets or sets the X position of the column menu.
    /// </summary>
    double ColumnMenuX { get; set; }

    /// <summary>
    /// Gets or sets the Y position of the column menu.
    /// </summary>
    double ColumnMenuY { get; set; }
}
