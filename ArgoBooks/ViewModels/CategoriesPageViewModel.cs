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
    private string _modalCategoryType = "Product";

    [ObservableProperty]
    private string _modalSelectedIcon = "box";

    [ObservableProperty]
    private string _modalSelectedColor = "#4A90D9";

    [ObservableProperty]
    private decimal _modalDefaultTaxRate;

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
    /// Category types (Product/Service).
    /// </summary>
    public ObservableCollection<string> CategoryTypes { get; } = ["Product", "Service"];

    /// <summary>
    /// Available icons.
    /// </summary>
    public ObservableCollection<IconOption> AvailableIcons { get; } =
    [
        new("box", "M21 16.5c0 .38-.21.71-.53.88l-7.9 4.44c-.16.12-.36.18-.57.18-.21 0-.41-.06-.57-.18l-7.9-4.44A.991.991 0 0 1 3 16.5v-9c0-.38.21-.71.53-.88l7.9-4.44c.16-.12.36-.18.57-.18.21 0 .41.06.57.18l7.9 4.44c.32.17.53.5.53.88v9z"),
        new("tag", "M17.63 5.84C17.27 5.33 16.67 5 16 5L5 5.01C3.9 5.01 3 5.9 3 7v10c0 1.1.9 1.99 2 1.99L16 19c.67 0 1.27-.33 1.63-.84L22 12l-4.37-6.16z"),
        new("folder", "M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"),
        new("shopping-cart", "M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12.9-1.63h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49c.08-.14.12-.31.12-.48 0-.55-.45-1-1-1H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"),
        new("truck", "M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zm13.5-9l1.96 2.5H17V9.5h2.5zm-1.5 9c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z"),
        new("tools", "M22.7 19l-9.1-9.1c.9-2.3.4-5-1.5-6.9-2-2-5-2.4-7.4-1.3L9 6 6 9 1.6 4.7C.4 7.1.9 10.1 2.9 12.1c1.9 1.9 4.6 2.4 6.9 1.5l9.1 9.1c.4.4 1 .4 1.4 0l2.3-2.3c.5-.4.5-1.1.1-1.4z"),
        new("home", "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"),
        new("computer", "M20 18c1.1 0 1.99-.9 1.99-2L22 6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2H0v2h24v-2h-4zM4 6h16v10H4V6z"),
        new("phone", "M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z"),
        new("lightbulb", "M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7z"),
        new("settings", "M19.14 12.94c.04-.31.06-.63.06-.94 0-.31-.02-.63-.06-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"),
        new("star", "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"),
        new("heart", "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"),
        new("dollar", "M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z")
    ];

    /// <summary>
    /// Available colors.
    /// </summary>
    public ObservableCollection<ColorOption> AvailableColors { get; } =
    [
        new("#4A90D9", "Blue"),
        new("#10B981", "Green"),
        new("#F59E0B", "Orange"),
        new("#8B5CF6", "Purple"),
        new("#EF4444", "Red"),
        new("#EC4899", "Pink"),
        new("#14B8A6", "Teal"),
        new("#6B7280", "Gray")
    ];

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CategoriesPageViewModel()
    {
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
                .Where(c => c.Name.ToLowerInvariant().Contains(query))
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
            Color = category.Color,
            Icon = category.Icon,
            IconPath = GetIconPath(category.Icon),
            DefaultTaxRate = category.DefaultTaxRate,
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
                Icon = cat.Icon,
                IconPath = GetIconPath(cat.Icon)
            });
        }
    }

    private string GetIconPath(string iconName)
    {
        return AvailableIcons.FirstOrDefault(i => i.Name == iconName)?.Path
            ?? AvailableIcons.First().Path;
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
            Color = ModalSelectedColor,
            Icon = ModalSelectedIcon,
            DefaultTaxRate = ModalDefaultTaxRate,
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
        ModalSelectedColor = category.Color;
        ModalSelectedIcon = category.Icon;
        ModalDefaultTaxRate = category.DefaultTaxRate;

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
        _editingCategory.Color = ModalSelectedColor;
        _editingCategory.Icon = ModalSelectedIcon;
        _editingCategory.DefaultTaxRate = ModalDefaultTaxRate;

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
        ModalCategoryType = "Product";
        ModalSelectedIcon = "box";
        ModalSelectedColor = "#4A90D9";
        ModalDefaultTaxRate = 0;
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

    /// <summary>
    /// Selects an icon for the category.
    /// </summary>
    [RelayCommand]
    private void SelectIcon(string? iconName)
    {
        if (!string.IsNullOrEmpty(iconName))
        {
            ModalSelectedIcon = iconName;
        }
    }

    /// <summary>
    /// Selects a color for the category.
    /// </summary>
    [RelayCommand]
    private void SelectColor(string? colorHex)
    {
        if (!string.IsNullOrEmpty(colorHex))
        {
            ModalSelectedColor = colorHex;
        }
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
    private string _color = "#4A90D9";

    [ObservableProperty]
    private string _icon = "box";

    [ObservableProperty]
    private string _iconPath = string.Empty;

    [ObservableProperty]
    private decimal _defaultTaxRate;

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
/// Represents an icon option.
/// </summary>
public class IconOption
{
    public string Name { get; }
    public string Path { get; }

    public IconOption(string name, string path)
    {
        Name = name;
        Path = path;
    }
}

/// <summary>
/// Represents a color option.
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
