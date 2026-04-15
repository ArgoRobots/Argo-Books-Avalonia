using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing revenue/sales records.
/// </summary>
public partial class RevenueModals : UserControl
{
    public RevenueModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private RevenueModalsViewModel? _previousViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousViewModel != null)
        {
            _previousViewModel.ScrollToLineItemsRequested -= OnScrollToLineItemsRequested;
            _previousViewModel = null;
        }

        if (DataContext is RevenueModalsViewModel vm)
        {
            vm.ScrollToLineItemsRequested += OnScrollToLineItemsRequested;
            _previousViewModel = vm;
        }
    }

    private void OnScrollToLineItemsRequested(object? sender, EventArgs e)
    {
        // Scroll to bring the line items section into view
        LineItemsSection?.BringIntoView();
    }
}
