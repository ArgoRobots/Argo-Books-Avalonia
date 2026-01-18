using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Dialog prompting user to save, discard, or cancel when there are unsaved changes.
/// </summary>
public partial class UnsavedChangesDialog : UserControl
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
        KeyDown += Dialog_KeyDown;
    }

    private void Dialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is UnsavedChangesDialogViewModel vm && vm.IsOpen)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    vm.CancelCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    vm.SaveCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
