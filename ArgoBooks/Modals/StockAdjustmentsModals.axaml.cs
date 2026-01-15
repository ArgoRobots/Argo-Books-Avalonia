using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and viewing stock adjustment records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class StockAdjustmentsModals : UserControl
{
    public StockAdjustmentsModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is StockAdjustmentsModalsViewModel vm)
        {
            if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsViewModalOpen)
                vm.CloseViewModalCommand.Execute(null);
            else if (vm.IsAddModalOpen)
                vm.CloseAddModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
