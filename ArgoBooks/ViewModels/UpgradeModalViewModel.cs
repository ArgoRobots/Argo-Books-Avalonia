using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
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
    private string _premiumMonthlyPrice = "$10 CAD";

    [ObservableProperty]
    private string _premiumBillingPeriod = "/month";

    [ObservableProperty]
    private string _premiumYearlyPrice = "or $100/year";

    [ObservableProperty]
    private string _premiumYearlySavings = "(save $20)";

    private bool _hasFetchedPlans;

    public ObservableCollection<string> FreePlanFeatures { get; } = new(TranslateDefaults(DefaultFreeFeatures));

    public ObservableCollection<string> PremiumPlanFeatures { get; } = new(TranslateDefaults(DefaultPremiumFeatures));

    private static readonly string[] DefaultFreeFeatures =
    [
        "Up to 10 products",
        "Unlimited transactions",
        "Real-time analytics",
        "Receipt management",
        "5 invoices / month",
        "AI spreadsheet import (100/month)",
        "AI receipt scanning (5/month)"
    ];

    private static readonly string[] DefaultPremiumFeatures =
    [
        "Everything in Free",
        "Unlimited products",
        "Biometric login security",
        "Unlimited invoices & payments",
        "AI receipt scanning (500/month)",
        "Predictive analytics",
        "Priority support"
    ];

    private static IEnumerable<string> TranslateDefaults(string[] features)
        => features.Select(f => f.Translate());

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
    }

    #region Commands

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;

        // Retry fetching plans if the previous attempt failed (e.g. was offline at startup)
        if (!_hasFetchedPlans || IsOffline)
        {
            _ = FetchPlansAsync();
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
        try
        {
            var response = await HttpClient.GetStringAsync(PricingApiUrl);
            var apiResponse = JsonSerializer.Deserialize<PlansApiResponse>(response);

            IsOffline = false;

            if (apiResponse?.Pricing != null)
            {
                PremiumMonthlyPrice = apiResponse.Pricing.PremiumPriceDisplay;
                PremiumBillingPeriod = "/month";
                if (apiResponse.Pricing.PremiumYearlyPriceDisplay is not null &&
                    apiResponse.Pricing.PremiumYearlySavingsDisplay is not null)
                {
                    PremiumYearlyPrice = $"or {apiResponse.Pricing.PremiumYearlyPriceDisplay}/year";
                    PremiumYearlySavings = $"(save {apiResponse.Pricing.PremiumYearlySavingsDisplay})";
                }
            }

            if (apiResponse?.Plans != null)
            {
                if (apiResponse.Plans.Free?.Features is { Count: > 0 })
                {
                    FreePlanFeatures.Clear();
                    foreach (var feature in apiResponse.Plans.Free.Features)
                        FreePlanFeatures.Add(feature.DisplayText.Translate());
                }

                if (apiResponse.Plans.Premium?.Features is { Count: > 0 })
                {
                    PremiumPlanFeatures.Clear();
                    foreach (var feature in apiResponse.Plans.Premium.Features)
                        PremiumPlanFeatures.Add(feature.DisplayText.Translate());
                }
            }

            _hasFetchedPlans = true;
        }
        catch (Exception ex)
        {
            // HttpRequestException with a status code means the server responded (not a connectivity issue)
            var isConnectivityError = ex is HttpRequestException { StatusCode: null }
                || (ex is TaskCanceledException tce && (tce.InnerException is TimeoutException || tce.CancellationToken != default));

            if (isConnectivityError)
            {
                IsOffline = true;
                // Don't set _hasFetchedPlans so Open() will retry when the modal is opened
            }
            else
            {
                // Server error or bad JSON — show plans with defaults, don't mark offline
                IsOffline = false;
                _hasFetchedPlans = true;
                App.ErrorLogger?.LogError(ex, ErrorCategory.Network, "Failed to fetch plans from API");
            }
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

        public string DisplayText => Detail != null ? $"{Label} ({Detail})" : Label;
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
