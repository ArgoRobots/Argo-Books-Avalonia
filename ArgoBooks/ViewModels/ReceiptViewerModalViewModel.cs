using ArgoBooks.Core.Enums;
using ArgoBooks.Localization;
using ArgoBooks.Services;
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
    private async Task Delete()
    {
        if (string.IsNullOrEmpty(ReceiptId)) return;

        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            if (companyData == null) return;

            var receipt = companyData.Receipts.FirstOrDefault(r => r.Id == ReceiptId);
            if (receipt == null) return;

            var dialog = App.ConfirmationDialog;
            if (dialog == null) return;

            // Check if receipt is linked to a transaction
            var isLinked = !string.IsNullOrEmpty(receipt.TransactionId);
            var message = "Are you sure you want to delete this receipt?".Translate();
            if (isLinked)
            {
                message += "\n\n" + "This receipt is linked to a {0} transaction ({1}). The receipt will be removed from the transaction.".TranslateFormat(
                    receipt.TransactionType, receipt.TransactionId);
            }

            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Delete Receipt".Translate(),
                Message = message,
                PrimaryButtonText = "Delete".Translate(),
                CancelButtonText = "Cancel".Translate(),
                IsPrimaryDestructive = true
            });

            if (result != ConfirmationResult.Primary) return;

            App.EventLogService?.CapturePreDeletionSnapshot("Receipt", receipt.Id);

            // Unlink from transaction if linked
            string? linkedTransactionId = null;
            string? linkedTransactionType = null;
            if (isLinked)
            {
                linkedTransactionId = receipt.TransactionId;
                linkedTransactionType = receipt.TransactionType;

                if (receipt.TransactionType == "Expense")
                {
                    var expense = companyData.Expenses.FirstOrDefault(e => e.Id == receipt.TransactionId);
                    if (expense != null) expense.ReceiptId = null;
                }
                else if (receipt.TransactionType == "Revenue")
                {
                    var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == receipt.TransactionId);
                    if (revenue != null) revenue.ReceiptId = null;
                }
            }

            companyData.Receipts.Remove(receipt);

            // Record undo/redo action
            var deletedReceipt = receipt;
            var action = new DelegateAction(
                $"Delete receipt {deletedReceipt.Id}",
                () =>
                {
                    companyData.Receipts.Add(deletedReceipt);
                    // Re-link transaction
                    if (linkedTransactionType == "Expense")
                    {
                        var expense = companyData.Expenses.FirstOrDefault(e => e.Id == linkedTransactionId);
                        if (expense != null) expense.ReceiptId = deletedReceipt.Id;
                    }
                    else if (linkedTransactionType == "Revenue")
                    {
                        var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == linkedTransactionId);
                        if (revenue != null) revenue.ReceiptId = deletedReceipt.Id;
                    }
                },
                () =>
                {
                    companyData.Receipts.Remove(deletedReceipt);
                    if (linkedTransactionType == "Expense")
                    {
                        var expense = companyData.Expenses.FirstOrDefault(e => e.Id == linkedTransactionId);
                        if (expense != null) expense.ReceiptId = null;
                    }
                    else if (linkedTransactionType == "Revenue")
                    {
                        var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == linkedTransactionId);
                        if (revenue != null) revenue.ReceiptId = null;
                    }
                });

            App.UndoRedoManager.RecordAction(action);
            App.CompanyManager?.MarkAsChanged();

            Close();
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Validation, "ReceiptViewer.Delete");
        }
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

                if (File.Exists(ReceiptPath))
                {
                    // Copy the file directly if it exists on disk
                    File.Copy(ReceiptPath, destinationPath, overwrite: true);
                    App.AddNotification("Success", "Receipt saved successfully", NotificationType.Success);
                }
                else
                {
                    // Fall back to in-memory FileData (base64) when temp file no longer exists
                    var receipt = App.CompanyManager?.CompanyData?.Receipts
                        .FirstOrDefault(r => r.Id == ReceiptId);
                    if (receipt?.FileData != null)
                    {
                        var bytes = Convert.FromBase64String(receipt.FileData);
                        await File.WriteAllBytesAsync(destinationPath, bytes);
                        App.AddNotification("Success", "Receipt saved successfully", NotificationType.Success);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await (App.ConfirmationDialog?.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Error",
                Message = $"Failed to save receipt: {ex.Message}",
                PrimaryButtonText = "OK",
                CancelButtonText = null
            }) ?? Task.CompletedTask);
        }
    }
}
