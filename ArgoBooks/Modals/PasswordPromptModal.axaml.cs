using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for entering password to unlock encrypted company files.
/// </summary>
public partial class PasswordPromptModal : UserControl
{
    private bool _eventsSubscribed;

    public PasswordPromptModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PasswordPromptModalViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.FocusPasswordRequested += OnFocusPasswordRequested;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PasswordPromptModalViewModel.IsOpen))
        {
            if (DataContext is PasswordPromptModalViewModel { IsOpen: true })
                ModalAnimationHelper.AnimateIn(ModalBorder);
            else
                ModalAnimationHelper.AnimateOut(ModalBorder);
        }
        else if (e.PropertyName == nameof(PasswordPromptModalViewModel.ShowWindowsHelloSuccess))
        {
            if (DataContext is PasswordPromptModalViewModel { ShowWindowsHelloSuccess: true })
            {
                // Trigger the success animation
                Dispatcher.UIThread.Post(() =>
                {
                    SuccessAnimationControl?.PlayAnimation();
                }, DispatcherPriority.Background);
            }
            else
            {
                // Reset the animation
                Dispatcher.UIThread.Post(() =>
                {
                    SuccessAnimationControl?.ResetAnimation();
                }, DispatcherPriority.Background);
            }
        }
    }

    private void OnFocusPasswordRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PasswordTextBox?.Focus();
            PasswordTextBox?.SelectAll();
        }, DispatcherPriority.Background);
    }

    private void PasswordTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PasswordPromptModalViewModel vm)
        {
            vm.SubmitCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is PasswordPromptModalViewModel vm)
        {
            vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}
