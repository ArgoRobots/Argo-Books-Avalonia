using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Departments page.
/// </summary>
public partial class DepartmentsPageViewModel : ViewModelBase
{
    #region Search

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterDepartments();
    }

    #endregion

    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "Name";

    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.None;

    /// <summary>
    /// Sorts the departments list by the specified column.
    /// </summary>
    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
        {
            SortDirection = SortDirection switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.None,
                _ => SortDirection.Ascending
            };
        }
        else
        {
            SortColumn = column;
            SortDirection = SortDirection.Ascending;
        }
        FilterDepartments();
    }

    #endregion

    #region Pagination

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    /// <summary>
    /// Available page size options for the dropdown.
    /// </summary>
    public ObservableCollection<int> PageSizeOptions { get; } = [10, 25, 50, 100];

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        FilterDepartments();
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    [ObservableProperty]
    private string _paginationText = "0 departments";

    /// <summary>
    /// Page numbers for pagination display.
    /// </summary>
    public ObservableCollection<int> PageNumbers { get; } = [];

    /// <summary>
    /// Gets whether we can navigate to the previous page.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets whether we can navigate to the next page.
    /// </summary>
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    [RelayCommand]
    private void GoToPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            FilterDepartments();
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }
    }

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    [RelayCommand]
    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            FilterDepartments();
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }
    }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    [RelayCommand]
    private void GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages && page != CurrentPage)
        {
            CurrentPage = page;
            FilterDepartments();
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
        }
    }

    /// <summary>
    /// Updates the page numbers collection for pagination display.
    /// </summary>
    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        for (int i = 1; i <= TotalPages; i++)
        {
            PageNumbers.Add(i);
        }
    }

    /// <summary>
    /// Updates the pagination text to display item count.
    /// </summary>
    private void UpdatePaginationText(int totalItems)
    {
        PaginationText = totalItems == 1 ? "1 department" : $"{totalItems} departments";
    }

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalDepartments;

    [ObservableProperty]
    private int _totalEmployees;

    [ObservableProperty]
    private int _newThisMonth;

    [ObservableProperty]
    private int _activeDepartments;

    #endregion

    #region Departments Collections

    /// <summary>
    /// All departments (unfiltered).
    /// </summary>
    private readonly List<Department> _allDepartments = [];

    /// <summary>
    /// Departments for display (filtered).
    /// </summary>
    public ObservableCollection<DepartmentDisplayItem> Departments { get; } = [];

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
    private string _modalDepartmentName = string.Empty;

    [ObservableProperty]
    private string _modalDescription = string.Empty;

    [ObservableProperty]
    private DepartmentIconOption? _modalSelectedIcon;

    [ObservableProperty]
    private DepartmentColorOption? _modalSelectedColor;

    [ObservableProperty]
    private string? _modalError;

    /// <summary>
    /// The department being edited (null for add).
    /// </summary>
    private Department? _editingDepartment;

    /// <summary>
    /// The department being deleted.
    /// </summary>
    private DepartmentDisplayItem? _deletingDepartment;

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Available icons for dropdown.
    /// </summary>
    public ObservableCollection<DepartmentIconOption> AvailableIcons { get; } =
    [
        new("💰", "Dollar Sign"),
        new("⚙️", "Cogs"),
        new("📈", "Chart Line"),
        new("💻", "Laptop Code"),
        new("👥", "User Friends"),
        new("🎧", "Headset"),
        new("🏢", "Building"),
        new("💼", "Briefcase"),
        new("🚚", "Truck"),
        new("🛒", "Shopping Cart"),
        new("📦", "Box"),
        new("🔧", "Tools"),
        new("📊", "Analytics"),
        new("🎯", "Target")
    ];

    /// <summary>
    /// Available colors for dropdown.
    /// </summary>
    public ObservableCollection<DepartmentColorOption> AvailableColors { get; } =
    [
        new("blue", "Blue", "#3b82f6"),
        new("green", "Green", "#22c55e"),
        new("yellow", "Yellow", "#eab308"),
        new("purple", "Purple", "#a855f7"),
        new("red", "Red", "#ef4444"),
        new("cyan", "Cyan", "#06b6d4"),
        new("orange", "Orange", "#f97316"),
        new("pink", "Pink", "#ec4899")
    ];

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public DepartmentsPageViewModel()
    {
        // Set default selections
        _modalSelectedIcon = AvailableIcons.FirstOrDefault();
        _modalSelectedColor = AvailableColors.FirstOrDefault();
        LoadDepartments();

        // Subscribe to undo/redo state changes to refresh UI
        // This is necessary because a new ViewModel instance is created on each navigation,
        // but undo/redo actions capture the old ViewModel instance
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to shared modal events to refresh data
        if (App.DepartmentModalsViewModel != null)
        {
            App.DepartmentModalsViewModel.DepartmentSaved += OnDepartmentModalClosed;
            App.DepartmentModalsViewModel.DepartmentDeleted += OnDepartmentModalClosed;
        }
    }

    /// <summary>
    /// Handles department modal closed events by refreshing the departments.
    /// </summary>
    private void OnDepartmentModalClosed(object? sender, EventArgs e)
    {
        LoadDepartments();
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the departments.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadDepartments();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads departments from the company data.
    /// </summary>
    private void LoadDepartments()
    {
        _allDepartments.Clear();
        Departments.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Departments == null)
            return;

        _allDepartments.AddRange(companyData.Departments);
        UpdateStatistics();
        FilterDepartments();
    }

    /// <summary>
    /// Refreshes the departments from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshDepartments()
    {
        LoadDepartments();
    }

    /// <summary>
    /// Updates the statistics cards.
    /// </summary>
    private void UpdateStatistics()
    {
        var companyData = App.CompanyManager?.CompanyData;

        TotalDepartments = _allDepartments.Count;
        TotalEmployees = companyData?.Employees.Count ?? 0;

        // Count departments created this month
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        NewThisMonth = _allDepartments.Count(d => d.CreatedAt >= startOfMonth);

        // Count departments with at least one employee (active departments)
        var deptIdsWithEmployees = companyData?.Employees
            .Where(e => !string.IsNullOrEmpty(e.DepartmentId))
            .Select(e => e.DepartmentId)
            .Distinct()
            .ToHashSet() ?? [];
        ActiveDepartments = _allDepartments.Count(d => deptIdsWithEmployees.Contains(d.Id));
    }

    /// <summary>
    /// Filters departments based on search query.
    /// </summary>
    private void FilterDepartments()
    {
        Departments.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var departments = _allDepartments.AsEnumerable();

        // Apply search filter using Levenshtein distance
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            departments = departments
                .Select(d => new
                {
                    Department = d,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, d.Name),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, d.Description ?? string.Empty)
                })
                .Where(x => x.NameScore >= 0 || x.DescScore >= 0)
                .OrderByDescending(x => Math.Max(x.NameScore, x.DescScore))
                .Select(x => x.Department);
        }

        // Create display items with employee counts
        var displayItems = departments.Select(dept => CreateDisplayItem(dept, companyData)).ToList();

        // Apply sorting (only if not searching, since search has its own relevance sorting)
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            if (SortDirection != SortDirection.None)
            {
                displayItems = SortColumn switch
                {
                    "Name" => SortDirection == SortDirection.Ascending
                        ? displayItems.OrderBy(d => d.Name).ToList()
                        : displayItems.OrderByDescending(d => d.Name).ToList(),
                    "Description" => SortDirection == SortDirection.Ascending
                        ? displayItems.OrderBy(d => d.Description).ToList()
                        : displayItems.OrderByDescending(d => d.Description).ToList(),
                    "Employees" => SortDirection == SortDirection.Ascending
                        ? displayItems.OrderBy(d => d.EmployeeCount).ToList()
                        : displayItems.OrderByDescending(d => d.EmployeeCount).ToList(),
                    _ => displayItems.OrderBy(d => d.Name).ToList()
                };
            }
            else if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                // Default sort by name when not searching
                displayItems = displayItems.OrderBy(d => d.Name).ToList();
            }
        }

        // Calculate pagination
        var totalItems = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

        // Ensure current page is valid
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;
        if (CurrentPage < 1)
            CurrentPage = 1;

        UpdatePageNumbers();
        UpdatePaginationText(totalItems);
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));

        // Apply pagination
        var pagedItems = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        // Add paginated items to collection
        foreach (var item in pagedItems)
        {
            Departments.Add(item);
        }
    }

    /// <summary>
    /// Creates a display item for a department.
    /// </summary>
    private DepartmentDisplayItem CreateDisplayItem(Department department, CompanyData? companyData)
    {
        // Count employees in this department
        var employeeCount = companyData?.Employees.Count(e => e.DepartmentId == department.Id) ?? 0;

        return new DepartmentDisplayItem
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description ?? string.Empty,
            Icon = department.Icon,
            IconColor = department.IconColor,
            EmployeeCount = employeeCount,
            CreatedAt = department.CreatedAt
        };
    }

    #endregion

    #region Add Department

    /// <summary>
    /// Opens the Add Department modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.DepartmentModalsViewModel?.OpenAddModal();
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
    /// Saves a new department.
    /// </summary>
    [RelayCommand]
    private void SaveNewDepartment()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Generate new ID
        companyData.IdCounters.Department++;
        var newId = $"DEP-{companyData.IdCounters.Department:D3}";

        var newDepartment = new Department
        {
            Id = newId,
            Name = ModalDepartmentName.Trim(),
            Description = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim(),
            Icon = ModalSelectedIcon?.Icon ?? "🏢",
            IconColor = ModalSelectedColor?.Value ?? "blue",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Departments.Add(newDepartment);
        companyData.MarkAsModified();

        // Record undo action
        var departmentToUndo = newDepartment;
        App.UndoRedoManager?.RecordAction(new DepartmentAddAction(
            $"Add department '{newDepartment.Name}'",
            departmentToUndo,
            () =>
            {
                companyData.Departments.Remove(departmentToUndo);
                companyData.MarkAsModified();
                LoadDepartments();
            },
            () =>
            {
                companyData.Departments.Add(departmentToUndo);
                companyData.MarkAsModified();
                LoadDepartments();
            }));

        // Reload and close
        LoadDepartments();
        CloseAddModal();
    }

    #endregion

    #region Edit Department

    /// <summary>
    /// Opens the Edit Department modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(DepartmentDisplayItem? item)
    {
        App.DepartmentModalsViewModel?.OpenEditModal(item);
    }

    /// <summary>
    /// Closes the Edit modal.
    /// </summary>
    [RelayCommand]
    private void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingDepartment = null;
        ClearModalFields();
    }

    /// <summary>
    /// Saves changes to an existing department.
    /// </summary>
    [RelayCommand]
    private void SaveEditedDepartment()
    {
        if (!ValidateModal() || _editingDepartment == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Store old values for undo
        var oldName = _editingDepartment.Name;
        var oldDescription = _editingDepartment.Description;
        var oldIcon = _editingDepartment.Icon;
        var oldIconColor = _editingDepartment.IconColor;

        // Store new values
        var newName = ModalDepartmentName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim();
        var newIcon = ModalSelectedIcon?.Icon ?? "🏢";
        var newIconColor = ModalSelectedColor?.Value ?? "blue";

        // Update the department
        var departmentToEdit = _editingDepartment;
        departmentToEdit.Name = newName;
        departmentToEdit.Description = newDescription;
        departmentToEdit.Icon = newIcon;
        departmentToEdit.IconColor = newIconColor;

        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager?.RecordAction(new DepartmentEditAction(
            $"Edit department '{newName}'",
            departmentToEdit,
            () =>
            {
                departmentToEdit.Name = oldName;
                departmentToEdit.Description = oldDescription;
                departmentToEdit.Icon = oldIcon;
                departmentToEdit.IconColor = oldIconColor;
                companyData.MarkAsModified();
                LoadDepartments();
            },
            () =>
            {
                departmentToEdit.Name = newName;
                departmentToEdit.Description = newDescription;
                departmentToEdit.Icon = newIcon;
                departmentToEdit.IconColor = newIconColor;
                companyData.MarkAsModified();
                LoadDepartments();
            }));

        // Reload and close
        LoadDepartments();
        CloseEditModal();
    }

    #endregion

    #region Delete Department

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(DepartmentDisplayItem? item)
    {
        App.DepartmentModalsViewModel?.OpenDeleteConfirm(item);
    }

    /// <summary>
    /// Closes the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingDepartment = null;
    }

    /// <summary>
    /// Confirms and deletes the department.
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
    {
        if (_deletingDepartment == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var department = companyData.Departments.FirstOrDefault(d => d.Id == _deletingDepartment.Id);
        if (department != null)
        {
            // Store employees with this department for undo
            var employeesInDept = companyData.Employees.Where(e => e.DepartmentId == department.Id).ToList();
            var originalDeptIds = employeesInDept.ToDictionary(e => e.Id, e => e.DepartmentId);

            // Clear department reference from employees
            foreach (var emp in employeesInDept)
            {
                emp.DepartmentId = null;
            }

            var deletedDepartment = department;
            companyData.Departments.Remove(department);
            companyData.MarkAsModified();

            // Record undo action
            App.UndoRedoManager?.RecordAction(new DepartmentDeleteAction(
                $"Delete department '{deletedDepartment.Name}'",
                deletedDepartment,
                () =>
                {
                    // Undo: restore department and employee department references
                    companyData.Departments.Add(deletedDepartment);
                    foreach (var kvp in originalDeptIds)
                    {
                        var emp = companyData.Employees.FirstOrDefault(e => e.Id == kvp.Key);
                        if (emp != null)
                        {
                            emp.DepartmentId = kvp.Value;
                        }
                    }
                    companyData.MarkAsModified();
                    LoadDepartments();
                },
                () =>
                {
                    // Redo: delete again
                    foreach (var kvp in originalDeptIds)
                    {
                        var emp = companyData.Employees.FirstOrDefault(e => e.Id == kvp.Key);
                        if (emp != null)
                        {
                            emp.DepartmentId = null;
                        }
                    }
                    companyData.Departments.Remove(deletedDepartment);
                    companyData.MarkAsModified();
                    LoadDepartments();
                }));
        }

        LoadDepartments();
        CloseDeleteConfirm();
    }

    /// <summary>
    /// Gets the name of the department being deleted (for display in confirmation).
    /// </summary>
    public string DeletingDepartmentName => _deletingDepartment?.Name ?? string.Empty;

    /// <summary>
    /// Gets the employee count of the department being deleted.
    /// </summary>
    public int DeletingDepartmentEmployeeCount => _deletingDepartment?.EmployeeCount ?? 0;

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalDepartmentName = string.Empty;
        ModalDescription = string.Empty;
        ModalSelectedIcon = AvailableIcons.FirstOrDefault();
        ModalSelectedColor = AvailableColors.FirstOrDefault();
        ModalError = null;
    }

    private bool ValidateModal()
    {
        ModalError = null;

        if (string.IsNullOrWhiteSpace(ModalDepartmentName))
        {
            ModalError = "Department name is required.";
            return false;
        }

        // Check for duplicate names
        var existingWithSameName = _allDepartments.Any(d =>
            d.Name.Equals(ModalDepartmentName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (_editingDepartment == null || d.Id != _editingDepartment.Id));

        if (existingWithSameName)
        {
            ModalError = "A department with this name already exists.";
            return false;
        }

        return true;
    }

    #endregion
}

