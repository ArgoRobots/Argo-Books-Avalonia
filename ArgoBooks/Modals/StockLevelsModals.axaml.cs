using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for viewing and managing stock level records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class StockLevelsModals : UserControl
{
    public StockLevelsModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is StockLevelsModalsViewModel vm)
        {
            if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsAddItemModalOpen)
                vm.CloseAddItemModalCommand.Execute(null);
            else if (vm.IsAdjustStockModalOpen)
                vm.CloseAdjustStockModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
