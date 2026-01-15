using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Rental Inventory page.
/// </summary>
public partial class RentalInventoryPage : UserControl
{
    public RentalInventoryPage()
    {
        InitializeComponent();
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is RentalInventoryPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is RentalInventoryPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.ColumnMenuX = position.X;
                viewModel.ColumnMenuY = position.Y;
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is RentalInventoryPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
