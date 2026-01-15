using Avalonia.Controls;
using Avalonia.Input;
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

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ExpenseModalsViewModel vm)
        {
            vm.ScrollToLineItemsRequested += OnScrollToLineItemsRequested;
        }
    }

    private void OnScrollToLineItemsRequested(object? sender, EventArgs e)
    {
        // Scroll to bring the line items section into view
        LineItemsSection?.BringIntoView();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ExpenseModalsViewModel vm)
        {
            if (vm.IsItemStatusModalOpen)
                vm.CloseItemStatusModalCommand.Execute(null);
            else if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsAddEditModalOpen)
                vm.CloseAddEditModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
