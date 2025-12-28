using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class CustomersPage : UserControl
{
    public CustomersPage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is CustomersPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
