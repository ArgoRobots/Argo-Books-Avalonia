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

    /// <summary>
    /// Contract hours in a normal week. Only asked of salaried staff, and only used by the
    /// Record of Employment: block 15A wants insurable hours and a salaried pay run records
    /// none. Blank is allowed, and the ROE worksheet then says the hours are unknown rather
    /// than printing zero, because zero hours would cost the employee their EI claim.
    /// </summary>
    [ObservableProperty]
    private string _standardHoursPerWeek = string.Empty;

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

    /// <summary>
    /// Their last day, if they have left. Kept separate from archiving: a leaver still needs a
    /// T4 and a record of employment, and both want the actual last day rather than the day
    /// someone got round to tidying the employee list.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _endDate;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _nameError = string.Empty;

    [ObservableProperty]
    private string _payRateError = string.Empty;

    [ObservableProperty]
    private string _endDateError = string.Empty;

    /// <summary>
    /// Social insurance number, needed to file a T4. Held as typed and stripped to digits on
    /// save, because people write it with spaces or dashes and rejecting that would be rude.
    /// </summary>
    [ObservableProperty]
    private string _sin = string.Empty;

    [ObservableProperty]
    private string _sinError = string.Empty;

    [ObservableProperty]
    private string _addressStreet = string.Empty;

    [ObservableProperty]
    private string _addressCity = string.Empty;

    [ObservableProperty]
    private string _addressProvince = string.Empty;

    [ObservableProperty]
    private string _addressPostalCode = string.Empty;

    /// <summary>Box 45, mandatory on every T4 since 2023.</summary>
    [ObservableProperty]
    private DentalBenefitCode _dentalBenefit = DentalBenefitCode.NotEligible;

    public ObservableCollection<DentalBenefitCode> DentalOptions { get; } =
    [
        DentalBenefitCode.NotEligible,
        DentalBenefitCode.PayeeOnly,
        DentalBenefitCode.PayeeAndSpouse,
        DentalBenefitCode.PayeeAndChildren,
        DentalBenefitCode.PayeeSpouseAndChildren,
    ];

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

    /// <summary>
    /// Explains a short province list, so someone whose staff work somewhere not yet covered
    /// finds out here rather than after entering an employee they can never pay.
    /// </summary>
    [ObservableProperty]
    private string _provinceSupportNote = string.Empty;

    /// <summary>Provinces and territories, so a partial rate table can be recognised as partial.</summary>
    private const int AllProvincesAndTerritories = 13;

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
        StandardHoursPerWeek = employee.StandardHoursPerWeek?.ToString("0.##") ?? string.Empty;
        FederalClaimAmount = Money(employee.FederalClaimAmount);
        ProvincialClaimAmount = Money(employee.ProvincialClaimAmount);
        IsCppExempt = employee.IsCppExempt;
        IsEiExempt = employee.IsEiExempt;
        StartDate = employee.StartDate.HasValue ? new DateTimeOffset(employee.StartDate.Value) : null;
        EndDate = employee.EndDate.HasValue ? new DateTimeOffset(employee.EndDate.Value) : null;
        Sin = employee.Sin;
        AddressStreet = employee.Address.Street;
        AddressCity = employee.Address.City;
        AddressProvince = employee.Address.State;
        AddressPostalCode = employee.Address.ZipCode;
        DentalBenefit = employee.DentalBenefit;
        Notes = employee.Notes;

        NameError = string.Empty;
        PayRateError = string.Empty;
        EndDateError = string.Empty;
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

        // A last day before the first day would put pay periods outside the employment and
        // make the record of employment nonsense.
        EndDateError = StartDate is { } start && EndDate is { } end && end < start
            ? "The end date cannot be before the start date."
            : string.Empty;

        // Checked but not required. Someone can be hired and paid before their SIN arrives,
        // and blocking the whole employee record over it would stop payroll running at all.
        // Year end is where a missing SIN actually becomes a problem, and it is reported there.
        string sinDigits = new(Sin.Where(char.IsAsciiDigit).ToArray());
        SinError = Sin.Trim().Length > 0 && sinDigits.Length != 9
            ? "A social insurance number is 9 digits."
            : string.Empty;

        if (NameError.Length > 0 || PayRateError.Length > 0 || EndDateError.Length > 0 || SinError.Length > 0)
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

        // Null rather than zero when blank, and cleared outright for an hourly employee whose
        // real hours are entered on every run. Zero would read as "worked no hours" on an ROE.
        decimal hours = Parse(StandardHoursPerWeek);
        employee.StandardHoursPerWeek = IsSalaried && hours > 0 ? hours : null;

        employee.FederalClaimAmount = Parse(FederalClaimAmount);
        employee.ProvincialClaimAmount = Parse(ProvincialClaimAmount);
        employee.IsCppExempt = IsCppExempt;
        employee.IsEiExempt = IsEiExempt;
        employee.StartDate = StartDate?.DateTime;
        employee.EndDate = EndDate?.DateTime;
        employee.Sin = new string(Sin.Where(char.IsAsciiDigit).ToArray());
        employee.Address.Street = AddressStreet.Trim();
        employee.Address.City = AddressCity.Trim();
        employee.Address.State = AddressProvince.Trim();
        employee.Address.ZipCode = AddressPostalCode.Trim();
        employee.DentalBenefit = DentalBenefit;
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
        Sin = e.Sin,
        DentalBenefit = e.DentalBenefit,
        Address = new Core.Models.Common.Address
        {
            Street = e.Address.Street,
            City = e.Address.City,
            State = e.Address.State,
            ZipCode = e.Address.ZipCode,
            Country = e.Address.Country,
        },
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
        target.Sin = from.Sin;
        target.DentalBenefit = from.DentalBenefit;
        target.Address = from.Address;
        target.Notes = from.Notes;
        target.IsArchived = from.IsArchived;
        target.UpdatedAt = from.UpdatedAt;
    }

    #region Filter modal

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string _filterProvince = "All";

    [ObservableProperty]
    private string _filterPayType = "All";

    [ObservableProperty]
    private string _filterFrequency = "All";

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Archived"];

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
        FilterStatus = "All";
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
            ProvinceSupportNote = "No CRA payroll tables are loaded for today, so a pay run "
                                  + "cannot be calculated until the rates are updated.";
            return;
        }

        foreach (string code in table.Provinces.Keys.OrderBy(c => c, StringComparer.Ordinal))
        {
            SupportedProvinces.Add(code);
        }

        ProvinceSupportNote = SupportedProvinces.Count < AllProvincesAndTerritories
            ? $"Payroll can only be calculated for {string.Join(", ", SupportedProvinces)} at the "
              + "moment. Other provinces are being added."
            : string.Empty;

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
        StandardHoursPerWeek = string.Empty;
        FederalClaimAmount = string.Empty;
        ProvincialClaimAmount = string.Empty;
        IsCppExempt = false;
        IsEiExempt = false;
        StartDate = null;
        EndDate = null;
        Sin = string.Empty;
        AddressStreet = string.Empty;
        AddressCity = string.Empty;
        AddressProvince = string.Empty;
        AddressPostalCode = string.Empty;
        DentalBenefit = DentalBenefitCode.NotEligible;
        Notes = string.Empty;
        NameError = string.Empty;
        PayRateError = string.Empty;
        EndDateError = string.Empty;
        SinError = string.Empty;
    }
}
