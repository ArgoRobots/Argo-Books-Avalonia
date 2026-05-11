using System.Collections.ObjectModel;
using System.Net.Http;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// View-model for the refund modal. Two visible steps in one modal:
///   1. Pick payment(s) and line items, optionally enter a reason.
///   2. Enter the 6-digit code emailed to the locked owner address.
/// Plus terminal states: Cooling-Off (status poll), Success, Failure.
/// </summary>
public partial class RefundModalViewModel : ObservableObject
{
    public enum Step
    {
        LineItems,
        EnterCode,
        Polling,
        Success,
        Failure,
        Details,
    }

    private readonly RefundService _refundService;
    private readonly Invoice _invoice;
    private readonly List<Payment> _allPayments; // all payments for this invoice
    private long _requestId;
    private CancellationTokenSource? _pollCts;
    private System.Timers.Timer? _countdownTimer;

    /// <summary>
    /// Set by the coordinator (RefundModalsViewModel) so the in-modal X button
    /// can close the modal without knowing about the coordinator directly.
    /// </summary>
    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();

    // ---------- header ----------
    public string InvoiceNumber => _invoice.InvoiceNumber;
    public string CustomerDisplay { get; }
    public decimal Total => _invoice.Total;
    public string Currency => string.IsNullOrEmpty(_invoice.OriginalCurrency) ? "USD" : _invoice.OriginalCurrency;

    // ---------- step + spinner ----------
    [ObservableProperty]
    private Step _currentStep = Step.LineItems;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public bool IsLineItemsStep => CurrentStep == Step.LineItems;
    public bool IsEnterCodeStep => CurrentStep == Step.EnterCode;
    public bool IsPollingStep => CurrentStep == Step.Polling;
    public bool IsSuccessStep => CurrentStep == Step.Success;
    public bool IsFailureStep => CurrentStep == Step.Failure;
    public bool IsDetailsStep => CurrentStep == Step.Details;

    /// <summary>
    /// Read-only terminal states where the only action is dismiss, MINUS
    /// Success (the SuccessAnimation overlay supplies its own Done button,
    /// so the footer Done would double-up). Drives the footer Done button.
    /// </summary>
    public bool IsNonSuccessTerminalStep =>
        CurrentStep == Step.Failure ||
        CurrentStep == Step.Details;

    /// <summary>
    /// Whether the footer Border should render. Hidden while the busy overlay
    /// is active OR the success animation is showing, since both of those
    /// fully cover the modal body and the footer would otherwise leak through
    /// on top (XAML draw order — footer is declared after the overlays).
    /// </summary>
    public bool ShowRefundFooter => !IsBusy && CurrentStep != Step.Success;

    /// <summary>Headline shown in the busy overlay (depends on what the user just clicked).</summary>
    public string BusyLabel => CurrentStep switch
    {
        Step.LineItems => "Sending verification code...",
        Step.EnterCode => "Issuing refund...",
        _ => "Working..."
    };

    /// <summary>Sub-label shown in the busy overlay.</summary>
    public string BusySubLabel => CurrentStep switch
    {
        Step.LineItems => "We're emailing a 6-digit code to your owner email so you can confirm this refund.",
        Step.EnterCode => "Confirming the code and sending the refund through to the payment provider.",
        _ => string.Empty
    };

    partial void OnCurrentStepChanged(Step value)
    {
        OnPropertyChanged(nameof(IsLineItemsStep));
        OnPropertyChanged(nameof(IsEnterCodeStep));
        OnPropertyChanged(nameof(IsPollingStep));
        OnPropertyChanged(nameof(IsSuccessStep));
        OnPropertyChanged(nameof(IsFailureStep));
        OnPropertyChanged(nameof(IsNonSuccessTerminalStep));
        OnPropertyChanged(nameof(ShowRefundFooter));
        OnPropertyChanged(nameof(BusyLabel));
        OnPropertyChanged(nameof(BusySubLabel));
        OnPropertyChanged(nameof(IsDetailsStep));
    }

    // ---------- step 1: payments + line items ----------
    public ObservableCollection<RefundablePaymentRow> Payments { get; } = new();
    public ObservableCollection<RefundableLineRow> LineRows { get; } = new();

