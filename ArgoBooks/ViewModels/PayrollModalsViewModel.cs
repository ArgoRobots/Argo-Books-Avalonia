using System.Collections.ObjectModel;
using System.Globalization;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Add, edit and filter modals for the Employees page. The pay run flow lives in
/// <see cref="PayRunModalsViewModel"/> so this stays a plain entity form.
/// </summary>
public partial class PayrollModalsViewModel : ViewModelBase
{
    private Employee? _editing;

    [ObservableProperty]
    private bool _isEmployeeModalOpen;

    [ObservableProperty]
    private string _modalTitle = "Add employee";

    #region Form fields

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _employeeNumber = string.Empty;

    [ObservableProperty]
    private string _province = "AB";

    [ObservableProperty]
    private bool _isSalaried = true;

    /// <summary>
    /// Money fields are strings so an empty box shows its placeholder instead of "0.00".
    /// Matches how every other modal in the app takes an amount.
    /// </summary>
    [ObservableProperty]
    private string _payRate = string.Empty;

    [ObservableProperty]
    private PayFrequency _payFrequency = PayFrequency.Biweekly;

    [ObservableProperty]
    private string _federalClaimAmount = string.Empty;

    [ObservableProperty]
    private string _provincialClaimAmount = string.Empty;

    [ObservableProperty]
    private bool _isCppExempt;

    [ObservableProperty]
    private bool _isEiExempt;

    [ObservableProperty]
    private DateTimeOffset? _startDate;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _nameError = string.Empty;

    [ObservableProperty]
    private string _payRateError = string.Empty;

    /// <summary>Label under the pay rate box, since the same field means two different things.</summary>
    public string PayRateHint => IsSalaried
        ? "Annual salary before deductions."
        : "Rate per hour.";

    partial void OnIsSalariedChanged(bool value) => OnPropertyChanged(nameof(PayRateHint));

    #endregion

    /// <summary>
    /// Provinces the app can actually calculate for. Only those with a rate table are offered,
    /// so an employee cannot be created that no pay run could ever include.
    /// </summary>
    public ObservableCollection<string> SupportedProvinces { get; } = [];

    public ObservableCollection<PayFrequency> Frequencies { get; } =
        [PayFrequency.Weekly, PayFrequency.Biweekly, PayFrequency.SemiMonthly, PayFrequency.Monthly];

    public PayrollModalsViewModel()
    {
        RefreshSupportedProvinces();
    }

    /// <summary>Event raised after a save, so the page can reload.</summary>
    public event EventHandler? EmployeeSaved;

    public void OpenAddEmployeeModal()
    {
        _editing = null;
        ModalTitle = "Add employee";
        Clear();
        RefreshSupportedProvinces();
        IsEmployeeModalOpen = true;
    }

    public void OpenEditEmployeeModal(Employee employee)
    {
        _editing = employee;
        ModalTitle = "Edit employee";
        RefreshSupportedProvinces();

        Name = employee.Name;
        EmployeeNumber = employee.EmployeeNumber;
        Province = employee.Province;
        IsSalaried = employee.PayType == PayType.Salary;
        PayRate = Money(employee.PayRate);
        PayFrequency = employee.PayFrequency;
        FederalClaimAmount = Money(employee.FederalClaimAmount);
        ProvincialClaimAmount = Money(employee.ProvincialClaimAmount);
        IsCppExempt = employee.IsCppExempt;
        IsEiExempt = employee.IsEiExempt;
        StartDate = employee.StartDate.HasValue ? new DateTimeOffset(employee.StartDate.Value) : null;
        Notes = employee.Notes;

        NameError = string.Empty;
        PayRateError = string.Empty;
        IsEmployeeModalOpen = true;
    }

    [RelayCommand]
    private void CloseEmployeeModal() => IsEmployeeModalOpen = false;

