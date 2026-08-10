using System.Collections.ObjectModel;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Services;
using ArgoBooks.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Employees page, the list of people on the payroll.
///
/// Archived employees are hidden by default but never deleted, because a T4 must still be
/// produceable for someone who left part way through the year.
/// </summary>
public partial class EmployeesPageViewModel : SortablePageViewModelBase
{
    private readonly List<Employee> _all = [];

    public ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    public EmployeesTableColumnWidths ColumnWidths => App.EmployeesColumnWidths;

    public ObservableCollection<EmployeeDisplayItem> Employees { get; } = [];

    #region Statistics

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private string _annualPayroll = "$0";

    [ObservableProperty]
    private int _archivedCount;

    #endregion

    #region Filters

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _showArchived;

    partial void OnSearchQueryChanged(string value) => Filter();

    partial void OnShowArchivedChanged(bool value)
    {
        Filter();
        OnPropertyChanged(nameof(HasNoEmployees));
    }

    #endregion

    #region Empty states

    /// <summary>True when the company has no employees at all, so the page invites adding one.</summary>
    public bool HasNoEmployees => _all.Count == 0;

    /// <summary>True when a search or filter hides everything, which is a different message.</summary>
    public bool HasNoMatches => _all.Count > 0 && Employees.Count == 0;

    #endregion

    public EmployeesPageViewModel()
    {
        Load();
    }

    #region Commands

    [RelayCommand]
    private void AddEmployee() => App.PayrollModalsViewModel?.OpenAddEmployeeModal();

    [RelayCommand]
    private void EditEmployee(EmployeeDisplayItem? item)
    {
        if (item == null)
        {
            return;
        }

        Employee? employee = _all.FirstOrDefault(e => e.Id == item.Id);
        if (employee != null)
        {
            App.PayrollModalsViewModel?.OpenEditEmployeeModal(employee);
        }
    }

    /// <summary>
    /// Archives rather than deletes. Pay history references the employee, and a T4 has to
    /// remain produceable, so removing the record outright would break both.
    /// </summary>
    [RelayCommand]
    private void ArchiveEmployee(EmployeeDisplayItem? item)
    {
        if (item == null)
        {
            return;
        }

        Employee? employee = _all.FirstOrDefault(e => e.Id == item.Id);
        if (employee == null)
        {
            return;
        }

        employee.IsArchived = !employee.IsArchived;
        employee.UpdatedAt = DateTime.UtcNow;
        App.CompanyManager?.MarkAsChanged();
        Load();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        ShowArchived = false;
    }

    #endregion

    /// <summary>
    /// Re-applies the filter when the sort column or page changes. The list is small enough
    /// that rebuilding it is cheaper than tracking incremental changes.
    /// </summary>
    protected override void OnSortOrPageChanged() => Filter();

    /// <summary>Reloads from company data. Called on navigation and after any edit.</summary>
    public void Load()
    {
        _all.Clear();

        List<Employee>? employees = App.CompanyManager?.CompanyData?.Employees;
        if (employees != null)
        {
            _all.AddRange(employees);
        }

        UpdateStatistics();
        Filter();
        OnPropertyChanged(nameof(HasNoEmployees));
    }

    private void UpdateStatistics()
    {
        List<Employee> active = _all.Where(e => !e.IsArchived).ToList();

        ActiveCount = active.Count;
        ArchivedCount = _all.Count - active.Count;

        // Salaried people have a known annual cost. Hourly people do not until their hours are
        // entered on a pay run, so they are left out rather than guessed at.
        decimal annual = active
            .Where(e => e.PayType == PayType.Salary)
            .Sum(e => e.PayRate);

        AnnualPayroll = $"${annual:N0}";
    }

    private void Filter()
    {
        Employees.Clear();

        IEnumerable<Employee> filtered = _all.Where(e => ShowArchived || !e.IsArchived);

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string q = SearchQuery.Trim();
            filtered = filtered.Where(e =>
                e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.EmployeeNumber.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        foreach (Employee e in filtered.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Employees.Add(EmployeeDisplayItem.From(e));
        }

        OnPropertyChanged(nameof(HasNoMatches));
    }
}

/// <summary>One row of the employees table, already formatted for display.</summary>
public partial class EmployeeDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _employeeNumber = string.Empty;

    [ObservableProperty]
    private string _province = string.Empty;

    [ObservableProperty]
    private string _payType = string.Empty;

    [ObservableProperty]
    private string _payRate = string.Empty;

    [ObservableProperty]
    private string _frequency = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private bool _isArchived;

    public static EmployeeDisplayItem From(Employee e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        EmployeeNumber = e.EmployeeNumber,
        Province = e.Province,
        PayType = e.PayType == Core.Models.Payroll.PayType.Salary ? "Salary" : "Hourly",
        PayRate = e.PayType == Core.Models.Payroll.PayType.Salary
            ? $"{CurrencyService.Format(e.PayRate)} / year"
            : $"{CurrencyService.Format(e.PayRate)} / hour",
        Frequency = e.PayFrequency.DisplayName(),
        Status = e.IsArchived ? "Archived" : "Active",
        IsArchived = e.IsArchived,
    };
}
