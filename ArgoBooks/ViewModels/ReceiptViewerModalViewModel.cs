using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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

    [RelayCommand]
    private async Task Download()
    {
        if (string.IsNullOrEmpty(ReceiptPath)) return;

        try
        {
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel?.StorageProvider == null) return;

            // Determine file extension from source
            var sourceExtension = Path.GetExtension(ReceiptPath);
            if (string.IsNullOrEmpty(sourceExtension))
                sourceExtension = ".png";

            var filters = new[]
            {
                new FilePickerFileType("Image files") { Patterns = [$"*{sourceExtension}"] }
            };

            var suggestedName = $"Receipt_{ReceiptId}{sourceExtension}";

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Receipt Image",
                SuggestedFileName = suggestedName,
                FileTypeChoices = filters,
                DefaultExtension = sourceExtension.TrimStart('.')
            });

            if (result != null)
            {
                var destinationPath = result.Path.LocalPath;

                // Copy the file
                if (File.Exists(ReceiptPath))
                {
                    File.Copy(ReceiptPath, destinationPath, overwrite: true);
                    App.AddNotification("Success", "Receipt saved successfully", NotificationType.Success);
                }
            }
        }
        catch (Exception ex)
        {
            App.AddNotification("Error", $"Failed to save receipt: {ex.Message}", NotificationType.Error);
        }
    }
}
