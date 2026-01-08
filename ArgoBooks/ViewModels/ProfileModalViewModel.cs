using ArgoBooks.Core.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the user profile modal.
/// </summary>
public partial class ProfileModalViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;
    private readonly HeaderViewModel? _headerViewModel;

    [ObservableProperty]
    private bool _isOpen;

    #region Profile Fields

    [ObservableProperty]
    private string? _displayName;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _role;

    [ObservableProperty]
    private string? _initials;

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private Bitmap? _avatarSource;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _department;

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public ProfileModalViewModel()
    {
        // Design-time defaults
        DisplayName = "John Doe";
        Email = "john.doe@company.com";
        Role = "Administrator";
        Initials = "JD";
        Phone = "+1 (555) 123-4567";
        Department = "Finance";
    }

    /// <summary>
    /// Constructor with dependencies.
    /// </summary>
    public ProfileModalViewModel(INavigationService? navigationService, HeaderViewModel? headerViewModel)
    {
        _navigationService = navigationService;
        _headerViewModel = headerViewModel;

        // Sync with header view model
        if (_headerViewModel != null)
        {
            DisplayName = _headerViewModel.UserDisplayName;
            Email = _headerViewModel.UserEmail;
            Role = _headerViewModel.UserRole;
            Initials = _headerViewModel.UserInitials;
            HasAvatar = _headerViewModel.HasUserAvatar;
            AvatarSource = _headerViewModel.UserAvatarSource;
        }
    }

    #region Commands

    /// <summary>
    /// Opens the profile modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        // Refresh data from header
        if (_headerViewModel != null)
        {
            DisplayName = _headerViewModel.UserDisplayName;
            Email = _headerViewModel.UserEmail;
            Role = _headerViewModel.UserRole;
            Initials = _headerViewModel.UserInitials;
            HasAvatar = _headerViewModel.HasUserAvatar;
            AvatarSource = _headerViewModel.UserAvatarSource;
        }
        IsOpen = true;
    }

    /// <summary>
    /// Closes the profile modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Saves the profile changes.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        // Update header view model
        _headerViewModel?.SetUserInfo(DisplayName, Email, Role, AvatarSource);
        Close();
    }

    /// <summary>
    /// Opens file picker to change avatar.
    /// </summary>
    [RelayCommand]
    private void ChangeAvatar()
    {
        // TODO: Open file picker for avatar
    }

    /// <summary>
    /// Removes the current avatar.
    /// </summary>
    [RelayCommand]
    private void RemoveAvatar()
    {
        AvatarSource = null;
        HasAvatar = false;
    }

    #endregion
}
