namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Interface for table column width managers.
/// </summary>
public interface ITableColumnWidths
{
    /// <summary>
    /// Sets the available width for the table and recalculates column widths.
    /// </summary>
    void SetAvailableWidth(double width);

    /// <summary>
    /// Resize a column by a delta amount.
    /// </summary>
    void ResizeColumn(string columnName, double delta);

    /// <summary>
    /// Auto-size a column to fit content.
    /// </summary>
    void AutoSizeColumn(string columnName);
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
