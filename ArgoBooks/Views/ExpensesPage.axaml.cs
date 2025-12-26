using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

public partial class ExpensesPage : UserControl
{
    private ScrollViewer? _headerScrollViewer;
    private ScrollViewer? _contentScrollViewer;

    public ExpensesPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _headerScrollViewer = this.FindControl<ScrollViewer>("HeaderScrollViewer");
        _contentScrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
    }

    private void OnTableHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (DataContext is ExpensesPageViewModel viewModel)
            {
                viewModel.IsColumnMenuOpen = true;
            }
        }
    }

    private void OnContentScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Sync header horizontal scroll with content scroll
        if (_headerScrollViewer != null && _contentScrollViewer != null)
        {
            _headerScrollViewer.Offset = new Vector(_contentScrollViewer.Offset.X, 0);
        }
    }
}
