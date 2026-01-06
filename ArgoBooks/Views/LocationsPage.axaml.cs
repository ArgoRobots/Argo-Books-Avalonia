using Avalonia.Controls;

namespace ArgoBooks.Views;

public partial class LocationsPage : UserControl
{
    public LocationsPage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ViewModels.LocationsPageViewModel viewModel)
        {
            viewModel.ColumnWidths.RecalculateColumnWidths(e.NewSize.Width - 48); // 48 = left + right padding
        }
    }
}
