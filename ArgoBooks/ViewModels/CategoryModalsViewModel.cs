using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for category modals, shared between CategoriesPage and AppShell.
/// </summary>
public partial class CategoryModalsViewModel : ObservableObject
{
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

    [ObservableProperty]
    private bool _deleteSubcategories;

    private Category? _editingCategory;
    private CategoryDisplayItem? _addingSubCategoryParent;
    private CategoryDisplayItem? _movingCategory;
    private bool _isExpensesTab = true;

    #endregion

    #region Move Modal Properties

    [ObservableProperty]
    private CategoryDisplayItem? _moveTargetCategory;

    [ObservableProperty]
    private string? _moveError;

    public ObservableCollection<CategoryDisplayItem> MoveTargetCategories { get; } = [];

    public string MovingCategoryName => _movingCategory?.Name ?? string.Empty;

    #endregion

    #region Sub-Category Properties

    public bool IsAddingSubCategory => _addingSubCategoryParent != null;
    public string AddingSubCategoryParentName => _addingSubCategoryParent?.Name ?? string.Empty;

    /// <summary>
    /// Gets the translated title for the Add Category modal.
    /// </summary>
    public string AddModalTitle => IsAddingSubCategory
        ? LanguageService.Instance.Translate("Add Sub-Category")
        : LanguageService.Instance.Translate("Add Category");

    #endregion


    #region Dropdown Options

    public ObservableCollection<string> ItemTypes { get; } = ["Product", "Service"];

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

    #region Events

    public event EventHandler? CategorySaved;
    public event EventHandler? CategoryDeleted;

    #endregion

    public CategoryModalsViewModel()
    {
        _modalSelectedIconOption = AvailableIcons.FirstOrDefault();
    }

