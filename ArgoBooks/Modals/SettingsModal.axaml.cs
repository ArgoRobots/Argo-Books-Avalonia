using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class SettingsModal : UserControl
{
    public SettingsModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SettingsModalViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsModalViewModel.IsOpen))
                {
                    if (vm.IsOpen)
                    {
                        // Animate in
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (ModalBorder != null)
                            {
                                ModalBorder.Opacity = 1;
                                ModalBorder.RenderTransform = new ScaleTransform(1, 1);
                            }
                            ModalBorder?.Focus();
                        }, DispatcherPriority.Render);
                    }
                    else
                    {
                        // Reset for next open
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
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SettingsModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }

    private void PasswordBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SettingsModalViewModel vm)
        {
            vm.ClosePasswordModalCommand.Execute(null);
        }
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is SettingsModalViewModel vm)
        {
            // Close password modals first if open
            if (vm.IsAddPasswordModalOpen || vm.IsChangePasswordModalOpen || vm.IsRemovePasswordModalOpen)
            {
                vm.ClosePasswordModalCommand.Execute(null);
            }
            else
            {
                vm.CloseCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}
