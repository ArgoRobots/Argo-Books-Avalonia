using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Stock Levels modals (Add Item, Adjust Stock).
/// </summary>
public partial class StockLevelsModalsViewModel : ViewModelBase
{
    #region Events

    /// <summary>
    /// Raised when an inventory item is saved (added or adjusted).
    /// </summary>
    public event EventHandler? ItemSaved;

    #endregion

    #region Adjust Stock Modal State

    [ObservableProperty]
    private bool _isAdjustStockModalOpen;

    [ObservableProperty]
    private string? _selectedItemId;

    [ObservableProperty]
    private string _selectedItemProductName = string.Empty;

    [ObservableProperty]
    private int _currentStock;

    [ObservableProperty]
    private string _adjustmentQuantity = string.Empty;

    [ObservableProperty]
    private string _adjustmentType = "Add";

    [ObservableProperty]
    private string _adjustmentReason = string.Empty;

    [ObservableProperty]
    private string? _adjustmentError;

    /// <summary>
    /// Adjustment type options for dropdown.
    /// </summary>
    public ObservableCollection<string> AdjustmentTypes { get; } = ["Add", "Remove", "Set"];

    /// <summary>
    /// Calculated new stock level based on adjustment type and quantity.
    /// </summary>
    public string CalculatedNewStock
    {
        get
        {
            if (!int.TryParse(AdjustmentQuantity, out var qty))
                return CurrentStock.ToString();

            return AdjustmentType switch
            {
                "Add" => (CurrentStock + qty).ToString(),
                "Remove" => Math.Max(0, CurrentStock - qty).ToString(),
                "Set" => qty.ToString(),
                _ => CurrentStock.ToString()
            };
        }
    }

    partial void OnAdjustmentQuantityChanged(string value)
    {
        OnPropertyChanged(nameof(CalculatedNewStock));
    }

    partial void OnAdjustmentTypeChanged(string value)
    {
        OnPropertyChanged(nameof(CalculatedNewStock));
    }

    #endregion

    #region Add Item Modal State

    [ObservableProperty]
    private bool _isAddItemModalOpen;

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private Location? _selectedLocation;

    [ObservableProperty]
    private string _addItemSku = string.Empty;

    [ObservableProperty]
    private string _addItemQuantity = string.Empty;

    [ObservableProperty]
    private string _addItemReorderPoint = "10";

    [ObservableProperty]
    private string _addItemOverstockThreshold = "100";

    [ObservableProperty]
    private string? _addItemError;

    [ObservableProperty]
    private string? _addItemProductError;

    [ObservableProperty]
    private bool _hasLocationError;

    /// <summary>
    /// Available products for Add Item modal.
    /// </summary>
    public ObservableCollection<Product> AvailableProducts { get; } = [];

    /// <summary>
    /// Available locations for Add Item modal.
    /// </summary>
    public ObservableCollection<Location> AvailableLocations { get; } = [];

    #endregion

    #region Adjust Stock Commands

    /// <summary>
    /// Opens the adjust stock modal for an item.
    /// </summary>
    public void OpenAdjustStockModal(string itemId, string productName, int currentStock)
    {
        SelectedItemId = itemId;
        SelectedItemProductName = productName;
        CurrentStock = currentStock;
        AdjustmentQuantity = string.Empty;
        AdjustmentType = "Add";
        AdjustmentReason = string.Empty;
        AdjustmentError = null;
        IsAdjustStockModalOpen = true;
    }

    /// <summary>
    /// Closes the adjust stock modal.
    /// </summary>
    [RelayCommand]
    private void CloseAdjustStockModal()
    {
        IsAdjustStockModalOpen = false;
        ClearAdjustmentFields();
    }

    /// <summary>
    /// Saves the stock adjustment.
    /// </summary>
    [RelayCommand]
    private void SaveAdjustment()
    {
        if (string.IsNullOrEmpty(SelectedItemId)) return;

        AdjustmentError = null;

        // Validate quantity
        if (!int.TryParse(AdjustmentQuantity, out var quantity) || quantity < 0)
        {
            AdjustmentError = "Please enter a valid quantity.";
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var inventoryItem = companyData.Inventory?.FirstOrDefault(i => i.Id == SelectedItemId);
        if (inventoryItem == null) return;

        // Store old values for undo
        var oldInStock = inventoryItem.InStock;
        var oldStatus = inventoryItem.Status;

        // Calculate new stock
        var newStock = AdjustmentType switch
        {
            "Add" => inventoryItem.InStock + quantity,
            "Remove" => Math.Max(0, inventoryItem.InStock - quantity),
            "Set" => quantity,
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
            AdjustmentType = AdjustmentType switch
            {
                "Add" => Core.Enums.AdjustmentType.Add,
                "Remove" => Core.Enums.AdjustmentType.Remove,
                "Set" => Core.Enums.AdjustmentType.Set,
                _ => Core.Enums.AdjustmentType.Add
            },
            Quantity = quantity,
            PreviousStock = oldInStock,
            NewStock = newStock,
            Reason = AdjustmentReason,
            Timestamp = DateTime.UtcNow
        };

        companyData.StockAdjustments.Add(adjustmentRecord);
        companyData.MarkAsModified();

        // Record undo action
        var itemToUndo = inventoryItem;
        var adjustmentToUndo = adjustmentRecord;
        var productName = SelectedItemProductName;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Adjust stock for '{productName}'",
            () =>
            {
                itemToUndo.InStock = oldInStock;
                itemToUndo.Status = oldStatus;
                companyData.StockAdjustments?.Remove(adjustmentToUndo);
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                itemToUndo.InStock = newStock;
                itemToUndo.Status = itemToUndo.CalculateStatus();
                companyData.StockAdjustments?.Add(adjustmentToUndo);
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            }));

