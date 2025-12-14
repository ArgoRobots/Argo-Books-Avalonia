using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the upgrade modal.
/// </summary>
public partial class UpgradeModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isEnterKeyModalOpen;

    [ObservableProperty]
    private string? _licenseKey;

    [ObservableProperty]
    private bool _isVerifying;

    [ObservableProperty]
    private string? _verificationError;

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
        LicenseKey = null;
        VerificationError = null;
    }

    [RelayCommand]
    private void SelectPremium()
    {
        Close();
        PremiumSelected?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectAIPlan()
    {
        Close();
        AIPlanSelected?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenEnterKey()
    {
        IsOpen = false;
        IsEnterKeyModalOpen = true;
        LicenseKey = null;
        VerificationError = null;
    }

    [RelayCommand]
    private void CloseEnterKey()
    {
        IsEnterKeyModalOpen = false;
        LicenseKey = null;
        VerificationError = null;
    }

    [RelayCommand]
    private async Task VerifyKey()
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            VerificationError = "Please enter a license key";
            return;
        }

        IsVerifying = true;
        VerificationError = null;

        try
        {
            // Simulate verification delay
            await Task.Delay(1000);

            // TODO: Implement actual key verification
            // For now, just close the modal
            IsEnterKeyModalOpen = false;
            KeyVerified?.Invoke(this, LicenseKey);
        }
        catch (Exception ex)
        {
            VerificationError = ex.Message;
        }
        finally
        {
            IsVerifying = false;
        }
    }

    #endregion

    #region Events

    public event EventHandler? PremiumSelected;
    public event EventHandler? AIPlanSelected;
    public event EventHandler<string>? KeyVerified;

    #endregion
}
