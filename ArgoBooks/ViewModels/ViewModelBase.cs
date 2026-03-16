using ArgoBooks.Core.Enums;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// Provides common functionality like property change notifications and busy state.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    /// <summary>
    /// Shows a "Discard Changes?" confirmation dialog for Add modals.
    /// Returns true if the user confirmed they want to discard.
    /// </summary>
    protected static async Task<bool> ConfirmDiscardNewAsync()
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return true;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Discard Changes?".Translate(),
            Message = "You have entered data that will be lost. Are you sure you want to close?".Translate(),
            PrimaryButtonText = "Discard".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        return result == ConfirmationResult.Primary;
    }

    /// <summary>
    /// Shows a "Discard Changes?" confirmation dialog for Edit modals.
    /// Returns true if the user confirmed they want to discard.
    /// </summary>
    protected static async Task<bool> ConfirmDiscardEditsAsync()
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return true;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Discard Changes?".Translate(),
            Message = "You have unsaved changes that will be lost. Are you sure you want to close?".Translate(),
            PrimaryButtonText = "Discard".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        return result == ConfirmationResult.Primary;
    }

    /// <summary>
    /// Shows a "Discard Changes?" confirmation dialog for Filter modals.
    /// Returns true if the user confirmed they want to discard.
    /// </summary>
    protected static async Task<bool> ConfirmDiscardFiltersAsync()
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return true;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Discard Changes?".Translate(),
            Message = "You have unapplied filter changes. Are you sure you want to close?".Translate(),
            PrimaryButtonText = "Discard".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        return result == ConfirmationResult.Primary;
    }

    /// <summary>
    /// Executes an async operation while showing a busy indicator.
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="message">Optional message to display while busy</param>
    protected async Task ExecuteBusyAsync(Func<Task> operation, string? message = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = message;
            await operation();
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Executes an async operation while showing a busy indicator and returns a result.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="message">Optional message to display while busy</param>
    /// <returns>The result of the operation</returns>
    protected async Task<T?> ExecuteBusyAsync<T>(Func<Task<T>> operation, string? message = null)
    {
        if (IsBusy) return default;

        try
        {
            IsBusy = true;
            BusyMessage = message;
            return await operation();
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }
}
