using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class ReturnsPage : UserControl
{
    public ReturnsPage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ReturnsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
