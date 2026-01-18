using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Customers page.
/// </summary>
public partial class CustomersPage : UserControl
{
    public CustomersPage()
    {
        InitializeComponent();
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is CustomersPageViewModel viewModel)
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
        if (DataContext is CustomersPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }
}
