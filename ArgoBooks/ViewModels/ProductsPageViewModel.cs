using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Data;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Products/Services page.
/// </summary>
public partial class ProductsPageViewModel : SortablePageViewModelBase
{
    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public ProductsTableColumnWidths ColumnWidths => App.ProductsColumnWidths;

    #endregion

    #region Tab Selection

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Gets whether the Expenses tab is selected (products/services purchased).
    /// </summary>
    public bool IsExpensesTabSelected => SelectedTabIndex == 0;

    /// <summary>
    /// Gets whether the Revenue tab is selected (products/services sold).
    /// </summary>
    public bool IsRevenueTabSelected => SelectedTabIndex == 1;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsExpensesTabSelected));
        OnPropertyChanged(nameof(IsRevenueTabSelected));
        OnPropertyChanged(nameof(RemainingProductsText));
        OnPropertyChanged(nameof(CanAddProduct));
        ColumnWidths.SetTabMode(IsExpensesTabSelected);
        FilterProducts();
    }

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterProducts();
    }

    [ObservableProperty]
    private string _filterItemType = "All";

    [ObservableProperty]
    private string? _filterCategory;

    [ObservableProperty]
    private string? _filterSupplier;

    [ObservableProperty]
    private string? _filterCountry;

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _physicalProducts;

    [ObservableProperty]
    private int _services;

    #endregion

    #region Plan Status and Product Limits

    private const int FreeProductLimit = 10;

    [ObservableProperty]
    private bool _hasStandard;

    [ObservableProperty]
    private int _expenseProductsCount;

    [ObservableProperty]
    private int _revenueProductsCount;

    /// <summary>
    /// Gets remaining expense products the user can add (when no standard plan).
    /// </summary>
    public int RemainingExpenseProducts => Math.Max(0, FreeProductLimit - ExpenseProductsCount);

    /// <summary>
    /// Gets remaining revenue products the user can add (when no standard plan).
    /// </summary>
    public int RemainingRevenueProducts => Math.Max(0, FreeProductLimit - RevenueProductsCount);

    /// <summary>
    /// Gets whether the user can add more products to the current tab.
    /// </summary>
    public bool CanAddProduct => HasStandard || (IsExpensesTabSelected ? RemainingExpenseProducts > 0 : RemainingRevenueProducts > 0);

    /// <summary>
    /// Gets the text showing remaining products for the current tab.
    /// </summary>
    public string RemainingProductsText
    {
        get
        {
            var remaining = IsExpensesTabSelected ? RemainingExpenseProducts : RemainingRevenueProducts;
            return $"{remaining} of {FreeProductLimit} remaining";
        }
    }

    /// <summary>
    /// Gets whether to show the upgrade button (when limit is reached).
    /// </summary>
    public bool ShowUpgradeButton => !HasStandard && !CanAddProduct;

    /// <summary>
    /// Event raised when the upgrade button is clicked.
    /// </summary>
    public event EventHandler? UpgradeRequested;

    /// <summary>
    /// Gets whether to show the remaining products label (only when no standard plan).
    /// </summary>
    public bool ShowRemainingProducts => !HasStandard;

    partial void OnExpenseProductsCountChanged(int value)
    {
        OnPropertyChanged(nameof(RemainingExpenseProducts));
        OnPropertyChanged(nameof(RemainingProductsText));
        OnPropertyChanged(nameof(CanAddProduct));
        OnPropertyChanged(nameof(ShowUpgradeButton));
    }

    partial void OnRevenueProductsCountChanged(int value)
    {
        OnPropertyChanged(nameof(RemainingRevenueProducts));
        OnPropertyChanged(nameof(RemainingProductsText));
        OnPropertyChanged(nameof(CanAddProduct));
        OnPropertyChanged(nameof(ShowUpgradeButton));
    }

    partial void OnHasStandardChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAddProduct));
        OnPropertyChanged(nameof(ShowRemainingProducts));
        OnPropertyChanged(nameof(ShowUpgradeButton));
    }

    [RelayCommand]
    private void Upgrade()
    {
        UpgradeRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Products Collection

    /// <summary>
    /// All products (unfiltered).
    /// </summary>
    private readonly List<Product> _allProducts = [];

    /// <summary>
    /// Expense products (purchased) for display.
    /// </summary>
    public ObservableCollection<ProductDisplayItem> ExpenseProducts { get; } = [];

    /// <summary>
    /// Revenue products (sold) for display.
    /// </summary>
    public ObservableCollection<ProductDisplayItem> RevenueProducts { get; } = [];

    /// <summary>
    /// Gets the current tab's products for display.
    /// </summary>
    public ObservableCollection<ProductDisplayItem> CurrentProducts =>
        IsExpensesTabSelected ? ExpenseProducts : RevenueProducts;

    /// <summary>
    /// Available categories for filter/modal dropdown.
    /// </summary>
    public ObservableCollection<CategoryOption> AvailableCategories { get; } = [];

    /// <summary>
    /// Category items for the searchable category input (excludes "All Categories").
    /// </summary>
    public ObservableCollection<CategoryItem> CategoryItems { get; } = [];

    /// <summary>
    /// Gets whether there are any categories available.
    /// </summary>
    public bool HasCategories => CategoryItems.Count > 0;

    /// <summary>
    /// Available suppliers for filter/modal dropdown.
    /// </summary>
    public ObservableCollection<SupplierOption> AvailableSuppliers { get; } = [];

    /// <summary>
    /// Available countries for filter/modal dropdown.
    /// </summary>
    public ObservableCollection<string> AvailableCountries { get; } = [];

    /// <summary>
    /// Item type options for filter.
    /// </summary>
    public ObservableCollection<string> ItemTypeOptions { get; } = ["All", "Product", "Service"];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 products";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterProducts();

    #endregion

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
        // Update ModalCategory when CategoryId changes
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

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Item types for dropdown.
    /// </summary>
    public ObservableCollection<string> ItemTypes { get; } = ["Product", "Service"];

    /// <summary>
    /// All countries for dropdown.
    /// </summary>
    public IReadOnlyList<string> CountryOptions { get; } = Countries.Names;

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ProductsPageViewModel()
    {
        LoadProducts();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to product modal events to refresh data
        if (App.ProductModalsViewModel != null)
        {
            App.ProductModalsViewModel.ProductSaved += OnProductSaved;
            App.ProductModalsViewModel.ProductDeleted += OnProductDeleted;
            App.ProductModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.ProductModalsViewModel.FiltersCleared += OnFiltersCleared;
            App.ProductModalsViewModel.OpenCategoriesRequested += OnOpenCategoriesRequested;
        }

        // Subscribe to plan status changes so we update when user upgrades
        App.PlanStatusChanged += OnPlanStatusChanged;
    }

    /// <summary>
    /// Handles plan status changes by updating HasStandard.
    /// </summary>
    private void OnPlanStatusChanged(object? sender, PlanStatusChangedEventArgs e)
    {
        HasStandard = e.HasStandard;
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the products.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadProducts();
    }

    private void OnProductSaved(object? sender, EventArgs e)
    {
        LoadProducts();
    }

    private void OnProductDeleted(object? sender, EventArgs e)
    {
        LoadProducts();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        var modals = App.ProductModalsViewModel;
        if (modals != null)
        {
            FilterItemType = modals.FilterItemType;
            FilterCategory = modals.FilterCategory;
            FilterSupplier = modals.FilterSupplier;
            FilterCountry = modals.FilterCountry;
        }
        CurrentPage = 1;
        FilterProducts();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterItemType = "All";
        FilterCategory = null;
        FilterSupplier = null;
        FilterCountry = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterProducts();
    }

    private void OnOpenCategoriesRequested(object? sender, EventArgs e)
    {
        App.NavigationService?.NavigateTo("Categories", new Dictionary<string, object?> { { "openAddModal", true }, { "selectedTabIndex", SelectedTabIndex } });
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads products from the company data.
    /// </summary>
    private void LoadProducts()
    {
        _allProducts.Clear();
        ExpenseProducts.Clear();
        RevenueProducts.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null)
            return;

        _allProducts.AddRange(companyData.Products);
        UpdateStatistics();
        UpdateDropdownOptions();
        FilterProducts();
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        TotalProducts = _allProducts.Count;

        // Count products vs services based on category ItemType
        var productCategories = companyData.Categories
            .Where(c => c.ItemType == "Product")
            .Select(c => c.Id)
            .ToHashSet();

        var serviceCategories = companyData.Categories
            .Where(c => c.ItemType == "Service")
            .Select(c => c.Id)
            .ToHashSet();

        PhysicalProducts = _allProducts.Count(p =>
            string.IsNullOrEmpty(p.CategoryId) ||
            productCategories.Contains(p.CategoryId));

        Services = _allProducts.Count(p =>
            !string.IsNullOrEmpty(p.CategoryId) &&
            serviceCategories.Contains(p.CategoryId));

        // Count expense vs revenue products based on category type
        var expenseCategoryIds = companyData.Categories
            .Where(c => c.Type == CategoryType.Purchase)
            .Select(c => c.Id)
            .ToHashSet();

        var revenueCategoryIds = companyData.Categories
            .Where(c => c.Type == CategoryType.Sales)
            .Select(c => c.Id)
            .ToHashSet();

        ExpenseProductsCount = _allProducts.Count(p =>
            string.IsNullOrEmpty(p.CategoryId) || expenseCategoryIds.Contains(p.CategoryId));

        RevenueProductsCount = _allProducts.Count(p =>
            !string.IsNullOrEmpty(p.CategoryId) && revenueCategoryIds.Contains(p.CategoryId));
    }

    /// <summary>
    /// Updates the dropdown options from company data.
    /// </summary>
    private void UpdateDropdownOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Update categories
        AvailableCategories.Clear();
        AvailableCategories.Add(new CategoryOption { Id = null, Name = "All Categories" });

        var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;
        var categories = companyData.Categories
            .Where(c => c.Type == targetType)
            .OrderBy(c => c.Name);

        foreach (var cat in categories)
        {
            AvailableCategories.Add(new CategoryOption { Id = cat.Id, Name = cat.Name, ItemType = cat.ItemType });
        }

        // Update CategoryItems for the searchable input (excludes "All Categories")
        CategoryItems.Clear();
        foreach (var cat in categories)
        {
            CategoryItems.Add(new CategoryItem { Id = cat.Id, Name = cat.Name });
        }
        OnPropertyChanged(nameof(HasCategories));

        // Update suppliers
        AvailableSuppliers.Clear();
        AvailableSuppliers.Add(new SupplierOption { Id = null, Name = "All Suppliers" });

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            AvailableSuppliers.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
        }

        // Update countries
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

    /// <summary>
    /// Refreshes the products from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshProducts()
    {
        LoadProducts();
    }

    /// <summary>
    /// Filters products based on current tab, search query, and filters.
    /// </summary>
    private void FilterProducts()
    {
        var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;
        var targetCollection = IsExpensesTabSelected ? ExpenseProducts : RevenueProducts;

        targetCollection.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Get categories for the current tab type
        var categoryIds = companyData.Categories
            .Where(c => c.Type == targetType)
            .Select(c => c.Id)
            .ToHashSet();

        // Filter products by category type
        var filtered = _allProducts
            .Where(p => string.IsNullOrEmpty(p.CategoryId) || categoryIds.Contains(p.CategoryId))
            .ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(p => new
                {
                    Product = p,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Name),
                    SkuScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Sku),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Description ?? string.Empty)
                })
                .Where(x => x.NameScore >= 0 || x.SkuScore >= 0 || x.DescScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.NameScore, x.SkuScore), x.DescScore))
                .Select(x => x.Product)
                .ToList();
        }

        // Apply item type filter
        if (FilterItemType != "All")
        {
            var itemTypeCategories = companyData.Categories
                .Where(c => c.ItemType == FilterItemType)
                .Select(c => c.Id)
                .ToHashSet();

            filtered = filtered
                .Where(p => !string.IsNullOrEmpty(p.CategoryId) && itemTypeCategories.Contains(p.CategoryId))
                .ToList();
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(FilterCategory) && FilterCategory != "All Categories")
        {
            var categoryOption = AvailableCategories.FirstOrDefault(c => c.Name == FilterCategory);
            if (categoryOption?.Id != null)
            {
                filtered = filtered.Where(p => p.CategoryId == categoryOption.Id).ToList();
            }
        }

        // Apply supplier filter
        if (!string.IsNullOrWhiteSpace(FilterSupplier) && FilterSupplier != "All Suppliers")
        {
            var supplierOption = AvailableSuppliers.FirstOrDefault(s => s.Name == FilterSupplier);
            if (supplierOption?.Id != null)
            {
                filtered = filtered.Where(p => p.SupplierId == supplierOption.Id).ToList();
            }
        }

        // Apply country filter (via supplier)
        if (!string.IsNullOrWhiteSpace(FilterCountry) && FilterCountry != "All Countries")
        {
            var supplierIdsInCountry = companyData.Suppliers
                .Where(s => s.Address.Country.Equals(FilterCountry, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Id)
                .ToHashSet();

            filtered = filtered.Where(p => !string.IsNullOrEmpty(p.SupplierId) && supplierIdsInCountry.Contains(p.SupplierId)).ToList();
        }

        // Create display items
        var displayItems = filtered.Select(product =>
        {
            var category = companyData.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
            var supplier = companyData.Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);

            return new ProductDisplayItem
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Description = product.Description,
                ItemType = category?.ItemType ?? "Product",
                CategoryName = category?.Name ?? "-",
                SupplierName = supplier?.Name ?? "-",
                CountryOfOrigin = supplier?.Address.Country ?? "-",
                ReorderPoint = product.TrackInventory ? "10" : "-",
                OverstockThreshold = product.TrackInventory ? "100" : "-",
                UnitPrice = product.UnitPrice,
                CostPrice = product.CostPrice,
                TrackInventory = product.TrackInventory
            };
        }).ToList();

        // Apply sorting (only if not searching, since search has its own relevance sorting)
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<ProductDisplayItem, object?>>
                {
                    ["Name"] = p => p.Name,
                    ["Type"] = p => p.ItemType,
                    ["Description"] = p => p.Description,
                    ["Category"] = p => p.CategoryName,
                    ["Supplier"] = p => p.SupplierName,
                    ["Country"] = p => p.CountryOfOrigin
                },
                p => p.Name);
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedProducts = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedProducts)
        {
            targetCollection.Add(item);
        }

        OnPropertyChanged(nameof(CurrentProducts));
    }

    protected override void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, startPage + 4);
        startPage = Math.Max(1, endPage - 4);

        for (var i = startPage; i <= endPage; i++)
        {
            PageNumbers.Add(i);
        }
    }

    private void UpdatePaginationText(int totalCount)
    {
        PaginationText = PaginationTextHelper.FormatPaginationText(
            totalCount, CurrentPage, PageSize, TotalPages, "product");
    }

    #endregion

    #region Add Product

    /// <summary>
    /// Opens the Add Product modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.ProductModalsViewModel?.OpenAddModal(IsExpensesTabSelected);
    }

    /// <summary>
    /// Closes the Add modal.
    /// </summary>
    [RelayCommand]
    private void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    /// <summary>
    /// Saves a new product.
    /// </summary>
    [RelayCommand]
    private void SaveNewProduct()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Generate new ID
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

        // Record undo action
        var productToUndo = newProduct;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Add product '{newProduct.Name}'",
            () =>
            {
                companyData.Products.Remove(productToUndo);
                companyData.MarkAsModified();
                LoadProducts();
            },
            () =>
            {
                companyData.Products.Add(productToUndo);
                companyData.MarkAsModified();
                LoadProducts();
            }));

        // Reload and close
        LoadProducts();
        CloseAddModal();
    }

    #endregion

    #region Edit Product

    /// <summary>
    /// Opens the Edit Product modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(ProductDisplayItem? item)
    {
        App.ProductModalsViewModel?.OpenEditModal(item, IsExpensesTabSelected);
    }

    /// <summary>
    /// Closes the Edit modal.
    /// </summary>
    [RelayCommand]
    private void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingProduct = null;
        ClearModalFields();
    }

    /// <summary>
    /// Saves changes to an existing product.
    /// </summary>
    [RelayCommand]
    private void SaveEditedProduct()
    {
        if (!ValidateModal() || _editingProduct == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Store old values for undo
        var oldName = _editingProduct.Name;
        var oldDescription = _editingProduct.Description;
        var oldSku = _editingProduct.Sku;
        var oldCategoryId = _editingProduct.CategoryId;
        var oldSupplierId = _editingProduct.SupplierId;
        var oldUnitPrice = _editingProduct.UnitPrice;
        var oldCostPrice = _editingProduct.CostPrice;
        var oldTrackInventory = _editingProduct.TrackInventory;

        // Store new values
        var newName = ModalProductName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? string.Empty : ModalDescription.Trim();
        var newSku = string.IsNullOrWhiteSpace(ModalSku) ? _editingProduct.Id : ModalSku.Trim();
        var newCategoryId = ModalCategory?.Id;
        var newSupplierId = ModalSupplier?.Id;
        var newUnitPrice = decimal.TryParse(ModalUnitPrice, out var unitPrice) ? unitPrice : 0;
        var newCostPrice = decimal.TryParse(ModalCostPrice, out var costPrice) ? costPrice : 0;
        var newTrackInventory = ModalItemType == "Product" && !string.IsNullOrWhiteSpace(ModalReorderPoint);

        // Update the product
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

        // Record undo action
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
                LoadProducts();
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
                LoadProducts();
            }));

        // Reload and close
        LoadProducts();
        CloseEditModal();
    }

    #endregion

    #region Delete Product

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(ProductDisplayItem? item)
    {
        App.ProductModalsViewModel?.OpenDeleteConfirm(item);
    }

    /// <summary>
    /// Closes the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingProduct = null;
    }

    /// <summary>
    /// Confirms and deletes the product.
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
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

            // Record undo action
            App.UndoRedoManager?.RecordAction(new DelegateAction(
                $"Delete product '{deletedProduct.Name}'",
                () =>
                {
                    companyData.Products.Add(deletedProduct);
                    companyData.MarkAsModified();
                    LoadProducts();
                },
                () =>
                {
                    companyData.Products.Remove(deletedProduct);
                    companyData.MarkAsModified();
                    LoadProducts();
                }));
        }

        LoadProducts();
        CloseDeleteConfirm();
    }

    /// <summary>
    /// Gets the name of the product being deleted (for display in confirmation).
    /// </summary>
    public string DeletingProductName => _deletingProduct?.Name ?? string.Empty;

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
    {
        App.ProductModalsViewModel?.OpenFilterModal(IsExpensesTabSelected);
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
    /// Applies the current filters and closes the modal.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        CurrentPage = 1;
        FilterProducts();
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterItemType = "All";
        FilterCategory = null;
        FilterSupplier = null;
        FilterCountry = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterProducts();
        CloseFilterModal();
    }

    #endregion

    #region Modal Helpers

    /// <summary>
    /// Updates categories available in the modal based on current tab.
    /// </summary>
    private void UpdateModalCategories()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        AvailableCategories.Clear();

        var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;
        var categories = companyData.Categories
            .Where(c => c.Type == targetType)
            .OrderBy(c => c.Name);

        foreach (var cat in categories)
        {
            AvailableCategories.Add(new CategoryOption { Id = cat.Id, Name = cat.Name, ItemType = cat.ItemType });
        }

        // Update CategoryItems for the searchable input
        CategoryItems.Clear();
        foreach (var cat in categories)
        {
            CategoryItems.Add(new CategoryItem { Id = cat.Id, Name = cat.Name });
        }
        OnPropertyChanged(nameof(HasCategories));

        // Reset suppliers
        AvailableSuppliers.Clear();
        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            AvailableSuppliers.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
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
        // Clear all errors first
        ModalError = null;
        ModalProductNameError = null;
        ModalCategoryError = null;

        var isValid = true;

        // Validate product name (required)
        if (string.IsNullOrWhiteSpace(ModalProductName))
        {
            ModalProductNameError = "Product name is required.";
            isValid = false;
        }
        else
        {
            // Check for duplicate names
            var existingWithSameName = _allProducts.Any(p =>
                p.Name.Equals(ModalProductName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingProduct == null || p.Id != _editingProduct.Id));

            if (existingWithSameName)
            {
                ModalProductNameError = "A product with this name already exists.";
                isValid = false;
            }
        }

        // Validate category (required when categories exist)
        if (HasCategories && string.IsNullOrEmpty(ModalCategoryId))
        {
            ModalCategoryError = "Category is required.";
            isValid = false;
        }

        return isValid;
    }

    #endregion
}

/// <summary>
/// Display model for products in the UI.
/// </summary>
public partial class ProductDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _sku = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _itemType = "Product";

    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string _supplierName = string.Empty;

    [ObservableProperty]
    private string _countryOfOrigin = string.Empty;

    [ObservableProperty]
    private string _reorderPoint = string.Empty;

    [ObservableProperty]
    private string _overstockThreshold = string.Empty;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _costPrice;

    [ObservableProperty]
    private bool _trackInventory;

    /// <summary>
    /// Whether this is a service (no inventory tracking).
    /// </summary>
    public bool IsService => ItemType == "Service";

    /// <summary>
    /// CSS-like badge class for item type.
    /// </summary>
    public string TypeBadgeClass => ItemType == "Product" ? "info" : "secondary";
}

/// <summary>
/// Category option for dropdown.
/// </summary>
public class CategoryOption
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Product";

    public override string ToString() => Name;
}

/// <summary>
/// Supplier option for dropdown.
/// </summary>
public class SupplierOption
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
