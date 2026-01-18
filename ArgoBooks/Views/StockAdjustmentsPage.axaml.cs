using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Stock Adjustments page.
/// </summary>
public partial class StockAdjustmentsPage : UserControl
{
    public StockAdjustmentsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles header size changes to update responsive layout.
    /// </summary>
    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is StockAdjustmentsPageViewModel viewModel && e.WidthChanged)
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
    }

    /// <summary>
    /// Handles table size changes to recalculate column widths.
    /// </summary>
    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is StockAdjustmentsPageViewModel viewModel)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width - 48); // Account for padding
        }
    }

    /// <summary>
    /// Handles right-click on table header to show column visibility menu.
    /// </summary>
    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is StockAdjustmentsPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.ColumnMenuX = position.X;
                viewModel.ColumnMenuY = position.Y;
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }
}
