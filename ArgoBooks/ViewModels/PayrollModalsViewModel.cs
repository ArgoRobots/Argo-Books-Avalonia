using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Payroll;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Add and edit modals for payroll. Currently the employee form; the pay run flow joins it
/// here later so both live behind one shell-hosted control.
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

    [ObservableProperty]
    private decimal? _payRate;

    [ObservableProperty]
    private PayFrequency _payFrequency = PayFrequency.Biweekly;

    [ObservableProperty]
    private decimal? _federalClaimAmount;

    [ObservableProperty]
    private decimal? _provincialClaimAmount;

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
        PayRate = employee.PayRate == 0 ? null : employee.PayRate;
        PayFrequency = employee.PayFrequency;
        FederalClaimAmount = employee.FederalClaimAmount == 0 ? null : employee.FederalClaimAmount;
        ProvincialClaimAmount = employee.ProvincialClaimAmount == 0 ? null : employee.ProvincialClaimAmount;
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
        NameError = string.IsNullOrWhiteSpace(Name) ? "Enter a name." : string.Empty;
        PayRateError = PayRate is null or <= 0 ? "Enter a pay rate." : string.Empty;

        if (NameError.Length > 0 || PayRateError.Length > 0)
        {
            return;
        }

        Core.Data.CompanyData? data = App.CompanyManager?.CompanyData;
        if (data == null)
        {
            return;
        }

        Employee employee = _editing ?? new Employee
        {
            Id = NextId(data),
            CreatedAt = DateTime.UtcNow,
        };

        employee.Name = Name.Trim();
        employee.EmployeeNumber = EmployeeNumber.Trim();
        employee.Province = Province;
        employee.PayType = IsSalaried ? PayType.Salary : PayType.Hourly;
        employee.PayRate = PayRate ?? 0m;
        employee.PayFrequency = PayFrequency;
        employee.FederalClaimAmount = FederalClaimAmount ?? 0m;
        employee.ProvincialClaimAmount = ProvincialClaimAmount ?? 0m;
        employee.IsCppExempt = IsCppExempt;
        employee.IsEiExempt = IsEiExempt;
        employee.StartDate = StartDate?.DateTime;
        employee.Notes = Notes.Trim();
        employee.UpdatedAt = DateTime.UtcNow;

        if (_editing == null)
        {
            data.Employees.Add(employee);
        }

        App.CompanyManager?.MarkAsChanged();
        IsEmployeeModalOpen = false;
        EmployeeSaved?.Invoke(this, EventArgs.Empty);
    }

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

    private void Clear()
    {
        Name = string.Empty;
        EmployeeNumber = string.Empty;
        IsSalaried = true;
        PayRate = null;
        PayFrequency = PayFrequency.Biweekly;
        FederalClaimAmount = null;
        ProvincialClaimAmount = null;
        IsCppExempt = false;
        IsEiExempt = false;
        StartDate = null;
        Notes = string.Empty;
        NameError = string.Empty;
        PayRateError = string.Empty;
    }
}
