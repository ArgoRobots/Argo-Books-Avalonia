using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the self-contained bank statement import modal.
/// Parses a bank statement file, lets the user review and categorise each line,
/// then creates plain Expense/Revenue transactions (no bank-match flag).
/// Entry point: <see cref="OpenAsync"/>.
/// </summary>
public partial class BankStatementImportModalViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private int _includedCount;

    public ObservableCollection<ImportLineRow> Rows { get; } = [];
    public ObservableCollection<Category> AvailableCategories { get; } = [];
    public ObservableCollection<Supplier> AvailableSuppliers { get; } = [];
    public ObservableCollection<Customer> AvailableCustomers { get; } = [];

    // -----------------------------------------------------------------------
    // Entry point
    // -----------------------------------------------------------------------

    /// <summary>
    /// Parses <paramref name="filePath"/> and opens the modal for user review.
    /// Returns immediately without blocking; the modal closes itself after import
    /// or cancel.
    /// </summary>
    public async Task OpenAsync(string filePath)
    {
        List<BankStatementLine> lines;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".pdf")
        {
            // TODO: share with App.ImportPdfStatementAsync
            lines = await ImportPdfStatementAsync(filePath);
        }
        else
        {
            var parser = new BankStatementImportService(App.ErrorLogger);
            if (ext == ".csv")
                lines = await parser.ParseCsvAsync(filePath);
            else
                lines = await parser.ParseExcelAsync(filePath);
        }

        if (lines.Count == 0) return;

        PopulateRows(lines);
        IsOpen = true;
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    [RelayCommand]
    private void Import()
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;

        var toImport = Rows.Where(r => r.IsIncluded).ToList();
        if (toImport.Count == 0) return;

        var resolutions = toImport.Select(r =>
        {
            // Rebuild the line with any edits the user made.
            var line = new BankStatementLine
            {
                Id = string.IsNullOrEmpty(r.SourceLine.Id) ? Guid.NewGuid().ToString("N") : r.SourceLine.Id,
                Date = r.Date.DateTime,
                Description = r.Description,
                Amount = r.CreateAsRevenue ? Math.Abs(r.Amount) : -Math.Abs(r.Amount)
            };

            return new BankLineResolution
            {
                Line = line,
                Type = r.CreateAsRevenue ? BookRecordType.Revenue : BookRecordType.Expense,
                CategoryId = r.ResolvedCategoryId,
                NewCategoryName = null,
                CounterpartyId = r.ResolvedCounterpartyId,
                NewCounterpartyName = r.ResolvedCounterpartyId == null ? r.NewCounterpartyName : null
            };
        }).ToList();

        // Snapshot id counters before CreateFromLines bumps them.
        var preCounters = new IdCounterSnapshot(data.IdCounters);

        // linkToBankLine: false -> plain transactions, no bank-match flag.
        var creation = new BankLineImportService().CreateFromLines(data, resolutions, linkToBankLine: false);

        // Learn a rule per categorised line so the next import is pre-filled.
        var ruleCaptures = new List<RuleLearningCapture>();
        foreach (var res in resolutions.Where(x => x.CategoryId != null))
        {
            var normalized = MerchantNormalizer.Normalize(res.Line.Description);
            var existing = data.BankCategoryRules.FirstOrDefault(r =>
                r.Pattern == normalized && r.MatchType == RuleMatchType.Contains);

            RulePriorState? prior = existing == null ? null : new RulePriorState(
                existing.CategoryId, existing.TransactionType, existing.CounterpartyId,
                existing.Source, existing.UpdatedAt);

            var rule = CategoryRuleService.Learn(data, res.Line.Description, res.CategoryId!, res.Type, res.CounterpartyId);

            var post = new RulePostState(rule.CategoryId, rule.TransactionType, rule.CounterpartyId,
                rule.Source, rule.UpdatedAt);

            ruleCaptures.Add(new RuleLearningCapture(rule, prior, post));
        }

        var postCounters = new IdCounterSnapshot(data.IdCounters);

        App.UndoRedoManager.RecordAction(new DelegateAction(
            "Import bank statement".Translate(),
            () => UndoImport(data, creation, ruleCaptures, preCounters),
            () => RedoImport(data, creation, ruleCaptures, postCounters)));

        App.CompanyManager?.MarkAsChanged();
        IsOpen = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsOpen = false;
    }

    // -----------------------------------------------------------------------
    // Undo / Redo
    // -----------------------------------------------------------------------

    private void UndoImport(CompanyData data, BankImportCreation creation,
        List<RuleLearningCapture> ruleCaptures, IdCounterSnapshot preCounters)
    {
        foreach (var tx in creation.CreatedTransactions)
        {
            if (tx is Expense e) data.Expenses.Remove(e);
            else if (tx is Revenue r) data.Revenues.Remove(r);
        }

        foreach (var entity in creation.CreatedEntities)
        {
            if (entity is Supplier s) data.Suppliers.Remove(s);
            else if (entity is Customer c) data.Customers.Remove(c);
            else if (entity is Category cat) data.Categories.Remove(cat);
        }

        foreach (var cap in ruleCaptures)
        {
            if (cap.Prior == null)
            {
                data.BankCategoryRules.Remove(cap.Rule);
            }
            else
            {
                cap.Rule.CategoryId = cap.Prior.CategoryId;
                cap.Rule.TransactionType = cap.Prior.TransactionType;
                cap.Rule.CounterpartyId = cap.Prior.CounterpartyId;
                cap.Rule.Source = cap.Prior.Source;
                cap.Rule.UpdatedAt = cap.Prior.UpdatedAt;
            }
        }

        preCounters.RestoreTo(data.IdCounters);
        data.MarkAsModified();
        App.CompanyManager?.MarkAsChanged();
    }

    private void RedoImport(CompanyData data, BankImportCreation creation,
        List<RuleLearningCapture> ruleCaptures, IdCounterSnapshot postCounters)
    {
        foreach (var entity in creation.CreatedEntities)
        {
            if (entity is Supplier s && !data.Suppliers.Contains(s)) data.Suppliers.Add(s);
            else if (entity is Customer c && !data.Customers.Contains(c)) data.Customers.Add(c);
            else if (entity is Category cat && !data.Categories.Contains(cat)) data.Categories.Add(cat);
        }

        foreach (var tx in creation.CreatedTransactions)
        {
            if (tx is Expense e && !data.Expenses.Contains(e)) data.Expenses.Add(e);
            else if (tx is Revenue r && !data.Revenues.Contains(r)) data.Revenues.Add(r);
        }

        foreach (var cap in ruleCaptures)
        {
            if (cap.Prior == null)
            {
                if (!data.BankCategoryRules.Contains(cap.Rule))
                    data.BankCategoryRules.Add(cap.Rule);
            }
            else
            {
                cap.Rule.CategoryId = cap.Post.CategoryId;
                cap.Rule.TransactionType = cap.Post.TransactionType;
                cap.Rule.CounterpartyId = cap.Post.CounterpartyId;
                cap.Rule.Source = cap.Post.Source;
                cap.Rule.UpdatedAt = cap.Post.UpdatedAt;
            }
        }

        postCounters.RestoreTo(data.IdCounters);
        data.MarkAsModified();
        App.CompanyManager?.MarkAsChanged();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void PopulateRows(List<BankStatementLine> lines)
    {
        var data = App.CompanyManager?.CompanyData;

        AvailableCategories.Clear();
        AvailableSuppliers.Clear();
        AvailableCustomers.Clear();

        if (data != null)
        {
            foreach (var c in data.Categories.OrderBy(c => c.Name)) AvailableCategories.Add(c);
            foreach (var s in data.Suppliers.OrderBy(s => s.Name)) AvailableSuppliers.Add(s);
            foreach (var c in data.Customers.OrderBy(c => c.Name)) AvailableCustomers.Add(c);
        }

        Rows.Clear();

        foreach (var line in lines)
        {
            var row = new ImportLineRow(line);

            // Rule pre-fill.
            if (data != null)
            {
                var rule = CategoryRuleService.Match(data.BankCategoryRules, line.Description);
                if (rule != null)
                {
                    row.ResolvedCategoryId = rule.CategoryId;
                    if (rule.TransactionType.HasValue)
                        row.CreateAsRevenue = rule.TransactionType.Value == BookRecordType.Revenue;
                    if (rule.CounterpartyId != null)
                        row.ResolvedCounterpartyId = rule.CounterpartyId;
                    row.SyncObjectsFromIds(AvailableCategories, AvailableSuppliers, AvailableCustomers);
                }
            }

            row.PropertyChanged += (_, _) => RefreshIncludedCount();
            Rows.Add(row);
        }

        RefreshIncludedCount();
    }

    private void RefreshIncludedCount()
    {
        IncludedCount = Rows.Count(r => r.IsIncluded);
    }

    /// <summary>
    /// Premium-gated PDF bank statement import.
    /// Reuses the same gates as App.ImportPdfStatementAsync.
    /// </summary>
    private static async Task<List<BankStatementLine>> ImportPdfStatementAsync(string filePath)
    {
        // TODO: share with App.ImportPdfStatementAsync
        if (App.LicenseService?.LoadLicense() != true)
        {
            App.OpenUpgradeModal();
            return [];
        }

        using var usage = new ReceiptUsageService(App.LicenseService, App.ErrorLogger);
        var check = await usage.CheckUsageAsync();
        if (!check.CanScan)
        {
            if (check.ErrorMessage != null)
                await UpgradePromptHelper.ShowUsageCheckFailedAsync(check.ErrorMessage);
            else
                await UpgradePromptHelper.ShowReceiptScanLimitPromptAsync(check.ScanCount, check.MonthlyLimit, check.ResetsAt);
            return [];
        }

        if (App.PdfStatementExtractor == null) return [];

        var bytes = await File.ReadAllBytesAsync(filePath);
        var extracted = await App.PdfStatementExtractor.ExtractAsync(bytes, Path.GetFileName(filePath));
        if (extracted.Count == 0) return [];

        // Let the user review/edit extracted rows via the existing PDF review modal.
        var approved = await (App.PdfStatementReviewModalViewModel?.ReviewAsync(extracted)
            ?? Task.FromResult<List<BankStatementLine>?>(null));
        if (approved == null) return [];

        await usage.IncrementUsageAsync();
        return approved;
    }

    // -----------------------------------------------------------------------
    // Nested helper types (rule capture, id-counter snapshot)
    // -----------------------------------------------------------------------

    private sealed record RulePriorState(
        string CategoryId,
        BookRecordType? TransactionType,
        string? CounterpartyId,
        RuleSource Source,
        DateTime UpdatedAt);

    private sealed record RulePostState(
        string CategoryId,
        BookRecordType? TransactionType,
        string? CounterpartyId,
        RuleSource Source,
        DateTime UpdatedAt);

    private sealed record RuleLearningCapture(
        BankCategoryRule Rule,
        RulePriorState? Prior,
        RulePostState Post);

    private sealed class IdCounterSnapshot
    {
        private readonly int _expense;
        private readonly int _revenue;
        private readonly int _supplier;
        private readonly int _customer;
        private readonly int _category;

        public IdCounterSnapshot(IdCounters counters)
        {
            _expense = counters.Expense;
            _revenue = counters.Revenue;
            _supplier = counters.Supplier;
            _customer = counters.Customer;
            _category = counters.Category;
        }

        public void RestoreTo(IdCounters counters)
        {
            counters.Expense = _expense;
            counters.Revenue = _revenue;
            counters.Supplier = _supplier;
            counters.Customer = _customer;
            counters.Category = _category;
        }
    }
}

