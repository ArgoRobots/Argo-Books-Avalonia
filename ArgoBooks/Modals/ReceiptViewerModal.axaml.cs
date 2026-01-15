using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal for viewing receipt images.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class ReceiptViewerModal : UserControl
{
    public ReceiptViewerModal()
    {
        InitializeComponent();
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ReceiptViewerModalViewModel vm)
            vm.CloseCommand.Execute(null);
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ReceiptViewerModalViewModel vm)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    vm.CloseCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F:
                    vm.ToggleFullscreenCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
