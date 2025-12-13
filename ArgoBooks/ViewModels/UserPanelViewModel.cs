using ArgoBooks.Core.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the user panel dropdown.
/// </summary>
public partial class UserPanelViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;
    private readonly HeaderViewModel? _headerViewModel;

    [ObservableProperty]
    private bool _isOpen;

    #region User Info

    /// <summary>
    /// User's display name.
    /// </summary>
    public string? UserDisplayName => _headerViewModel?.UserDisplayName;

    /// <summary>
    /// User's initials for avatar fallback.
    /// </summary>
    public string? UserInitials => _headerViewModel?.UserInitials;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string? UserEmail => _headerViewModel?.UserEmail;

    /// <summary>
    /// User's role.
    /// </summary>
    public string? UserRole => _headerViewModel?.UserRole;

    /// <summary>
    /// Whether the user has an avatar image.
    /// </summary>
    public bool HasUserAvatar => _headerViewModel?.HasUserAvatar ?? false;

    /// <summary>
    /// User's avatar image source.
    /// </summary>
    public Bitmap? UserAvatarSource => _headerViewModel?.UserAvatarSource;

    #endregion

    #region Company Info

    [ObservableProperty]
    private string? _companyName = "Acme Corporation";

    [ObservableProperty]
    private string? _companyPlan = "Professional";

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public UserPanelViewModel()
    {
        // Design-time defaults
        _headerViewModel = new HeaderViewModel();
    }

    /// <summary>
    /// Constructor with dependencies.
    /// </summary>
    public UserPanelViewModel(INavigationService? navigationService, HeaderViewModel headerViewModel)
    {
        _navigationService = navigationService;
        _headerViewModel = headerViewModel;

        // Subscribe to header property changes to update our bindings
        if (_headerViewModel != null)
        {
            _headerViewModel.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(HeaderViewModel.UserDisplayName):
                        OnPropertyChanged(nameof(UserDisplayName));
                        break;
                    case nameof(HeaderViewModel.UserInitials):
                        OnPropertyChanged(nameof(UserInitials));
                        break;
                    case nameof(HeaderViewModel.UserEmail):
                        OnPropertyChanged(nameof(UserEmail));
                        break;
                    case nameof(HeaderViewModel.UserRole):
                        OnPropertyChanged(nameof(UserRole));
                        break;
                    case nameof(HeaderViewModel.HasUserAvatar):
                        OnPropertyChanged(nameof(HasUserAvatar));
                        break;
                    case nameof(HeaderViewModel.UserAvatarSource):
                        OnPropertyChanged(nameof(UserAvatarSource));
                        break;
                }
            };
        }
    }

    #region Commands

    /// <summary>
    /// Opens the user panel.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
    }

    /// <summary>
    /// Closes the user panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Toggles the user panel.
    /// </summary>
    [RelayCommand]
    private void Toggle()
    {
        IsOpen = !IsOpen;
    }

    /// <summary>
    /// Opens user profile/account settings.
    /// </summary>
    [RelayCommand]
    private void OpenProfile()
    {
        Close();
        _navigationService?.NavigateTo("Profile");
    }

    /// <summary>
    /// Opens application settings.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        Close();
        _navigationService?.NavigateTo("Settings");
    }

    /// <summary>
    /// Opens help and support.
    /// </summary>
    [RelayCommand]
    private void OpenHelp()
    {
        Close();
        _navigationService?.NavigateTo("Help");
    }

    /// <summary>
    /// Opens keyboard shortcuts reference.
    /// </summary>
    [RelayCommand]
    private void OpenKeyboardShortcuts()
    {
        Close();
        // TODO: Show keyboard shortcuts modal
    }

    /// <summary>
    /// Switches to a different company.
    /// </summary>
    [RelayCommand]
    private void SwitchCompany()
    {
        Close();
        // TODO: Show company switcher
    }

    /// <summary>
    /// Closes the current company file.
    /// </summary>
    [RelayCommand]
    private void CloseCompany()
    {
        Close();
        // TODO: Close current company and return to welcome screen
    }

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    [RelayCommand]
    private void SignOut()
    {
        Close();
        // TODO: Implement sign out logic
    }

    #endregion
}
