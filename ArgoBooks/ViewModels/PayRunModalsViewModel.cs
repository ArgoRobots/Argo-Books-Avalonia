using System.Collections.ObjectModel;
using System.Globalization;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// The three step pay run: pick the period and the people, enter the amounts, review and
/// approve.
///
/// The draft run is built on entering step 2 and recalculated on every amount change, so
/// step 3 is only ever displaying figures that already exist rather than computing them as
/// it renders.
/// </summary>
public partial class PayRunModalsViewModel : ViewModelBase
{
    private readonly PayrollService _payroll = new();
    private PayRun? _draft;

    [ObservableProperty]
    private bool _isRunModalOpen;

    /// <summary>1 = period, 2 = amounts, 3 = review.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(BackButtonText))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    private int _step = 1;

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;

    public string BackButtonText => Step == 1 ? "Cancel" : "Back";
    public string NextButtonText => Step == 3 ? "Approve" : "Next";

    #region Step 1: the period

    [ObservableProperty]
    private DateTimeOffset? _payDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _periodStart;

    [ObservableProperty]
    private DateTimeOffset? _periodEnd;

    /// <summary>
    /// Which CRA edition the pay date falls in. Shown because three provinces carry different
    /// figures in each half of 2026, so a wrong pay date is a silent class of error.
    /// </summary>
    [ObservableProperty]
    private string _rateEditionNote = string.Empty;

    /// <summary>Set when no edition covers the pay date. Blocks the run rather than guessing.</summary>
    [ObservableProperty]
    private string _blockingError = string.Empty;

    public ObservableCollection<PayRunEmployeeSelection> SelectableEmployees { get; } = [];

    /// <summary>
    /// Why there is nobody to pay, or empty when there is. Two different sentences rather than
    /// one, because the fix is different: an employer with no employees has to add one, and an
    /// employer whose staff are all archived has to restore one. An empty list said neither and
    /// read like something had failed to load.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNobodyToPay))]
    private string _noEmployeesMessage = string.Empty;

    public bool HasNobodyToPay => NoEmployeesMessage.Length > 0;

    partial void OnPayDateChanged(DateTimeOffset? value)
    {
        RefreshRateEdition();
        PrefillPeriod();
    }

    #endregion

    #region Step 2: the amounts

    public ObservableCollection<PayRunAmountRow> AmountRows { get; } = [];

    [ObservableProperty]
    private string _totalGross = "$0.00";

    #endregion

    #region Step 3: the review

