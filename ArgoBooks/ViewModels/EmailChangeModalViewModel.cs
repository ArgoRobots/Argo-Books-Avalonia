using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// 4-step email change wizard. The owner email is locked from in-place edit;
/// changing it requires:
///   Step 1: enter new email
///   Step 2: enter the .argo file password (only shown if encrypted)
///   Step 3: enter the 6-digit code emailed to the OLD address
///   Step 4: enter the 6-digit code emailed to the NEW address
/// On success the OLD address receives a notification with a 30-day revert link.
/// </summary>
public partial class EmailChangeModalViewModel : ObservableObject
{
    public enum Step
    {
        EnterNewEmail,
        EnterPassword,
        EnterOldCode,
        EnterNewCode,
        Success,
        Failure,
    }

    private readonly RefundService _refundService;
    private readonly bool _fileIsEncrypted;
    private readonly Func<string, bool> _verifyFilePassword;
    private long _changeId;

    public string CurrentOwnerEmail { get; }

    public EmailChangeModalViewModel(
        RefundService refundService,
        string currentOwnerEmail,
        bool fileIsEncrypted,
        Func<string, bool> verifyFilePassword)
    {
        _refundService = refundService;
        CurrentOwnerEmail = currentOwnerEmail;
        _fileIsEncrypted = fileIsEncrypted;
        _verifyFilePassword = verifyFilePassword;
    }

    [ObservableProperty]
    private Step _currentStep = Step.EnterNewEmail;

    public bool IsEnterNewEmail => CurrentStep == Step.EnterNewEmail;
    public bool IsEnterPassword => CurrentStep == Step.EnterPassword;
    public bool IsEnterOldCode  => CurrentStep == Step.EnterOldCode;
    public bool IsEnterNewCode  => CurrentStep == Step.EnterNewCode;
    public bool IsSuccess       => CurrentStep == Step.Success;
    public bool IsFailure       => CurrentStep == Step.Failure;

    partial void OnCurrentStepChanged(Step value)
    {
        OnPropertyChanged(nameof(IsEnterNewEmail));
        OnPropertyChanged(nameof(IsEnterPassword));
        OnPropertyChanged(nameof(IsEnterOldCode));
        OnPropertyChanged(nameof(IsEnterNewCode));
        OnPropertyChanged(nameof(IsSuccess));
        OnPropertyChanged(nameof(IsFailure));
    }

    [ObservableProperty]
    private string _newEmail = string.Empty;

    [ObservableProperty]
    private string _filePassword = string.Empty;

    [ObservableProperty]
    private string _oldCode = string.Empty;

    [ObservableProperty]
    private string _newCode = string.Empty;

    [ObservableProperty]
    private string? _maskedOldEmail;

    [ObservableProperty]
    private string? _maskedNewEmail;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public bool CanContinueFromNewEmail
        => !IsBusy && IsValidEmail(NewEmail) && !string.Equals(NewEmail.Trim(), CurrentOwnerEmail, StringComparison.OrdinalIgnoreCase);
    public bool CanContinueFromPassword => !IsBusy && FilePassword.Length > 0;
    public bool CanSubmitOldCode => !IsBusy && OldCode.Length == 6 && OldCode.All(char.IsDigit);
    public bool CanSubmitNewCode => !IsBusy && NewCode.Length == 6 && NewCode.All(char.IsDigit);

