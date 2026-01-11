using Avalonia.Controls;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Locations page.
/// </summary>
public partial class LocationsPage : UserControl
{
    public LocationsPage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is ViewModels.LocationsPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
