using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

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
