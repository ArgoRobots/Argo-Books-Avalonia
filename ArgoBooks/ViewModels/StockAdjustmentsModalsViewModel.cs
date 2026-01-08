using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Stock Adjustments modals (Add Adjustment, View Details, Delete).
/// </summary>
public partial class StockAdjustmentsModalsViewModel : ViewModelBase
{
    #region Events

    /// <summary>
    /// Raised when a stock adjustment is saved.
    /// </summary>
    public event EventHandler? AdjustmentSaved;

    /// <summary>
    /// Raised when a stock adjustment is deleted.
    /// </summary>
    public event EventHandler? AdjustmentDeleted;

    #endregion

    #region Add Adjustment Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private InventoryItemDisplayOption? _selectedInventoryOption;

    /// <summary>
    /// Gets the selected inventory item from the option.
    /// </summary>
    public InventoryItem? SelectedInventoryItem => SelectedInventoryOption?.InventoryItem;

    [ObservableProperty]
    private string _adjustmentType = "Add";

    [ObservableProperty]
    private string _adjustmentQuantity = string.Empty;

    [ObservableProperty]
    private string _adjustmentReason = string.Empty;

    [ObservableProperty]
    private string _referenceNumber = string.Empty;

    [ObservableProperty]
    private string? _addModalError;

    [ObservableProperty]
    private bool _hasInventoryError;

    /// <summary>
    /// Adjustment type options for dropdown.
    /// </summary>
    public ObservableCollection<string> AdjustmentTypes { get; } = ["Add", "Remove", "Set"];

    /// <summary>
    /// Available inventory items for selection.
    /// </summary>
    public ObservableCollection<InventoryItemDisplayOption> AvailableInventoryItems { get; } = [];

    /// <summary>
    /// Calculated new stock level based on adjustment type and quantity.
    /// </summary>
    public string CalculatedNewStock
    {
        get
        {
            if (SelectedInventoryItem == null) return "0";
            if (!int.TryParse(AdjustmentQuantity, out var qty)) return SelectedInventoryItem.InStock.ToString();

            return AdjustmentType switch
            {
                "Add" => (SelectedInventoryItem.InStock + qty).ToString(),
                "Remove" => Math.Max(0, SelectedInventoryItem.InStock - qty).ToString(),
                "Set" => qty.ToString(),
                _ => SelectedInventoryItem.InStock.ToString()
            };
        }
    }

    /// <summary>
    /// Current stock of selected item.
    /// </summary>
    public int CurrentStock => SelectedInventoryItem?.InStock ?? 0;

    partial void OnAdjustmentQuantityChanged(string value) => OnPropertyChanged(nameof(CalculatedNewStock));
    partial void OnAdjustmentTypeChanged(string value) => OnPropertyChanged(nameof(CalculatedNewStock));
    partial void OnSelectedInventoryOptionChanged(InventoryItemDisplayOption? value)
    {
        OnPropertyChanged(nameof(SelectedInventoryItem));
        OnPropertyChanged(nameof(CurrentStock));
        OnPropertyChanged(nameof(CalculatedNewStock));
    }

    #endregion

    #region View Modal State

    [ObservableProperty]
    private bool _isViewModalOpen;

    [ObservableProperty]
    private StockAdjustmentDisplayItem? _viewingAdjustment;

    #endregion

    #region Delete Confirmation State

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private StockAdjustmentDisplayItem? _deletingAdjustment;

    #endregion

    #region Add Modal Commands

    /// <summary>
    /// Opens the add adjustment modal.
    /// </summary>
    [RelayCommand]
    public void OpenAddModal()
    {
        LoadInventoryItems();
        ClearAddModalFields();
        IsAddModalOpen = true;
    }

