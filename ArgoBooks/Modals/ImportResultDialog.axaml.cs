using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class ImportResultDialog : UserControl
{
    public ImportResultDialog()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ImportResultDialogViewModel { IsOpen: true } vm &&
            (e.Key == Key.Enter || e.Key == Key.Escape))
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
    }
}
