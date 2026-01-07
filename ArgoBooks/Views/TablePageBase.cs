using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Views;

/// <summary>
/// Base class for table pages that provides common event handlers for
/// table size changes and column menu interactions.
/// </summary>
public abstract class TablePageBase : UserControl
{
    /// <summary>
    /// Handles the table size changed event.
    /// Updates column widths when the table width changes.
    /// </summary>
    protected void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ITablePageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    /// <summary>
    /// Handles the table header pointer pressed event.
    /// Opens the column menu on right-click.
    /// </summary>
    protected void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is IColumnMenuViewModel viewModel)
            {
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }
}
