using Avalonia.Controls;
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

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is LostDamagedPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }
}