    // ---------- read-only details (when invoice is fully refunded) ----------
    public ObservableCollection<RefundHistoryRow> RefundHistory { get; } = new();

    [ObservableProperty]
    private string _reason = string.Empty;

    /// <summary>Computed sum of selected line items, in original currency.</summary>
    [ObservableProperty]
    private decimal _refundTotal;

    /// <summary>Sum of selected payments' refundable amounts. The refund total cannot exceed this.</summary>
    [ObservableProperty]
    private decimal _selectedPaymentsRefundable;

    [ObservableProperty]
    private string? _lineItemsValidationMessage;

    public bool CanContinueFromLineItems => RefundTotal > 0
        && SelectedPaymentsRefundable > 0
        && RefundTotal <= SelectedPaymentsRefundable
        && string.IsNullOrEmpty(LineItemsValidationMessage);

    // ---------- step 2: code entry ----------
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string? _maskedEmail;

    /// <summary>
    /// Body text for Step 2's "code was sent to..." message. Falls back to a
    /// generic phrasing if the server didn't return a masked email (e.g. a
    /// race during account setup), so the user never sees a sentence with a
    /// dangling blank where the address should be.
    /// </summary>
    public string CodeSentMessage => string.IsNullOrWhiteSpace(MaskedEmail)
        ? "A 6-digit code was sent to your portal owner email. Enter it below to confirm the refund."
        : $"A 6-digit code was sent to {MaskedEmail}. Enter it below to confirm the refund.";

    partial void OnMaskedEmailChanged(string? value) => OnPropertyChanged(nameof(CodeSentMessage));

    [ObservableProperty]
    private int _codeExpiresInSeconds;

    [ObservableProperty]
    private string _countdownDisplay = string.Empty;

    [ObservableProperty]
    private int _attemptsRemaining = 5;

    public bool CanSubmitCode => Code.Length == 6 && Code.All(char.IsDigit) && !IsBusy;

