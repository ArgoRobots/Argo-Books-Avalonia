using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating, editing, and filtering invoices.
/// </summary>
public partial class InvoiceModals : UserControl
{
    public InvoiceModals()
    {
        InitializeComponent();
    }

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Close modals when clicking the overlay
        if (DataContext is ViewModels.InvoiceModalsViewModel vm)
        {
            if (vm.IsCreateEditModalOpen)
                vm.CloseCreateEditModalCommand.Execute(null);
            else if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsHistoryModalOpen)
                vm.CloseHistoryModalCommand.Execute(null);
        }
    }
}
