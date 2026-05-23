using System.Collections.ObjectModel;
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
    private string _receiptId = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private bool _isLoadingPages;

    /// <summary>Resolved page image paths for the current receipt (one entry per PDF page).</summary>
    public ObservableCollection<string> ReceiptPages { get; } = new();

    /// <summary>True when there is nothing to show and no render in progress.</summary>
    public bool HasNoPages => ReceiptPages.Count == 0 && !IsLoadingPages;

    // Guards against a stale async render finishing after a newer Show / Close.
    private int _renderToken;

    public ReceiptViewerModalViewModel()
    {
        ReceiptPages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoPages));
    }

    partial void OnIsLoadingPagesChanged(bool value) => OnPropertyChanged(nameof(HasNoPages));

    /// <summary>
    /// Shows the receipt viewer modal for the given receipt, resolving and rendering all of its
    /// pages asynchronously.
    /// </summary>
    /// <param name="receiptId">ID of the receipt to display.</param>
    /// <param name="title">Optional custom title.</param>
    public void Show(string receiptId, string? title = null)
    {
        ReceiptId = receiptId;
        Title = title ?? $"Receipt for {receiptId}";
        IsFullscreen = false;
        ReceiptPages.Clear();
        IsOpen = true;
        _ = LoadPagesAsync(receiptId);
    }

    private async Task LoadPagesAsync(string receiptId)
    {
        var token = ++_renderToken;
        var receipt = App.CompanyManager?.CompanyData?.Receipts.FirstOrDefault(r => r.Id == receiptId);
        if (receipt == null || string.IsNullOrEmpty(receipt.FileData))
            return;

        IsLoadingPages = true;
        try
        {
            var paths = await ReceiptPageRenderer.GetPagePathsAsync(receipt);
            if (token != _renderToken)
                return; // superseded by a newer Show / Close

            foreach (var p in paths)
            {
                if (token != _renderToken)
                    return;
                ReceiptPages.Add(p);
            }
        }
        finally
        {
            if (token == _renderToken)
                IsLoadingPages = false;
        }
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
        _renderToken++; // cancel any in-flight page render
        IsLoadingPages = false;
        ReceiptPages.Clear();
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
        if (string.IsNullOrEmpty(ReceiptId)) return;

        try
        {
            // Always download the original receipt file (e.g. the source PDF), not a rendered page.
            var receipt = App.CompanyManager?.CompanyData?.Receipts
                .FirstOrDefault(r => r.Id == ReceiptId);
            if (receipt?.FileData == null) return;

            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel?.StorageProvider == null) return;

            // Determine file extension from the original file name
            var sourceExtension = Path.GetExtension(receipt.FileName);
            if (string.IsNullOrEmpty(sourceExtension))
                sourceExtension = ".png";

            var filters = new[]
            {
                new FilePickerFileType("Receipt file") { Patterns = [$"*{sourceExtension}"] }
            };

            var suggestedName = $"Receipt_{ReceiptId}{sourceExtension}";

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Receipt",
                SuggestedFileName = suggestedName,
                FileTypeChoices = filters,
                DefaultExtension = sourceExtension.TrimStart('.')
            });

            if (result != null)
            {
                var destinationPath = result.Path.LocalPath;
                var bytes = Convert.FromBase64String(receipt.FileData);
                await File.WriteAllBytesAsync(destinationPath, bytes);
                App.AddNotification("Success", "Receipt saved successfully", NotificationType.Success);
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
