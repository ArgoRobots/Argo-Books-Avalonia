using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class SwitchAccountModal : UserControl
{
    public SwitchAccountModal()
    {
        InitializeComponent();

        // Animate and focus the modal when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is SwitchAccountModalViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(SwitchAccountModalViewModel.IsOpen))
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
                                ModalBorder?.Focus();
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

    /// <summary>
    /// Handles keyboard input for the modal.
    /// </summary>
    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SwitchAccountModalViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                if (vm.SelectedAccount != null)
                {
                    vm.SelectAccountCommand.Execute(vm.SelectedAccount);
                    e.Handled = true;
                }
                break;
        }
    }
}
