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
    /// Handles table size changes to recalculate column widths.
    /// </summary>
    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is StockAdjustmentsPageViewModel viewModel)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width - 48); // Account for padding
        }
    }
}
