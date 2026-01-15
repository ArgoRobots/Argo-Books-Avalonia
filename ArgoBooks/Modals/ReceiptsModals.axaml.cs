using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Receipts modals.
/// </summary>
public partial class ReceiptsModals : UserControl
{
    public ReceiptsModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ReceiptsModalsViewModel vm)
        {
            if (vm.IsScanReviewModalOpen)
                vm.CloseScanReviewModalCommand.Execute(null);
            else if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