/// <summary>
/// Display model for departments in the UI.
/// </summary>
public partial class DepartmentDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = "🏢";

    [ObservableProperty]
    private string _iconColor = "blue";

    [ObservableProperty]
    private int _employeeCount;

    [ObservableProperty]
    private DateTime _createdAt;

    /// <summary>
    /// Display string for employee count.
    /// </summary>
    public string EmployeeCountDisplay => EmployeeCount == 1 ? "1 employee" : $"{EmployeeCount} employees";
}

/// <summary>
/// Represents an icon option for dropdown.
/// </summary>
public class DepartmentIconOption
{
    public string Icon { get; }
    public string Name { get; }
    public string DisplayName => $"{Icon} {Name}";

    public DepartmentIconOption(string icon, string name)
    {
        Icon = icon;
        Name = name;
    }
}

/// <summary>
/// Represents a color option for dropdown.
/// </summary>
public class DepartmentColorOption
{
    public string Value { get; }
    public string Name { get; }
    public string HexColor { get; }

    public DepartmentColorOption(string value, string name, string hexColor)
    {
        Value = value;
        Name = name;
        HexColor = hexColor;
    }
}

/// <summary>
/// Undoable action for adding a department.
/// </summary>
public class DepartmentAddAction : IUndoableAction
{
    private readonly Department _department;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public DepartmentAddAction(string description, Department department, Action undoAction, Action redoAction)
    {
        Description = description;
        _department = department;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing a department.
/// </summary>
public class DepartmentEditAction : IUndoableAction
{
    private readonly Department _department;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public DepartmentEditAction(string description, Department department, Action undoAction, Action redoAction)
    {
        Description = description;
        _department = department;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting a department.
/// </summary>
public class DepartmentDeleteAction : IUndoableAction
{
    private readonly Department _department;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public DepartmentDeleteAction(string description, Department department, Action undoAction, Action redoAction)
    {
        Description = description;
        _department = department;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
