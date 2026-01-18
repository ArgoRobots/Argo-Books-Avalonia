using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Reusable confirmation dialog for user prompts and decisions.
/// </summary>
public partial class ConfirmationDialog : UserControl
{
    public ConfirmationDialog()
    {
        InitializeComponent();
        KeyDown += Dialog_KeyDown;
    }

    private void Dialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ConfirmationDialogViewModel vm && vm.IsOpen)
        {
            if (e.Key == Key.Enter)
            {
                vm.PrimaryActionCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
