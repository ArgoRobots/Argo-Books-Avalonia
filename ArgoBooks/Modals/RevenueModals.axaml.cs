using Avalonia.Controls;
using Avalonia.Input;
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

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RevenueModalsViewModel vm)
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
        if (e.Key == Key.Escape && DataContext is RevenueModalsViewModel vm)
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
