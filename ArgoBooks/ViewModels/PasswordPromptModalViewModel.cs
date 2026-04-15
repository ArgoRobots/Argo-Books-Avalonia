using ArgoBooks.Localization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the password prompt modal.
/// Used when opening encrypted company files.
/// </summary>
public partial class PasswordPromptModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _windowsHelloAvailable;

    [ObservableProperty]
    private bool _isWindowsHelloAuthenticating;

    [ObservableProperty]
    private bool _showWindowsHelloSuccess;

    private TaskCompletionSource<string?>? _completionSource;
    private TaskCompletionSource<bool>? _windowsHelloSuccessCompletionSource;

    /// <summary>
    /// Event raised when the password textbox should be focused.
    /// </summary>
    public event EventHandler? FocusPasswordRequested;

    /// <summary>
    /// Event raised when Windows Hello authentication is requested.
    /// </summary>
    public event EventHandler? WindowsHelloAuthRequested;

    /// <summary>
    /// Gets the task that completes when the user submits a password.
    /// Use this to wait for password retries after calling ShowError.
    /// </summary>
    public Task<string?> WaitForPasswordAsync()
    {
        return _completionSource?.Task ?? Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Icon for password visibility toggle.
    /// </summary>
    public string PasswordVisibilityIcon => IsPasswordVisible ? Icons.EyeOff : Icons.Eye;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public PasswordPromptModalViewModel()
    {
    }

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordVisibilityIcon));
    }

    /// <summary>
    /// Shows the password prompt and waits for user input.
    /// </summary>
    /// <param name="companyName">Name of the company file.</param>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="windowsHelloAvailable">Whether Windows Hello is available and enabled for this file.</param>
    /// <param name="message">Optional custom message. If null, defaults to "Enter password for {companyName}".</param>
    /// <returns>The entered password, or null if cancelled.</returns>
    public Task<string?> ShowAsync(string companyName, string filePath, bool windowsHelloAvailable = false, string? message = null)
    {
        CompanyName = companyName;
        FilePath = filePath;
        Message = message ?? string.Format("Enter password for {0}".Translate(), companyName);
        Password = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        IsLoading = false;
        IsPasswordVisible = false;
        WindowsHelloAvailable = windowsHelloAvailable;
        IsWindowsHelloAuthenticating = false;
        ShowWindowsHelloSuccess = false;
        IsOpen = true;

        // Use RunContinuationsAsynchronously to prevent TrySetResult from running
        // the awaiting continuation inline, which can block UI updates
        _completionSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _completionSource.Task;
    }

    /// <summary>
    /// Shows an error message (e.g., wrong password) and keeps modal open for retry.
    /// </summary>
    public void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        IsLoading = false;
        IsWindowsHelloAuthenticating = false;
        IsOpen = true; // Ensure modal stays/reopens

        // Create a new completion source for the retry
        // Use RunContinuationsAsynchronously to prevent inline continuations
        _completionSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Request focus on the password textbox
        FocusPasswordRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when Windows Hello authentication succeeds.
    /// Shows success animation and then completes with a special marker.
    /// </summary>
    public void OnWindowsHelloSuccess()
    {
        IsWindowsHelloAuthenticating = false;
        ShowWindowsHelloSuccess = true;
        _windowsHelloSuccessCompletionSource = new TaskCompletionSource<bool>();
    }

    /// <summary>
    /// Called when the success animation continue button is clicked.
    /// </summary>
    [RelayCommand]
    private void ContinueAfterSuccess()
    {
        ShowWindowsHelloSuccess = false;
        IsOpen = false;
        _windowsHelloSuccessCompletionSource?.TrySetResult(true);
        // Complete with special marker to indicate Windows Hello was used
        _completionSource?.TrySetResult("__WINDOWS_HELLO__");
    }

    /// <summary>
    /// Called when Windows Hello authentication fails.
    /// </summary>
    public void OnWindowsHelloFailed()
    {
        IsWindowsHelloAuthenticating = false;
        // Don't show error - user may have cancelled, just let them try password
    }

    /// <summary>
    /// Closes the modal after successful password entry.
    /// Call this when the password was accepted.
    /// </summary>
    public void Close()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CloseCore();
        }
        else
        {
            Dispatcher.UIThread.Post(CloseCore);
        }
    }

    private void CloseCore()
    {
        IsOpen = false;
        IsLoading = false;
        IsWindowsHelloAuthenticating = false;
        ShowWindowsHelloSuccess = false;
        Password = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
    }

    #region Commands

    /// <summary>
    /// Toggles password visibility.
    /// </summary>
    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    /// <summary>
    /// Submits the password.
    /// </summary>
    [RelayCommand]
    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter the password";
            HasError = true;
            return;
        }

        HasError = false;
        IsLoading = true;

        // Complete with the password but keep modal open
        // The caller will close it on success or call ShowError on failure
        _completionSource?.TrySetResult(Password);
    }

    /// <summary>
    /// Authenticates using Windows Hello.
    /// </summary>
    [RelayCommand]
    private void UseWindowsHello()
    {
        if (!WindowsHelloAvailable) return;

        HasError = false;
        IsWindowsHelloAuthenticating = true;

        // Request Windows Hello authentication from the app
        WindowsHelloAuthRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cancels the password prompt, showing confirmation if a password was entered.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!string.IsNullOrEmpty(Password))
        {
            if (!await ConfirmDiscardNewAsync()) return;
        }

        IsOpen = false;
        Password = string.Empty;
        IsWindowsHelloAuthenticating = false;
        ShowWindowsHelloSuccess = false;
        _completionSource?.TrySetResult(null);
    }

    #endregion
}
