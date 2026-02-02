using ArgoBooks.ViewModels;
using Avalonia.Controls;

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
        if (DataContext is StockAdjustmentsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
