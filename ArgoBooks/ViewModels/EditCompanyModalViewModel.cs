using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Localization;
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
    private string? _businessType;

    [ObservableProperty]
    private string? _industry;

    [ObservableProperty]
    private Bitmap? _logoSource;

    [ObservableProperty]
    private bool _hasLogo;

    [ObservableProperty]
    private string? _logoPath;

    [ObservableProperty]
    private string _phoneNumber = "";

    [ObservableProperty]
    private CountryDialCode? _selectedPhoneCountry;

    [ObservableProperty]
    private string? _country;

    [ObservableProperty]
    private string? _city;

    [ObservableProperty]
    private string? _address;

    /// <summary>
    /// Available business types (shared data source).
    /// </summary>
    public static string[] BusinessTypes { get; } =
    [
        "Sole Proprietorship",
        "Partnership",
        "Corporation",
        "LLC",
        "Non-Profit",
        "Other"
    ];

    /// <summary>
    /// Available industries (shared data source).
    /// </summary>
    public static string[] Industries { get; } =
    [
        "Retail",
        "Services",
        "Manufacturing",
        "Technology",
        "Healthcare",
        "Food & Beverage",
        "Construction",
        "Transportation",
        "Real Estate",
        "Other"
    ];

    // Store original values for detecting changes
    private string _originalCompanyName = "";
    private string? _originalBusinessType;
    private string? _originalIndustry;
    private Bitmap? _originalLogo;
    private string _originalPhoneNumber = "";
    private CountryDialCode? _originalSelectedPhoneCountry;
    private string? _originalCountry;
    private string? _originalCity;
    private string? _originalAddress;

    /// <summary>
    /// Whether any changes have been made.
    /// </summary>
    public bool HasChanges =>
        CompanyName != _originalCompanyName ||
        BusinessType != _originalBusinessType ||
        Industry != _originalIndustry ||
        LogoSource != _originalLogo ||
        PhoneNumber != _originalPhoneNumber ||
        SelectedPhoneCountry != _originalSelectedPhoneCountry ||
        Country != _originalCountry ||
        City != _originalCity ||
        Address != _originalAddress;

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
    public void Open(
        string companyName,
        string? businessType = null,
        string? industry = null,
        Bitmap? logo = null,
        string? phone = null,
        string? country = null,
        string? city = null,
        string? address = null)
    {
        _originalCompanyName = companyName;
        _originalBusinessType = businessType;
        _originalIndustry = industry;
        _originalLogo = logo;

        CompanyName = companyName;
        BusinessType = businessType;
        Industry = industry;
        LogoSource = logo;
        HasLogo = logo != null;
        LogoPath = null;

        // Parse the phone number to extract country code and local number
        _originalPhoneNumber = "";
        _originalSelectedPhoneCountry = null;
        PhoneNumber = "";
        SelectedPhoneCountry = null;

        if (!string.IsNullOrWhiteSpace(phone))
        {
            // Try to parse the phone number - format is typically "+1 (555) 555-5555"
            // The PhoneInput control will handle parsing when we set the value
            var dialCodes = PhoneInput.AllDialCodes;
            foreach (var dialCode in dialCodes.OrderByDescending(d => d.DialCode.Length))
            {
                if (phone.StartsWith(dialCode.DialCode))
                {
                    _originalSelectedPhoneCountry = dialCode;
                    _originalPhoneNumber = phone[dialCode.DialCode.Length..].Trim();
                    SelectedPhoneCountry = dialCode;
                    PhoneNumber = _originalPhoneNumber;
                    break;
                }
            }

            // If no country code matched, just set the raw phone number
            if (SelectedPhoneCountry == null && !string.IsNullOrWhiteSpace(phone))
            {
                _originalPhoneNumber = phone;
                PhoneNumber = phone;
            }
        }

        _originalCountry = country;
        _originalCity = city;
        _originalAddress = address;
        Country = country;
        City = city;
        Address = address;

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
    private async Task CloseAsync()
    {
        await RequestCloseAsync();
    }

    /// <summary>
    /// Requests to close the modal, showing unsaved changes dialog if needed.
    /// </summary>
    public async void RequestClose()
    {
        await RequestCloseAsync();
    }

    /// <summary>
    /// Requests to close the modal, showing unsaved changes dialog if needed.
    /// </summary>
    private async Task RequestCloseAsync()
    {
        if (HasChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Unsaved Changes".Translate(),
                    Message = "You have unsaved changes to your company information. Do you want to save them before closing?".Translate(),
                    PrimaryButtonText = "Save".Translate(),
                    SecondaryButtonText = "Don't Save".Translate(),
                    CancelButtonText = "Cancel".Translate()
                });

                switch (result)
                {
                    case ConfirmationResult.Primary:
                        // Save and close
                        Save();
                        return;
                    case ConfirmationResult.Secondary:
                        // Don't save, just close
                        IsOpen = false;
                        return;
                    case ConfirmationResult.Cancel:
                    case ConfirmationResult.None:
                        // Stay open
                        return;
                }
            }
        }

        IsOpen = false;
    }

    /// <summary>
    /// Saves the changes and closes the modal.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        // Build the full phone number with country code
        string? fullPhone = null;
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            var dialCode = SelectedPhoneCountry?.DialCode ?? "";
            fullPhone = string.IsNullOrEmpty(dialCode) ? PhoneNumber : $"{dialCode} {PhoneNumber}";
        }

        CompanySaved?.Invoke(this, new CompanyEditedEventArgs
        {
            CompanyName = CompanyName,
            BusinessType = BusinessType,
            Industry = Industry,
            LogoSource = LogoSource,
            LogoPath = LogoPath,
            Phone = fullPhone,
            Country = Country,
            City = City,
            Address = Address
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

    partial void OnBusinessTypeChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnIndustryChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnPhoneNumberChanged(string value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnSelectedPhoneCountryChanged(CountryDialCode? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnCountryChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnCityChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
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
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public Bitmap? LogoSource { get; set; }
    public string? LogoPath { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
}
