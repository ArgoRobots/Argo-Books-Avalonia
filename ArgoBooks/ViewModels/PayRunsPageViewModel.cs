using System.Collections.ObjectModel;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using ArgoBooks.Core.Services.Payroll;
using ArgoBooks.Helpers;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Pay runs page: the history of payrolls, and the entry point to running
/// a new one.
///
/// Voided runs stay in the list. A pay stub may already be in someone's hands, so the record
/// has to survive the correction.
/// </summary>
public partial class PayRunsPageViewModel : SortablePageViewModelBase
{
    private readonly List<PayRun> _all = [];
    private readonly PayrollService _payroll = new();

    public ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    public PayRunsTableColumnWidths ColumnWidths => App.PayRunsColumnWidths;

    public ObservableCollection<PayRunDisplayItem> PayRuns { get; } = [];

    #region Statistics

    [ObservableProperty]
    private string _yearToDateGross = "$0";

    [ObservableProperty]
    private string _yearToDateRemittance = "$0";

    [ObservableProperty]
    private int _approvedCount;

    #endregion

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value) => Filter();

    /// <summary>True when no payroll has ever been run, so the page leads with Run payroll.</summary>
    public bool HasNoPayRuns => _all.Count == 0;

    #region Premium

    /// <summary>
    /// Payroll is premium only. Two CRA rate updates a year with hard deadlines is a recurring
    /// maintenance cost, so it sits against recurring revenue.
    ///
    /// The page itself stays visible on the free plan rather than being hidden, because an
    /// owner with staff seeing what it does is the whole conversion lever. Only running a
    /// payroll is blocked.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPayrollUpgradePrompt))]
    private bool _hasPremium;

    public bool ShowPayrollUpgradePrompt => !HasPremium;

    /// <summary>Raised when the upgrade prompt is clicked, so the shell can open the modal.</summary>
    public event EventHandler? UpgradeRequested;

    [RelayCommand]
    private void Upgrade() => UpgradeRequested?.Invoke(this, EventArgs.Empty);

    #endregion

    public PayRunsPageViewModel()
    {
        Load();

        if (App.PayRunModalsViewModel is { } modals)
        {
            modals.PayRunChanged += OnPayRunChanged;
        }

        App.PlanStatusChanged += OnPlanStatusChanged;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        if (App.PayRunModalsViewModel is { } modals)
        {
            modals.PayRunChanged -= OnPayRunChanged;
        }

        App.PlanStatusChanged -= OnPlanStatusChanged;
    }

    private void OnPlanStatusChanged(object? sender, PlanStatusChangedEventArgs e) => HasPremium = e.HasPremium;

    private void OnPayRunChanged(object? sender, EventArgs e) => Load();

    #region Column visibility

    [ObservableProperty]
    private string _paginationText = "0 pay runs";

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    [ObservableProperty]
    private bool _showPayDateColumn = ColumnVisibilityHelper.Load("PayRuns", "PayDate", true);

    [ObservableProperty]
    private bool _showPeriodColumn = ColumnVisibilityHelper.Load("PayRuns", "Period", true);

    [ObservableProperty]
    private bool _showEmployeesColumn = ColumnVisibilityHelper.Load("PayRuns", "Employees", true);

    [ObservableProperty]
    private bool _showGrossColumn = ColumnVisibilityHelper.Load("PayRuns", "Gross", true);

    [ObservableProperty]
    private bool _showNetColumn = ColumnVisibilityHelper.Load("PayRuns", "Net", true);

    [ObservableProperty]
    private bool _showStatusColumn = ColumnVisibilityHelper.Load("PayRuns", "Status", true);

    partial void OnShowPayDateColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("PayDate", value); ColumnVisibilityHelper.Save("PayRuns", "PayDate", value); }
    partial void OnShowPeriodColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Period", value); ColumnVisibilityHelper.Save("PayRuns", "Period", value); }
    partial void OnShowEmployeesColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Employees", value); ColumnVisibilityHelper.Save("PayRuns", "Employees", value); }
    partial void OnShowGrossColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Gross", value); ColumnVisibilityHelper.Save("PayRuns", "Gross", value); }
    partial void OnShowNetColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Net", value); ColumnVisibilityHelper.Save("PayRuns", "Net", value); }
    partial void OnShowStatusColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Status", value); ColumnVisibilityHelper.Save("PayRuns", "Status", value); }

    [RelayCommand]
    private void ToggleColumnMenu() => IsColumnMenuOpen = !IsColumnMenuOpen;

    [RelayCommand]
    private void CloseColumnMenu() => IsColumnMenuOpen = false;

    [RelayCommand]
    private void ResetColumnVisibility()
    {
        ColumnWidths.ResetWidths();
        ColumnVisibilityHelper.ResetPage("PayRuns");
        ShowPayDateColumn = true;
        ShowPeriodColumn = true;
        ShowEmployeesColumn = true;
        ShowGrossColumn = true;
        ShowNetColumn = true;
        ShowStatusColumn = true;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void RunPayroll()
    {
        if (!HasPremium)
        {
            UpgradeRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        App.PayRunModalsViewModel?.OpenRunModal();
    }

    /// <summary>
    /// Saves a pay stub PDF for every employee on the run. One file each, since a stub is
    /// handed to one person and nobody should see anyone else's pay.
    /// </summary>
    [RelayCommand]
    private async Task DownloadStubsAsync(PayRunDisplayItem? item)
    {
        CompanyData? data = App.CompanyManager?.CompanyData;
        PayRun? run = _all.FirstOrDefault(r => r.Id == item?.Id);

        if (data == null || run == null || run.Lines.Count == 0)
        {
            return;
        }

        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.StorageProvider == null)
        {
            return;
        }

        try
        {
            IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Choose where to save the pay stubs".Translate(),
                    AllowMultiple = false,
                });

            if (folders.Count == 0)
            {
                return;
            }

            string directory = folders[0].Path.LocalPath;
            string symbol = CurrencyService.CurrentSymbol;

            foreach (PayRunLine line in run.Lines)
            {
                // Year to date up to but not including this run, so the stub's own figures
                // are what gets added to it rather than counted twice.
                PayrollYearToDate ytd = _payroll.YearToDateFor(data, line.EmployeeId, run);

                byte[] bytes = await Task.Run(() => PayStubPdfRenderer.Render(run, line, ytd, data, symbol));
                string name = $"{run.PayDate:yyyy-MM-dd}-{Sanitize(line.EmployeeName)}.pdf";

                await File.WriteAllBytesAsync(Path.Combine(directory, name), bytes);
            }
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Validation, "Payroll.DownloadStubs");
        }
    }

    /// <summary>
    /// Cancels an approved run by writing a reversing one. Nothing is deleted, because a stub
    /// may already be in someone's hands.
    /// </summary>
    [RelayCommand]
    private async Task VoidRunAsync(PayRunDisplayItem? item)
    {
        CompanyData? data = App.CompanyManager?.CompanyData;
        PayRun? run = _all.FirstOrDefault(r => r.Id == item?.Id);

        if (data == null || run == null || run.Status != PayRunStatus.Approved)
        {
            return;
        }

        if (App.ConfirmationDialog is { } confirm)
        {
            ConfirmationResult result = await confirm.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Void this pay run?".Translate(),
                Message = ("Pay run {0} will be reversed and the wage expenses it created will be " +
                           "removed. The run stays in the list so the history survives.").TranslateFormat(run.Id),
                PrimaryButtonText = "Void".Translate(),
                CancelButtonText = "Cancel".Translate(),
                IsPrimaryDestructive = true,
            });

            if (result != ConfirmationResult.Primary)
            {
                return;
            }
        }

        // Snapshot what void is about to remove, so undo can put it back.
        List<Core.Models.Transactions.Expense> removed = run.Lines
            .Where(l => l.ExpenseId is { Length: > 0 })
            .Select(l => data.Expenses.FirstOrDefault(e => e.Id == l.ExpenseId))
            .Where(e => e != null)
            .Select(e => e!)
            .ToList();

        List<(PayRunLine Line, string? ExpenseId)> expenseIds =
            run.Lines.Select(l => (l, l.ExpenseId)).ToList();

        PayRun? reversal = _payroll.Void(data, run);
        if (reversal == null)
        {
            return;
        }

        App.CompanyManager?.MarkAsChanged();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Void pay run {run.Id}",
            () =>
            {
                data.PayRuns.Remove(reversal);
                data.Expenses.AddRange(removed);
                run.Status = PayRunStatus.Approved;
                foreach ((PayRunLine line, string? expenseId) in expenseIds)
                {
                    line.ExpenseId = expenseId;
                }

                App.CompanyManager?.MarkAsChanged();
                Load();
            },
            () =>
            {
                data.PayRuns.Add(reversal);
                data.Expenses.RemoveAll(e => removed.Any(r => r.Id == e.Id));
                run.Status = PayRunStatus.Void;
                foreach ((PayRunLine line, string? _) in expenseIds)
                {
                    line.ExpenseId = null;
                }

                App.CompanyManager?.MarkAsChanged();
                Load();
            }));

        Load();
    }

    #endregion

    protected override void OnSortOrPageChanged() => Filter();

    public void Load()
    {
        _all.Clear();

        List<PayRun>? runs = App.CompanyManager?.CompanyData?.PayRuns;
        if (runs != null)
        {
            _all.AddRange(runs);
        }

        UpdateStatistics();
        Filter();
        OnPropertyChanged(nameof(HasNoPayRuns));
    }

    private void UpdateStatistics()
    {
        int year = DateTime.Today.Year;

        // Money totals include voided runs, because their reversals are included too and the
        // pair cancels to zero. The count does not: a voided run and its reversal are not two
        // more payrolls the owner ran.
        List<PayRun> thisYear = _all
            .Where(r => r.Status != PayRunStatus.Draft && r.PayDate.Year == year)
            .ToList();

        ApprovedCount = thisYear.Count(r => r.Status == PayRunStatus.Approved
                                            && r.VoidsPayRunId is not { Length: > 0 });
        YearToDateGross = CurrencyService.Format(thisYear.Sum(r => r.TotalGross));
        YearToDateRemittance = CurrencyService.Format(thisYear.Sum(r => r.TotalRemittance));
    }

    private void Filter()
    {
        PayRuns.Clear();

        IEnumerable<PayRun> filtered = _all;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string q = SearchQuery.Trim();
            filtered = filtered.Where(r =>
                r.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Lines.Any(l => l.EmployeeName.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        // Newest first: the run someone wants is almost always the last one.
        List<PayRun> ordered = filtered
            .OrderByDescending(r => r.PayDate)
            .ThenByDescending(r => r.Id, StringComparer.Ordinal)
            .ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling((double)ordered.Count / PageSize));
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }

        foreach (PayRun run in ordered.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
        {
            PayRuns.Add(PayRunDisplayItem.From(run));
        }

        PaginationText = PaginationTextHelper.FormatPaginationText(
            ordered.Count, CurrentPage, PageSize, TotalPages, "pay run", "pay runs");

        NotifyPaginationChanged();
    }

    private static string Sanitize(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string result = new(name.Select(c => invalid.Contains(c) || c == ' ' ? '-' : c).ToArray());
        return string.IsNullOrEmpty(result.Trim('-')) ? "employee" : result.Trim('-');
    }
}

