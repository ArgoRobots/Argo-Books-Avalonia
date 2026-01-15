using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal for viewing receipt images.
/// </summary>
public partial class ReceiptViewerModal : UserControl
{
    private bool _eventsSubscribed;

    public ReceiptViewerModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ReceiptViewerModalViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ReceiptViewerModalViewModel.IsOpen))
                {
                    if (vm.IsOpen)
                        ModalAnimationHelper.AnimateIn(ModalBorder);
                    else
                        ModalAnimationHelper.AnimateOut(ModalBorder);
                }
            };
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ReceiptViewerModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
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
