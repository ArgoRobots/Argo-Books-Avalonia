using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Telemetry;
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
    private const string LicenseValidationUrl = "https://argorobots.com/api/license/validate.php";
    private const string ApiHostUrl = "https://argorobots.com";
    private readonly IConnectivityService _connectivityService = new ConnectivityService();
    private const string PricingApiUrl = "https://argorobots.com/api/pricing/plans.php";
    private const string PremiumUpgradeUrl = "https://argorobots.com/upgrade/premium/";
    private const string CancelSubscriptionUrl = "https://argorobots.com/community/users/subscription.php";

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

    private bool _hasFetchedPricing;

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

        // Fetch pricing in the background when modal opens (once per session)
        if (!_hasFetchedPricing)
        {
            _ = FetchPricingAsync();
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
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
    }

    [RelayCommand]
    private async Task RequestCloseEnterKey()
    {
        // If no data was entered, just close
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            CloseEnterKey();
            return;
        }

        // Data was entered - ask for confirmation
        var dialog = App.ConfirmationDialog;
        if (dialog != null)
        {
            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Discard Changes?".Translate(),
                Message = "You have entered data that will be lost. Are you sure you want to close?".Translate(),
                PrimaryButtonText = "Discard".Translate(),
                CancelButtonText = "Cancel".Translate(),
                IsPrimaryDestructive = true
            });

            if (result != ConfirmationResult.Primary)
                return;
        }

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
            var response = await ValidateLicenseAsync(key);

            if (response?.Success == true)
            {
                IsVerificationSuccess = true;
                // Fix server message: change "can be redeemed" to "has been redeemed"
                var message = response.Message ?? "License activated successfully!";
                SuccessMessage = message.Replace("can be redeemed", "has been redeemed");

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
            App.ErrorLogger?.LogError(ex, ErrorCategory.Network, "License verification request failed");
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
    /// Validates a license key against the server.
    /// </summary>
    private async Task<LicenseResponse?> ValidateLicenseAsync(string licenseKey)
    {
        var requestBody = new { license_key = licenseKey };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(LicenseValidationUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LicenseResponse>(responseJson);
    }

    #endregion

    #region Events

    public event EventHandler<string>? KeyVerified;

    #endregion

    #region Pricing API

    /// <summary>
    /// Fetches plan pricing from the website API.
    /// Falls back to hardcoded defaults if the request fails.
    /// </summary>
    private async Task FetchPricingAsync()
    {
        try
        {
            var response = await HttpClient.GetStringAsync(PricingApiUrl);
            var pricing = JsonSerializer.Deserialize<PricingApiResponse>(response);

            if (pricing?.Premium != null)
            {
                PremiumMonthlyPrice = pricing.Premium.PriceDisplay;
                PremiumBillingPeriod = pricing.Premium.BillingPeriod;
            }

            _hasFetchedPricing = true;
        }
        catch (Exception ex)
        {
            _hasFetchedPricing = true;

            // Only log if it's not a connectivity issue
            var isConnectivityError = ex is HttpRequestException
                || (ex is TaskCanceledException tce && (tce.InnerException is TimeoutException || tce.CancellationToken != default));

            if (!isConnectivityError || await _connectivityService.IsInternetAvailableAsync())
            {
                App.ErrorLogger?.LogError(ex, ErrorCategory.Network, "Failed to fetch pricing from API");
            }
        }
    }

    private class PricingApiResponse
    {
        [JsonPropertyName("premium")]
        public PlanPricing? Premium { get; init; }
    }

    private class PlanPricing
    {
        [JsonPropertyName("price_display")]
        public string PriceDisplay { get; init; } = "$10 CAD";

        [JsonPropertyName("billing_period")]
        public string BillingPeriod { get; init; } = "/month";
    }

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

        [JsonPropertyName("activation_date")]
        public string? ActivationDate { get; init; }

        [JsonPropertyName("key")]
        public string? Key { get; init; }
    }

    #endregion
}
