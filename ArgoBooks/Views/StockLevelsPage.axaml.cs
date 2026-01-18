using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Stock Levels page.
/// </summary>
public partial class StockLevelsPage : UserControl
{
    public StockLevelsPage()
    {
        InitializeComponent();
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is StockLevelsPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.ColumnMenuX = position.X;
                viewModel.ColumnMenuY = position.Y;
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }

    /// <summary>
    /// Handles the table size changed event to update column widths.
    /// </summary>
    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is StockLevelsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    /// <summary>
    /// Handles the header size changed event for responsive layout.
    /// </summary>
    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is StockLevelsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }
}
