using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
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

    private async void OnCopyPairingCode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsModalViewModel vm) return;

        var code = vm.ShortCodeDisplay;
        if (string.IsNullOrEmpty(code)) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;

        await clipboard.SetTextAsync(code);

        // Briefly swap the copy glyph for a checkmark to confirm the copy.
        if (sender is Button { Content: PathIcon icon })
        {
            var original = icon.Data;
            icon.Data = Geometry.Parse(ArgoBooks.Icons.Check);
            try
            {
                await Task.Delay(1200);
            }
            finally
            {
                icon.Data = original;
            }
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
