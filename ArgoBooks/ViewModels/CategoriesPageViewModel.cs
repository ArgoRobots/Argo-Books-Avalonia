using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Categories page.
/// </summary>
public partial class CategoriesPageViewModel : SortablePageViewModelBase
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
        CurrentPage = 1;
        FilterCategories();
    }

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 categories";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterCategories();

    /// <inheritdoc />
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
            totalCount, CurrentPage, PageSize, TotalPages, "category", "categories");
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

    [ObservableProperty]
    private bool _isMoveModalOpen;

    #endregion

    #region Column Visibility and Widths

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    [ObservableProperty]
    private double _columnMenuX;

    [ObservableProperty]
    private double _columnMenuY;

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public CategoriesTableColumnWidths ColumnWidths => App.CategoriesColumnWidths;

    [ObservableProperty]
    private bool _showNameColumn = true;

    [ObservableProperty]
    private bool _showParentColumn = true;

    [ObservableProperty]
    private bool _showDescriptionColumn = true;

    [ObservableProperty]
    private bool _showTypeColumn = true;

    [ObservableProperty]
    private bool _showProductCountColumn = true;

    partial void OnShowNameColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Name", value);
    partial void OnShowParentColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Parent", value);
    partial void OnShowDescriptionColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Description", value);
    partial void OnShowTypeColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Type", value);
    partial void OnShowProductCountColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("ProductCount", value);

    [RelayCommand]
    private void ToggleColumnMenu()
    {
        IsColumnMenuOpen = !IsColumnMenuOpen;
    }

    [RelayCommand]
    private void CloseColumnMenu()
    {
        IsColumnMenuOpen = false;
    }

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalCategoryName = string.Empty;

    [ObservableProperty]
    private string _modalDescription = string.Empty;

    [ObservableProperty]
    private string _modalItemType = "Product";

    [ObservableProperty]
    private IconOption? _modalSelectedIconOption;

    [ObservableProperty]
    private string? _modalError;

    [ObservableProperty]
    private string? _modalCategoryNameError;

    /// <summary>
    /// The category being edited (null for add).
    /// </summary>
    private Category? _editingCategory;

    /// <summary>
    /// The category being deleted.
    /// </summary>
    private CategoryDisplayItem? _deletingCategory;

    /// <summary>
    /// Whether to also delete subcategories when deleting a parent category.
    /// </summary>
    [ObservableProperty]
    private bool _deleteSubcategories;

    /// <summary>
    /// The parent category when adding a sub-category.
    /// </summary>
    private CategoryDisplayItem? _addingSubCategoryParent;

    /// <summary>
    /// The category being moved.
    /// </summary>
    private CategoryDisplayItem? _movingCategory;

    #endregion

    #region Sub-Category Add Properties

    /// <summary>
    /// Gets whether we're adding a sub-category (vs top-level category).
    /// </summary>
    public bool IsAddingSubCategory => _addingSubCategoryParent != null;

    /// <summary>
    /// Gets the name of the parent category when adding a sub-category.
    /// </summary>
    public string AddingSubCategoryParentName => _addingSubCategoryParent?.Name ?? string.Empty;

    #endregion

    #region Move Modal Properties

    [ObservableProperty]
    private CategoryDisplayItem? _moveTargetCategory;

    [ObservableProperty]
    private string? _moveError;

    /// <summary>
    /// Available target categories for moving.
    /// </summary>
    public ObservableCollection<CategoryDisplayItem> MoveTargetCategories { get; } = [];

    /// <summary>
    /// Gets the name of the category being moved.
    /// </summary>
    public string MovingCategoryName => _movingCategory?.Name ?? string.Empty;

    #endregion

    #region Dropdown Options

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

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to shared modal events to refresh data
        if (App.CategoryModalsViewModel != null)
        {
            App.CategoryModalsViewModel.CategorySaved += OnCategoryModalClosed;
            App.CategoryModalsViewModel.CategoryDeleted += OnCategoryModalClosed;
        }
    }

    /// <summary>
    /// Handles category modal closed events by refreshing the categories.
    /// </summary>
    private void OnCategoryModalClosed(object? sender, EventArgs e)
    {
        LoadCategories();
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the categories.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
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
    /// Filters and organizes categories based on current tab, search, sorting, and pagination.
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

        // Build display items list (flat, with parent-child info)
        var displayItems = new List<CategoryDisplayItem>();

        // Build hierarchy - parent categories first, then children
        var parentCategories = categories.Where(c => string.IsNullOrEmpty(c.ParentId)).ToList();

        foreach (var parent in parentCategories)
        {
            var childCount = CountChildren(parent.Id);
            displayItems.Add(CreateDisplayItem(parent, null, childCount));

            // Add children
            var children = categories.Where(c => c.ParentId == parent.Id);
            foreach (var child in children)
            {
                var grandchildCount = CountChildren(child.Id);
                displayItems.Add(CreateDisplayItem(child, parent.Name, grandchildCount, isChild: true));
            }
        }

        // Add orphaned categories (have a parent ID that doesn't exist)
        var orphans = categories.Where(c =>
            !string.IsNullOrEmpty(c.ParentId) &&
            !categories.Any(p => p.Id == c.ParentId));

        foreach (var orphan in orphans)
        {
            var childCount = CountChildren(orphan.Id);
            displayItems.Add(CreateDisplayItem(orphan, "Unknown", childCount));
        }

        // Apply sorting
        if (SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<CategoryDisplayItem, object?>>
                {
                    ["Name"] = x => x.Name,
                    ["Parent"] = x => x.ParentName,
                    ["Description"] = x => x.Description,
                    ["Type"] = x => x.ItemType,
                    ["ProductCount"] = x => x.ProductCount
                });
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination
        var pagedItems = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedItems)
        {
            targetCollection.Add(item);
        }

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

    #endregion

    #region Add Category

    /// <summary>
    /// Opens the Add Category modal (for top-level category).
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.CategoryModalsViewModel?.OpenAddModal(IsExpensesTabSelected);
    }

    /// <summary>
    /// Opens the Add Category modal for adding a sub-category under a parent.
    /// </summary>
    [RelayCommand]
    private void OpenAddSubCategoryModal(CategoryDisplayItem? parent)
    {
        App.CategoryModalsViewModel?.OpenAddSubCategoryModal(parent, IsExpensesTabSelected);
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

        // Use the sub-category parent if adding a sub-category
        var parentId = IsAddingSubCategory ? _addingSubCategoryParent?.Id : null;

        var newCategory = new Category
        {
            Id = newId,
            Name = ModalCategoryName.Trim(),
            Type = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales,
            ParentId = parentId,
            Description = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim(),
            ItemType = ModalItemType,
            Color = "#4A90D9",
            Icon = ModalSelectedIconOption?.Icon ?? "📦",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Categories.Add(newCategory);
        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Add category '{newCategory.Name}'",
            () =>
            {
                companyData.Categories.Remove(newCategory);
                companyData.MarkAsModified();
                LoadCategories();
            },
            () =>
            {
                companyData.Categories.Add(newCategory);
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
        App.CategoryModalsViewModel?.OpenEditModal(item, IsExpensesTabSelected);
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
        var oldDescription = _editingCategory.Description;
        var oldItemType = _editingCategory.ItemType;
        var oldIcon = _editingCategory.Icon;

        // Store new values (parent is changed via Move, not Edit)
        var newName = ModalCategoryName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim();
        var newItemType = ModalItemType;
        var newIcon = ModalSelectedIconOption?.Icon ?? "📦";

        // Update the category (keep parent unchanged)
        var categoryToEdit = _editingCategory;
        categoryToEdit.Name = newName;
        categoryToEdit.Description = newDescription;
        categoryToEdit.ItemType = newItemType;
        categoryToEdit.Icon = newIcon;

        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit category '{newName}'",
            () =>
            {
                categoryToEdit.Name = oldName;
                categoryToEdit.Description = oldDescription;
                categoryToEdit.ItemType = oldItemType;
                categoryToEdit.Icon = oldIcon;
                companyData.MarkAsModified();
                LoadCategories();
            },
            () =>
            {
                categoryToEdit.Name = newName;
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
        App.CategoryModalsViewModel?.OpenDeleteConfirm(item);
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
            // Store child categories for undo
            var children = companyData.Categories.Where(c => c.ParentId == category.Id).ToList();
            var childOriginalParents = children.ToDictionary(c => c.Id, c => c.ParentId);
            var deletedChildren = new List<Category>();
            var shouldDeleteSubcategories = DeleteSubcategories;

            if (shouldDeleteSubcategories)
            {
                // Delete subcategories
                deletedChildren.AddRange(children);
                foreach (var child in children)
                {
                    companyData.Categories.Remove(child);
                }
            }
            else
            {
                // Clear parent reference - subcategories become top-level
                foreach (var child in children)
                {
                    child.ParentId = null;
                }
            }

            var deletedCategory = category;
            companyData.Categories.Remove(category);
            companyData.MarkAsModified();

            // Record undo action
            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Delete category '{deletedCategory.Name}'",
                () =>
                {
                    // Undo: restore category
                    companyData.Categories.Add(deletedCategory);

                    if (shouldDeleteSubcategories)
                    {
                        // Restore deleted children
                        foreach (var child in deletedChildren)
                        {
                            companyData.Categories.Add(child);
                        }
                    }
                    else
                    {
                        // Restore child parent references
                        foreach (var kvp in childOriginalParents)
                        {
                            var child = companyData.Categories.FirstOrDefault(c => c.Id == kvp.Key);
                            child?.ParentId = kvp.Value;
                        }
                    }
                    companyData.MarkAsModified();
                    LoadCategories();
                },
                () =>
                {
                    // Redo: delete again
                    if (shouldDeleteSubcategories)
                    {
                        foreach (var child in deletedChildren)
                        {
                            companyData.Categories.Remove(child);
                        }
                    }
                    else
                    {
                        foreach (var kvp in childOriginalParents)
                        {
                            var child = companyData.Categories.FirstOrDefault(c => c.Id == kvp.Key);
                            child?.ParentId = null;
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

    /// <summary>
    /// Gets whether the category being deleted has child categories.
    /// </summary>
    public bool DeletingCategoryHasChildren
    {
        get
        {
            if (_deletingCategory == null)
                return false;

            var companyData = App.CompanyManager?.CompanyData;
            if (companyData == null)
                return false;

            return companyData.Categories.Any(c => c.ParentId == _deletingCategory.Id);
        }
    }

    #endregion

    #region Move Category

    /// <summary>
    /// Opens the Move Category modal.
    /// </summary>
    [RelayCommand]
    private void OpenMoveModal(CategoryDisplayItem? item)
    {
        App.CategoryModalsViewModel?.OpenMoveModal(item, IsExpensesTabSelected);
    }

    /// <summary>
    /// Closes the Move modal.
    /// </summary>
    [RelayCommand]
    private void CloseMoveModal()
    {
        IsMoveModalOpen = false;
        _movingCategory = null;
        MoveTargetCategory = null;
        MoveError = null;
    }

    /// <summary>
    /// Confirms and moves the category to the new parent.
    /// </summary>
    [RelayCommand]
    private void ConfirmMove()
    {
        if (_movingCategory == null || MoveTargetCategory == null)
        {
            MoveError = "Please select a target category.".Translate().Translate();
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;

        var category = companyData?.Categories.FirstOrDefault(c => c.Id == _movingCategory.Id);
        if (category == null)
            return;

        // Store old parent for undo
        var oldParentId = category.ParentId;
        var newParentId = string.IsNullOrEmpty(MoveTargetCategory.Id) ? null : MoveTargetCategory.Id;

        // Don't move if same parent
        if (oldParentId == newParentId)
        {
            MoveError = "Category is already under this parent.".Translate().Translate();
            return;
        }

        // Update parent
        category.ParentId = newParentId;
        companyData?.MarkAsModified();

        // Record undo action
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Move category '{category.Name}'",
            () =>
            {
                category.ParentId = oldParentId;
                companyData?.MarkAsModified();
                LoadCategories();
            },
            () =>
            {
                category.ParentId = newParentId;
                companyData?.MarkAsModified();
                LoadCategories();
            }));

        LoadCategories();
        CloseMoveModal();
    }

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalCategoryName = string.Empty;
        ModalDescription = string.Empty;
        ModalItemType = "Product";
        ModalSelectedIconOption = AvailableIcons.FirstOrDefault();
        ModalError = null;
        ModalCategoryNameError = null;
        _addingSubCategoryParent = null;
    }

    private bool ValidateModal()
    {
        // Clear all errors first
        ModalError = null;
        ModalCategoryNameError = null;

        var isValid = true;

        // Validate category name (required)
        if (string.IsNullOrWhiteSpace(ModalCategoryName))
        {
            ModalCategoryNameError = "Category name is required.";
            isValid = false;
        }
        else
        {
            // Check for duplicate names within the same type
            var targetType = IsExpensesTabSelected ? CategoryType.Purchase : CategoryType.Sales;
            var existingWithSameName = _allCategories.Any(c =>
                c.Type == targetType &&
                c.Name.Equals(ModalCategoryName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingCategory == null || c.Id != _editingCategory.Id));

            if (existingWithSameName)
            {
                ModalCategoryNameError = "A category with this name already exists.";
                isValid = false;
            }
        }

        return isValid;
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
public class IconOption(string icon, string name)
{
    public string Icon { get; } = icon;
    public string Name { get; } = name;
    public string DisplayName => $"{Icon} {Name}";
}
