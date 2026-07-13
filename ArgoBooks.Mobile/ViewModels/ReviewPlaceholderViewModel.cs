using System;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// Minimal stand-in for the Task 4 review screen: shows just enough of the ReceiptScanResult
/// (supplier, date, total, line item count) to prove the capture -> AI scan flow works end to end.
/// Task 4 replaces this with the full editable review (per-line-item product picker, expense/
/// revenue toggle, "Add to my books"); nothing here builds a CapturedTransaction or pushes to sync.
/// </summary>
public partial class ReviewPlaceholderViewModel : ViewModelBase
{
    private readonly Action _onDone;

    public ReceiptScanResult Result { get; }

    public string SupplierText => string.IsNullOrWhiteSpace(Result.SupplierName) ? "(no supplier detected)" : Result.SupplierName!;

    public string DateText => Result.TransactionDate?.ToString("MMM d, yyyy") ?? "(no date detected)";

    public string TotalText => Result.TotalAmount is { } total ? total.ToString("C") : "(no total detected)";

    public string LineItemCountText => $"{Result.LineItems.Count} line item(s) detected";

    public ReviewPlaceholderViewModel(ReceiptScanResult result, Action onDone)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
        _onDone = onDone ?? throw new ArgumentNullException(nameof(onDone));
    }

    [RelayCommand]
    private void Done() => _onDone();
}
