using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class UpgradeModal : UserControl
{
    public UpgradeModal()
    {
        InitializeComponent();

        // Animate the modal when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is UpgradeModalViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(UpgradeModalViewModel.IsOpen))
                    {
                        if (vm.IsOpen)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (ModalBorder != null)
                                {
                                    ModalBorder.Opacity = 1;
                                    ModalBorder.RenderTransform = new ScaleTransform(1, 1);
                                }
                            }, DispatcherPriority.Render);
                        }
                        else
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (ModalBorder != null)
                                {
                                    ModalBorder.Opacity = 0;
                                    ModalBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                };
            }
        };
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
