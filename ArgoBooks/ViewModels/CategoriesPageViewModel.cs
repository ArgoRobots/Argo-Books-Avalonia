using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
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

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CategoriesPageViewModel()
    {
        // Set default selections
        _modalSelectedIconOption = AvailableIcons.FirstOrDefault();
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

        // Apply search filter using Levenshtein distance
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            categories = categories
                .Select(c => new
                {
                    Category = c,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, c.Name),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, c.Description ?? string.Empty)
                })
                .Where(x => x.NameScore >= 0 || x.DescScore >= 0)
                .OrderByDescending(x => Math.Max(x.NameScore, x.DescScore))
                .Select(x => x.Category)
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
            Color = "#4A90D9",
            Icon = ModalSelectedIconOption?.Icon ?? "📦",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Categories.Add(newCategory);
        companyData.MarkAsModified();

        // Record undo action
        var categoryToUndo = newCategory;
        App.UndoRedoManager?.RecordAction(new CategoryAddAction(
            $"Add category '{newCategory.Name}'",
            categoryToUndo,
            () =>
            {
                companyData.Categories.Remove(categoryToUndo);
                companyData.MarkAsModified();
                LoadCategories();
            },
            () =>
            {
                companyData.Categories.Add(categoryToUndo);
                companyData.MarkAsModified();
                LoadCategories();
            }));

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

        // Store old values for undo
        var oldName = _editingCategory.Name;
        var oldParentId = _editingCategory.ParentId;
        var oldDescription = _editingCategory.Description;
        var oldItemType = _editingCategory.ItemType;
        var oldIcon = _editingCategory.Icon;

        // Store new values
        var newName = ModalCategoryName.Trim();
        var newParentId = ModalParentCategory?.Id != string.Empty ? ModalParentCategory?.Id : null;
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim();
        var newItemType = ModalItemType;
        var newIcon = ModalSelectedIconOption?.Icon ?? "📦";

        // Update the category
        var categoryToEdit = _editingCategory;
        categoryToEdit.Name = newName;
        categoryToEdit.ParentId = newParentId;
        categoryToEdit.Description = newDescription;
        categoryToEdit.ItemType = newItemType;
        categoryToEdit.Icon = newIcon;

        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager?.RecordAction(new CategoryEditAction(
            $"Edit category '{newName}'",
            categoryToEdit,
            () =>
            {
                categoryToEdit.Name = oldName;
                categoryToEdit.ParentId = oldParentId;
                categoryToEdit.Description = oldDescription;
                categoryToEdit.ItemType = oldItemType;
                categoryToEdit.Icon = oldIcon;
                companyData.MarkAsModified();
                LoadCategories();
            },
            () =>
            {
                categoryToEdit.Name = newName;
                categoryToEdit.ParentId = newParentId;
                categoryToEdit.Description = newDescription;
                categoryToEdit.ItemType = newItemType;
                categoryToEdit.Icon = newIcon;
                companyData.MarkAsModified();
                LoadCategories();
            }));

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
            // Store child parent IDs for undo
            var children = companyData.Categories.Where(c => c.ParentId == category.Id).ToList();
            var childOriginalParents = children.ToDictionary(c => c.Id, c => c.ParentId);

            // Clear parent reference instead of deleting children
            foreach (var child in children)
            {
                child.ParentId = null;
            }

            var deletedCategory = category;
            companyData.Categories.Remove(category);
            companyData.MarkAsModified();

            // Record undo action
            App.UndoRedoManager?.RecordAction(new CategoryDeleteAction(
                $"Delete category '{deletedCategory.Name}'",
                deletedCategory,
                () =>
                {
                    // Undo: restore category and child parent references
                    companyData.Categories.Add(deletedCategory);
                    foreach (var kvp in childOriginalParents)
                    {
                        var child = companyData.Categories.FirstOrDefault(c => c.Id == kvp.Key);
                        if (child != null)
                        {
                            child.ParentId = kvp.Value;
                        }
                    }
                    companyData.MarkAsModified();
                    LoadCategories();
                },
                () =>
                {
                    // Redo: delete again
                    foreach (var kvp in childOriginalParents)
                    {
                        var child = companyData.Categories.FirstOrDefault(c => c.Id == kvp.Key);
                        if (child != null)
                        {
                            child.ParentId = null;
                        }
                    }
                    companyData.Categories.Remove(deletedCategory);
                    companyData.MarkAsModified();
                    LoadCategories();
                }));
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
/// Undoable action for adding a category.
/// </summary>
public class CategoryAddAction : IUndoableAction
{
    private readonly Category _category;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public CategoryAddAction(string description, Category category, Action undoAction, Action redoAction)
    {
        Description = description;
        _category = category;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing a category.
/// </summary>
public class CategoryEditAction : IUndoableAction
{
    private readonly Category _category;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public CategoryEditAction(string description, Category category, Action undoAction, Action redoAction)
    {
        Description = description;
        _category = category;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting a category.
/// </summary>
public class CategoryDeleteAction : IUndoableAction
{
    private readonly Category _category;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public CategoryDeleteAction(string description, Category category, Action undoAction, Action redoAction)
    {
        Description = description;
        _category = category;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
