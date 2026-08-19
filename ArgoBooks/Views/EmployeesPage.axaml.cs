using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Employees page.
/// </summary>
public partial class EmployeesPage : UserControl
{
    public EmployeesPage()
    {
        InitializeComponent();
    }

    private void OnTableSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is EmployeesPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ColumnWidths.SetAvailableWidth(e.NewSize.Width);
        }
    }

    private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is EmployeesPageViewModel viewModel && e.WidthChanged)
        {
            viewModel.ResponsiveHeader.HeaderWidth = e.NewSize.Width;
        }
    }
}
