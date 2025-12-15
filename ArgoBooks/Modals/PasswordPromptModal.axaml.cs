using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class PasswordPromptModal : UserControl
{
    public PasswordPromptModal()
    {
        InitializeComponent();
    }

    private void PasswordTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PasswordPromptModalViewModel vm)
        {
            vm.SubmitCommand.Execute(null);
            e.Handled = true;
        }
    }
}
