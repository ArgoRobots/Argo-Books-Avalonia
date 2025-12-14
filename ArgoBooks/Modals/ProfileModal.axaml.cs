using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class ProfileModal : UserControl
{
    public ProfileModal()
    {
        InitializeComponent();

        // Focus the modal when it opens
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ProfileModalViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ProfileModalViewModel.IsOpen) && vm.IsOpen)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ModalBorder?.Focus();
                        }, DispatcherPriority.Background);
                    }
                };
            }
        };
    }

    /// <summary>
    /// Closes the modal when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProfileModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
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
