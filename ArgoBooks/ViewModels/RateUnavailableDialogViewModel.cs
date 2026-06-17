using System.Threading.Tasks;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>The user's choice in the rate-unavailable dialog.</summary>
public enum RateRetryResult { Retry, Cancel }

/// <summary>
/// Modal shown when a bulk operation (import) cannot fetch the exact-date exchange rates it needs.
/// Explains whether the device is offline or the Argo server is unreachable, and lets the user
/// reconnect and retry, or cancel.
/// </summary>
public partial class RateUnavailableDialogViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _message = "";

    private TaskCompletionSource<RateRetryResult>? _completionSource;

    /// <summary>Shows the dialog for the given reason and returns the user's choice.</summary>
    public Task<RateRetryResult> ShowAsync(RateUnavailableReason reason)
    {
        (Title, Message) = reason switch
        {
            RateUnavailableReason.NoInternet => (
                "You appear to be offline",
                "Argo needs current exchange rates to import these amounts accurately. Connect to the internet and press Retry."),
            RateUnavailableReason.ServerUnreachable => (
                "Exchange rates are temporarily unavailable",
                "Argo's exchange-rate service could not be reached. Please wait a moment and press Retry."),
            _ => (
                "Could not get exchange rates",
                "Argo could not fetch the exchange rates needed for this import. Check your connection and press Retry."),
        };

        IsOpen = true;
        _completionSource = new TaskCompletionSource<RateRetryResult>();
        return _completionSource.Task;
    }

    [RelayCommand]
    private void Retry()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(RateRetryResult.Retry);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(RateRetryResult.Cancel);
    }
}
