using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the receipt viewer modal.
/// </summary>
public partial class ReceiptViewerModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _receiptPath = string.Empty;

    [ObservableProperty]
    private string _receiptId = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isFullscreen;

    /// <summary>
    /// Shows the receipt viewer modal with the specified receipt.
    /// </summary>
    /// <param name="receiptPath">Path to the receipt image.</param>
    /// <param name="receiptId">ID of the receipt for display.</param>
    /// <param name="title">Optional custom title.</param>
    public void Show(string receiptPath, string receiptId, string? title = null)
    {
        ReceiptPath = receiptPath;
        ReceiptId = receiptId;
        Title = title ?? $"Receipt for {receiptId}";
        IsFullscreen = false;
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        IsFullscreen = false;
        ReceiptPath = string.Empty;
        ReceiptId = string.Empty;
        Title = string.Empty;
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }
}
