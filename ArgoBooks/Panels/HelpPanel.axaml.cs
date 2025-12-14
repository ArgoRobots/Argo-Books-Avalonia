using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class HelpPanel : UserControl
{
    public HelpPanel()
    {
        InitializeComponent();
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is HelpPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }
}
