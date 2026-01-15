using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing storage location records.
/// </summary>
public partial class LocationsModals : UserControl
{
    public LocationsModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is LocationsModalsViewModel vm)
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