    /// <summary>
    /// Closes the add adjustment modal.
    /// </summary>
    [RelayCommand]
    private void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearAddModalFields();
    }

    /// <summary>
    /// Navigates to Stock Levels page and opens the create inventory item modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateInventoryItem()
    {
        IsAddModalOpen = false;
        App.NavigationService?.NavigateTo("StockLevels", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Saves the new stock adjustment.
    /// </summary>
    [RelayCommand]
    private void SaveAdjustment()
    {
        AddModalError = null;
        HasInventoryError = false;

        // Validate
        if (SelectedInventoryItem == null)
        {
            HasInventoryError = true;
            return;
        }

        if (!int.TryParse(AdjustmentQuantity, out var quantity) || quantity < 0)
        {
            AddModalError = "Please enter a valid quantity.";
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var inventoryItem = companyData.Inventory?.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (inventoryItem == null) return;

        // Store old values for undo
        var oldInStock = inventoryItem.InStock;
        var oldStatus = inventoryItem.Status;

        // Calculate new stock
        var adjustmentTypeEnum = AdjustmentType switch
        {
            "Add" => Core.Enums.AdjustmentType.Add,
            "Remove" => Core.Enums.AdjustmentType.Remove,
            "Set" => Core.Enums.AdjustmentType.Set,
            _ => Core.Enums.AdjustmentType.Add
        };

        var newStock = adjustmentTypeEnum switch
        {
            Core.Enums.AdjustmentType.Add => inventoryItem.InStock + quantity,
            Core.Enums.AdjustmentType.Remove => Math.Max(0, inventoryItem.InStock - quantity),
            Core.Enums.AdjustmentType.Set => quantity,
            _ => inventoryItem.InStock
        };

        // Apply the change
        inventoryItem.InStock = newStock;
        inventoryItem.Status = inventoryItem.CalculateStatus();
        inventoryItem.LastUpdated = DateTime.UtcNow;

        // Create stock adjustment record
        companyData.IdCounters.StockAdjustment++;
        var adjustmentRecord = new StockAdjustment
        {
            Id = $"ADJ-{companyData.IdCounters.StockAdjustment:D5}",
            InventoryItemId = inventoryItem.Id,
            AdjustmentType = adjustmentTypeEnum,
            Quantity = quantity,
            PreviousStock = oldInStock,
            NewStock = newStock,
            Reason = AdjustmentReason,
            ReferenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
            Timestamp = DateTime.UtcNow
        };

        companyData.StockAdjustments.Add(adjustmentRecord);
        companyData.MarkAsModified();

        // Get product name for undo description
        var product = companyData.Products?.FirstOrDefault(p => p.Id == inventoryItem.ProductId);
        var productName = product?.Name ?? "Unknown Product";

        // Record undo action
        var itemToUndo = inventoryItem;
        var adjustmentToUndo = adjustmentRecord;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Adjust stock for '{productName}'",
            () =>
            {
                itemToUndo.InStock = oldInStock;
                itemToUndo.Status = oldStatus;
                companyData.StockAdjustments?.Remove(adjustmentToUndo);
                companyData.MarkAsModified();
                AdjustmentSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                itemToUndo.InStock = newStock;
                itemToUndo.Status = itemToUndo.CalculateStatus();
                companyData.StockAdjustments?.Add(adjustmentToUndo);
                companyData.MarkAsModified();
                AdjustmentSaved?.Invoke(this, EventArgs.Empty);
            }));

        // Notify and close
        AdjustmentSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    private void LoadInventoryItems()
    {
        AvailableInventoryItems.Clear();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var inventory = companyData.Inventory ?? [];
        var products = companyData.Products ?? [];
        var locations = companyData.Locations ?? [];

        foreach (var item in inventory)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            var location = locations.FirstOrDefault(l => l.Id == item.LocationId);

            AvailableInventoryItems.Add(new InventoryItemDisplayOption
            {
                InventoryItem = item,
                DisplayText = $"{product?.Name ?? "Unknown"} @ {location?.Name ?? "Unknown"}",
                ProductName = product?.Name ?? "Unknown",
                LocationName = location?.Name ?? "Unknown",
                CurrentStock = item.InStock
            });
        }
    }

    private void ClearAddModalFields()
    {
        SelectedInventoryOption = null;
        AdjustmentType = "Add";
        AdjustmentQuantity = string.Empty;
        AdjustmentReason = string.Empty;
        ReferenceNumber = string.Empty;
        AddModalError = null;
        HasInventoryError = false;
    }

    #endregion

    #region View Modal Commands

    /// <summary>
    /// Opens the view details modal.
    /// </summary>
    public void OpenViewModal(StockAdjustmentDisplayItem item)
    {
        ViewingAdjustment = item;
        IsViewModalOpen = true;
    }

    /// <summary>
    /// Closes the view details modal.
    /// </summary>
    [RelayCommand]
    private void CloseViewModal()
    {
        IsViewModalOpen = false;
        ViewingAdjustment = null;
    }

    #endregion

    #region Delete Confirmation Commands

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    public async void OpenDeleteConfirm(StockAdjustmentDisplayItem item)
    {
        if (item == null) return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Stock Adjustment",
            Message = $"Are you sure you want to delete this stock adjustment?\n\nProduct: {item.ProductName}\nQuantity: {item.Quantity}",
            PrimaryButtonText = "Delete",
            CancelButtonText = "Cancel",
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.StockAdjustments == null) return;

        var adjustment = companyData.StockAdjustments.FirstOrDefault(a => a.Id == item.Id);
        if (adjustment == null) return;

        // Find the inventory item and reverse the adjustment
        var inventoryItem = companyData.Inventory?.FirstOrDefault(i => i.Id == adjustment.InventoryItemId);

        // Store values for undo
        var deletedAdjustment = adjustment;
        var oldInventoryStock = inventoryItem?.InStock;
        var oldInventoryStatus = inventoryItem?.Status;

        // Reverse the adjustment on inventory if item still exists
        if (inventoryItem != null)
        {
            inventoryItem.InStock = adjustment.PreviousStock;
            inventoryItem.Status = inventoryItem.CalculateStatus();
            inventoryItem.LastUpdated = DateTime.UtcNow;
        }

        // Remove the adjustment record
        companyData.StockAdjustments.Remove(adjustment);
        companyData.MarkAsModified();

        // Record undo action
        var adjustmentProductName = item.ProductName;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Delete adjustment for '{adjustmentProductName}'",
            () =>
            {
                // Undo: restore the adjustment
                companyData.StockAdjustments.Add(deletedAdjustment);
                if (inventoryItem != null && oldInventoryStock.HasValue)
                {
                    inventoryItem.InStock = oldInventoryStock.Value;
                    inventoryItem.Status = oldInventoryStatus ?? inventoryItem.CalculateStatus();
                }
                companyData.MarkAsModified();
                AdjustmentDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                // Redo: delete again
                companyData.StockAdjustments.Remove(deletedAdjustment);
                if (inventoryItem != null)
                {
                    inventoryItem.InStock = deletedAdjustment.PreviousStock;
                    inventoryItem.Status = inventoryItem.CalculateStatus();
                }
                companyData.MarkAsModified();
                AdjustmentDeleted?.Invoke(this, EventArgs.Empty);
            }));

        // Notify
        AdjustmentDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Filter Modal State

    /// <summary>
    /// Raised when filters are applied.
    /// </summary>
    public event EventHandler<AdjustmentsFilterAppliedEventArgs>? FiltersApplied;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private DateTimeOffset? _filterStartDate;

    [ObservableProperty]
    private DateTimeOffset? _filterEndDate;

    [ObservableProperty]
    private string _filterProduct = "All";

    [ObservableProperty]
    private string _filterType = "All";

    /// <summary>
    /// Available products for filter dropdown.
    /// </summary>
    public ObservableCollection<string> FilterProducts { get; } = ["All"];

    /// <summary>
    /// Adjustment type options for filter dropdown.
    /// </summary>
    public ObservableCollection<string> FilterTypeOptions { get; } = ["All", "Add", "Remove", "Set"];

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal(IEnumerable<string> products,
        DateTimeOffset? startDate, DateTimeOffset? endDate,
        string currentProduct, string currentType)
    {
        FilterProducts.Clear();
        FilterProducts.Add("All");
        foreach (var prod in products.Where(p => p != "All"))
            FilterProducts.Add(prod);

        FilterStartDate = startDate;
        FilterEndDate = endDate;
        FilterProduct = currentProduct;
        FilterType = currentType;
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
    /// Applies the current filters.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, new AdjustmentsFilterAppliedEventArgs(
            FilterStartDate, FilterEndDate, FilterProduct, FilterType));
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterStartDate = null;
        FilterEndDate = null;
        FilterProduct = "All";
        FilterType = "All";
    }

    #endregion
}

/// <summary>
/// Event args for filter applied events.
/// </summary>
public class AdjustmentsFilterAppliedEventArgs : EventArgs
{
    public DateTimeOffset? StartDate { get; }
    public DateTimeOffset? EndDate { get; }
    public string Product { get; }
    public string Type { get; }

    public AdjustmentsFilterAppliedEventArgs(DateTimeOffset? startDate, DateTimeOffset? endDate, string product, string type)
    {
        StartDate = startDate;
        EndDate = endDate;
        Product = product;
        Type = type;
    }
}

/// <summary>
/// Display option for inventory items in the dropdown.
/// </summary>
public class InventoryItemDisplayOption
{
    public InventoryItem? InventoryItem { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }

    public override string ToString() => DisplayText;
}