/// <summary>One row of the pay runs table, already formatted for display.</summary>
public partial class PayRunDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _payDate = string.Empty;

    [ObservableProperty]
    private string _period = string.Empty;

    [ObservableProperty]
    private string _employees = string.Empty;

    [ObservableProperty]
    private string _gross = string.Empty;

    [ObservableProperty]
    private string _net = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    /// <summary>Only an approved run can be voided, and only it has stubs worth printing.</summary>
    [ObservableProperty]
    private bool _isApproved;

    public static PayRunDisplayItem From(PayRun run) => new()
    {
        Id = run.Id,
        PayDate = run.PayDate.ToString("yyyy-MM-dd"),
        Period = $"{run.PeriodStart:yyyy-MM-dd} to {run.PeriodEnd:yyyy-MM-dd}",
        Employees = run.Lines.Count.ToString(),
        Gross = CurrencyService.Format(run.TotalGross),
        Net = CurrencyService.Format(run.TotalNetPay),
        Status = run.Status switch
        {
            PayRunStatus.Draft => "Draft",
            PayRunStatus.Void => "Void",
            _ => run.VoidsPayRunId is { Length: > 0 } ? "Reversal" : "Approved",
        },
        IsApproved = run.Status == PayRunStatus.Approved && run.VoidsPayRunId is not { Length: > 0 },
    };
}