    partial void OnCodeChanged(string value) => OnPropertyChanged(nameof(CanSubmitCode));
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSubmitCode));
        OnPropertyChanged(nameof(ShowRefundFooter));
    }

    // ---------- step 3: polling / cooling-off ----------
    [ObservableProperty]
    private string? _coolingOffMessage;

    // ---------- terminal states ----------
    [ObservableProperty]
    private string? _terminalMessage;

    public RefundModalViewModel(RefundService refundService, Invoice invoice, IEnumerable<Payment> invoicePayments, string customerName)
    {
        _refundService = refundService;
        _invoice = invoice;
        CustomerDisplay = customerName;
        _allPayments = invoicePayments.ToList();

        BuildPaymentRows();
        BuildLineRows();
        BuildRefundHistory();

        // No money left to refund but past refunds exist → open in read-only
        // details mode. If neither, fall through to LineItems and let it show
        // its own validation.
        if (Payments.Count == 0 && RefundHistory.Count > 0)
        {
            CurrentStep = Step.Details;
        }
    }

    private void BuildRefundHistory()
    {
        var refunds = _allPayments
            .Where(p => p.IsRefund)
            .OrderByDescending(p => p.Date);
        foreach (var r in refunds)
        {
            RefundHistory.Add(new RefundHistoryRow
            {
                Date = r.Date,
                Amount = Math.Abs(r.Amount),
                Reason = r.RefundReason,
                Currency = string.IsNullOrEmpty(r.OriginalCurrency) ? "USD" : r.OriginalCurrency,
            });
        }
    }

    private void BuildPaymentRows()
    {
        // Show only refundable portal payments (Source=Online, !IsRefund, has providerPaymentId).
        // For each, the "refundable" amount is the original amount minus any refunds already
        // associated with the same providerPaymentId (best-effort linkage via local payments
        // where IsRefund=true and notes/reference match).
        var portalPayments = _allPayments
            .Where(p => !p.IsRefund && p.Source == "Online" && !string.IsNullOrEmpty(p.ProviderPaymentId))
            .ToList();

        foreach (var p in portalPayments)
        {
            var alreadyRefunded = _allPayments
                .Where(r => r.IsRefund && r.RefundedFromPaymentId == p.Id)
                .Sum(r => Math.Abs(r.Amount));
            var refundable = Math.Max(0, p.Amount - alreadyRefunded);
            if (refundable <= 0) continue;

            var row = new RefundablePaymentRow
            {
                LocalPaymentId = p.Id,
                ProviderPaymentId = p.ProviderPaymentId!,
                Provider = p.PaymentMethod.ToString(),
                Date = p.Date,
                OriginalAmount = p.Amount,
                AlreadyRefunded = alreadyRefunded,
                Refundable = refundable,
                Currency = string.IsNullOrEmpty(p.OriginalCurrency) ? "USD" : p.OriginalCurrency,
                IsSelected = false,
            };
            row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RefundablePaymentRow.IsSelected)) RecomputeTotals(); };
            Payments.Add(row);
        }

        // Auto-select if there's exactly one refundable payment.
        if (Payments.Count == 1)
        {
            Payments[0].IsSelected = true;
        }
    }

    private void BuildLineRows()
    {
        // Line items from the invoice
        if (_invoice.LineItems != null)
        {
            foreach (var li in _invoice.LineItems)
            {
                var row = new RefundableLineRow
                {
                    Label = string.IsNullOrEmpty(li.Description) ? "(unnamed item)" : li.Description,
                    Detail = li.Quantity > 1 ? $"{li.Quantity} × {li.UnitPrice:C}" : "",
                    Amount = li.Amount,
                    IsSelected = true,
                    Kind = "lineItem",
                };
                row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RefundableLineRow.IsSelected)) RecomputeTotals(); };
                LineRows.Add(row);
            }
        }

        // Tax (treat as one toggleable row at face value, not recomputed)
        if (_invoice.TaxAmount > 0)
        {
            var row = new RefundableLineRow
            {
                Label = "Tax",
                Detail = "",
                Amount = _invoice.TaxAmount,
                IsSelected = true,
                Kind = "tax",
            };
            row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RefundableLineRow.IsSelected)) RecomputeTotals(); };
            LineRows.Add(row);
        }

        // Custom fee
        if (_invoice.CustomFeeAmount > 0)
        {
            var label = string.IsNullOrEmpty(_invoice.CustomFeeLabel) ? "Custom fee" : _invoice.CustomFeeLabel;
            var row = new RefundableLineRow
            {
                Label = label,
                Detail = "",
                Amount = _invoice.CustomFeeAmount,
                IsSelected = true,
                Kind = "fee",
            };
            row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RefundableLineRow.IsSelected)) RecomputeTotals(); };
            LineRows.Add(row);
        }

        // Security deposit
        if (_invoice.SecurityDeposit > 0)
        {
            var row = new RefundableLineRow
            {
                Label = "Security deposit",
                Detail = "",
                Amount = _invoice.SecurityDeposit,
                IsSelected = true,
                Kind = "deposit",
            };
            row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RefundableLineRow.IsSelected)) RecomputeTotals(); };
            LineRows.Add(row);
        }

        // Discount — added as a negative-amount row so the refund total
        // reflects what the customer actually paid (gross minus discount).
        // Without this, refunding all line items + tax + fee inflates the
        // total by the discount amount and trips the "exceeds refundable"
        // guard. Mirrors InvoiceHtmlRenderer.CalculateDiscount for the
        // percent-vs-fixed resolution.
        if (_invoice.DiscountAmount > 0)
        {
            var discountValue = _invoice.DiscountIsPercent
                ? _invoice.Subtotal * (_invoice.DiscountAmount / 100m)
                : _invoice.DiscountAmount;
            var label = _invoice.DiscountIsPercent
                ? $"Discount ({_invoice.DiscountAmount}%)"
                : "Discount";
            var row = new RefundableLineRow
            {
                Label = label,
                Detail = "",
                Amount = -discountValue,
                IsSelected = true,
                Kind = "discount",
            };
            row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RefundableLineRow.IsSelected)) RecomputeTotals(); };
            LineRows.Add(row);
        }

        RecomputeTotals();
    }

    private void RecomputeTotals()
    {
        RefundTotal = LineRows.Where(r => r.IsSelected).Sum(r => r.Amount);
        SelectedPaymentsRefundable = Payments.Where(p => p.IsSelected).Sum(p => p.Refundable);

        var selectedPaymentCount = Payments.Count(p => p.IsSelected);
        if (selectedPaymentCount == 0)
        {
            LineItemsValidationMessage = "Pick at least one payment to refund against.";
        }
        else if (selectedPaymentCount > 1)
        {
            // ContinueAsync only sends the FIRST selected payment to the
            // refund API — multi-payment refunds aren't wired through to
            // the server yet. Block selecting >1 here so the user gets a
            // clear error instead of a confusing AMOUNT_EXCEEDS_REFUNDABLE
            // from the server when the refund total exceeds the first
            // payment's individual refundable amount.
            LineItemsValidationMessage = "Refund only one payment at a time. Issue a separate refund for each.";
        }
        else if (RefundTotal <= 0)
        {
            LineItemsValidationMessage = "Pick at least one item to refund.";
        }
        else if (RefundTotal > SelectedPaymentsRefundable)
        {
            LineItemsValidationMessage = $"Refund total ({RefundTotal:C}) exceeds the selected payment's refundable amount ({SelectedPaymentsRefundable:C}).";
        }
        else
        {
            LineItemsValidationMessage = null;
        }
        OnPropertyChanged(nameof(CanContinueFromLineItems));
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (!CanContinueFromLineItems) return;

        var primaryPayment = Payments.FirstOrDefault(p => p.IsSelected);
        if (primaryPayment == null) return;

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var draft = new RefundDraft(
                InvoiceId: _invoice.Id,
                InvoiceNumber: _invoice.InvoiceNumber,
                CustomerName: CustomerDisplay,
                Provider: primaryPayment.Provider.ToLowerInvariant(),
                ProviderPaymentId: primaryPayment.ProviderPaymentId,
                AmountCents: (long)Math.Round(RefundTotal * 100, 0),
                Currency: Currency,
                LineItems: LineRows.Where(r => r.IsSelected).Select(r => (object)new {
                    label = r.Label,
                    amount = r.Amount,
                    kind = r.Kind,
                }).ToList(),
                Reason: string.IsNullOrWhiteSpace(Reason) ? null : Reason
            );
            var result = await _refundService.RequestRefundAsync(draft);
            if (!result.Ok)
            {
                // Build a diagnostic-friendly message: include HTTP status and
                // ErrorCode so it's clear when the failure is server-side
                // (e.g. 404 = endpoints not deployed yet, 401 = bad API key,
                // 412 = email not verified, etc.) vs the friendly server message.
                var bits = new List<string>();
                if (result.HttpStatus > 0) bits.Add($"HTTP {result.HttpStatus}");
                if (!string.IsNullOrEmpty(result.ErrorCode)) bits.Add(result.ErrorCode);
                if (!string.IsNullOrEmpty(result.Message)) bits.Add(result.Message);
                ErrorMessage = bits.Count > 0
                    ? string.Join(" — ", bits)
                    : "Refund request failed (no response).";
                return;
            }
            _requestId = result.RequestId;
            MaskedEmail = result.MaskedEmail;
            CodeExpiresInSeconds = result.ExpiresInSeconds;
            CurrentStep = Step.EnterCode;
            StartCountdown();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SubmitCodeAsync()
    {
        if (!CanSubmitCode) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _refundService.ConfirmCodeAsync(_requestId, Code);
            if (!result.Ok)
            {
                if (result.ErrorCode == "WRONG_CODE" && result.AttemptsRemaining is { } left)
                {
                    AttemptsRemaining = left;
                    ErrorMessage = $"Wrong code. {left} attempts remaining.";
                }
                else
                {
                    ErrorMessage = result.Message ?? result.ErrorCode ?? "Could not confirm refund.";
                    if (result.State == "failed")
                    {
                        TerminalMessage = result.Message;
                        CurrentStep = Step.Failure;
                    }
                }
                return;
            }

            switch (result.State)
            {
                case "completed":
                    TerminalMessage = "Refund issued. The customer will see the money returned to their original payment method in 5–10 business days.";
                    CurrentStep = Step.Success;
                    _ = App.AutoSyncPortalPaymentsAsync();
                    break;
                case "cooling_off":
                    // Null-coalesce because CoolingOffSeconds is int? — without
                    // it, a missing value prints as empty ("...for  minutes").
                    // Round-up so 899s reads as "15 minutes" instead of the
                    // truncated 14 that integer division would produce.
                    var coolingMinutes = (int)Math.Ceiling((result.CoolingOffSeconds ?? 0) / 60.0);
                    if (coolingMinutes < 1) coolingMinutes = 1;
                    CoolingOffMessage = $"This refund is held for review for {coolingMinutes} minute(s). You can cancel it from the email we just sent. Otherwise it will process automatically.";
                    CurrentStep = Step.Polling;
                    BeginPolling();
                    break;
                case "failed":
                    TerminalMessage = result.Message ?? "Refund failed.";
                    CurrentStep = Step.Failure;
                    break;
                default:
                    StatusMessage = $"Server returned unexpected state: {result.State}.";
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResendCodeAsync()
    {
        if (_requestId == 0) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _refundService.ResendCodeAsync(_requestId);
            if (!result.Ok)
            {
                ErrorMessage = result.Message ?? result.ErrorCode ?? "Could not resend code.";
                return;
            }
            CodeExpiresInSeconds = 600;
            StartCountdown();
            StatusMessage = "Code re-sent.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (_requestId != 0)
        {
            try { await _refundService.CancelAsync(_requestId); } catch { /* best-effort */ }
        }
        _requestId = 0;
        Code = string.Empty;
        ErrorMessage = null;
        StopCountdown();
        StopPolling();
        CurrentStep = Step.LineItems;
    }

    private void StartCountdown()
    {
        StopCountdown();
        UpdateCountdownDisplay();
        _countdownTimer = new System.Timers.Timer(1000) { AutoReset = true };
        _countdownTimer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (CodeExpiresInSeconds > 0) CodeExpiresInSeconds--;
                UpdateCountdownDisplay();
                if (CodeExpiresInSeconds == 0) StopCountdown();
            });
        };
        _countdownTimer.Start();
    }

    private void StopCountdown()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    private void UpdateCountdownDisplay()
    {
        var min = CodeExpiresInSeconds / 60;
        var sec = CodeExpiresInSeconds % 60;
        CountdownDisplay = CodeExpiresInSeconds > 0 ? $"Code expires in {min}:{sec:D2}" : "Code expired — request a new one.";
    }

    private void BeginPolling()
    {
        // Cancel any pre-existing polling loop before starting a new one.
        // Without this, a second BeginPolling() call (e.g. re-entering the
        // Polling step after a re-test) would leak the prior background
        // task and let it race-mutate CurrentStep alongside the new loop.
        StopPolling();
        _pollCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            var ct = _pollCts.Token;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    var status = await _refundService.GetStatusAsync(_requestId, ct);
                    if (!status.Ok) continue;
                    if (status.State == "completed")
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            TerminalMessage = "Refund completed.";
                            CurrentStep = Step.Success;
                        });
                        _ = App.AutoSyncPortalPaymentsAsync();
                        return;
                    }
                    if (status.State == "failed" || status.State == "cancelled")
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            TerminalMessage = status.StateReason ?? $"Refund {status.State}.";
                            CurrentStep = status.State == "cancelled" ? Step.Failure : Step.Failure;
                        });
                        return;
                    }
                }
            }
            catch (TaskCanceledException) { /* expected on close */ }
        });
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    public void Dispose()
    {
        StopCountdown();
        StopPolling();
    }
}

public partial class RefundablePaymentRow : ObservableObject
{
    public string LocalPaymentId { get; set; } = string.Empty;
    public string ProviderPaymentId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal AlreadyRefunded { get; set; }
    public decimal Refundable { get; set; }
    public string Currency { get; set; } = "USD";

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayLabel =>
        $"{Provider} · {Date:MMM d, yyyy} · {Refundable:C} refundable" +
        (AlreadyRefunded > 0 ? $" (of {OriginalAmount:C})" : "");
}

public partial class RefundableLineRow : ObservableObject
{
    public string Label { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Kind { get; set; } = "lineItem"; // lineItem | tax | fee | deposit

    [ObservableProperty]
    private bool _isSelected;
}

public class RefundHistoryRow
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string Currency { get; set; } = "USD";

    public string DateDisplay => Date.ToString("MMM d, yyyy");
    public string ReasonDisplay => string.IsNullOrWhiteSpace(Reason) ? "No reason given" : Reason!;
}
