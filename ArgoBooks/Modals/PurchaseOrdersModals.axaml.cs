using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Purchase Orders modals.
/// </summary>
public partial class PurchaseOrdersModals : UserControl
{
    public PurchaseOrdersModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is PurchaseOrdersModalsViewModel vm)
        {
            if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsReceiveModalOpen)
                vm.CloseReceiveModalCommand.Execute(null);
            else if (vm.IsViewModalOpen)
                vm.CloseViewModalCommand.Execute(null);
            else if (vm.IsAddModalOpen)
                vm.CloseAddModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
