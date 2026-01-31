using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Returns modals (Filter, View Details, Undo).
/// </summary>
public partial class ReturnsModalsViewModel : ViewModelBase
{
    #region Events

    /// <summary>
    /// Raised when filters are applied.
    /// </summary>
    public event EventHandler? FiltersApplied;

    /// <summary>
    /// Raised when filters are cleared.
    /// </summary>
    public event EventHandler? FiltersCleared;

    /// <summary>
    /// Raised when a return is undone.
    /// </summary>
    public event EventHandler? ReturnUndone;

    #endregion

    #region Filter Modal State

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string _filterReason = "All";

    /// <summary>
    /// Status filter options.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Pending", "Approved", "Rejected", "Refunded"];

    /// <summary>
    /// Reason filter options.
    /// </summary>
    public ObservableCollection<string> ReasonOptions { get; } = ["All", "Defective", "Wrong Item", "Not as Described", "Changed Mind", "Other"];

    #endregion

    #region Filter Modal Commands

    /// <summary>
    /// Gets whether any filter values differ from their defaults.
    /// </summary>
    public bool HasFilterChanges =>
        FilterDateFrom != null ||
        FilterDateTo != null ||
        FilterStatus != "All" ||
        FilterReason != "All";

    /// <summary>
    /// Resets all filter values to their defaults.
    /// </summary>
    private void ResetFilterDefaults()
    {
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterStatus = "All";
        FilterReason = "All";
    }

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal()
    {
        IsFilterModalOpen = true;
    }

    /// <summary>
    /// Closes the filter modal.
    /// </summary>
    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Requests to close the filter modal, showing confirmation if there are unapplied changes.
    /// </summary>
    [RelayCommand]
    private async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unapplied filter changes. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;

                ResetFilterDefaults();
            }
        }

        CloseFilterModal();
    }

    /// <summary>
    /// Applies the current filters.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region View Details Modal State

    [ObservableProperty]
    private bool _isViewDetailsModalOpen;

    [ObservableProperty]
    private string _viewDetailsId = string.Empty;

    [ObservableProperty]
    private string _viewDetailsProduct = string.Empty;

    [ObservableProperty]
    private string _viewDetailsDate = string.Empty;

    [ObservableProperty]
    private string _viewDetailsRefund = string.Empty;

    [ObservableProperty]
    private string _viewDetailsReason = string.Empty;

    [ObservableProperty]
    private string _viewDetailsNotes = string.Empty;

    /// <summary>
    /// Opens the view details modal with the specified return details.
    /// </summary>
    public void OpenViewDetailsModal(string id, string product, string date, string refund, string reason, string notes)
    {
        ViewDetailsId = id;
        ViewDetailsProduct = product;
        ViewDetailsDate = date;
        ViewDetailsRefund = refund;
        ViewDetailsReason = reason;
        ViewDetailsNotes = string.IsNullOrWhiteSpace(notes) ? "No notes provided" : notes;
        IsViewDetailsModalOpen = true;
    }

    /// <summary>
    /// Closes the view details modal.
    /// </summary>
    [RelayCommand]
    private void CloseViewDetailsModal()
    {
        IsViewDetailsModalOpen = false;
    }

    #endregion

    #region Undo Return Modal State

    private Return? _undoReturn;

    [ObservableProperty]
    private bool _isUndoReturnModalOpen;

    [ObservableProperty]
    private string _undoReturnItemDescription = string.Empty;

    [ObservableProperty]
    private string _undoReturnReason = string.Empty;

    /// <summary>
    /// Opens the undo return modal for the specified return.
    /// </summary>
    public void OpenUndoReturnModal(Return returnItem, string description)
    {
        _undoReturn = returnItem;
        UndoReturnItemDescription = description;
        UndoReturnReason = string.Empty;
        IsUndoReturnModalOpen = true;
    }

    /// <summary>
    /// Closes the undo return modal.
    /// </summary>
    [RelayCommand]
    private void CloseUndoReturnModal()
    {
        IsUndoReturnModalOpen = false;
        _undoReturn = null;
    }

    /// <summary>
    /// Confirms the undo return action.
    /// </summary>
    [RelayCommand]
    private void ConfirmUndoReturn()
    {
        if (_undoReturn == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Remove the return record
        companyData.Returns.Remove(_undoReturn);

        // Restore the original transaction quantity
        if (_undoReturn.Type == ReturnType.Expense)
        {
            var expense = companyData.Expenses.FirstOrDefault(e => e.Id == _undoReturn.OriginalTransactionId);
            if (expense != null)
            {
                foreach (var returnedItem in _undoReturn.Items)
                {
                    var expenseItem = expense.Items.FirstOrDefault(i => i.ProductId == returnedItem.ProductId);
                    if (expenseItem != null)
                    {
                        expenseItem.Quantity += returnedItem.Quantity;
                    }
                }
            }
        }
        else if (_undoReturn.Type == ReturnType.Customer)
        {
            var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == _undoReturn.OriginalTransactionId);
            if (revenue != null)
            {
                foreach (var returnedItem in _undoReturn.Items)
                {
                    var revenueItem = revenue.Items.FirstOrDefault(i => i.ProductId == returnedItem.ProductId);
                    if (revenueItem != null)
                    {
                        revenueItem.Quantity += returnedItem.Quantity;
                    }
                }
            }
        }

        App.CompanyManager?.MarkDataAsModified();
        CloseUndoReturnModal();
        ReturnUndone?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
