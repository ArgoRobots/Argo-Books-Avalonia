using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for switching between user accounts.
/// </summary>
public partial class SwitchAccountModal : UserControl
{
    public SwitchAccountModal()
    {
        InitializeComponent();
        KeyDown += Modal_KeyDown;
    }

    /// <summary>
    /// Handles keyboard input for account selection.
    /// </summary>
    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SwitchAccountModalViewModel vm) return;

        if (e.Key == Key.Enter && vm.SelectedAccount != null)
        {
            vm.SelectAccountCommand.Execute(vm.SelectedAccount);
            e.Handled = true;
        }
    }
}
