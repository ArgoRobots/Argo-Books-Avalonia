using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing payment records.
/// </summary>
public partial class PaymentModals : UserControl
{
    public PaymentModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is PaymentModalsViewModel vm)
        {
            if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsEditModalOpen)
                vm.CloseEditModalCommand.Execute(null);
            else if (vm.IsAddModalOpen)
                vm.CloseAddModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
