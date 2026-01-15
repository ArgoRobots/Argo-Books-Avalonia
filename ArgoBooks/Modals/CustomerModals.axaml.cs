using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing customer records.
/// </summary>
public partial class CustomerModals : UserControl
{
    public CustomerModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is CustomerModalsViewModel vm)
        {
            if (vm.IsHistoryFilterModalOpen)
                vm.CloseHistoryFilterModalCommand.Execute(null);
            else if (vm.IsHistoryModalOpen)
                vm.CloseHistoryModalCommand.Execute(null);
            else if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsEditModalOpen)
                vm.CloseEditModalCommand.Execute(null);
            else if (vm.IsAddModalOpen)
                vm.CloseAddModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
