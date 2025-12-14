using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Edit Company modal.
/// </summary>
public partial class EditCompanyModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _companyName = "";

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    private Bitmap? _logoSource;

    [ObservableProperty]
    private bool _hasLogo;

    [ObservableProperty]
    private string? _logoPath;

    // Store original values for detecting changes
    private string _originalCompanyName = "";
    private string? _originalEmail;
    private string? _originalPhone;
    private string? _originalAddress;
    private Bitmap? _originalLogo;

    /// <summary>
    /// Whether any changes have been made.
    /// </summary>
    public bool HasChanges =>
        CompanyName != _originalCompanyName ||
        Email != _originalEmail ||
        Phone != _originalPhone ||
        Address != _originalAddress ||
        LogoSource != _originalLogo;

    /// <summary>
    /// Whether the form is valid for saving.
    /// </summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(CompanyName);

    /// <summary>
    /// Default constructor.
    /// </summary>
    public EditCompanyModalViewModel()
    {
    }

    /// <summary>
    /// Opens the modal with the current company info.
    /// </summary>
    public void Open(string companyName, string? email = null, string? phone = null, string? address = null, Bitmap? logo = null)
    {
        _originalCompanyName = companyName;
        _originalEmail = email;
        _originalPhone = phone;
        _originalAddress = address;
        _originalLogo = logo;

        CompanyName = companyName;
        Email = email;
        Phone = phone;
        Address = address;
        LogoSource = logo;
        HasLogo = logo != null;
        LogoPath = null;

        IsOpen = true;
    }

    /// <summary>
    /// Opens the modal.
    /// </summary>
    [RelayCommand]
    private void OpenModal()
    {
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal without saving.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Saves the changes and closes the modal.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        CompanySaved?.Invoke(this, new CompanyEditedEventArgs
        {
            CompanyName = CompanyName,
            Email = Email,
            Phone = Phone,
            Address = Address,
            LogoSource = LogoSource,
            LogoPath = LogoPath
        });

        IsOpen = false;
    }

    /// <summary>
    /// Opens the logo file picker.
    /// </summary>
    [RelayCommand]
    private void BrowseLogo()
    {
        BrowseLogoRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes the current logo.
    /// </summary>
    [RelayCommand]
    private void RemoveLogo()
    {
        LogoSource = null;
        HasLogo = false;
        LogoPath = null;
        OnPropertyChanged(nameof(HasChanges));
    }

    /// <summary>
    /// Sets the logo from a file.
    /// </summary>
    public void SetLogo(string path, Bitmap bitmap)
    {
        LogoPath = path;
        LogoSource = bitmap;
        HasLogo = true;
        OnPropertyChanged(nameof(HasChanges));
    }

    partial void OnCompanyNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasChanges));
    }

    partial void OnEmailChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnPhoneChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnAddressChanged(string? value) => OnPropertyChanged(nameof(HasChanges));

    /// <summary>
    /// Event raised when the company is saved.
    /// </summary>
    public event EventHandler<CompanyEditedEventArgs>? CompanySaved;

    /// <summary>
    /// Event raised when the logo browse button is clicked.
    /// </summary>
    public event EventHandler? BrowseLogoRequested;
}

/// <summary>
/// Event args for when company is edited.
/// </summary>
public class CompanyEditedEventArgs : EventArgs
{
    public string CompanyName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public Bitmap? LogoSource { get; set; }
    public string? LogoPath { get; set; }
}
