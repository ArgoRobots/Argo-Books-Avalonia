using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Result of a confirmation dialog.
/// </summary>
public enum ConfirmationResult
{
    None,
    Primary,    // e.g., "Save", "Yes", "OK"
    Secondary,  // e.g., "Don't Save", "No"
    Cancel      // e.g., "Cancel"
}

/// <summary>
/// Configuration for a confirmation dialog.
/// </summary>
public class ConfirmationDialogOptions
{
    public string Title { get; set; } = "Confirm";
    public string Message { get; set; } = "";
    public string? PrimaryButtonText { get; set; } = "OK";
    public string? SecondaryButtonText { get; set; }
    public string? CancelButtonText { get; set; } = "Cancel";
    public bool IsPrimaryDestructive { get; set; }
    public bool IsSecondaryDestructive { get; set; }
}

/// <summary>
/// ViewModel for a reusable confirmation dialog.
/// </summary>
public partial class ConfirmationDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _title = "Confirm";

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    private string _primaryButtonText = "OK";

    [ObservableProperty]
    private string _secondaryButtonText = "";

    [ObservableProperty]
    private string _cancelButtonText = "Cancel";

    [ObservableProperty]
    private bool _showPrimaryButton = true;

    [ObservableProperty]
    private bool _showSecondaryButton;

    [ObservableProperty]
    private bool _showCancelButton = true;

    [ObservableProperty]
    private bool _isPrimaryDestructive;

    [ObservableProperty]
    private bool _isSecondaryDestructive;

    private TaskCompletionSource<ConfirmationResult>? _completionSource;

    /// <summary>
    /// Shows the confirmation dialog with the specified options.
    /// </summary>
    /// <param name="options">Dialog configuration options.</param>
    /// <returns>The result indicating which button was clicked.</returns>
    public Task<ConfirmationResult> ShowAsync(ConfirmationDialogOptions options)
    {
        Title = options.Title;
        Message = options.Message;
        PrimaryButtonText = options.PrimaryButtonText ?? "OK";
        SecondaryButtonText = options.SecondaryButtonText ?? "";
        CancelButtonText = options.CancelButtonText ?? "Cancel";
        ShowPrimaryButton = !string.IsNullOrEmpty(options.PrimaryButtonText);
        ShowSecondaryButton = !string.IsNullOrEmpty(options.SecondaryButtonText);
        ShowCancelButton = !string.IsNullOrEmpty(options.CancelButtonText);
        IsPrimaryDestructive = options.IsPrimaryDestructive;
        IsSecondaryDestructive = options.IsSecondaryDestructive;

        IsOpen = true;

        _completionSource = new TaskCompletionSource<ConfirmationResult>();
        return _completionSource.Task;
    }

    /// <summary>
    /// Shows a simple confirmation dialog.
    /// </summary>
    public Task<ConfirmationResult> ShowAsync(string title, string message,
        string? primaryButton = "OK", string? cancelButton = "Cancel")
    {
        return ShowAsync(new ConfirmationDialogOptions
        {
            Title = title,
            Message = message,
            PrimaryButtonText = primaryButton,
            CancelButtonText = cancelButton
        });
    }

    [RelayCommand]
    private void PrimaryAction()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ConfirmationResult.Primary);
    }

    [RelayCommand]
    private void SecondaryAction()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ConfirmationResult.Secondary);
    }

    [RelayCommand]
    private void CancelAction()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ConfirmationResult.Cancel);
    }

    /// <summary>
    /// Closes the dialog without a result (e.g., when clicking backdrop).
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        _completionSource?.TrySetResult(ConfirmationResult.None);
    }
}
