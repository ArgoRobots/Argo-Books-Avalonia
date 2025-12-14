using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class UpgradeModal : UserControl
{
    public UpgradeModal()
    {
        InitializeComponent();
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is UpgradeModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void EnterKeyBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is UpgradeModalViewModel vm)
        {
            vm.CloseEnterKeyCommand.Execute(null);
        }
    }
}