    [RelayCommand]
    private void SaveEmployee()
    {
        decimal rate = Parse(PayRate);

        NameError = string.IsNullOrWhiteSpace(Name) ? "Enter a name." : string.Empty;
        PayRateError = rate <= 0 ? "Enter a pay rate." : string.Empty;

        if (NameError.Length > 0 || PayRateError.Length > 0)
        {
            return;
        }

        Core.Data.CompanyData? data = App.CompanyManager?.CompanyData;
        if (data == null)
        {
            return;
        }

        if (_editing == null)
        {
            AddEmployee(data, rate);
        }
        else
        {
            UpdateEmployee(data, _editing, rate);
        }

        App.CompanyManager?.MarkAsChanged();
        IsEmployeeModalOpen = false;
        EmployeeSaved?.Invoke(this, EventArgs.Empty);
    }

    private void AddEmployee(Core.Data.CompanyData data, decimal rate)
    {
        var employee = new Employee
        {
            Id = NextId(data),
            CreatedAt = DateTime.UtcNow,
        };

        ApplyFormTo(employee, rate);
        data.Employees.Add(employee);

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Add employee '{employee.Name}'",
            () =>
            {
                data.Employees.Remove(employee);
                App.CompanyManager?.MarkAsChanged();
                EmployeeSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                data.Employees.Add(employee);
                App.CompanyManager?.MarkAsChanged();
                EmployeeSaved?.Invoke(this, EventArgs.Empty);
            }));
    }

    /// <summary>
    /// Edits in place and records the before state, rather than swapping the instance out.
    /// Pay run lines hold the employee id, not a reference, but the archive command and the
    /// employees list both work off the same object, so replacing it would strand them.
    /// </summary>
    private void UpdateEmployee(Core.Data.CompanyData data, Employee employee, decimal rate)
    {
        Employee before = Snapshot(employee);
        ApplyFormTo(employee, rate);
        Employee after = Snapshot(employee);

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit employee '{employee.Name}'",
            () =>
            {
                Restore(employee, before);
                App.CompanyManager?.MarkAsChanged();
                EmployeeSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                Restore(employee, after);
                App.CompanyManager?.MarkAsChanged();
                EmployeeSaved?.Invoke(this, EventArgs.Empty);
            }));
    }

    private void ApplyFormTo(Employee employee, decimal rate)
    {
        employee.Name = Name.Trim();
        employee.EmployeeNumber = EmployeeNumber.Trim();
        employee.Province = Province;
        employee.PayType = IsSalaried ? PayType.Salary : PayType.Hourly;
        employee.PayRate = rate;
        employee.PayFrequency = PayFrequency;
        employee.FederalClaimAmount = Parse(FederalClaimAmount);
        employee.ProvincialClaimAmount = Parse(ProvincialClaimAmount);
        employee.IsCppExempt = IsCppExempt;
        employee.IsEiExempt = IsEiExempt;
        employee.StartDate = StartDate?.DateTime;
        employee.Notes = Notes.Trim();
        employee.UpdatedAt = DateTime.UtcNow;
    }

    private static Employee Snapshot(Employee e) => new()
    {
        Name = e.Name,
        EmployeeNumber = e.EmployeeNumber,
        Province = e.Province,
        PayType = e.PayType,
        PayRate = e.PayRate,
        PayFrequency = e.PayFrequency,
        FederalClaimAmount = e.FederalClaimAmount,
        ProvincialClaimAmount = e.ProvincialClaimAmount,
        IsCppExempt = e.IsCppExempt,
        IsEiExempt = e.IsEiExempt,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        Notes = e.Notes,
        IsArchived = e.IsArchived,
        UpdatedAt = e.UpdatedAt,
    };

    private static void Restore(Employee target, Employee from)
    {
        target.Name = from.Name;
        target.EmployeeNumber = from.EmployeeNumber;
        target.Province = from.Province;
        target.PayType = from.PayType;
        target.PayRate = from.PayRate;
        target.PayFrequency = from.PayFrequency;
        target.FederalClaimAmount = from.FederalClaimAmount;
        target.ProvincialClaimAmount = from.ProvincialClaimAmount;
        target.IsCppExempt = from.IsCppExempt;
        target.IsEiExempt = from.IsEiExempt;
        target.StartDate = from.StartDate;
        target.EndDate = from.EndDate;
        target.Notes = from.Notes;
        target.IsArchived = from.IsArchived;
        target.UpdatedAt = from.UpdatedAt;
    }

    #region Filter modal

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private string _filterStatus = "Active";

    [ObservableProperty]
    private string _filterProvince = "All";

    [ObservableProperty]
    private string _filterPayType = "All";

    [ObservableProperty]
    private string _filterFrequency = "All";

    public ObservableCollection<string> StatusOptions { get; } = ["Active", "Archived", "All"];

    public ObservableCollection<string> ProvinceFilterOptions { get; } = ["All"];

    public ObservableCollection<string> PayTypeOptions { get; } = ["All", "Salary", "Hourly"];

    public ObservableCollection<string> FrequencyOptions { get; } =
        ["All", "Weekly", "Biweekly", "Semi-monthly", "Monthly"];

    /// <summary>Raised when Apply is pressed, so the page re-runs its filter.</summary>
    public event EventHandler? FiltersApplied;

    /// <summary>Raised when Clear is pressed, so the page resets its own copy.</summary>
    public event EventHandler? FiltersCleared;

    [RelayCommand]
    public void OpenFilterModal()
    {
        // Only provinces someone actually works in are offered, so the list stays short
        // rather than listing every province the rate table happens to cover.
        ProvinceFilterOptions.Clear();
        ProvinceFilterOptions.Add("All");

        List<Employee>? employees = App.CompanyManager?.CompanyData?.Employees;
        if (employees != null)
        {
            foreach (string code in employees.Select(e => e.Province)
                                             .Where(p => !string.IsNullOrWhiteSpace(p))
                                             .Distinct()
                                             .OrderBy(p => p, StringComparer.Ordinal))
            {
                ProvinceFilterOptions.Add(code);
            }
        }

        if (!ProvinceFilterOptions.Contains(FilterProvince))
        {
            FilterProvince = "All";
        }

        IsFilterModalOpen = true;
    }

    [RelayCommand]
    public void CloseFilterModal() => IsFilterModalOpen = false;

    [RelayCommand]
    public void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        FilterStatus = "Active";
        FilterProvince = "All";
        FilterPayType = "All";
        FilterFrequency = "All";
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    /// <summary>
    /// Reads the provinces out of the rate table rather than hard-coding a list, so the
    /// dropdown grows by itself as editions gain provinces and never offers one that would
    /// fail at pay-run time.
    /// </summary>
    private void RefreshSupportedProvinces()
    {
        SupportedProvinces.Clear();

        Core.Models.Payroll.PayrollRateTable? table =
            new Core.Services.PayrollRateService().GetForDate(DateTime.Today);

        if (table == null)
        {
            // No edition covers today. The employee form still works; a pay run is what will
            // refuse, with a message that explains why.
            SupportedProvinces.Add(Province);
            return;
        }

        foreach (string code in table.Provinces.Keys.OrderBy(c => c, StringComparer.Ordinal))
        {
            SupportedProvinces.Add(code);
        }

        if (!SupportedProvinces.Contains(Province) && SupportedProvinces.Count > 0)
        {
            Province = SupportedProvinces[0];
        }
    }

    private static string NextId(Core.Data.CompanyData data)
    {
        int highest = 0;
        foreach (Employee e in data.Employees)
        {
            if (e.Id.StartsWith("EMP-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(e.Id[4..], out int n) && n > highest)
            {
                highest = n;
            }
        }

        return $"EMP-{highest + 1:D3}";
    }

    /// <summary>Blank rather than "0.00", so an unset optional amount shows its placeholder.</summary>
    private static string Money(decimal value) =>
        value == 0 ? string.Empty : value.ToString("0.00", CultureInfo.CurrentCulture);

    private static decimal Parse(string text) =>
        decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal d) ? d : 0m;

    private void Clear()
    {
        Name = string.Empty;
        EmployeeNumber = string.Empty;
        IsSalaried = true;
        PayRate = string.Empty;
        PayFrequency = PayFrequency.Biweekly;
        FederalClaimAmount = string.Empty;
        ProvincialClaimAmount = string.Empty;
        IsCppExempt = false;
        IsEiExempt = false;
        StartDate = null;
        Notes = string.Empty;
        NameError = string.Empty;
        PayRateError = string.Empty;
    }
}
