using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class ProductsPage : UserControl
{
    public ProductsPage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ProductsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
