using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for entering password to unlock encrypted company files.
/// </summary>
public partial class PasswordPromptModal : UserControl
{
    private bool _eventsSubscribed;
    private PasswordPromptModalViewModel? _subscribedVm;

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
            _subscribedVm = vm;
            vm.FocusPasswordRequested += OnFocusPasswordRequested;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_subscribedVm != null)
        {
            _subscribedVm.FocusPasswordRequested -= OnFocusPasswordRequested;
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
            _eventsSubscribed = false;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PasswordPromptModalViewModel.ShowBiometricSuccess))
        {
            if (DataContext is PasswordPromptModalViewModel { ShowBiometricSuccess: true })
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
}
