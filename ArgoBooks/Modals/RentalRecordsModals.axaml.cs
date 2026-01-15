using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing rental transaction records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class RentalRecordsModals : UserControl
{
    public RentalRecordsModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is RentalRecordsModalsViewModel vm)
        {
            if (vm.IsViewModalOpen)
                vm.CloseViewModalCommand.Execute(null);
            else if (vm.IsReturnModalOpen)
                vm.CloseReturnModalCommand.Execute(null);
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
