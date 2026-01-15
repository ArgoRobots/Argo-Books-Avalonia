using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing rental inventory items.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class RentalInventoryModals : UserControl
{
    public RentalInventoryModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is RentalInventoryModalsViewModel vm)
        {
            if (vm.IsRentOutModalOpen)
                vm.CloseRentOutModalCommand.Execute(null);
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
