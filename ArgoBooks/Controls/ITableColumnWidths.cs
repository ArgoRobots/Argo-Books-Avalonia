namespace ArgoBooks.Controls;

/// <summary>
/// Interface for table column width managers.
/// </summary>
public interface ITableColumnWidths
{
    /// <summary>
    /// Resize a column by a delta amount.
    /// </summary>
    void ResizeColumn(string columnName, double delta);

    /// <summary>
    /// Auto-size a column to fit content.
    /// </summary>
    void AutoSizeColumn(string columnName);
}