        // Notify and close
        ItemSaved?.Invoke(this, EventArgs.Empty);
        CloseAdjustStockModal();
    }

    private void ClearAdjustmentFields()
    {
        SelectedItemId = null;
        SelectedItemProductName = string.Empty;
        CurrentStock = 0;
        AdjustmentQuantity = string.Empty;
        AdjustmentType = "Add";
        AdjustmentReason = string.Empty;
        AdjustmentError = null;
    }

    #endregion

    #region Add Item Commands

    /// <summary>
    /// Navigates to Locations page and opens the create location modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateLocation()
    {
        // Close this modal
        IsAddItemModalOpen = false;

        // Navigate to Locations page with openAddModal parameter
        App.NavigationService?.NavigateTo("Locations", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Opens the add item modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddItemModal()
    {
        // Load available products and locations
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        AvailableProducts.Clear();
        foreach (var product in companyData.Products?.Where(p => p.TrackInventory) ?? [])
        {
            AvailableProducts.Add(product);
        }

        AvailableLocations.Clear();
        foreach (var location in companyData.Locations ?? [])
        {
            AvailableLocations.Add(location);
        }

        // Set defaults
        SelectedProduct = AvailableProducts.FirstOrDefault();
        SelectedLocation = AvailableLocations.FirstOrDefault();
        AddItemSku = string.Empty;
        AddItemQuantity = "0";
        AddItemReorderPoint = "10";
        AddItemOverstockThreshold = "100";
        AddItemError = null;
        AddItemProductError = null;
        HasLocationError = false;

        IsAddItemModalOpen = true;
    }

    /// <summary>
    /// Closes the add item modal.
    /// </summary>
    [RelayCommand]
    private void CloseAddItemModal()
    {
        IsAddItemModalOpen = false;
        ClearAddItemFields();
    }

    /// <summary>
    /// Saves a new inventory item.
    /// </summary>
    [RelayCommand]
    private void SaveNewItem()
    {
        AddItemError = null;
        AddItemProductError = null;
        HasLocationError = false;

        // Validate
        if (SelectedProduct == null)
        {
            AddItemProductError = "Please select a product.";
            return;
        }

        if (SelectedLocation == null)
        {
            HasLocationError = true;
            return;
        }

        if (!int.TryParse(AddItemQuantity, out var quantity) || quantity < 0)
        {
            AddItemError = "Please enter a valid quantity.";
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Check if item already exists for this product/location
        var existingItem = companyData.Inventory?.FirstOrDefault(i =>
            i.ProductId == SelectedProduct.Id && i.LocationId == SelectedLocation.Id);

        if (existingItem != null)
        {
            AddItemError = "An inventory item already exists for this product and location.";
            return;
        }

        // Generate new ID
        companyData.IdCounters.InventoryItem++;
        var newId = $"INV-ITM-{companyData.IdCounters.InventoryItem:D5}";

        // Parse thresholds
        int.TryParse(AddItemReorderPoint, out var reorderPoint);
        int.TryParse(AddItemOverstockThreshold, out var overstockThreshold);

        var newItem = new InventoryItem
        {
            Id = newId,
            ProductId = SelectedProduct.Id,
            Sku = string.IsNullOrWhiteSpace(AddItemSku) ? SelectedProduct.Sku : AddItemSku.Trim(),
            LocationId = SelectedLocation.Id,
            InStock = quantity,
            Reserved = 0,
            ReorderPoint = reorderPoint,
            OverstockThreshold = overstockThreshold,
            UnitCost = SelectedProduct.CostPrice,
            LastUpdated = DateTime.UtcNow
        };
        newItem.Status = newItem.CalculateStatus();

        companyData.Inventory?.Add(newItem);
        companyData.MarkAsModified();

        // Record undo action
        var itemToUndo = newItem;
        var productName = SelectedProduct.Name;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Add inventory item for '{productName}'",
            () =>
            {
                companyData.Inventory?.Remove(itemToUndo);
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Inventory?.Add(itemToUndo);
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            }));

        // Notify and close
        ItemSaved?.Invoke(this, EventArgs.Empty);
        CloseAddItemModal();
    }

    private void ClearAddItemFields()
    {
        SelectedProduct = null;
        SelectedLocation = null;
        AddItemSku = string.Empty;
        AddItemQuantity = string.Empty;
        AddItemReorderPoint = "10";
        AddItemOverstockThreshold = "100";
        AddItemError = null;
        AddItemProductError = null;
        HasLocationError = false;
    }

    #endregion
}
