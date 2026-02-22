using ArgoBooks.Localization;
using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for product modals, shared between ProductsPage and AppShell.
/// </summary>
public partial class ProductModalsViewModel : ObservableObject
{
    #region Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private bool _isEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalId = string.Empty;

    [ObservableProperty]
    private string _modalProductName = string.Empty;

    [ObservableProperty]
    private string _modalDescription = string.Empty;

    [ObservableProperty]
    private string _modalItemType = "Product";

    /// <summary>
    /// Gets whether a Product is selected (not Service) - used for showing threshold inputs.
    /// </summary>
    public bool IsProductSelected => ModalItemType == "Product";

    partial void OnModalItemTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsProductSelected));
    }

    [ObservableProperty]
    private CategoryOption? _modalCategory;

    [ObservableProperty]
    private string? _modalCategoryId;

    partial void OnModalCategoryIdChanged(string? value)
    {
        ModalCategory = value != null ? AvailableCategories.FirstOrDefault(c => c.Id == value) : null;
    }

    [ObservableProperty]
    private SupplierOption? _modalSupplier;

    [ObservableProperty]
    private string _modalReorderPoint = string.Empty;

    [ObservableProperty]
    private string _modalOverstockThreshold = string.Empty;

    [ObservableProperty]
    private string _modalUnitPrice = string.Empty;

    [ObservableProperty]
    private string _modalCostPrice = string.Empty;

    [ObservableProperty]
    private string _modalSku = string.Empty;

    [ObservableProperty]
    private string? _modalError;

    [ObservableProperty]
    private string? _modalProductNameError;

    [ObservableProperty]
    private string? _modalCategoryError;

    partial void OnModalProductNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            ModalProductNameError = null;
        }
    }

    partial void OnModalCategoryChanged(CategoryOption? value)
    {
        if (value != null)
        {
            ModalCategoryError = null;
        }
    }

    /// <summary>
    /// The product being edited (null for add).
    /// </summary>
    private Product? _editingProduct;

    /// <summary>
    /// Whether we're in expenses tab (purchase) or revenue tab (sales).
    /// Used to show the relevant price field (Cost Price for expenses, Unit Price for revenue).
    /// </summary>
    [ObservableProperty]
    private bool _isExpensesTab = true;

    // Original values for change detection in edit mode
    private string _originalProductName = string.Empty;
    private string _originalDescription = string.Empty;
    private string _originalItemType = "Product";
    private string? _originalCategoryId;
    private string? _originalSupplierId;
    private string _originalReorderPoint = string.Empty;
    private string _originalOverstockThreshold = string.Empty;
    private string _originalUnitPrice = string.Empty;
    private string _originalCostPrice = string.Empty;
    private string _originalSku = string.Empty;

    /// <summary>
    /// Returns true if any data has been entered in the Add modal.
    /// </summary>
    public bool HasAddModalEnteredData =>
        !string.IsNullOrWhiteSpace(ModalProductName) ||
        !string.IsNullOrWhiteSpace(ModalDescription) ||
        ModalCategory != null ||
        ModalSupplier != null ||
        !string.IsNullOrWhiteSpace(ModalReorderPoint) ||
        !string.IsNullOrWhiteSpace(ModalOverstockThreshold) ||
        !string.IsNullOrWhiteSpace(ModalUnitPrice) ||
        !string.IsNullOrWhiteSpace(ModalCostPrice) ||
        !string.IsNullOrWhiteSpace(ModalSku);

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal.
    /// </summary>
    public bool HasEditModalChanges =>
        ModalProductName != _originalProductName ||
        ModalDescription != _originalDescription ||
        ModalItemType != _originalItemType ||
        ModalCategory?.Id != _originalCategoryId ||
        ModalSupplier?.Id != _originalSupplierId ||
        ModalReorderPoint != _originalReorderPoint ||
        ModalOverstockThreshold != _originalOverstockThreshold ||
        ModalUnitPrice != _originalUnitPrice ||
        ModalCostPrice != _originalCostPrice ||
        ModalSku != _originalSku;

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterItemType = "All";

    [ObservableProperty]
    private string? _filterCategory;

    [ObservableProperty]
    private string? _filterSupplier;

    // Original filter values for change detection (captured when modal opens)
    private string _originalFilterItemType = "All";
    private string? _originalFilterCategory;
    private string? _originalFilterSupplier;

    /// <summary>
    /// Returns true if any filter has been changed from the state when the modal was opened.
    /// </summary>
    public bool HasFilterModalChanges =>
        FilterItemType != _originalFilterItemType ||
        FilterCategory != _originalFilterCategory ||
        FilterSupplier != _originalFilterSupplier;

    /// <summary>
    /// Captures the current filter state as original values for change detection.
    /// </summary>
    private void CaptureOriginalFilterValues()
    {
        _originalFilterItemType = FilterItemType;
        _originalFilterCategory = FilterCategory;
        _originalFilterSupplier = FilterSupplier;
    }

    #endregion

    #region Dropdown Options

    public ObservableCollection<CategoryOption> AvailableCategories { get; } = [];
    public ObservableCollection<CategoryItem> CategoryItems { get; } = [];
    public ObservableCollection<SupplierOption> AvailableSuppliers { get; } = [];
    public ObservableCollection<string> ItemTypes { get; } = ["Product", "Service"];
    public ObservableCollection<string> ItemTypeOptions { get; } = ["All", "Product", "Service"];

    public bool HasCategories => CategoryItems.Count > 0;

    #endregion

    #region Events

    public event EventHandler? ProductSaved;
    public event EventHandler? ProductDeleted;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;
    public event EventHandler? OpenCategoriesRequested;
    public event EventHandler? OpenSuppliersRequested;

    #endregion

    #region Add Product

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingProduct = null;
        ClearModalFields();
        UpdateDropdownOptions();
        IsAddModalOpen = true;
    }

    public void OpenAddModal(bool isExpensesTab)
    {
        IsExpensesTab = isExpensesTab;
        OpenAddModal();
    }

    [RelayCommand]
    public void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    /// <summary>
    /// Requests to close the Add modal, showing confirmation if data was entered.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseAddModalAsync()
    {
        if (HasAddModalEnteredData)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have entered data that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseAddModal();
    }

    [RelayCommand]
    public void OpenCategoriesWithAddModal()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        OpenCategoriesRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void OpenSuppliersWithAddModal()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        OpenSuppliersRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void SaveNewProduct()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        string newId;
        if (!string.IsNullOrWhiteSpace(ModalId))
        {
            newId = ModalId.Trim();
        }
        else
        {
            companyData.IdCounters.Product++;
            newId = $"PRD-{companyData.IdCounters.Product:D3}";
        }

        var reorderPoint = int.TryParse(ModalReorderPoint, out var rp) ? rp : 0;
        var overstockThreshold = int.TryParse(ModalOverstockThreshold, out var ot) ? ot : 0;

        var newProduct = new Product
        {
            Id = newId,
            Name = ModalProductName.Trim(),
            Description = string.IsNullOrWhiteSpace(ModalDescription) ? string.Empty : ModalDescription.Trim(),
            Sku = string.IsNullOrWhiteSpace(ModalSku) ? newId : ModalSku.Trim(),
            CategoryId = ModalCategory?.Id,
            SupplierId = ModalSupplier?.Id,
            UnitPrice = decimal.TryParse(ModalUnitPrice, out var unitPrice) ? unitPrice : 0,
            CostPrice = decimal.TryParse(ModalCostPrice, out var costPrice) ? costPrice : 0,
            TrackInventory = ModalItemType == "Product" && (reorderPoint > 0 || overstockThreshold > 0),
            ReorderPoint = reorderPoint,
            OverstockThreshold = overstockThreshold,
            Status = EntityStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.Products.Add(newProduct);
        companyData.MarkAsModified();

        var productToUndo = newProduct;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Add product '{newProduct.Name}'",
            () =>
            {
                companyData.Products.Remove(productToUndo);
                companyData.MarkAsModified();
                ProductSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Products.Add(productToUndo);
                companyData.MarkAsModified();
                ProductSaved?.Invoke(this, EventArgs.Empty);
            }));

        ProductSaved?.Invoke(this, EventArgs.Empty);

        // Mark the setup checklist item as complete
        TutorialService.Instance.CompleteChecklistItem(TutorialService.ChecklistItems.AddProduct);

        CloseAddModal();
    }

    #endregion

    #region Edit Product

    public void OpenEditModal(ProductDisplayItem? item)
    {
        if (item == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var product = companyData?.Products.FirstOrDefault(p => p.Id == item.Id);
        if (product == null)
            return;

        _editingProduct = product;
        UpdateDropdownOptions();

        ModalId = product.Id;
        ModalProductName = product.Name;
        ModalDescription = product.Description;
        ModalSku = product.Sku;
        ModalUnitPrice = product.UnitPrice.ToString("0.00");
        ModalCostPrice = product.CostPrice.ToString("0.00");

        if (companyData != null)
        {
            var category = companyData.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
            if (category != null)
            {
                ModalItemType = category.ItemType;
                ModalCategory = AvailableCategories.FirstOrDefault(c => c.Id == category.Id);
                ModalCategoryId = category.Id;
            }

            ModalSupplier = AvailableSuppliers.FirstOrDefault(s => s.Id == product.SupplierId);
        }

        ModalReorderPoint = product.ReorderPoint > 0 ? product.ReorderPoint.ToString() : string.Empty;
        ModalOverstockThreshold = product.OverstockThreshold > 0 ? product.OverstockThreshold.ToString() : string.Empty;

        // Store original values for change detection
        _originalProductName = ModalProductName;
        _originalDescription = ModalDescription;
        _originalItemType = ModalItemType;
        _originalCategoryId = ModalCategory?.Id;
        _originalSupplierId = ModalSupplier?.Id;
        _originalReorderPoint = ModalReorderPoint;
        _originalOverstockThreshold = ModalOverstockThreshold;
        _originalUnitPrice = ModalUnitPrice;
        _originalCostPrice = ModalCostPrice;
        _originalSku = ModalSku;

        ModalError = null;
        IsEditModalOpen = true;
    }

    public void OpenEditModal(ProductDisplayItem? item, bool isExpensesTab)
    {
        IsExpensesTab = isExpensesTab;
        OpenEditModal(item);
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingProduct = null;
        ClearModalFields();
    }

    /// <summary>
    /// Requests to close the Edit modal, showing confirmation if changes were made.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseEditModalAsync()
    {
        if (HasEditModalChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unsaved changes that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseEditModal();
    }

    [RelayCommand]
    public void SaveEditedProduct()
    {
        if (!ValidateModal() || _editingProduct == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var oldName = _editingProduct.Name;
        var oldDescription = _editingProduct.Description;
        var oldSku = _editingProduct.Sku;
        var oldCategoryId = _editingProduct.CategoryId;
        var oldSupplierId = _editingProduct.SupplierId;
        var oldUnitPrice = _editingProduct.UnitPrice;
        var oldCostPrice = _editingProduct.CostPrice;
        var oldTrackInventory = _editingProduct.TrackInventory;
        var oldReorderPoint = _editingProduct.ReorderPoint;
        var oldOverstockThreshold = _editingProduct.OverstockThreshold;

        var newName = ModalProductName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? string.Empty : ModalDescription.Trim();
        var newSku = string.IsNullOrWhiteSpace(ModalSku) ? _editingProduct.Id : ModalSku.Trim();
        var newCategoryId = ModalCategory?.Id;
        var newSupplierId = ModalSupplier?.Id;
        var newUnitPrice = decimal.TryParse(ModalUnitPrice, out var unitPrice) ? unitPrice : 0;
        var newCostPrice = decimal.TryParse(ModalCostPrice, out var costPrice) ? costPrice : 0;
        var newReorderPoint = int.TryParse(ModalReorderPoint, out var rp) ? rp : 0;
        var newOverstockThreshold = int.TryParse(ModalOverstockThreshold, out var ot) ? ot : 0;
        var newTrackInventory = ModalItemType == "Product" && (newReorderPoint > 0 || newOverstockThreshold > 0);

        // Check if anything actually changed
        var hasChanges = oldName != newName ||
                         oldDescription != newDescription ||
                         oldSku != newSku ||
                         oldCategoryId != newCategoryId ||
                         oldSupplierId != newSupplierId ||
                         oldUnitPrice != newUnitPrice ||
                         oldCostPrice != newCostPrice ||
                         oldTrackInventory != newTrackInventory ||
                         oldReorderPoint != newReorderPoint ||
                         oldOverstockThreshold != newOverstockThreshold;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

        var productToEdit = _editingProduct;
        App.EventLogService?.CapturePreModificationSnapshot("Product", productToEdit.Id);
        var changes = new Dictionary<string, FieldChange>();
        if (oldName != newName) changes["Name"] = new FieldChange { OldValue = oldName, NewValue = newName };
        if (oldDescription != newDescription) changes["Description"] = new FieldChange { OldValue = oldDescription ?? "", NewValue = newDescription ?? "" };
        if (oldSku != newSku) changes["SKU"] = new FieldChange { OldValue = oldSku ?? "", NewValue = newSku ?? "" };
        if (oldUnitPrice != newUnitPrice) changes["Unit Price"] = new FieldChange { OldValue = oldUnitPrice.ToString("F2"), NewValue = newUnitPrice.ToString("F2") };
        if (oldCostPrice != newCostPrice) changes["Cost Price"] = new FieldChange { OldValue = oldCostPrice.ToString("F2"), NewValue = newCostPrice.ToString("F2") };
        if (oldTrackInventory != newTrackInventory) changes["Track Inventory"] = new FieldChange { OldValue = oldTrackInventory.ToString(), NewValue = newTrackInventory.ToString() };
        if (oldReorderPoint != newReorderPoint) changes["Reorder Point"] = new FieldChange { OldValue = oldReorderPoint.ToString(), NewValue = newReorderPoint.ToString() };
        if (oldOverstockThreshold != newOverstockThreshold) changes["Overstock Threshold"] = new FieldChange { OldValue = oldOverstockThreshold.ToString(), NewValue = newOverstockThreshold.ToString() };
        if (changes.Count > 0) App.EventLogService?.SetPendingChanges(changes);
        productToEdit.Name = newName;
        productToEdit.Description = newDescription ?? string.Empty;
        productToEdit.Sku = newSku ?? string.Empty;
        productToEdit.CategoryId = newCategoryId;
        productToEdit.SupplierId = newSupplierId;
        productToEdit.UnitPrice = newUnitPrice;
        productToEdit.CostPrice = newCostPrice;
        productToEdit.TrackInventory = newTrackInventory;
        productToEdit.ReorderPoint = newReorderPoint;
        productToEdit.OverstockThreshold = newOverstockThreshold;
        productToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit product '{newName}'",
            () =>
            {
                productToEdit.Name = oldName;
                productToEdit.Description = oldDescription ?? string.Empty;
                productToEdit.Sku = oldSku ?? string.Empty;
                productToEdit.CategoryId = oldCategoryId;
                productToEdit.SupplierId = oldSupplierId;
                productToEdit.UnitPrice = oldUnitPrice;
                productToEdit.CostPrice = oldCostPrice;
                productToEdit.TrackInventory = oldTrackInventory;
                productToEdit.ReorderPoint = oldReorderPoint;
                productToEdit.OverstockThreshold = oldOverstockThreshold;
                companyData.MarkAsModified();
                ProductSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                productToEdit.Name = newName;
                productToEdit.Description = newDescription ?? string.Empty;
                productToEdit.Sku = newSku ?? string.Empty;
                productToEdit.CategoryId = newCategoryId;
                productToEdit.SupplierId = newSupplierId;
                productToEdit.UnitPrice = newUnitPrice;
                productToEdit.CostPrice = newCostPrice;
                productToEdit.TrackInventory = newTrackInventory;
                productToEdit.ReorderPoint = newReorderPoint;
                productToEdit.OverstockThreshold = newOverstockThreshold;
                companyData.MarkAsModified();
                ProductSaved?.Invoke(this, EventArgs.Empty);
            }));

        ProductSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Product

    public async void OpenDeleteConfirm(ProductDisplayItem? item)
    {
        if (item == null)
            return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null)
            return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Product".Translate(),
            Message = "Are you sure you want to delete this product?\n\n{0}".TranslateFormat(item.Name),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var product = companyData.Products.FirstOrDefault(p => p.Id == item.Id);
        if (product != null)
        {
            var deletedProduct = product;
            App.EventLogService?.CapturePreDeletionSnapshot("Product", deletedProduct.Id);
            companyData.Products.Remove(product);
            companyData.MarkAsModified();

            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Delete product '{deletedProduct.Name}'",
                () =>
                {
                    companyData.Products.Add(deletedProduct);
                    companyData.MarkAsModified();
                    ProductDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Products.Remove(deletedProduct);
                    companyData.MarkAsModified();
                    ProductDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        ProductDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Filter Modal

    [RelayCommand]
    public void OpenFilterModal()
    {
        UpdateDropdownOptions();
        CaptureOriginalFilterValues();
        IsFilterModalOpen = true;
    }

    public void OpenFilterModal(bool isExpensesTab)
    {
        IsExpensesTab = isExpensesTab;
        OpenFilterModal();
    }

    [RelayCommand]
    public void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Requests to close the Filter modal, showing confirmation if filter changes exist.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterModalChanges)
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
            }

            // Restore filter values to the state when modal was opened
            FilterItemType = _originalFilterItemType;
            FilterCategory = _originalFilterCategory;
            FilterSupplier = _originalFilterSupplier;
        }

        CloseFilterModal();
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    private void ResetFilterDefaults()
    {
        FilterItemType = "All";
        FilterCategory = null;
        FilterSupplier = null;
    }

    #endregion

    #region Modal Helpers

    private void UpdateDropdownOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        AvailableCategories.Clear();
        var targetType = IsExpensesTab ? CategoryType.Expense : CategoryType.Revenue;
        var categories = companyData.Categories
            .Where(c => c.Type == targetType)
            .OrderBy(c => c.Name);

        foreach (var cat in categories)
        {
            AvailableCategories.Add(new CategoryOption { Id = cat.Id, Name = cat.Name, ItemType = cat.ItemType });
        }

        CategoryItems.Clear();
        foreach (var cat in categories)
        {
            CategoryItems.Add(new CategoryItem { Id = cat.Id, Name = cat.Name });
        }
        OnPropertyChanged(nameof(HasCategories));

        AvailableSuppliers.Clear();
        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            AvailableSuppliers.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
        }
    }

    private void ClearModalFields()
    {
        ModalId = string.Empty;
        ModalProductName = string.Empty;
        ModalDescription = string.Empty;
        ModalItemType = "Product";
        ModalCategory = null;
        ModalCategoryId = null;
        ModalSupplier = null;
        ModalReorderPoint = string.Empty;
        ModalOverstockThreshold = string.Empty;
        ModalUnitPrice = string.Empty;
        ModalCostPrice = string.Empty;
        ModalSku = string.Empty;
        ModalError = null;
        ModalProductNameError = null;
        ModalCategoryError = null;
    }

    private bool ValidateModal()
    {
        ModalError = null;
        ModalProductNameError = null;
        ModalCategoryError = null;

        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalProductName))
        {
            ModalProductNameError = "Product name is required.".Translate();
            isValid = false;
        }
        else
        {
            var companyData = App.CompanyManager?.CompanyData;
            var existingWithSameName = companyData?.Products.Any(p =>
                p.Name.Equals(ModalProductName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingProduct == null || p.Id != _editingProduct.Id)) ?? false;

            if (existingWithSameName)
            {
                ModalProductNameError = "A product with this name already exists.".Translate();
                isValid = false;
            }
        }

        if (HasCategories && string.IsNullOrEmpty(ModalCategoryId))
        {
            ModalCategoryError = "Category is required.".Translate();
            isValid = false;
        }

        return isValid;
    }

    #endregion
}
