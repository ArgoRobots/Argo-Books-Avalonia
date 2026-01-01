using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
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
        if (value != null)
        {
            ModalCategory = AvailableCategories.FirstOrDefault(c => c.Id == value);
        }
        else
        {
            ModalCategory = null;
        }
    }

    [ObservableProperty]
    private SupplierOption? _modalSupplier;

    [ObservableProperty]
    private string _modalCountryOfOrigin = string.Empty;

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

    /// <summary>
    /// The product being edited (null for add).
    /// </summary>
    private Product? _editingProduct;

    /// <summary>
    /// The product being deleted.
    /// </summary>
    private ProductDisplayItem? _deletingProduct;

    /// <summary>
    /// Whether we're in expenses tab (purchase) or revenue tab (sales).
    /// </summary>
    private bool _isExpensesTab = true;

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterItemType = "All";

    [ObservableProperty]
    private string? _filterCategory;

    [ObservableProperty]
    private string? _filterSupplier;

    [ObservableProperty]
    private string? _filterCountry;

    #endregion

    #region Dropdown Options

    public ObservableCollection<CategoryOption> AvailableCategories { get; } = [];
    public ObservableCollection<CategoryItem> CategoryItems { get; } = [];
    public ObservableCollection<SupplierOption> AvailableSuppliers { get; } = [];
    public ObservableCollection<string> AvailableCountries { get; } = [];
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
        _isExpensesTab = isExpensesTab;
        OpenAddModal();
    }

    [RelayCommand]
    public void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    [RelayCommand]
    public void OpenCategoriesWithAddModal()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        OpenCategoriesRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void SaveNewProduct()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        companyData.IdCounters.Product++;
        var newId = $"PRD-{companyData.IdCounters.Product:D3}";

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
            TrackInventory = ModalItemType == "Product" && !string.IsNullOrWhiteSpace(ModalReorderPoint),
            Status = EntityStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.Products.Add(newProduct);
        companyData.MarkAsModified();

        var productToUndo = newProduct;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
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
            if (ModalSupplier != null)
            {
                var supplier = companyData.Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
                ModalCountryOfOrigin = supplier?.Address.Country ?? string.Empty;
            }
        }

        ModalReorderPoint = product.TrackInventory ? "10" : string.Empty;
        ModalOverstockThreshold = product.TrackInventory ? "100" : string.Empty;

        ModalError = null;
        IsEditModalOpen = true;
    }

    public void OpenEditModal(ProductDisplayItem? item, bool isExpensesTab)
    {
        _isExpensesTab = isExpensesTab;
        OpenEditModal(item);
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingProduct = null;
        ClearModalFields();
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

        var newName = ModalProductName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? string.Empty : ModalDescription.Trim();
        var newSku = string.IsNullOrWhiteSpace(ModalSku) ? _editingProduct.Id : ModalSku.Trim();
        var newCategoryId = ModalCategory?.Id;
        var newSupplierId = ModalSupplier?.Id;
        var newUnitPrice = decimal.TryParse(ModalUnitPrice, out var unitPrice) ? unitPrice : 0;
        var newCostPrice = decimal.TryParse(ModalCostPrice, out var costPrice) ? costPrice : 0;
        var newTrackInventory = ModalItemType == "Product" && !string.IsNullOrWhiteSpace(ModalReorderPoint);

        // Check if anything actually changed
        var hasChanges = oldName != newName ||
                         oldDescription != newDescription ||
                         oldSku != newSku ||
                         oldCategoryId != newCategoryId ||
                         oldSupplierId != newSupplierId ||
                         oldUnitPrice != newUnitPrice ||
                         oldCostPrice != newCostPrice ||
                         oldTrackInventory != newTrackInventory;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

        var productToEdit = _editingProduct;
        productToEdit.Name = newName;
        productToEdit.Description = newDescription;
        productToEdit.Sku = newSku;
        productToEdit.CategoryId = newCategoryId;
        productToEdit.SupplierId = newSupplierId;
        productToEdit.UnitPrice = newUnitPrice;
        productToEdit.CostPrice = newCostPrice;
        productToEdit.TrackInventory = newTrackInventory;
        productToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Edit product '{newName}'",
            () =>
            {
                productToEdit.Name = oldName;
                productToEdit.Description = oldDescription;
                productToEdit.Sku = oldSku;
                productToEdit.CategoryId = oldCategoryId;
                productToEdit.SupplierId = oldSupplierId;
                productToEdit.UnitPrice = oldUnitPrice;
                productToEdit.CostPrice = oldCostPrice;
                productToEdit.TrackInventory = oldTrackInventory;
                companyData.MarkAsModified();
                ProductSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                productToEdit.Name = newName;
                productToEdit.Description = newDescription;
                productToEdit.Sku = newSku;
                productToEdit.CategoryId = newCategoryId;
                productToEdit.SupplierId = newSupplierId;
                productToEdit.UnitPrice = newUnitPrice;
                productToEdit.CostPrice = newCostPrice;
                productToEdit.TrackInventory = newTrackInventory;
                companyData.MarkAsModified();
                ProductSaved?.Invoke(this, EventArgs.Empty);
            }));

        ProductSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Product

    public void OpenDeleteConfirm(ProductDisplayItem? item)
    {
        if (item == null)
            return;

        _deletingProduct = item;
        OnPropertyChanged(nameof(DeletingProductName));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingProduct = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingProduct == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var product = companyData.Products.FirstOrDefault(p => p.Id == _deletingProduct.Id);
        if (product != null)
        {
            var deletedProduct = product;
            companyData.Products.Remove(product);
            companyData.MarkAsModified();

            App.UndoRedoManager?.RecordAction(new DelegateAction(
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
        CloseDeleteConfirm();
    }

    public string DeletingProductName => _deletingProduct?.Name ?? string.Empty;

    #endregion

    #region Filter Modal

    [RelayCommand]
    public void OpenFilterModal()
    {
        UpdateDropdownOptions();
        IsFilterModalOpen = true;
    }

    public void OpenFilterModal(bool isExpensesTab)
    {
        _isExpensesTab = isExpensesTab;
        OpenFilterModal();
    }

    [RelayCommand]
    public void CloseFilterModal()
    {
        IsFilterModalOpen = false;
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
        FilterItemType = "All";
        FilterCategory = null;
        FilterSupplier = null;
        FilterCountry = null;
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region Modal Helpers

    private void UpdateDropdownOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        AvailableCategories.Clear();
        var targetType = _isExpensesTab ? CategoryType.Purchase : CategoryType.Sales;
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

        AvailableCountries.Clear();
        AvailableCountries.Add("All Countries");
        var countries = companyData.Suppliers
            .Select(s => s.Address.Country)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);

        foreach (var country in countries)
        {
            AvailableCountries.Add(country);
        }
    }

    private void ClearModalFields()
    {
        ModalProductName = string.Empty;
        ModalDescription = string.Empty;
        ModalItemType = "Product";
        ModalCategory = null;
        ModalCategoryId = null;
        ModalSupplier = null;
        ModalCountryOfOrigin = string.Empty;
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
            ModalProductNameError = "Product name is required.";
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
                ModalProductNameError = "A product with this name already exists.";
                isValid = false;
            }
        }

        if (HasCategories && string.IsNullOrEmpty(ModalCategoryId))
        {
            ModalCategoryError = "Category is required.";
            isValid = false;
        }

        return isValid;
    }

    #endregion
}