    partial void OnNewEmailChanged(string value)    => OnPropertyChanged(nameof(CanContinueFromNewEmail));
    partial void OnFilePasswordChanged(string value)=> OnPropertyChanged(nameof(CanContinueFromPassword));
    partial void OnOldCodeChanged(string value)     => OnPropertyChanged(nameof(CanSubmitOldCode));
    partial void OnNewCodeChanged(string value)     => OnPropertyChanged(nameof(CanSubmitNewCode));
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanContinueFromNewEmail));
        OnPropertyChanged(nameof(CanContinueFromPassword));
        OnPropertyChanged(nameof(CanSubmitOldCode));
        OnPropertyChanged(nameof(CanSubmitNewCode));
    }

    private static bool IsValidEmail(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        try { var addr = new System.Net.Mail.MailAddress(s); return addr.Address == s.Trim(); }
        catch { return false; }
    }

    [RelayCommand]
    private void ContinueFromNewEmail()
    {
        if (!CanContinueFromNewEmail) return;
        ErrorMessage = null;
        // Skip password step entirely if the file isn't encrypted.
        CurrentStep = _fileIsEncrypted ? Step.EnterPassword : Step.EnterOldCode;
        if (CurrentStep == Step.EnterOldCode) _ = RequestEmailChangeAsync(passwordVerified: false);
    }

    [RelayCommand]
    private async Task ContinueFromPasswordAsync()
    {
        if (!CanContinueFromPassword) return;
        IsBusy = true; ErrorMessage = null;
        try
        {
            var ok = false;
            try { ok = _verifyFilePassword(FilePassword); }
            catch { ok = false; }
            if (!ok)
            {
                ErrorMessage = "Wrong file password.";
                return;
            }
            FilePassword = string.Empty; // clear from memory once verified
            await RequestEmailChangeAsync(passwordVerified: true);
        }
        finally { IsBusy = false; }
    }

    private async Task RequestEmailChangeAsync(bool passwordVerified)
    {
        IsBusy = true; ErrorMessage = null;
        try
        {
            var result = await _refundService.RequestEmailChangeAsync(NewEmail.Trim(), passwordVerified);
            if (!result.Ok)
            {
                ErrorMessage = result.ErrorCode switch
                {
                    "EMAIL_IN_USE" => "That email is already used by another portal account.",
                    "COOLDOWN_ACTIVE" => "Email was changed less than 24h ago. Try again later.",
                    "INVALID_EMAIL" => "Invalid email address.",
                    _ => result.Message ?? "Could not start the email change.",
                };
                return;
            }
            _changeId = result.ChangeId;
            MaskedOldEmail = result.MaskedOldEmail;
            CurrentStep = Step.EnterOldCode;
            StatusMessage = $"Code sent to {result.MaskedOldEmail}.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SubmitOldCodeAsync()
    {
        if (!CanSubmitOldCode) return;
        IsBusy = true; ErrorMessage = null;
        try
        {
            var result = await _refundService.ConfirmEmailChangeOldAsync(_changeId, OldCode);
            if (!result.Ok)
            {
                ErrorMessage = result.ErrorCode == "WRONG_CODE" ? "Wrong code. Try again." : (result.Message ?? "Could not verify code.");
                return;
            }
            MaskedNewEmail = result.MaskedNewEmail;
            CurrentStep = Step.EnterNewCode;
            StatusMessage = $"Code sent to {result.MaskedNewEmail}.";
            OldCode = string.Empty;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SubmitNewCodeAsync()
    {
        if (!CanSubmitNewCode) return;
        IsBusy = true; ErrorMessage = null;
        try
        {
            var result = await _refundService.ConfirmEmailChangeNewAsync(_changeId, NewCode);
            if (!result.Ok)
            {
                ErrorMessage = result.ErrorCode == "WRONG_CODE" ? "Wrong code. Try again." : (result.Message ?? "Could not verify code.");
                return;
            }
            CurrentStep = Step.Success;
            StatusMessage = $"Owner email is now {result.NewEmail}. The OLD address received a revert link valid for 30 days.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ResendOldCodeAsync()
    {
        if (_changeId == 0) return;
        IsBusy = true;
        try { await _refundService.ResendEmailChangeCodeAsync(_changeId, "old"); StatusMessage = "Code re-sent."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ResendNewCodeAsync()
    {
        if (_changeId == 0) return;
        IsBusy = true;
        try { await _refundService.ResendEmailChangeCodeAsync(_changeId, "new"); StatusMessage = "Code re-sent."; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CancelChangeAsync()
    {
        if (_changeId == 0) return;
        try { await _refundService.CancelEmailChangeAsync(_changeId); } catch { /* best-effort */ }
    }
}