/// <summary>
/// Display and resolution wrapper for a single bank statement line in the import modal.
/// </summary>
public partial class ImportLineRow : ObservableObject
{
    public BankStatementLine SourceLine { get; }

    public ImportLineRow(BankStatementLine line)
    {
        SourceLine = line;
        _isIncluded = true;
        _date = line.Date == default ? DateTimeOffset.Now : new DateTimeOffset(line.Date);
        _description = line.Description;
        _amount = line.Amount;
        _createAsRevenue = line.Amount > 0;
    }

    [ObservableProperty] private bool _isIncluded;

    [ObservableProperty] private DateTimeOffset _date;

    [ObservableProperty] private string _description;

    [ObservableProperty] private decimal _amount;

    /// <summary>True = Revenue, False = Expense. Defaults from the sign of the bank line amount.</summary>
    [ObservableProperty] private bool _createAsRevenue;

    [ObservableProperty] private string? _resolvedCategoryId;

    [ObservableProperty] private string? _resolvedCounterpartyId;

    [ObservableProperty] private string? _newCounterpartyName;

    partial void OnResolvedCategoryIdChanged(string? value) { /* hook for future needsReview logic */ }

    partial void OnNewCounterpartyNameChanged(string? value) { /* hook for future needsReview logic */ }

    [RelayCommand] private void SetExpense() => CreateAsRevenue = false;
    [RelayCommand] private void SetRevenue() => CreateAsRevenue = true;

