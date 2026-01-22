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
    public string PasswordVisibilityIcon => IsPasswordVisible
        ? "M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z"
        : "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";

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
    /// <returns>The entered password, or null if cancelled.</returns>
    public Task<string?> ShowAsync(string companyName, string filePath, bool windowsHelloAvailable = false)
    {
        CompanyName = companyName;
        FilePath = filePath;
        Password = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        IsLoading = false;
        IsPasswordVisible = false;
        WindowsHelloAvailable = windowsHelloAvailable;
        IsWindowsHelloAuthenticating = false;
        ShowWindowsHelloSuccess = false;
        IsOpen = true;

        _completionSource = new TaskCompletionSource<string?>();
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
        _completionSource = new TaskCompletionSource<string?>();

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
    /// Cancels the password prompt.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
        Password = string.Empty;
        IsWindowsHelloAuthenticating = false;
        ShowWindowsHelloSuccess = false;
        _completionSource?.TrySetResult(null);
    }

    #endregion
}
