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
