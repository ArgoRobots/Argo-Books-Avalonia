using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class SettingsModal : UserControl
{
    private bool _eventsSubscribed;

    public SettingsModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SettingsModalViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;

            vm.FocusPasswordRequested += OnFocusPasswordRequested;

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

    private void OnFocusPasswordRequested(object? sender, EventArgs e)
    {
        if (DataContext is not SettingsModalViewModel vm) return;

        Dispatcher.UIThread.Post(() =>
        {
            TextBox? targetTextBox = null;

            if (vm.IsChangePasswordModalOpen)
            {
                targetTextBox = ChangeCurrentPasswordTextBox;
            }
            else if (vm.IsRemovePasswordModalOpen)
            {
                targetTextBox = RemoveCurrentPasswordTextBox;
            }

            if (targetTextBox != null)
            {
                targetTextBox.Focus();
                targetTextBox.SelectAll();
            }
        }, DispatcherPriority.Background);
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
