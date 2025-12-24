using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the login modal.
/// </summary>
public partial class LoginModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _accountId = string.Empty;

    [ObservableProperty]
    private string _accountName = string.Empty;

    [ObservableProperty]
    private string _accountDescription = string.Empty;

    [ObservableProperty]
    private string _accountInitials = string.Empty;

    [ObservableProperty]
    private Color _accountColor = Color.Parse("#3B82F6");

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Icon for password visibility toggle.
    /// </summary>
    public string PasswordVisibilityIcon => IsPasswordVisible
        ? "M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z"
        : "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";

    /// <summary>
    /// Login button text.
    /// </summary>
    public string LoginButtonText => IsLoading ? "Signing in..." : "Sign In";

    /// <summary>
    /// Default constructor.
    /// </summary>
    public LoginModalViewModel()
    {
    }

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordVisibilityIcon));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoginButtonText));
    }

    /// <summary>
    /// Opens the modal for a specific account.
    /// </summary>
    public void OpenForAccount(AccountItem account)
    {
        AccountId = account.Id;
        AccountName = account.Name;
        AccountDescription = account.Description;
        AccountInitials = account.Initials;
        AccountColor = Color.Parse(account.Color);
        Password = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        IsLoading = false;
        IsPasswordVisible = false;
        IsOpen = true;
    }

    #region Commands

    /// <summary>
    /// Opens the modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        Password = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        IsLoading = false;
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Password = string.Empty;
    }

    /// <summary>
    /// Cancels the login.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Close();
        LoginCancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles password visibility.
    /// </summary>
    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    /// <summary>
    /// Performs login.
    /// </summary>
    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your password";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        // Simulate login delay
        await Task.Delay(800);

        // For demo purposes, accept any password
        // In a real app, this would validate against a backend
        IsLoading = false;

        LoginSuccessful?.Invoke(this, new LoginEventArgs(AccountId, RememberMe));
        Close();
    }

    /// <summary>
    /// Opens forgot password flow.
    /// </summary>
    [RelayCommand]
    private void ForgotPassword()
    {
        ForgotPasswordRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    public event EventHandler<LoginEventArgs>? LoginSuccessful;
    public event EventHandler? LoginCancelled;
    public event EventHandler? ForgotPasswordRequested;

    #endregion
}

/// <summary>
/// Event arguments for login events.
/// </summary>
public class LoginEventArgs : EventArgs
{
    public string AccountId { get; }
    public bool RememberMe { get; }

    public LoginEventArgs(string accountId, bool rememberMe)
    {
        AccountId = accountId;
        RememberMe = rememberMe;
    }
}
