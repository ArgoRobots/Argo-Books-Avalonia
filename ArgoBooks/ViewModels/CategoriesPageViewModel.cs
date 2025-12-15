using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Categories page.
/// </summary>
public partial class CategoriesPageViewModel : ViewModelBase
{
    #region Tab Selection

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Gets whether the Expenses tab is selected.
    /// </summary>
    public bool IsExpensesTabSelected => SelectedTabIndex == 0;

    /// <summary>
    /// Gets whether the Revenue tab is selected.
    /// </summary>
    public bool IsRevenueTabSelected => SelectedTabIndex == 1;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsExpensesTabSelected));
        OnPropertyChanged(nameof(IsRevenueTabSelected));
        FilterCategories();
    }

    #endregion

    #region Search

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        FilterCategories();
    }

    #endregion

    #region Categories Collections

    /// <summary>
    /// All categories (unfiltered).
    /// </summary>
    private readonly List<Category> _allCategories = [];

    /// <summary>
    /// Expense categories (Purchase type) for display.
    /// </summary>
    public ObservableCollection<CategoryDisplayItem> ExpenseCategories { get; } = [];

    /// <summary>
    /// Revenue categories (Sales type) for display.
    /// </summary>
    public ObservableCollection<CategoryDisplayItem> RevenueCategories { get; } = [];

    /// <summary>
    /// Gets the current tab's categories for display.
    /// </summary>
    public ObservableCollection<CategoryDisplayItem> CurrentCategories =>
        IsExpensesTabSelected ? ExpenseCategories : RevenueCategories;

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private bool _isEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalCategoryName = string.Empty;

    [ObservableProperty]
    private CategoryDisplayItem? _modalParentCategory;

    [ObservableProperty]
    private string _modalDescription = string.Empty;

    [ObservableProperty]
    private string _modalItemType = "Product";

    [ObservableProperty]
    private IconOption? _modalSelectedIconOption;

    [ObservableProperty]
    private ColorOption? _modalSelectedColorOption;

    [ObservableProperty]
    private string? _modalError;

    /// <summary>
    /// The category being edited (null for add).
    /// </summary>
    private Category? _editingCategory;

    /// <summary>
    /// The category being deleted.
    /// </summary>
    private CategoryDisplayItem? _deletingCategory;

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Available parent categories for the current tab.
    /// </summary>
    public ObservableCollection<CategoryDisplayItem> AvailableParentCategories { get; } = [];

    /// <summary>
    /// Item types (Product/Service).
    /// </summary>
    public ObservableCollection<string> ItemTypes { get; } = ["Product", "Service"];

    /// <summary>
    /// Available icons for dropdown.
    /// </summary>
    public ObservableCollection<IconOption> AvailableIcons { get; } =
    [
        new("📦", "Box"),
        new("🏷️", "Tag"),
        new("📁", "Folder"),
        new("🛒", "Shopping Cart"),
        new("🚚", "Truck"),
        new("🔧", "Tools"),
        new("🏠", "Home"),
        new("💻", "Computer"),
        new("📱", "Phone"),
        new("💡", "Light Bulb"),
        new("⚙️", "Settings"),
        new("⭐", "Star"),
        new("❤️", "Heart"),
        new("💵", "Dollar")
    ];

    /// <summary>
    /// Available colors for dropdown.
    /// </summary>
    public ObservableCollection<ColorOption> AvailableColors { get; } =
    [
        new("#4A90D9", "Blue"),
        new("#10B981", "Green"),
        new("#F59E0B", "Orange"),
        new("#8B5CF6", "Purple"),
        new("#EF4444", "Red")
    ];

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CategoriesPageViewModel()
    {
        // Set default selections
        _modalSelectedIconOption = AvailableIcons.FirstOrDefault();
        _modalSelectedColorOption = AvailableColors.FirstOrDefault();
        LoadCategories();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads categories from the company data.
    /// </summary>
    private void LoadCategories()
    {
        _allCategories.Clear();
        ExpenseCategories.Clear();
        RevenueCategories.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        _allCategories.AddRange(companyData.Categories);
        FilterCategories();
    }

    /// <summary>
    /// Refreshes the categories from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshCategories()
    {
        LoadCategories();
    }

    /// <summary>
    /// Filters and organizes categories based on current tab and search.
    /// </summary>
    private void FilterCategories()
    {
        var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;
        var targetCollection = IsExpensesTabSelected ? ExpenseCategories : RevenueCategories;

        targetCollection.Clear();

        // Get all categories of the current type
        var categories = _allCategories
            .Where(c => c.Type == targetType)
            .ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            categories = categories
                .Where(c => c.Name.ToLowerInvariant().Contains(query) ||
                           (c.Description?.ToLowerInvariant().Contains(query) ?? false))
                .ToList();
        }

        // Build hierarchy - parent categories first, then children
        var parentCategories = categories.Where(c => string.IsNullOrEmpty(c.ParentId)).ToList();

        foreach (var parent in parentCategories.OrderBy(c => c.Name))
        {
            var childCount = CountChildren(parent.Id);
            targetCollection.Add(CreateDisplayItem(parent, null, childCount));

            // Add children
            var children = categories.Where(c => c.ParentId == parent.Id).OrderBy(c => c.Name);
            foreach (var child in children)
            {
                var grandchildCount = CountChildren(child.Id);
                targetCollection.Add(CreateDisplayItem(child, parent.Name, grandchildCount, isChild: true));
            }
        }

        // Add orphaned categories (have a parent ID that doesn't exist)
        var orphans = categories.Where(c =>
            !string.IsNullOrEmpty(c.ParentId) &&
            !categories.Any(p => p.Id == c.ParentId));

        foreach (var orphan in orphans.OrderBy(c => c.Name))
        {
            var childCount = CountChildren(orphan.Id);
            targetCollection.Add(CreateDisplayItem(orphan, "Unknown", childCount));
        }

        // Update available parent categories for modal
        UpdateAvailableParentCategories();
        OnPropertyChanged(nameof(CurrentCategories));
    }

    private CategoryDisplayItem CreateDisplayItem(Category category, string? parentName, int childCount, bool isChild = false)
    {
        // Count products using this category
        var productCount = App.CompanyManager?.CompanyData?.Products
            .Count(p => p.CategoryId == category.Id) ?? 0;

        return new CategoryDisplayItem
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            ParentName = parentName ?? string.Empty,
            Description = category.Description ?? string.Empty,
            ItemType = category.ItemType,
            Color = category.Color,
            Icon = category.Icon,
            ProductCount = productCount,
            ChildCount = childCount,
            IsChild = isChild,
            Type = category.Type
        };
    }

    private int CountChildren(string parentId)
    {
        return _allCategories.Count(c => c.ParentId == parentId);
    }

    private void UpdateAvailableParentCategories()
    {
        AvailableParentCategories.Clear();

        // Add empty option for no parent
        AvailableParentCategories.Add(new CategoryDisplayItem { Id = string.Empty, Name = "None (Top Level)" });

        var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;

        // Only include top-level categories as potential parents
        var topLevelCategories = _allCategories
            .Where(c => c.Type == targetType && string.IsNullOrEmpty(c.ParentId))
            .OrderBy(c => c.Name);

        foreach (var cat in topLevelCategories)
        {
            // Don't allow a category to be its own parent
            if (_editingCategory != null && cat.Id == _editingCategory.Id)
                continue;

            AvailableParentCategories.Add(new CategoryDisplayItem
            {
                Id = cat.Id,
                Name = cat.Name,
                Color = cat.Color,
                Icon = cat.Icon
            });
        }
    }

    #endregion

    #region Add Category

    /// <summary>
    /// Opens the Add Category modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        _editingCategory = null;
        ClearModalFields();
        UpdateAvailableParentCategories();
        IsAddModalOpen = true;
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
    /// Saves a new category.
    /// </summary>
    [RelayCommand]
    private void SaveNewCategory()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Generate new ID
        companyData.IdCounters.Category++;
        var typePrefix = IsExpensesTabSelected ? "PUR" : "SAL";
        var newId = $"CAT-{typePrefix}-{companyData.IdCounters.Category:D3}";

        var newCategory = new Category
        {
            Id = newId,
            Name = ModalCategoryName.Trim(),
            Type = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales,
            ParentId = ModalParentCategory?.Id != string.Empty ? ModalParentCategory?.Id : null,
            Description = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim(),
            ItemType = ModalItemType,
            Color = ModalSelectedColorOption?.Hex ?? "#4A90D9",
            Icon = ModalSelectedIconOption?.Icon ?? "📦",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Categories.Add(newCategory);
        companyData.MarkAsModified();

        // Reload and close
        LoadCategories();
        CloseAddModal();
    }

    #endregion

    #region Edit Category

    /// <summary>
    /// Opens the Edit Category modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(CategoryDisplayItem? item)
    {
        if (item == null)
            return;

        var category = _allCategories.FirstOrDefault(c => c.Id == item.Id);
        if (category == null)
            return;

        _editingCategory = category;
        UpdateAvailableParentCategories();

        // Populate fields
        ModalCategoryName = category.Name;
        ModalDescription = category.Description ?? string.Empty;
        ModalItemType = category.ItemType;
        ModalSelectedColorOption = AvailableColors.FirstOrDefault(c => c.Hex == category.Color) ?? AvailableColors.First();
        ModalSelectedIconOption = AvailableIcons.FirstOrDefault(i => i.Icon == category.Icon) ?? AvailableIcons.First();

        // Set parent
        if (!string.IsNullOrEmpty(category.ParentId))
        {
            ModalParentCategory = AvailableParentCategories.FirstOrDefault(c => c.Id == category.ParentId);
        }
        else
        {
            ModalParentCategory = AvailableParentCategories.FirstOrDefault(c => c.Id == string.Empty);
        }

        ModalError = null;
        IsEditModalOpen = true;
    }

    /// <summary>
    /// Closes the Edit modal.
    /// </summary>
    [RelayCommand]
    private void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingCategory = null;
        ClearModalFields();
    }

    /// <summary>
    /// Saves changes to an existing category.
    /// </summary>
    [RelayCommand]
    private void SaveEditedCategory()
    {
        if (!ValidateModal() || _editingCategory == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Update the category
        _editingCategory.Name = ModalCategoryName.Trim();
        _editingCategory.ParentId = ModalParentCategory?.Id != string.Empty ? ModalParentCategory?.Id : null;
        _editingCategory.Description = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim();
        _editingCategory.ItemType = ModalItemType;
        _editingCategory.Color = ModalSelectedColorOption?.Hex ?? "#4A90D9";
        _editingCategory.Icon = ModalSelectedIconOption?.Icon ?? "📦";

        companyData.MarkAsModified();

        // Reload and close
        LoadCategories();
        CloseEditModal();
    }

    #endregion

    #region Delete Category

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(CategoryDisplayItem? item)
    {
        if (item == null)
            return;

        _deletingCategory = item;
        OnPropertyChanged(nameof(DeletingCategoryName));
        IsDeleteConfirmOpen = true;
    }

    /// <summary>
    /// Closes the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingCategory = null;
    }

    /// <summary>
    /// Confirms and deletes the category.
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
    {
        if (_deletingCategory == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var category = companyData.Categories.FirstOrDefault(c => c.Id == _deletingCategory.Id);
        if (category != null)
        {
            // Remove any child categories first
            var children = companyData.Categories.Where(c => c.ParentId == category.Id).ToList();
            foreach (var child in children)
            {
                // Clear parent reference instead of deleting children
                child.ParentId = null;
            }

            companyData.Categories.Remove(category);
            companyData.MarkAsModified();
        }

        LoadCategories();
        CloseDeleteConfirm();
    }

    /// <summary>
    /// Gets the name of the category being deleted (for display in confirmation).
    /// </summary>
    public string DeletingCategoryName => _deletingCategory?.Name ?? string.Empty;

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalCategoryName = string.Empty;
        ModalParentCategory = AvailableParentCategories.FirstOrDefault(c => c.Id == string.Empty);
        ModalDescription = string.Empty;
        ModalItemType = "Product";
        ModalSelectedIconOption = AvailableIcons.FirstOrDefault();
        ModalSelectedColorOption = AvailableColors.FirstOrDefault();
        ModalError = null;
    }

    private bool ValidateModal()
    {
        ModalError = null;

        if (string.IsNullOrWhiteSpace(ModalCategoryName))
        {
            ModalError = "Category name is required.";
            return false;
        }

        // Check for duplicate names within the same type
        var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;
        var existingWithSameName = _allCategories.Any(c =>
            c.Type == targetType &&
            c.Name.Equals(ModalCategoryName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (_editingCategory == null || c.Id != _editingCategory.Id));

        if (existingWithSameName)
        {
            ModalError = "A category with this name already exists.";
            return false;
        }

        return true;
    }

    #endregion
}

/// <summary>
/// Display model for categories in the UI.
/// </summary>
public partial class CategoryDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _parentId;

    [ObservableProperty]
    private string _parentName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _itemType = "Product";

    [ObservableProperty]
    private string _color = "#4A90D9";

    [ObservableProperty]
    private string _icon = "📦";

    [ObservableProperty]
    private int _productCount;

    [ObservableProperty]
    private int _childCount;

    [ObservableProperty]
    private bool _isChild;

    [ObservableProperty]
    private CategoryType _type;

    /// <summary>
    /// Display string for product/service count.
    /// </summary>
    public string ProductCountDisplay => ProductCount == 1 ? "1 item" : $"{ProductCount} items";
}

/// <summary>
/// Represents an icon option for dropdown.
/// </summary>
public class IconOption
{
    public string Icon { get; }
    public string Name { get; }
    public string DisplayName => $"{Icon} {Name}";

    public IconOption(string icon, string name)
    {
        Icon = icon;
        Name = name;
    }
}

/// <summary>
/// Represents a color option for dropdown.
/// </summary>
public class ColorOption
{
    public string Hex { get; }
    public string Name { get; }

    public ColorOption(string hex, string name)
    {
        Hex = hex;
        Name = name;
    }
}
