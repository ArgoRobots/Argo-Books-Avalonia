using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing expense records.
/// </summary>
public partial class ExpenseModals : UserControl
{
    public ExpenseModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private ExpenseModalsViewModel? _previousViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousViewModel != null)
        {
            _previousViewModel.ScrollToLineItemsRequested -= OnScrollToLineItemsRequested;
            _previousViewModel = null;
        }

        if (DataContext is ExpenseModalsViewModel vm)
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
