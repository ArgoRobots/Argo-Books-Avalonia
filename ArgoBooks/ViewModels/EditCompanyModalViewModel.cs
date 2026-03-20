using System.Linq;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Services;
using ArgoBooks.Data;
using ArgoBooks.Localization;
using ArgoBooks.Services;
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

    [ObservableProperty]
    private string? _provinceState;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _selectedCurrency = "USD - US Dollar ($)";

    // Currency change error state
    [ObservableProperty]
    private bool _hasCurrencyError;

    [ObservableProperty]
    private string _currencyErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSavingCurrency;

    /// <summary>
    /// Priority/common currencies shown at the top of the dropdown.
    /// </summary>
    public IReadOnlyList<string> PriorityCurrencies => Data.Currencies.Priority;

    /// <summary>
    /// All available currencies.
    /// </summary>
    public IReadOnlyList<string> Currencies => Data.Currencies.All;

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
    private string? _originalProvinceState;
    private string _originalEmail = "";
    private string _originalCurrency = "USD - US Dollar ($)";
    private string? _pendingCurrencyCode;

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
        Address != _originalAddress ||
        ProvinceState != _originalProvinceState ||
        Email != _originalEmail ||
        SelectedCurrency != _originalCurrency;

    /// <summary>
    /// Whether the form is valid for saving.
    /// </summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(CompanyName) && !string.IsNullOrWhiteSpace(Country);

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
        string? address = null,
        string? provinceState = null,
        string? email = null,
        string? currencyCode = null)
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

        // Load currency
        var currencyDisplayString = CurrencyService.GetDisplayString(currencyCode ?? "USD");
        _originalCurrency = currencyDisplayString;
        SelectedCurrency = currencyDisplayString;
        HasCurrencyError = false;
        CurrencyErrorMessage = string.Empty;

        // Parse the phone number to extract country code and local number
        _originalPhoneNumber = "";
        _originalSelectedPhoneCountry = null;
        PhoneNumber = "";
        SelectedPhoneCountry = null;

        if (!string.IsNullOrWhiteSpace(phone))
        {
            // Try to parse the phone number - format is typically "+1 5551000000"
            var dialCodes = PhoneInput.AllDialCodes;
            CountryDialCode? matchedDialCode;

            // Find all dial codes that match the phone prefix
            var matchingCodes = dialCodes
                .Where(d => phone.StartsWith(d.DialCode))
                .OrderByDescending(d => d.DialCode.Length)
                .ToList();

            if (matchingCodes.Count > 0)
            {
                // If the company country is set and multiple codes share the same dial code,
                // prefer the one matching the company country
                matchedDialCode = matchingCodes.FirstOrDefault(d =>
                    !string.IsNullOrEmpty(country) &&
                    d.Name.Equals(country, StringComparison.OrdinalIgnoreCase))
                    ?? matchingCodes[0];

                var remaining = phone[matchedDialCode.DialCode.Length..].Trim();
                _originalSelectedPhoneCountry = matchedDialCode;
                _originalPhoneNumber = remaining;
                SelectedPhoneCountry = matchedDialCode;
                PhoneNumber = remaining;
            }
            else
            {
                // No dial code prefix found — extract raw digits and default to US
                var digits = new string(phone.Where(char.IsDigit).ToArray());
                _originalPhoneNumber = digits;
                PhoneNumber = digits;
                var usDialCode = dialCodes.FirstOrDefault(c => c.Code == "US");
                _originalSelectedPhoneCountry = usDialCode;
                SelectedPhoneCountry = usDialCode;
            }
        }

        _originalCountry = country;
        _originalCity = city;
        _originalAddress = address;
        _originalProvinceState = provinceState;
        _originalEmail = email ?? "";
        Country = country;
        City = city;
        Address = address;
        ProvinceState = provinceState;
        Email = email ?? "";

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
        try
        {
            await RequestCloseAsync();
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.UI, "EditCompanyModal.RequestClose");
        }
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
                        await SaveAsync();
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

    [RelayCommand]
    private void DismissCurrencyError()
    {
        HasCurrencyError = false;
        CurrencyErrorMessage = string.Empty;
        // Revert the currency selection since the user cancelled
        if (_pendingCurrencyCode != null)
        {
            SelectedCurrency = _originalCurrency;
            _pendingCurrencyCode = null;
        }
    }

    [RelayCommand]
    private async Task RetryCurrencySaveAsync()
    {
        if (_pendingCurrencyCode == null)
        {
            HasCurrencyError = false;
            CurrencyErrorMessage = string.Empty;
            await SaveAsync();
            return;
        }

        // Keep the modal open but switch to loading state
        CurrencyErrorMessage = string.Empty;
        IsSavingCurrency = true;

        // Run the preload and a minimum display time in parallel so the spinner is always visible
        var preloadTask = PreloadExchangeRatesForCurrencyAsync(_pendingCurrencyCode);
        var minimumDisplayTask = Task.Delay(1000);
        await Task.WhenAll(preloadTask, minimumDisplayTask);

        var success = await preloadTask;
        IsSavingCurrency = false;

        if (!success)
        {
            // Still failing — error state is already set by PreloadExchangeRatesForCurrencyAsync
            return;
        }

        // Rates loaded successfully — dismiss error and proceed with save
        HasCurrencyError = false;
        CurrencyErrorMessage = string.Empty;
        _pendingCurrencyCode = null;
        await SaveAsync(skipCurrencyValidation: true);
    }

    /// <summary>
    /// Saves the changes and closes the modal.
    /// </summary>
    [RelayCommand]
    private Task SaveAsync() => SaveAsync(skipCurrencyValidation: false);

    private async Task SaveAsync(bool skipCurrencyValidation)
    {
        if (!CanSave) return;

        var currencyChanged = SelectedCurrency != _originalCurrency;

        // If currency changed and we haven't already validated rates via retry
        if (currencyChanged && !skipCurrencyValidation)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Change Default Currency".Translate(),
                    Message = "Changing the default currency will update how all amounts are displayed throughout the app. Existing transactions will be converted using historical exchange rates. This may result in small rounding differences. You can change it back any time.".Translate(),
                    PrimaryButtonText = "Change Currency".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = false
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }

            // Preload exchange rates for the new currency
            var newCurrencyCode = CurrencyService.ParseCurrencyCode(SelectedCurrency);
            IsSavingCurrency = true;
            var success = await PreloadExchangeRatesForCurrencyAsync(newCurrencyCode);
            IsSavingCurrency = false;

            if (!success)
            {
                // Store the pending currency so retry can pick it up
                _pendingCurrencyCode = newCurrencyCode;
                return;
            }
            _pendingCurrencyCode = null;
        }

        // Build the full phone number with country code
        string? fullPhone = null;
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            var dialCode = SelectedPhoneCountry?.DialCode ?? "";
            fullPhone = string.IsNullOrEmpty(dialCode) ? PhoneNumber : $"{dialCode} {PhoneNumber}";
        }

        // If currency changed, save it to company settings and notify
        if (currencyChanged)
        {
            var newCurrencyCode = CurrencyService.ParseCurrencyCode(SelectedCurrency);
            var settings = App.CompanyManager?.CompanyData?.Settings;
            if (settings != null)
            {
                settings.Localization.Currency = newCurrencyCode;
            }
            _originalCurrency = SelectedCurrency;

            // Preload exchange rates for all transaction dates so display conversions are exact
            if (!string.Equals(newCurrencyCode, "USD", StringComparison.OrdinalIgnoreCase))
            {
                var exchangeService = ExchangeRateService.Instance;
                var companyData = App.CompanyManager?.CompanyData;
                if (exchangeService != null && companyData != null)
                {
                    var transactionDates = companyData.Expenses.Select(e => e.Date)
                        .Concat(companyData.Revenues.Select(r => r.Date))
                        .Append(DateTime.Today);
                    await exchangeService.PreloadRatesAsync(transactionDates);
                }
            }

            CurrencyService.NotifyCurrencyChanged();
        }

        CompanySaved?.Invoke(this, new CompanyEditedEventArgs
        {
            CompanyName = CompanyName,
            BusinessType = BusinessType,
            Industry = Industry,
            LogoSource = LogoSource,
            LogoPath = LogoPath,
            Phone = fullPhone,
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            Country = Country,
            City = City,
            Address = Address,
            ProvinceState = ProvinceState
        });

        IsOpen = false;
    }

    /// <summary>
    /// Preloads exchange rates for the selected currency.
    /// Returns true if successful, false if exchange rates could not be fetched.
    /// </summary>
    private async Task<bool> PreloadExchangeRatesForCurrencyAsync(string currencyCode)
    {
        // Skip if USD (no conversion needed)
        if (string.Equals(currencyCode, "USD", StringComparison.OrdinalIgnoreCase))
            return true;

        var exchangeService = ExchangeRateService.Instance;
        if (exchangeService == null)
        {
            HasCurrencyError = true;
            CurrencyErrorMessage = "Exchange rate service is not available. Please restart the application.".Translate();
            return false;
        }

        // Check if there are any transactions that need conversion
        var companyData = App.CompanyManager?.CompanyData;
        var hasTransactions = companyData != null &&
            (companyData.Expenses.Any() || companyData.Revenues.Any());

        // Check connectivity first
        var connectivityService = new ConnectivityService();
        var hasInternet = await connectivityService.IsInternetAvailableAsync();

        if (!hasInternet)
        {
            if (hasTransactions)
            {
                HasCurrencyError = true;
                CurrencyErrorMessage = "No internet connection. Exchange rates are required to convert existing transactions.".Translate();
                return false;
            }
            // No transactions — allow the change without rates
            return true;
        }

        // Try to get exchange rate for today
        var today = DateTime.Today;
        var rate = await exchangeService.GetExchangeRateAsync(currencyCode, "USD", today, fetchIfMissing: true);

        if (rate <= 0)
        {
            if (hasTransactions)
            {
                HasCurrencyError = true;
                CurrencyErrorMessage = "Unable to fetch exchange rates. Please check your connection and try again.".Translate();
                return false;
            }
            // No transactions — allow the change even if rate fetch failed
            return true;
        }

        // Rate available - try to preload more dates in the background
        try
        {
            var dates = Enumerable.Range(1, 30).Select(i => today.AddDays(-i)).ToList();
            await exchangeService.PreloadRatesAsync(dates);
        }
        catch
        {
            // Preloading additional dates failed, but we have today's rate so it's OK
        }

        return true;
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
    partial void OnCountryChanged(string? value)
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasChanges));
    }
    partial void OnCityChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnAddressChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnProvinceStateChanged(string? value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(HasChanges));
    partial void OnSelectedCurrencyChanged(string value) => OnPropertyChanged(nameof(HasChanges));

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
    public string? Email { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? ProvinceState { get; set; }
}
