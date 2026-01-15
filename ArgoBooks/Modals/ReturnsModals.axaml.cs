using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing return records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class ReturnsModals : UserControl
{
    public ReturnsModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ReturnsModalsViewModel vm)
        {
            if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
