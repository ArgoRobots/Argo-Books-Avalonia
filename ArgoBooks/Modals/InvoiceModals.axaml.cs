using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

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

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is InvoiceModalsViewModel vm)
        {
            if (vm.IsPreviewModalOpen)
                vm.ClosePreviewModalCommand.Execute(null);
            else if (vm.IsHistoryModalOpen)
                vm.CloseHistoryModalCommand.Execute(null);
            else if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsCreateEditModalOpen)
                vm.CloseCreateEditModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
