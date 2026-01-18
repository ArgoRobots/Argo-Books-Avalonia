using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Lost/Damaged items page.
/// </summary>
public partial class LostDamagedPage : UserControl
{
    public LostDamagedPage()
    {
        InitializeComponent();
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is LostDamagedPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.ColumnMenuX = position.X;
                viewModel.ColumnMenuY = position.Y;
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is LostDamagedPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is LostDamagedPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
