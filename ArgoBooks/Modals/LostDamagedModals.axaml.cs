using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing lost/damaged item records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class LostDamagedModals : UserControl
{
    public LostDamagedModals()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is LostDamagedModalsViewModel vm)
        {
            if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            e.Handled = true;
        }
    }
}