    #region Add Category

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingCategory = null;
        _addingSubCategoryParent = null;
        ClearModalFields();
        OnPropertyChanged(nameof(IsAddingSubCategory));
        OnPropertyChanged(nameof(AddingSubCategoryParentName));
        OnPropertyChanged(nameof(AddModalTitle));
        IsAddModalOpen = true;
    }

    public void OpenAddModal(bool isExpensesTab)
    {
        _isExpensesTab = isExpensesTab;
        OpenAddModal();
    }

    public void OpenAddSubCategoryModal(CategoryDisplayItem? parent, bool isExpensesTab)
    {
        if (parent == null) return;
        _isExpensesTab = isExpensesTab;
        _editingCategory = null;
        ClearModalFields();
        _addingSubCategoryParent = parent;
        OnPropertyChanged(nameof(IsAddingSubCategory));
        OnPropertyChanged(nameof(AddingSubCategoryParentName));
        OnPropertyChanged(nameof(AddModalTitle));
        IsAddModalOpen = true;
    }

    [RelayCommand]
    public void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    [RelayCommand]
    public void SaveNewCategory()
    {
        if (!ValidateModal()) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        companyData.IdCounters.Category++;
        var typePrefix = _isExpensesTab ? "PUR" : "SAL";
        var newId = $"CAT-{typePrefix}-{companyData.IdCounters.Category:D3}";
        var parentId = IsAddingSubCategory ? _addingSubCategoryParent?.Id : null;

        var newCategory = new Category
        {
            Id = newId,
            Name = ModalCategoryName.Trim(),
            Type = _isExpensesTab ? CategoryType.Purchase : CategoryType.Sales,
            ParentId = parentId,
            Description = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim(),
            ItemType = ModalItemType,
            Color = "#4A90D9",
            Icon = ModalSelectedIconOption?.Icon ?? "📦",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Categories.Add(newCategory);
        companyData.MarkAsModified();

        var categoryToUndo = newCategory;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Add category '{newCategory.Name}'",
            () => { companyData.Categories.Remove(categoryToUndo); companyData.MarkAsModified(); CategorySaved?.Invoke(this, EventArgs.Empty); },
            () => { companyData.Categories.Add(categoryToUndo); companyData.MarkAsModified(); CategorySaved?.Invoke(this, EventArgs.Empty); }));

        CategorySaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Category

    public void OpenEditModal(CategoryDisplayItem? item, bool isExpensesTab)
    {
        if (item == null) return;
        _isExpensesTab = isExpensesTab;

        var companyData = App.CompanyManager?.CompanyData;
        var category = companyData?.Categories.FirstOrDefault(c => c.Id == item.Id);
        if (category == null) return;

        _editingCategory = category;
        ModalCategoryName = category.Name;
        ModalDescription = category.Description ?? string.Empty;
        ModalItemType = category.ItemType;
        ModalSelectedIconOption = AvailableIcons.FirstOrDefault(i => i.Icon == category.Icon) ?? AvailableIcons.First();
        ModalError = null;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingCategory = null;
        ClearModalFields();
    }

    [RelayCommand]
    public void SaveEditedCategory()
    {
        if (!ValidateModal() || _editingCategory == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var oldName = _editingCategory.Name;
        var oldDescription = _editingCategory.Description;
        var oldItemType = _editingCategory.ItemType;
        var oldIcon = _editingCategory.Icon;

        var newName = ModalCategoryName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim();
        var newItemType = ModalItemType;
        var newIcon = ModalSelectedIconOption?.Icon ?? "📦";

        // Check if anything actually changed
        var hasChanges = oldName != newName ||
                         oldDescription != newDescription ||
                         oldItemType != newItemType ||
                         oldIcon != newIcon;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

        var categoryToEdit = _editingCategory;
        categoryToEdit.Name = newName;
        categoryToEdit.Description = newDescription;
        categoryToEdit.ItemType = newItemType;
        categoryToEdit.Icon = newIcon;
        companyData.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit category '{newName}'",
            () => { categoryToEdit.Name = oldName; categoryToEdit.Description = oldDescription; categoryToEdit.ItemType = oldItemType; categoryToEdit.Icon = oldIcon; companyData.MarkAsModified(); CategorySaved?.Invoke(this, EventArgs.Empty); },
            () => { categoryToEdit.Name = newName; categoryToEdit.Description = newDescription; categoryToEdit.ItemType = newItemType; categoryToEdit.Icon = newIcon; companyData.MarkAsModified(); CategorySaved?.Invoke(this, EventArgs.Empty); }));

        CategorySaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Category

    public async void OpenDeleteConfirm(CategoryDisplayItem? item)
    {
        if (item == null) return;

        var companyData = App.CompanyManager?.CompanyData;

        var category = companyData?.Categories.FirstOrDefault(c => c.Id == item.Id);
        if (category == null) return;

        var children = companyData?.Categories.Where(c => c.ParentId == category.Id).ToList();
        var hasChildren = children?.Count > 0;

        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        // If category has children, ask about subcategories
        var deleteSubcategories = false;
        if (hasChildren)
        {
            var subResult = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Delete Category",
                Message = $"This category has {children?.Count} subcategories.\n\nDo you want to delete them as well, or move them to the top level?",
                PrimaryButtonText = "Delete All",
                SecondaryButtonText = "Move to Top Level",
                CancelButtonText = "Cancel",
                IsPrimaryDestructive = true
            });

            if (subResult == ConfirmationResult.Cancel || subResult == ConfirmationResult.None)
                return;

            deleteSubcategories = subResult == ConfirmationResult.Primary;
        }
        else
        {
            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Delete Category",
                Message = $"Are you sure you want to delete this category?\n\n{item.Name}",
                PrimaryButtonText = "Delete",
                CancelButtonText = "Cancel",
                IsPrimaryDestructive = true
            });

            if (result != ConfirmationResult.Primary)
                return;
        }

        var childOriginalParents = children?.ToDictionary(c => c.Id, c => c.ParentId);
        var deletedChildren = new List<Category>();
        var shouldDeleteSubcategories = deleteSubcategories;

        if (shouldDeleteSubcategories)
        {
            deletedChildren.AddRange(children ?? []);
            foreach (var child in children ?? []) companyData?.Categories.Remove(child);
        }
        else
        {
            foreach (var child in children ?? []) child.ParentId = null;
        }
        
        var deletedCategory = category;
        companyData?.Categories.Remove(category);
        companyData?.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Delete category '{deletedCategory.Name}'",
            () =>
            {
                companyData?.Categories.Add(deletedCategory);
                if (shouldDeleteSubcategories) { foreach (var child in deletedChildren) companyData?.Categories.Add(child); }
                else { foreach (var kvp in childOriginalParents ?? []) { var child = companyData?.Categories.FirstOrDefault(c => c.Id == kvp.Key);
                    child?.ParentId = kvp.Value;
                } }
                companyData?.MarkAsModified();
                CategoryDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                if (shouldDeleteSubcategories) { foreach (var child in deletedChildren) companyData?.Categories.Remove(child); }
                else { foreach (var kvp in childOriginalParents ?? []) { var child = companyData?.Categories.FirstOrDefault(c => c.Id == kvp.Key);
                    child?.ParentId = null;
                } }
                companyData?.Categories.Remove(deletedCategory);
                companyData?.MarkAsModified();
                CategoryDeleted?.Invoke(this, EventArgs.Empty);
            }));

        CategoryDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Move Category

    public void OpenMoveModal(CategoryDisplayItem? item, bool isExpensesTab)
    {
        if (item == null) return;
        _isExpensesTab = isExpensesTab;
        _movingCategory = item;
        MoveError = null;
        UpdateMoveTargetCategories();
        OnPropertyChanged(nameof(MovingCategoryName));
        IsMoveModalOpen = true;
    }

    [RelayCommand]
    public void CloseMoveModal()
    {
        IsMoveModalOpen = false;
        _movingCategory = null;
        MoveTargetCategory = null;
        MoveError = null;
    }

    [RelayCommand]
    public void ConfirmMove()
    {
        if (_movingCategory == null || MoveTargetCategory == null)
        {
            MoveError = "Please select a target category.";
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;

        var category = companyData?.Categories.FirstOrDefault(c => c.Id == _movingCategory.Id);
        if (category == null) return;

        var oldParentId = category.ParentId;
        var newParentId = string.IsNullOrEmpty(MoveTargetCategory.Id) ? null : MoveTargetCategory.Id;

        if (oldParentId == newParentId)
        {
            MoveError = "Category is already under this parent.";
            return;
        }

        category.ParentId = newParentId;
        companyData?.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Move category '{category.Name}'",
            () => { category.ParentId = oldParentId; companyData?.MarkAsModified(); CategorySaved?.Invoke(this, EventArgs.Empty); },
            () => { category.ParentId = newParentId; companyData?.MarkAsModified(); CategorySaved?.Invoke(this, EventArgs.Empty); }));

        CategorySaved?.Invoke(this, EventArgs.Empty);
        CloseMoveModal();
    }

    private void UpdateMoveTargetCategories()
    {
        MoveTargetCategories.Clear();
        MoveTargetCategories.Add(new CategoryDisplayItem { Id = string.Empty, Name = "None (Make Top-Level)" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var targetType = _isExpensesTab ? CategoryType.Purchase : CategoryType.Sales;
        var topLevelCategories = companyData.Categories
            .Where(c => c.Type == targetType && string.IsNullOrEmpty(c.ParentId))
            .OrderBy(c => c.Name);

        foreach (var cat in topLevelCategories)
        {
            if (_movingCategory != null && cat.Id == _movingCategory.Id) continue;
            MoveTargetCategories.Add(new CategoryDisplayItem { Id = cat.Id, Name = cat.Name, Icon = cat.Icon });
        }

        if (_movingCategory?.ParentId != null)
        {
            MoveTargetCategory = MoveTargetCategories.FirstOrDefault(c => c.Id == _movingCategory.ParentId);
        }
    }

    #endregion

    #region Helpers

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
        ModalError = null;
        ModalCategoryNameError = null;
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalCategoryName))
        {
            ModalCategoryNameError = "Category name is required.";
            isValid = false;
        }
        else
        {
            var companyData = App.CompanyManager?.CompanyData;
            var targetType = _isExpensesTab ? CategoryType.Purchase : CategoryType.Sales;
            var existingWithSameName = companyData?.Categories.Any(c =>
                c.Type == targetType &&
                c.Name.Equals(ModalCategoryName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingCategory == null || c.Id != _editingCategory.Id)) ?? false;

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
