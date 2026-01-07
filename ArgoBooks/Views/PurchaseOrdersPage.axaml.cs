using ArgoBooks.ViewModels;
using Avalonia.Controls;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Purchase Orders page.
/// </summary>
public partial class PurchaseOrdersPage : UserControl
{
    public PurchaseOrdersPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles table size changes to recalculate column widths.
    /// </summary>
    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is PurchaseOrdersPageViewModel viewModel)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width - 48); // Account for padding
        }
    }
}