    // Object-typed wrappers for SearchableDropdown (which binds SelectedItem, not SelectedValue).
    private Category? _resolvedCategoryObject;
    public Category? ResolvedCategoryObject
    {
        get => _resolvedCategoryObject;
        set
        {
            if (SetProperty(ref _resolvedCategoryObject, value))
                ResolvedCategoryId = value?.Id;
        }
    }

    private Supplier? _resolvedSupplierObject;
    public Supplier? ResolvedSupplierObject
    {
        get => _resolvedSupplierObject;
        set
        {
            if (SetProperty(ref _resolvedSupplierObject, value))
                ResolvedCounterpartyId = value?.Id;
        }
    }

    private Customer? _resolvedCustomerObject;
    public Customer? ResolvedCustomerObject
    {
        get => _resolvedCustomerObject;
        set
        {
            if (SetProperty(ref _resolvedCustomerObject, value))
                ResolvedCounterpartyId = value?.Id;
        }
    }

    /// <summary>
    /// Syncs the object-typed wrapper properties from the Id fields after a rule pre-fill.
    /// </summary>
    public void SyncObjectsFromIds(
        IEnumerable<Category> categories,
        IEnumerable<Supplier> suppliers,
        IEnumerable<Customer> customers)
    {
        if (ResolvedCategoryId != null)
            _resolvedCategoryObject = categories.FirstOrDefault(c => c.Id == ResolvedCategoryId);

        if (ResolvedCounterpartyId != null)
        {
            _resolvedSupplierObject = suppliers.FirstOrDefault(s => s.Id == ResolvedCounterpartyId);
            _resolvedCustomerObject = customers.FirstOrDefault(c => c.Id == ResolvedCounterpartyId);
        }

        OnPropertyChanged(nameof(ResolvedCategoryObject));
        OnPropertyChanged(nameof(ResolvedSupplierObject));
        OnPropertyChanged(nameof(ResolvedCustomerObject));
    }
}