    public ObservableCollection<PayRunReviewRow> ReviewRows { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    [ObservableProperty]
    private bool _hasWarnings;

    [ObservableProperty]
    private string _totalNetPay = "$0.00";

    [ObservableProperty]
    private string _totalRemittance = "$0.00";

    [ObservableProperty]
    private string _totalCost = "$0.00";

    /// <summary>
    /// When the withheld amounts are due. A regular remitter pays by the 15th of the month
    /// after the one the employees were paid in.
    /// </summary>
    [ObservableProperty]
    private string _remittanceDueNote = string.Empty;

    #endregion

    /// <summary>Raised after approve or void, so the pay runs page reloads.</summary>
    public event EventHandler? PayRunChanged;

    #region Opening

    public void OpenRunModal()
    {
        _draft = null;
        Step = 1;
        PayDate = DateTimeOffset.Now;
        BlockingError = string.Empty;

        // Nothing from the last run may survive into this one. The amount rows in particular
        // outlived a close, so a second open began with rows already present and the discard
        // guard fired on a run nobody had touched yet.
        ResetRunState();

        RefreshRateEdition();
        PrefillPeriod();
        LoadSelectableEmployees();

        IsRunModalOpen = true;
    }

    [RelayCommand]
    private void CloseRunModal()
    {
        IsRunModalOpen = false;
        _draft = null;
    }

    /// <summary>
    /// True once the run holds work that retyping would cost something.
    ///
    /// Step 1 is deliberately not guarded: the pay date, the period and the employee ticks are
    /// all prefilled defaults, so abandoning there loses nothing anyone chose. From step 2 the
    /// amounts are hand entered, which is the part worth asking about, and the rows survive
    /// stepping back so the check does not depend on where they are now.
    /// </summary>
    private bool HasRunInProgress => Step > 1 || AmountRows.Count > 0;

    [RelayCommand]
    private async Task RequestCloseRunModalAsync()
    {
        if (HasRunInProgress && !await ConfirmDiscardNewAsync())
        {
            return;
        }

        CloseRunModal();
    }

    private void LoadSelectableEmployees()
    {
        SelectableEmployees.Clear();
        NoEmployeesMessage = string.Empty;

        List<Employee> employees = App.CompanyManager?.CompanyData?.Employees ?? [];

        foreach (Employee e in employees.Where(e => !e.IsArchived).OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            SelectableEmployees.Add(new PayRunEmployeeSelection
            {
                Id = e.Id,
                Name = e.Name,
                Detail = $"{e.Province} - {e.PayFrequency.DisplayName()}",
                IsSelected = true,
            });
        }

        if (SelectableEmployees.Count > 0)
        {
            return;
        }

        // Archived staff are excluded from a pay run but still exist, so "no employees" would be
        // wrong here and would send the employer off to add someone they already have.
        NoEmployeesMessage = employees.Count > 0
            ? "Every employee is archived, so there is nobody to pay. Restore someone on the Employees page first."
            : "There are no employees yet. Add one on the Employees page, then come back and run payroll.";
    }

    private void RefreshRateEdition()
    {
        DateTime date = PayDate?.DateTime ?? DateTime.Today;
        PayrollRateTable? table = new PayrollRateService().GetForDate(date);

        if (table == null)
        {
            RateEditionNote = string.Empty;
            BlockingError = $"No CRA payroll tables are loaded for {date:d MMMM yyyy}. " +
                            "Update the rates before running payroll for this date.";
            return;
        }

        BlockingError = string.Empty;
        RateEditionNote = $"Using CRA tables effective {table.EffectiveFrom:d MMMM yyyy}.";
    }

    /// <summary>
    /// Prefills the period from the most common frequency among the selected employees, which
    /// is right for the overwhelmingly common case of everyone on the same schedule and easy
    /// to correct when it is not.
    /// </summary>
    private void PrefillPeriod()
    {
        DateTime end = PayDate?.DateTime.Date ?? DateTime.Today;

        PayFrequency frequency = App.CompanyManager?.CompanyData?.Employees
            .Where(e => !e.IsArchived)
            .GroupBy(e => e.PayFrequency)
            .OrderByDescending(g => g.Count())
            .Select(g => (PayFrequency?)g.Key)
            .FirstOrDefault() ?? PayFrequency.Biweekly;

        DateTime start = frequency switch
        {
            PayFrequency.Weekly => end.AddDays(-6),
            PayFrequency.Biweekly => end.AddDays(-13),
            PayFrequency.SemiMonthly => end.AddDays(-14),
            PayFrequency.Monthly => end.AddMonths(-1).AddDays(1),
            _ => end.AddDays(-13),
        };

        PeriodStart = new DateTimeOffset(start);
        PeriodEnd = new DateTimeOffset(end);
    }

    #endregion

    #region Navigation

    [RelayCommand]
    private void Back()
    {
        if (Step == 1)
        {
            CloseRunModal();
            return;
        }

        Step--;
    }

    [RelayCommand]
    private void Next()
    {
        switch (Step)
        {
            case 1:
                if (!BuildDraft())
                {
                    return;
                }

                Step = 2;
                break;

            case 2:
                Recalculate();
                BuildReview();
                Step = 3;
                break;

            case 3:
                Approve();
                break;
        }
    }

    #endregion

    #region Step 1 to 2: build the draft

    private bool BuildDraft()
    {
        CompanyData? data = App.CompanyManager?.CompanyData;
        if (data == null)
        {
            return false;
        }

        List<string> chosen = SelectableEmployees.Where(e => e.IsSelected).Select(e => e.Id).ToList();
        if (chosen.Count == 0)
        {
            BlockingError = "Select at least one employee.";
            return false;
        }

        DateTime payDate = PayDate?.DateTime.Date ?? DateTime.Today;

        _draft = _payroll.CreateDraft(
            data,
            payDate,
            PeriodStart?.DateTime.Date ?? payDate,
            PeriodEnd?.DateTime.Date ?? payDate,
            chosen);

        if (_draft == null)
        {
            // GetForDate returned null. Never fall back to another edition: the figures would
            // look plausible and be wrong.
            RefreshRateEdition();
            return false;
        }

        BlockingError = string.Empty;
        BuildAmountRows(data);
        return true;
    }

    /// <summary>
    /// Drops everything the previous run left behind.
    ///
    /// Detaching the handlers matters as much as clearing the list: a discarded row that is
    /// still subscribed keeps driving recalculation for a draft it no longer belongs to.
    /// </summary>
    private void ResetRunState()
    {
        ClearAmountRows();
        ReviewRows.Clear();
        Warnings.Clear();
        HasWarnings = false;
        TotalGross = CurrencyService.Format(0m);
        TotalNetPay = CurrencyService.Format(0m);
        TotalRemittance = CurrencyService.Format(0m);
        TotalCost = CurrencyService.Format(0m);
        RemittanceDueNote = string.Empty;
    }

    private void ClearAmountRows()
    {
        foreach (PayRunAmountRow existing in AmountRows)
        {
            existing.Changed -= OnAmountChanged;
        }

        AmountRows.Clear();
    }

    private void BuildAmountRows(CompanyData data)
    {
        ClearAmountRows();

        if (_draft == null)
        {
            return;
        }

        foreach (PayRunLine line in _draft.Lines)
        {
            Employee? employee = data.Employees.FirstOrDefault(e => e.Id == line.EmployeeId);
            bool hourly = employee?.PayType == PayType.Hourly;

            var row = new PayRunAmountRow
            {
                EmployeeId = line.EmployeeId,
                EmployeeName = line.EmployeeName,
                IsHourly = hourly,
                RateNote = employee == null
                    ? string.Empty
                    : hourly
                        ? $"{CurrencyService.Format(employee.PayRate)} / hour"
                        : $"{CurrencyService.Format(employee.PayRate)} / year",
                UnitLabel = hourly ? "hrs" : CurrencyService.CurrentSymbol,
                Hours = string.Empty,
                BasePay = hourly ? string.Empty : line.BasePay.ToString("0.00", CultureInfo.CurrentCulture),
                Bonus = string.Empty,
                VacationPay = string.Empty,
            };

            row.Changed += OnAmountChanged;
            AmountRows.Add(row);
        }

        // The same pair the amount-changed handler runs. Without the recalculate the rows show
        // an empty Gross until the first keystroke, even though the figures are already known:
        // a salaried run arrives with its base pay filled in and needs no input at all.
        Recalculate();
        UpdateGrossTotal();
    }

    private void OnAmountChanged(object? sender, EventArgs e)
    {
        Recalculate();
        UpdateGrossTotal();
    }

    /// <summary>Pushes the typed amounts onto the draft and re-runs the deductions.</summary>
    private void Recalculate()
    {
        CompanyData? data = App.CompanyManager?.CompanyData;
        if (_draft == null || data == null)
        {
            return;
        }

        foreach (PayRunLine line in _draft.Lines)
        {
            PayRunAmountRow? row = AmountRows.FirstOrDefault(r => r.EmployeeId == line.EmployeeId);
            if (row == null)
            {
                continue;
            }

            line.HoursWorked = Parse(row.Hours);
            line.Bonus = Parse(row.Bonus);
            line.VacationPay = Parse(row.VacationPay);

            // A salaried person's base pay stays editable, since a mid-period start or an
            // unpaid day is entered by adjusting it.
            if (!row.IsHourly)
            {
                line.BasePay = Parse(row.BasePay);
            }
        }

        _payroll.Recalculate(data, _draft);

        foreach (PayRunAmountRow row in AmountRows)
        {
            PayRunLine? line = _draft.Lines.FirstOrDefault(l => l.EmployeeId == row.EmployeeId);
            row.GrossDisplay = line == null ? CurrencyService.Format(0) : CurrencyService.Format(line.GrossPay);
        }
    }

    private void UpdateGrossTotal() =>
        TotalGross = CurrencyService.Format(_draft?.TotalGross ?? 0m);

    #endregion

    #region Step 3: the review

    private void BuildReview()
    {
        ReviewRows.Clear();
        Warnings.Clear();

        CompanyData? data = App.CompanyManager?.CompanyData;
        if (_draft == null || data == null)
        {
            return;
        }

        PayrollRateTable? rates = new PayrollRateService().GetForDate(_draft.PayDate);

        foreach (PayRunLine line in _draft.Lines)
        {
            ReviewRows.Add(new PayRunReviewRow
            {
                EmployeeName = line.EmployeeName,
                Gross = CurrencyService.Format(line.GrossPay),
                Cpp = CurrencyService.Format(line.CppEmployee + line.Cpp2Employee),
                Ei = CurrencyService.Format(line.EiEmployee),
                FederalTax = CurrencyService.Format(line.FederalTax),
                ProvincialTax = CurrencyService.Format(line.ProvincialTax),
                ProvincialTaxLabel = $"{line.Province} tax",
                NetPay = CurrencyService.Format(line.NetPay),
            });

            AddWarningsFor(data, line, rates);
        }

        HasWarnings = Warnings.Count > 0;

        TotalNetPay = CurrencyService.Format(_draft.TotalNetPay);
        TotalRemittance = CurrencyService.Format(_draft.TotalRemittance);
        TotalCost = CurrencyService.Format(_draft.TotalCost);

        // Regular remitters pay by the 15th of the month after the employees were paid.
        DateTime due = new DateTime(_draft.PayDate.Year, _draft.PayDate.Month, 1).AddMonths(1).AddDays(14);
        RemittanceDueNote = $"Due to CRA by {due:d MMMM yyyy}.";
    }

    /// <summary>
    /// Flags anything that changes the numbers without being visible on the row: a maximum
    /// reached partway through the period, or a missing TD1.
    /// </summary>
    private void AddWarningsFor(CompanyData data, PayRunLine line, PayrollRateTable? rates)
    {
        Employee? employee = data.Employees.FirstOrDefault(e => e.Id == line.EmployeeId);
        if (employee == null || rates == null)
        {
            return;
        }

        PayrollYearToDate ytd = _payroll.YearToDateFor(data, employee.Id, _draft);

        if (!employee.IsCppExempt
            && ytd.CppEmployee + line.CppEmployee >= rates.Cpp.MaxContributionEmployee)
        {
            Warnings.Add($"{employee.Name} has reached the CPP maximum for the year.");
        }

        if (!employee.IsEiExempt
            && ytd.EiEmployee + line.EiEmployee >= rates.Ei.MaxPremiumEmployee)
        {
            Warnings.Add($"{employee.Name} has reached the EI maximum for the year.");
        }

        if (employee.FederalClaimAmount == 0 && employee.ProvincialClaimAmount == 0)
        {
            Warnings.Add($"{employee.Name} has no TD1 on file, so the basic personal amount is used.");
        }

        if (!rates.Provinces.ContainsKey(employee.Province))
        {
            Warnings.Add($"{employee.Name} works in {employee.Province}, which has no rate table loaded.");
        }
    }

    #endregion

    #region Approve

    private void Approve()
    {
        CompanyData? data = App.CompanyManager?.CompanyData;
        if (_draft == null || data == null)
        {
            return;
        }

        PayRun run = _draft;
        data.PayRuns.Add(run);

        List<Core.Models.Transactions.Expense> expenses = _payroll.ApproveAndRecord(data, run);

        App.CompanyManager?.MarkAsChanged();

        // Undo has to take the expenses with it, otherwise the wages stay in the books after
        // the run they came from is gone.
        List<string> expenseIds = expenses.Select(e => e.Id).ToList();
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Approve pay run {run.Id}",
            () =>
            {
                data.PayRuns.Remove(run);
                data.Expenses.RemoveAll(e => expenseIds.Contains(e.Id));
                App.CompanyManager?.MarkAsChanged();
                PayRunChanged?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                data.PayRuns.Add(run);
                data.Expenses.AddRange(expenses);
                App.CompanyManager?.MarkAsChanged();
                PayRunChanged?.Invoke(this, EventArgs.Empty);
            }));

