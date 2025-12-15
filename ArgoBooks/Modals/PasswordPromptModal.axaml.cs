using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

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
        }
    }

    private void OnFocusPasswordRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PasswordTextBox?.Focus();
            if (PasswordTextBox != null)
            {
                PasswordTextBox.SelectAll();
            }
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
