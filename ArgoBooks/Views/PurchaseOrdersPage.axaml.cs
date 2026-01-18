using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

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

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is PurchaseOrdersPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.ColumnMenuX = position.X;
                viewModel.ColumnMenuY = position.Y;
                viewModel.IsColumnMenuOpen = true;
            }
        }
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

    /// <summary>
    /// Handles header size changes for responsive layout.
    /// </summary>
    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is PurchaseOrdersPageViewModel viewModel && e.WidthChanged)
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
    }
}
