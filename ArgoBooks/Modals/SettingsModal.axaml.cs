using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for configuring application and company settings.
/// </summary>
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
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (DataContext is SettingsModalViewModel vm && _eventsSubscribed)
        {
            vm.FocusPasswordRequested -= OnFocusPasswordRequested;
            _eventsSubscribed = false;
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
}
