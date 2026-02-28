using ArgoBooks.Controls;
using ArgoBooks.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Company Creation Wizard.
/// </summary>
public partial class CreateCompanyViewModel : ViewModelBase
{
    #region Wizard State

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _currentStep = 1;

    private const int TotalSteps = 2;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;

    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < TotalSteps;
    public bool IsLastStep => CurrentStep == TotalSteps;

    #endregion

    #region Step 1: Company Info

    [ObservableProperty]
    private string? _companyName;

    [ObservableProperty]
    private string? _businessType;

    [ObservableProperty]
    private string? _industry;

    public string[] BusinessTypes { get; } =
    [
        "Sole Proprietorship",
        "Partnership",
        "Corporation",
        "LLC",
        "Non-Profit",
        "Other"
    ];

    public string[] Industries { get; } =
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

    [ObservableProperty]
    private string _selectedCurrency = "USD - US Dollar ($)";

    /// <summary>
    /// All available currencies.
    /// </summary>
    public IReadOnlyList<string> Currencies => Data.Currencies.All;

    /// <summary>
    /// Priority/common currencies shown at the top of the dropdown.
    /// </summary>
    public IReadOnlyList<string> PriorityCurrencies => Data.Currencies.Priority;

    #endregion

    #region Step 1: Contact Information

    [ObservableProperty]
    private string _phoneNumber = "";

    [ObservableProperty]
    private CountryDialCode? _selectedPhoneCountry;

    [ObservableProperty]
    private string? _country;

    [ObservableProperty]
    private string? _city;

    [ObservableProperty]
    private string? _provinceState;

    [ObservableProperty]
    private string? _address;

    #endregion

    #region Step 3: Security & Logo

    [ObservableProperty]
    private bool _enablePassword;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _confirmPassword;

    [ObservableProperty]
    private bool _hasLogo;

    [ObservableProperty]
    private Bitmap? _logoSource;

    [ObservableProperty]
    private string? _logoPath;

    public bool PasswordsMatch => Password == ConfirmPassword;

    public bool ShowPasswordError => EnablePassword && !string.IsNullOrEmpty(ConfirmPassword) && !PasswordsMatch;

    #endregion

    #region Validation

    public bool IsStep1Valid => !string.IsNullOrWhiteSpace(CompanyName) && !string.IsNullOrWhiteSpace(Country);

    public bool IsStep2Valid => !EnablePassword || (PasswordsMatch && !string.IsNullOrWhiteSpace(Password));

    public bool CanCreate => IsStep1Valid && IsStep2Valid;

    #endregion

    /// <summary>
    /// Event raised when a company is created.
    /// </summary>
    public event EventHandler<CompanyCreatedEventArgs>? CompanyCreated;

    #region Commands

    [RelayCommand]
    private void Open()
    {
        Reset();
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Reset();
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < TotalSteps)
        {
            CurrentStep++;
            UpdateStepProperties();
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateStepProperties();
        }
    }

    [RelayCommand]
    private void GoToStep(int step)
    {
        if (step >= 1 && step <= TotalSteps)
        {
            CurrentStep = step;
            UpdateStepProperties();
        }
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        // This will be handled by the view to open file picker
        BrowseLogoRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        LogoSource = null;
        LogoPath = null;
        HasLogo = false;
    }

    [RelayCommand]
    private void CreateCompany()
    {
        if (!CanCreate) return;

        // Build the full phone number with country code
        string? fullPhone = null;
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            var dialCode = SelectedPhoneCountry?.DialCode ?? "";
            fullPhone = string.IsNullOrEmpty(dialCode) ? PhoneNumber : $"{dialCode} {PhoneNumber}";
        }

        var args = new CompanyCreatedEventArgs
        {
            CompanyName = CompanyName!,
            BusinessType = BusinessType,
            Industry = Industry,
            Address = Address,
            City = City,
            ProvinceState = ProvinceState,
            Country = Country,
            PhoneNumber = fullPhone,
            DefaultCurrency = CurrencyService.ParseCurrencyCode(SelectedCurrency),
            Password = EnablePassword ? Password : null,
            LogoPath = LogoPath
        };

        CompanyCreated?.Invoke(this, args);
        Close();
    }

    #endregion

    /// <summary>
    /// Event raised when browse logo is requested.
    /// </summary>
    public event EventHandler? BrowseLogoRequested;

    /// <summary>
    /// Sets the logo from file path.
    /// </summary>
    public void SetLogo(string path, Bitmap? bitmap)
    {
        LogoPath = path;
        LogoSource = bitmap;
        HasLogo = bitmap != null;
    }

    private void Reset()
    {
        CurrentStep = 1;
        CompanyName = null;
        BusinessType = null;
        Industry = null;
        SelectedCurrency = "USD - US Dollar ($)";
        PhoneNumber = "";
        SelectedPhoneCountry = null;
        Country = null;
        City = null;
        ProvinceState = null;
        Address = null;
        EnablePassword = false;
        Password = null;
        ConfirmPassword = null;
        LogoSource = null;
        LogoPath = null;
        HasLogo = false;
        UpdateStepProperties();
    }

    private void UpdateStepProperties()
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(IsLastStep));
    }

    partial void OnCompanyNameChanged(string? value)
    {
        OnPropertyChanged(nameof(IsStep1Valid));
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnPasswordChanged(string? value)
    {
        OnPropertyChanged(nameof(PasswordsMatch));
        OnPropertyChanged(nameof(ShowPasswordError));
        OnPropertyChanged(nameof(IsStep2Valid));
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnConfirmPasswordChanged(string? value)
    {
        OnPropertyChanged(nameof(PasswordsMatch));
        OnPropertyChanged(nameof(ShowPasswordError));
        OnPropertyChanged(nameof(IsStep2Valid));
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnCountryChanged(string? value)
    {
        OnPropertyChanged(nameof(IsStep1Valid));
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnEnablePasswordChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStep2Valid));
        OnPropertyChanged(nameof(CanCreate));
    }
}

/// <summary>
/// Event arguments for company creation.
/// </summary>
public class CompanyCreatedEventArgs : EventArgs
{
    public required string CompanyName { get; init; }
    public string? BusinessType { get; init; }
    public string? Industry { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? ProvinceState { get; init; }
    public string? Address { get; init; }
    public string? DefaultCurrency { get; init; }
    public string? Password { get; init; }
    public string? LogoPath { get; init; }
}
