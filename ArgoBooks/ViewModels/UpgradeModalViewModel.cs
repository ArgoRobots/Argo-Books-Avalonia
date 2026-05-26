using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the upgrade modal.
/// </summary>
public partial class UpgradeModalViewModel : ViewModelBase
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly string LicenseRedeemUrl = $"{ApiConfig.BaseUrl}/api/license/redeem.php";
    private static readonly string ApiHostUrl = ApiConfig.BaseUrl;
    private readonly IConnectivityService _connectivityService = new ConnectivityService();
    private static readonly string PricingApiUrl = $"{ApiConfig.BaseUrl}/api/pricing/plans.php";
    private static readonly string PremiumUpgradeUrl = $"{ApiConfig.BaseUrl}/pricing/";
    private static readonly string CancelSubscriptionUrl = $"{ApiConfig.BaseUrl}/community/users/subscription.php";

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isEnterKeyModalOpen;

    [ObservableProperty]
    private bool _isVerifying;

    [ObservableProperty]
    private string? _verificationError;

    [ObservableProperty]
    private bool _isVerificationSuccess;

    [ObservableProperty]
    private bool _showContinueButton;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private string _licenseKey = string.Empty;

    [ObservableProperty]
    private bool _isOffline;

    partial void OnIsVerificationSuccessChanged(bool value)
    {
        if (value)
        {
            // Show continue button after 2 second delay
            _ = ShowContinueButtonAfterDelayAsync();
        }
        else
        {
            ShowContinueButton = false;
        }
    }

    private async Task ShowContinueButtonAfterDelayAsync()
    {
        await Task.Delay(1500);
        if (IsVerificationSuccess)
        {
            ShowContinueButton = true;
        }
    }

    #region Pricing

    [ObservableProperty]
    private string _premiumMonthlyPrice = "";

    [ObservableProperty]
    private string _premiumBillingPeriod = "";

    [ObservableProperty]
    private string _premiumYearlyPrice = "";

    [ObservableProperty]
    private string _premiumYearlySavings = "";

    // New: yearly billed-per-month price (e.g., "$8.33 CAD") shown when yearly is selected
    [ObservableProperty]
    private string _premiumYearlyPerMonth = "";

    // New: strikethrough monthly price (e.g., "$10/month") shown above the yearly-per-month
    [ObservableProperty]
    private string _premiumMonthlyStrike = "";

    // New: "Save 17%" pill shown on the yearly toggle option
    [ObservableProperty]
    private string _yearlySavingsPercentDisplay = "";

    // New: billing-cycle toggle state. Yearly is the default.
    [ObservableProperty]
    private bool _isYearlyBilling = true;

    public bool IsMonthlyBilling => !IsYearlyBilling;

    partial void OnIsYearlyBillingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMonthlyBilling));
    }

    [ObservableProperty]
    private bool _isLoadingPlans;

    [ObservableProperty]
    private bool _hasLoadError;

    /// <summary>
    /// True when plans have been fetched successfully and should be displayed.
    /// </summary>
    public bool ShowPlans => !IsOffline && !IsLoadingPlans && !HasLoadError && _hasFetchedPlans;

    partial void OnIsOfflineChanged(bool value) => OnPropertyChanged(nameof(ShowPlans));
    partial void OnIsLoadingPlansChanged(bool value) => OnPropertyChanged(nameof(ShowPlans));
    partial void OnHasLoadErrorChanged(bool value) => OnPropertyChanged(nameof(ShowPlans));

    private bool _hasFetchedPlans;

    public ObservableCollection<string> FreePlanFeatures { get; } = [];

    public ObservableCollection<string> PremiumPlanFeatures { get; } = [];

    // Raw label/detail pairs from the API, kept so we can re-translate when the language changes
    private List<PlanFeature> _rawFreeFeatures = [];
    private List<PlanFeature> _rawPremiumFeatures = [];

    // Raw pricing strings from the API, kept so we can re-translate when the language changes
    private string? _rawPremiumYearlyPriceDisplay;
    private string? _rawPremiumYearlySavingsDisplay;

    // Raw numeric pricing from the API, used to derive the strike/per-month/savings-percent
    // strings for the yearly toggle state.
    private double _rawMonthlyPrice;
    private double _rawYearlyPrice;
    private string _rawCurrency = "CAD";

    #endregion

    #region Plan Status

    [ObservableProperty]
    private bool _hasPremium;

    /// <summary>
    /// Gets whether to show "Active" badge on Premium card.
    /// </summary>
    public bool ShowPremiumActive => HasPremium;

    /// <summary>
    /// Gets whether to show Select Premium button (doesn't have Premium).
    /// </summary>
    public bool ShowSelectPremium => !HasPremium;

    /// <summary>
    /// Gets whether to show the cancel subscription button for Premium.
    /// </summary>
    public bool ShowCancelPremium => HasPremium;

    partial void OnHasPremiumChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPremiumActive));
        OnPropertyChanged(nameof(ShowSelectPremium));
        OnPropertyChanged(nameof(ShowCancelPremium));
    }

    #endregion

    /// <summary>
    /// Gets the formatted license key for API calls (keeps dashes).
    /// </summary>
    private string GetFormattedLicenseKey()
    {
        return LicenseKey.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public UpgradeModalViewModel()
    {
        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        RefreshFeatureDisplay();
    }

    private void RefreshFeatureDisplay()
    {
        FreePlanFeatures.Clear();
        foreach (var feature in _rawFreeFeatures)
            FreePlanFeatures.Add(feature.DisplayText);

        PremiumPlanFeatures.Clear();
        foreach (var feature in _rawPremiumFeatures)
            PremiumPlanFeatures.Add(feature.DisplayText);

        RefreshPricingDisplay();
    }

    private void RefreshPricingDisplay()
    {
        // Period now also carries the currency code so it renders as e.g. "CAD/month"
        // at the same size and color as the period text. We construct it manually so
        // the slash is preserved and the word stays lowercase regardless of how the
        // translation pipeline handles "/month" or "month".
        var monthWord = "month".Translate();
        if (!string.IsNullOrEmpty(monthWord) && char.IsUpper(monthWord[0]))
        {
            monthWord = char.ToLowerInvariant(monthWord[0]) + monthWord.Substring(1);
        }
        PremiumBillingPeriod = string.IsNullOrEmpty(_rawCurrency)
            ? "/" + monthWord
            : _rawCurrency + "/" + monthWord;

        if (_rawPremiumYearlyPriceDisplay is not null && _rawPremiumYearlySavingsDisplay is not null)
        {
            PremiumYearlyPrice = "or {0}/year".TranslateFormat(_rawPremiumYearlyPriceDisplay);
            // Parens are added separately so Azure can translate the bare phrase reliably,
            // it leaves "(save {0})" unchanged for several languages (mt, nl, sk).
            PremiumYearlySavings = "(" + "save {0}".TranslateFormat(_rawPremiumYearlySavingsDisplay) + ")";
        }
        else
        {
            // Clear stale text from a prior fetch, otherwise an API response that omits
            // the yearly fields would leave the previous yearly pricing visible.
            PremiumYearlyPrice = string.Empty;
            PremiumYearlySavings = string.Empty;
        }

        // Rebuild the big-number strings from the raw numeric prices so the currency
        // sits with the period text rather than next to the dollar amount.
        if (_rawMonthlyPrice > 0 && _rawYearlyPrice > 0)
        {
            PremiumMonthlyPrice = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "${0:0}",
                _rawMonthlyPrice);

            var perMonth = _rawYearlyPrice / 12.0;
            PremiumYearlyPerMonth = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "${0:0.00}",
                perMonth);
            PremiumMonthlyStrike = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "${0:0}/month",
                _rawMonthlyPrice);

            var savingsPct = (int)Math.Round((1 - _rawYearlyPrice / (_rawMonthlyPrice * 12)) * 100);
            // Keep the % glued to the number rather than in the format string,
            // some translation passes strip lone '%' characters.
            var savingsPctText = savingsPct.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
            YearlySavingsPercentDisplay = "Save {0}".TranslateFormat(savingsPctText);
        }
        else
        {
            // Leave PremiumMonthlyPrice as set by the caller (the API display string) so we
            // don't blank out the price when only display strings are available.
            PremiumYearlyPerMonth = string.Empty;
            PremiumMonthlyStrike = string.Empty;
            YearlySavingsPercentDisplay = string.Empty;
        }
    }

    #region Commands

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;

        // Fetch plans on first open, or retry if a previous attempt failed
        if (!_hasFetchedPlans || IsOffline || HasLoadError)
        {
            _ = FetchPlansAsync();
        }
        else
        {
            // Rebuild display in case language changed while modal was closed
            RefreshFeatureDisplay();
        }
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        IsEnterKeyModalOpen = false;
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        VerificationError = null;
        SuccessMessage = null;
    }

    [RelayCommand]
    private void SelectPremium()
    {
        OpenUrl(PremiumUpgradeUrl);
        Close();
    }

    [RelayCommand]
    private void SelectMonthlyBilling() => IsYearlyBilling = false;

    [RelayCommand]
    private void SelectYearlyBilling() => IsYearlyBilling = true;

    [RelayCommand]
    private void CancelSubscription()
    {
        OpenUrl(CancelSubscriptionUrl);
        Close();
    }

    private static void OpenUrl(string url)
    {
        UrlHelper.SafeOpenUrl(url);
    }

    [RelayCommand]
    private async Task RequestCloseEnterKey()
    {
        // Don't allow closing during success animation - user must click Continue
        if (IsVerificationSuccess)
            return;

        // If no data was entered, just close
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            CloseEnterKey();
            return;
        }

        // Data was entered - ask for confirmation
        if (!await ConfirmDiscardNewAsync())
            return;

        CloseEnterKey();
    }

    [RelayCommand]
    private void OpenEnterKey()
    {
        IsOpen = false;
        IsEnterKeyModalOpen = true;
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        VerificationError = null;
        SuccessMessage = null;
    }

    [RelayCommand]
    private void CloseEnterKey()
    {
        IsEnterKeyModalOpen = false;
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        VerificationError = null;
        SuccessMessage = null;
    }

    [RelayCommand]
    private void GoBackToUpgrade()
    {
        IsEnterKeyModalOpen = false;
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        VerificationError = null;
        SuccessMessage = null;
        IsOpen = true;
    }

    [RelayCommand]
    private void ContinueAfterSuccess()
    {
        IsEnterKeyModalOpen = false;
        KeyVerified?.Invoke(this, LicenseKey);
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        SuccessMessage = null;
    }

    [RelayCommand]
    private async Task VerifyKey()
    {
        var key = GetFormattedLicenseKey();

        if (string.IsNullOrWhiteSpace(key))
        {
            VerificationError = "Please enter a license key";
            return;
        }

        // Format: XXXX-XXXX-XXXX-XXXX-XXXX (24 chars with dashes)
        if (key.Length != 24)
        {
            VerificationError = "License key must be in format XXXX-XXXX-XXXX-XXXX-XXXX";
            return;
        }

        IsVerifying = true;
        VerificationError = null;
        IsVerificationSuccess = false;

        try
        {
            var response = await RedeemLicenseAsync(key);

            if (response?.Success == true)
            {
                IsVerificationSuccess = true;
                SuccessMessage = response.Message ?? "License activated successfully!";

                // Save license securely
                var licenseType = response.Type?.ToLowerInvariant() ?? "";
                var hasPremium = licenseType.Contains("premium");

                if (App.LicenseService != null)
                {
                    try
                    {
                        await App.LicenseService.SaveLicenseAsync(hasPremium, key);
                    }
                    catch (Exception ex)
                    {
                        App.ErrorLogger?.LogError(ex, ErrorCategory.License, "Failed to save license after verification");
                        var dialog = App.ConfirmationDialog;
                        if (dialog != null)
                        {
                            await dialog.ShowAsync(new ConfirmationDialogOptions
                            {
                                Title = "Warning".Translate(),
                                Message = "Your license was activated but could not be saved locally. You may need to re-enter your license key next time.".Translate(),
                                PrimaryButtonText = "OK".Translate(),
                                SecondaryButtonText = null,
                                CancelButtonText = null
                            });
                        }
                    }
                }

                // User will click Continue button to close
            }
            else
            {
                VerificationError = response?.Message ?? "Invalid license key";
            }
        }
        catch (HttpRequestException)
        {
            VerificationError = await GetConnectivityErrorMessageAsync();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.CancellationToken != default)
        {
            VerificationError = await GetConnectivityErrorMessageAsync();
        }
        catch (TaskCanceledException)
        {
            VerificationError = "Request was cancelled.".Translate();
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, ErrorCategory.Network, "License redemption request failed");
            VerificationError = "Verification failed: {0}".TranslateFormat(ex.Message);
        }
        finally
        {
            IsVerifying = false;
        }
    }

    /// <summary>
    /// Determines whether the error is due to no internet or API being down.
    /// </summary>
    private async Task<string> GetConnectivityErrorMessageAsync()
    {
        try
        {
            // First check if we have internet at all
            var hasInternet = await _connectivityService.IsInternetAvailableAsync();

            if (!hasInternet)
            {
                return "No internet connection. Please check your network and try again.".Translate();
            }

            // We have internet, so check if the API host is reachable
            var isApiReachable = await _connectivityService.IsHostReachableAsync(ApiHostUrl);

            if (!isApiReachable)
            {
                return "Unable to reach Argo Books servers. The service may be temporarily unavailable. Please try again later.".Translate();
            }

            // API is reachable but something else went wrong
            return "Unable to verify license. Please try again.".Translate();
        }
        catch
        {
            // If connectivity check itself fails, assume no internet
            return "Unable to verify license. Please check your internet connection.".Translate();
        }
    }

    /// <summary>
    /// Redeems a license key on the server, marking it as used and binding to this device.
    /// </summary>
    private async Task<LicenseResponse?> RedeemLicenseAsync(string premiumKey)
    {
        var deviceId = App.LicenseService?.GetDeviceId() ?? "";

        var requestBody = new { premium_key = premiumKey, device_id = deviceId };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(LicenseRedeemUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LicenseResponse>(responseJson);
    }

    #endregion

    #region Events

    public event EventHandler<string>? KeyVerified;

    #endregion

    #region Plans API

    /// <summary>
    /// Fetches plan details and pricing from the website API.
    /// Updates features lists and pricing from the server so the app stays in sync.
    /// Sets IsOffline if the API is unreachable.
    /// Called once on app startup for free-tier users.
    /// </summary>
    public async Task FetchPlansAsync()
    {
        IsLoadingPlans = true;
        HasLoadError = false;
        // Reset IsOffline so this attempt's outcome isn't blended with a previous one.
        // The catch path re-sets it only on connectivity errors; a non-connectivity
        // failure (e.g. 5xx) must not leave the offline panel from a prior call visible.
        IsOffline = false;

        try
        {
            var response = await HttpClient.GetStringAsync(PricingApiUrl);
            var apiResponse = JsonSerializer.Deserialize<PlansApiResponse>(response);

            if (apiResponse?.Pricing != null)
            {
                PremiumMonthlyPrice = apiResponse.Pricing.PremiumPriceDisplay;
                _rawPremiumYearlyPriceDisplay = apiResponse.Pricing.PremiumYearlyPriceDisplay;
                _rawPremiumYearlySavingsDisplay = apiResponse.Pricing.PremiumYearlySavingsDisplay;
                _rawMonthlyPrice = apiResponse.Pricing.PremiumMonthlyPriceNumeric;
                _rawYearlyPrice = apiResponse.Pricing.PremiumYearlyPriceNumeric;
                _rawCurrency = apiResponse.Pricing.Currency ?? "CAD";
                RefreshPricingDisplay();
            }

            if (apiResponse?.Plans != null)
            {
                _rawFreeFeatures = apiResponse.Plans.Free?.Features ?? [];
                _rawPremiumFeatures = apiResponse.Plans.Premium?.Features ?? [];
                RefreshFeatureDisplay();
            }

            _hasFetchedPlans = true;
            OnPropertyChanged(nameof(ShowPlans));
        }
        catch (Exception ex)
        {
            var isConnectivityError = ex is HttpRequestException { StatusCode: null }
                || (ex is TaskCanceledException tce && (tce.InnerException is TimeoutException || tce.CancellationToken != default));

            if (isConnectivityError)
            {
                IsOffline = true;
            }
            else
            {
                HasLoadError = true;
                App.ErrorLogger?.LogError(ex, ErrorCategory.Network, "Failed to fetch plans from API");
            }
        }
        finally
        {
            IsLoadingPlans = false;
        }
    }

    #region API Response Models

    private class PlansApiResponse
    {
        [JsonPropertyName("plans")]
        public PlansData? Plans { get; init; }

        [JsonPropertyName("pricing")]
        public PricingData? Pricing { get; init; }
    }

    private class PlansData
    {
        [JsonPropertyName("free")]
        public PlanInfo? Free { get; init; }

        [JsonPropertyName("premium")]
        public PlanInfo? Premium { get; init; }
    }

    private class PlanInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("features")]
        public List<PlanFeature>? Features { get; init; }
    }

    private class PlanFeature
    {
        [JsonPropertyName("label")]
        public string Label { get; init; } = "";

        [JsonPropertyName("detail")]
        public string? Detail { get; init; }

        public string DisplayText => Detail != null ? $"{Label.Translate()} ({Detail.Translate()})" : Label.Translate();
    }

    private class PricingData
    {
        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("premium_price_display")]
        public string PremiumPriceDisplay { get; init; } = "$10 CAD";

        [JsonPropertyName("premium_yearly_price_display")]
        public string? PremiumYearlyPriceDisplay { get; init; }

        [JsonPropertyName("premium_yearly_savings_display")]
        public string? PremiumYearlySavingsDisplay { get; init; }

        [JsonPropertyName("premium_monthly_price")]
        public double PremiumMonthlyPriceNumeric { get; init; }

        [JsonPropertyName("premium_yearly_price")]
        public double PremiumYearlyPriceNumeric { get; init; }
    }

    #endregion

    #endregion

    #region Response Models

    private class LicenseResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("subscription_id")]
        public string? SubscriptionId { get; init; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; init; }

        [JsonPropertyName("duration_months")]
        public int DurationMonths { get; init; }
    }

    #endregion
}
