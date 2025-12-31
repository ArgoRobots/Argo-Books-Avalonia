using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the upgrade modal.
/// </summary>
public partial class UpgradeModalViewModel : ViewModelBase
{
    private static readonly HttpClient HttpClient = new();
    private const string LicenseValidationUrl = "https://argorobots.com/validate_license.php";
    private const string StandardUpgradeUrl = "http://localhost/argo-books-website/upgrade/standard/";
    private const string PremiumUpgradeUrl = "http://localhost/argo-books-website/upgrade/premium/";
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

    // Stores the license type from the last successful verification
    private string? _verifiedLicenseType;

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

    #region Plan Status

    [ObservableProperty]
    private bool _hasStandard;

    [ObservableProperty]
    private bool _hasPremium;

    /// <summary>
    /// Gets whether to show "Active" badge on Standard card (has Standard but not Premium).
    /// </summary>
    public bool ShowStandardActive => HasStandard && !HasPremium;

    /// <summary>
    /// Gets whether to show "Included" text on Standard card (has Premium).
    /// </summary>
    public bool ShowStandardIncluded => HasPremium;

    /// <summary>
    /// Gets whether to show Select Standard button (no plan at all).
    /// </summary>
    public bool ShowSelectStandard => !HasStandard && !HasPremium;

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

    partial void OnHasStandardChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowStandardActive));
        OnPropertyChanged(nameof(ShowSelectStandard));
    }

    partial void OnHasPremiumChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowStandardActive));
        OnPropertyChanged(nameof(ShowStandardIncluded));
        OnPropertyChanged(nameof(ShowSelectStandard));
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
        _verifiedLicenseType = null;
    }

    [RelayCommand]
    private void SelectStandard()
    {
        OpenUrl(StandardUpgradeUrl);
        Close();
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
    private void OpenEnterKey()
    {
        IsOpen = false;
        IsEnterKeyModalOpen = true;
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        VerificationError = null;
        SuccessMessage = null;
        _verifiedLicenseType = null;
    }

    [RelayCommand]
    private void CloseEnterKey()
    {
        IsEnterKeyModalOpen = false;
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        VerificationError = null;
        SuccessMessage = null;
        _verifiedLicenseType = null;
    }

    [RelayCommand]
    private void ContinueAfterSuccess()
    {
        IsEnterKeyModalOpen = false;
        KeyVerified?.Invoke(this, LicenseKey);
        IsVerificationSuccess = false;
        LicenseKey = string.Empty;
        SuccessMessage = null;
        _verifiedLicenseType = null;
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
                // Store the license type for saving when user clicks Continue
                _verifiedLicenseType = response.Type;

                IsVerificationSuccess = true;
                // Fix server message: change "can be redeemed" to "has been redeemed"
                var message = response.Message ?? "License activated successfully!";
                SuccessMessage = message.Replace("can be redeemed", "has been redeemed");

                // Save license securely
                var hasStandard = response.Type?.Equals("standard", StringComparison.OrdinalIgnoreCase) == true ||
                                  response.Type?.Equals("premium", StringComparison.OrdinalIgnoreCase) == true;
                var hasPremium = response.Type?.Equals("premium", StringComparison.OrdinalIgnoreCase) == true;

                if (App.LicenseService != null)
                {
                    try
                    {
                        await App.LicenseService.SaveLicenseAsync(hasStandard, hasPremium, key);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save license: {ex.Message}");
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
            VerificationError = "Unable to connect to the server. Please check your internet connection.";
        }
        catch (TaskCanceledException)
        {
            VerificationError = "Request timed out. Please try again.";
        }
        catch (Exception ex)
        {
            VerificationError = $"Verification failed: {ex.Message}";
        }
        finally
        {
            IsVerifying = false;
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var response = await HttpClient.PostAsync(LicenseValidationUrl, content, cts.Token);

        var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
        return JsonSerializer.Deserialize<LicenseResponse>(responseJson);
    }

    #endregion

    #region Events

    public event EventHandler<string>? KeyVerified;

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