        _draft = null;
        IsRunModalOpen = false;
        PayRunChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    private static decimal Parse(string text) =>
        decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal d) ? d : 0m;
}

/// <summary>One employee on the step 1 checklist.</summary>
public partial class PayRunEmployeeSelection : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _detail = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}

/// <summary>One employee's editable amounts on step 2.</summary>
public partial class PayRunAmountRow : ObservableObject
{
    /// <summary>Raised on any amount edit so the owning modal re-runs the deductions.</summary>
    public event EventHandler? Changed;

    [ObservableProperty]
    private string _employeeId = string.Empty;

    [ObservableProperty]
    private string _employeeName = string.Empty;

    [ObservableProperty]
    private bool _isHourly;

    [ObservableProperty]
    private string _rateNote = string.Empty;

    /// <summary>
    /// What the amount box is measured in: hours for an hourly employee, money for a salaried
    /// one. Sits beside the box because the column serves both and a placeholder disappears the
    /// moment anything is typed, which is precisely when the reader is checking what they
    /// entered.
    /// </summary>
    [ObservableProperty]
    private string _unitLabel = string.Empty;

    [ObservableProperty]
    private string _hours = string.Empty;

    [ObservableProperty]
    private string _basePay = string.Empty;

    [ObservableProperty]
    private string _bonus = string.Empty;

    [ObservableProperty]
    private string _vacationPay = string.Empty;

    [ObservableProperty]
    private string _grossDisplay = string.Empty;

    partial void OnHoursChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnBasePayChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnBonusChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnVacationPayChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
}

/// <summary>One employee's breakdown on step 3.</summary>
public partial class PayRunReviewRow : ObservableObject
{
    [ObservableProperty]
    private string _employeeName = string.Empty;

    [ObservableProperty]
    private string _gross = string.Empty;

    [ObservableProperty]
    private string _cpp = string.Empty;

    [ObservableProperty]
    private string _ei = string.Empty;

    [ObservableProperty]
    private string _federalTax = string.Empty;

    [ObservableProperty]
    private string _provincialTax = string.Empty;

    [ObservableProperty]
    private string _provincialTaxLabel = string.Empty;

    [ObservableProperty]
    private string _netPay = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;
}
