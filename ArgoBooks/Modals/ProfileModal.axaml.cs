using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.Utilities;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for viewing and editing user profile information.
/// </summary>
public partial class ProfileModal : UserControl
{
    public ProfileModal()
    {
        InitializeComponent();

        // Animate and focus the modal when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ProfileModalViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ProfileModalViewModel.IsOpen))
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

                            ModalHelper.ReturnFocusToAppShell(this);
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
        if (DataContext is not ProfileModalViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
