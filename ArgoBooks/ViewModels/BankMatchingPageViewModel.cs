using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Services;
using ArgoBooks.Helpers;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Bank Matching page. Imports a bank statement (via the smart importer) and
/// matches each line against recorded expenses, revenue, invoices and payments.
/// </summary>
public partial class BankMatchingPageViewModel : SortablePageViewModelBase
{
    private readonly BankMatchingService _matcher = new();
    private readonly BankMatchingOptions _options = new();
    private BankMatchingResult? _result;

    /// <summary>All bank line rows from the last match (unfiltered, unpaged).</summary>
    private readonly List<BankLineRow> _allRows = [];

    /// <summary>All unmatched book records from the last match (unfiltered).</summary>
    private List<BookRecordRef> _allMissing = [];

    public BankMatchingPageViewModel()
    {
        SortColumn = "Date";
        SortDirection = SortDirection.Descending;

        Reload();

        if (App.NavigationService != null)
            App.NavigationService.Navigated += OnNavigated;

        if (App.BankMatchingModalsViewModel != null)
        {
            App.BankMatchingModalsViewModel.CandidateChosen += OnCandidateChosen;
            App.BankMatchingModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.BankMatchingModalsViewModel.MissingFiltersApplied += OnMissingFiltersApplied;
        }
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        if (e.PageName == PageNames.BankMatching)
            Reload();
    }

    private void OnCandidateChosen(object? sender, BankMatchChosenEventArgs e)
    {
        var row = _allRows.FirstOrDefault(r => r.Line.Id == e.LineId);
        if (row != null)
            ConfirmCandidate(row, e.Candidate);
    }

    #region State

    /// <summary>Bank line rows for the current page.</summary>
    public ObservableCollection<BankLineRow> Lines { get; } = [];

    /// <summary>Book records that no bank line matched (possibly missing from the statement).</summary>
    public ObservableCollection<BookRecordRef> UnmatchedBookRecords { get; } = [];

    /// <summary>Resizable column widths for the bank lines table.</summary>
    public Controls.ColumnWidths.BankLinesTableColumnWidths BankLineColumns { get; } = new();

    /// <summary>Resizable column widths for the missing-records table.</summary>
    public Controls.ColumnWidths.MissingRecordsTableColumnWidths MissingColumns { get; } = new();

    public ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    [ObservableProperty]
    private bool _hasSession;

    [ObservableProperty]
    private int _matchedCount;

    [ObservableProperty]
    private int _suggestedCount;

    [ObservableProperty]
    private int _unmatchedLineCount;

    [ObservableProperty]
    private int _unmatchedBookCount;

    [ObservableProperty]
    private string _paginationText = "0 lines";

    public bool HasUnmatchedBook => UnmatchedBookCount > 0;

    partial void OnUnmatchedBookCountChanged(int value) => OnPropertyChanged(nameof(HasUnmatchedBook));

    #endregion

    #region Search, filter, tabs

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        ApplyFiltersAndPaginate();
    }

    // Bank-lines filter state (set from the filter modal).
    [ObservableProperty]
    private DateTime? _filterStartDate;

    [ObservableProperty]
    private DateTime? _filterEndDate;

    [ObservableProperty]
    private string _filterStatus = "All";

    // Missing-records search/filter/sort/pagination state (independent of the bank lines table).
    [ObservableProperty]
    private string? _missingSearchQuery;

    partial void OnMissingSearchQueryChanged(string? value)
    {
        MissingCurrentPage = 1;
        RefreshMissing();
    }

    [ObservableProperty]
    private DateTime? _missingFilterStartDate;

    [ObservableProperty]
    private DateTime? _missingFilterEndDate;

    [ObservableProperty]
    private string _missingFilterType = "All";

    [ObservableProperty]
    private string _missingSortColumn = "Date";

    [ObservableProperty]
    private SortDirection _missingSortDirection = SortDirection.Descending;

    [ObservableProperty]
    private int _missingCurrentPage = 1;

    [ObservableProperty]
    private int _missingTotalPages = 1;

    [ObservableProperty]
    private int _missingPageSize = 10;

    [ObservableProperty]
    private string _missingPaginationText = "0 records";

    partial void OnMissingCurrentPageChanged(int value) => RefreshMissing();

    partial void OnMissingPageSizeChanged(int value)
    {
        MissingCurrentPage = 1;
        RefreshMissing();
    }

    /// <summary>0 = Bank lines, 1 = Missing from statement.</summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    public bool IsLinesTab => SelectedTabIndex == 0;
    public bool IsMissingTab => SelectedTabIndex == 1;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsLinesTab));
        OnPropertyChanged(nameof(IsMissingTab));
    }

    #endregion

    #region Loading and matching

    /// <summary>Reloads from all imported sessions (lines accumulate across imports) and matches.</summary>
    public void Reload()
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null || data.BankImportSessions.Count == 0)
        {
            _result = null;
            HasSession = false;
            _allRows.Clear();
            _allMissing = [];
            Lines.Clear();
            UnmatchedBookRecords.Clear();
            ResetCounts();
            return;
        }

        HasSession = true;
        RunMatchingAndRebuild();
    }

    private void RunMatchingAndRebuild()
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;

        var allLines = data.BankImportSessions.SelectMany(s => s.Lines).ToList();
        _result = _matcher.MatchDeterministic(allLines, data, _options);
        _candidatesByLineId = _result.CandidatesByLineId;

        _allRows.Clear();
        foreach (var line in _result.Lines)
        {
            var top = ResolveTopCandidate(line)
                      ?? (line.MatchStatus == BankLineMatchStatus.Matched ? BuildMatchedCandidate(line) : null);
            _allRows.Add(new BankLineRow(line, top));
        }

        _allMissing = _result.UnmatchedBookRecords;
        UnmatchedBookCount = _allMissing.Count;
        RefreshMissing();

        RecomputeCounts();
        ApplyFiltersAndPaginate();
    }

    /// <summary>Applies the missing-tab search/filter/sort and pagination to produce the visible records.</summary>
    private void RefreshMissing()
    {
        IEnumerable<BookRecordRef> query = _allMissing;

        if (MissingFilterType != "All")
            query = query.Where(r => r.Type.ToString() == MissingFilterType);

        if (MissingFilterStartDate.HasValue)
            query = query.Where(r => r.Date == DateTime.MinValue || r.Date.Date >= MissingFilterStartDate.Value.Date);
        if (MissingFilterEndDate.HasValue)
            query = query.Where(r => r.Date == DateTime.MinValue || r.Date.Date <= MissingFilterEndDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(MissingSearchQuery))
        {
            var q = MissingSearchQuery.Trim();
            query = query.Where(r =>
                r.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Amount.ToString("C2").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Type.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList().ApplySort(
            MissingSortColumn,
            MissingSortDirection,
            new Dictionary<string, Func<BookRecordRef, object?>>
            {
                ["Date"] = r => r.Date,
                ["Description"] = r => r.Description,
                ["Amount"] = r => r.Amount
            },
            r => r.Date);

        var totalCount = filtered.Count;
        MissingTotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / MissingPageSize));
        if (MissingCurrentPage > MissingTotalPages) MissingCurrentPage = MissingTotalPages;
        MissingPaginationText = PaginationTextHelper.FormatPaginationText(totalCount, MissingCurrentPage, MissingPageSize, MissingTotalPages, "record");

        UnmatchedBookRecords.Clear();
        foreach (var r in filtered.Skip((MissingCurrentPage - 1) * MissingPageSize).Take(MissingPageSize))
            UnmatchedBookRecords.Add(r);
    }

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => ApplyFiltersAndPaginate();

    /// <summary>Applies search/filter, sorting and pagination to produce the visible page of rows.</summary>
    private void ApplyFiltersAndPaginate()
    {
        IEnumerable<BankLineRow> query = _allRows;

        if (FilterStatus != "All")
            query = query.Where(r => StatusName(r.Line.MatchStatus) == FilterStatus);

        if (FilterStartDate.HasValue)
            query = query.Where(r => r.Line.Date == DateTime.MinValue || r.Line.Date.Date >= FilterStartDate.Value.Date);
        if (FilterEndDate.HasValue)
            query = query.Where(r => r.Line.Date == DateTime.MinValue || r.Line.Date.Date <= FilterEndDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            query = query.Where(r =>
                r.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.MatchedDisplay.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.AmountDisplay.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Line.RawReference.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList().ApplySort(
            SortColumn,
            SortDirection,
            new Dictionary<string, Func<BankLineRow, object?>>
            {
                ["Date"] = r => r.Line.Date,
                ["Description"] = r => r.Description,
                ["Amount"] = r => r.Line.Amount,
                ["Status"] = r => StatusName(r.Line.MatchStatus)
            },
            r => r.Line.Date);

        var totalCount = filtered.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        UpdatePageNumbers();
        PaginationText = PaginationTextHelper.FormatPaginationText(totalCount, CurrentPage, PageSize, TotalPages, "line");

        Lines.Clear();
        foreach (var row in filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            Lines.Add(row);
    }

    protected override void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, startPage + 4);
        startPage = Math.Max(1, endPage - 4);
        for (var i = startPage; i <= endPage; i++)
            PageNumbers.Add(i);
    }

    private Dictionary<string, List<BankMatchCandidate>> _candidatesByLineId = [];

    private BankMatchCandidate? ResolveTopCandidate(Core.Models.BankMatching.BankStatementLine line) =>
        _candidatesByLineId.TryGetValue(line.Id, out var list) ? list.FirstOrDefault() : null;

    private static string StatusName(BankLineMatchStatus status) => status switch
    {
        BankLineMatchStatus.Matched => "Matched",
        BankLineMatchStatus.Suggested => "Suggested",
        BankLineMatchStatus.Ignored => "Ignored",
        _ => "Unmatched"
    };

    /// <summary>
    /// Builds a display candidate for an already-matched line by looking up its record in the
    /// company data (used for auto-matches, which aren't in the candidate map).
    /// </summary>
    private BankMatchCandidate? BuildMatchedCandidate(Core.Models.BankMatching.BankStatementLine line)
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null || line.MatchedRecordType is not { } type || line.MatchedRecordId is not { } id)
            return null;

        var desc = type switch
        {
            BookRecordType.Expense => data.Expenses.FirstOrDefault(e => e.Id == id)?.Description,
            BookRecordType.Revenue => data.Revenues.FirstOrDefault(r => r.Id == id)?.Description,
            BookRecordType.Invoice => data.Invoices.FirstOrDefault(i => i.Id == id)?.InvoiceNumber,
            BookRecordType.Payment => data.Payments.FirstOrDefault(p => p.Id == id) is { } pmt
                ? (string.IsNullOrWhiteSpace(pmt.Notes) ? pmt.ReferenceNumber : pmt.Notes)
                : null,
            _ => null
        };
        if (desc == null) return null;

        return new BankMatchCandidate
        {
            LineId = line.Id,
            RecordType = type,
            RecordId = id,
            RecordDescription = string.IsNullOrWhiteSpace(desc) ? id : desc,
            RecordAmount = line.Amount,
            Confidence = line.MatchConfidence
        };
    }

    private void ResetCounts()
    {
        MatchedCount = SuggestedCount = UnmatchedLineCount = UnmatchedBookCount = 0;
        PaginationText = PaginationTextHelper.FormatPaginationText(0, 1, PageSize, 1, "line");
    }

    private void RecomputeCounts()
    {
        MatchedCount = _allRows.Count(r => r.Line.MatchStatus == BankLineMatchStatus.Matched);
        SuggestedCount = _allRows.Count(r => r.Line.MatchStatus == BankLineMatchStatus.Suggested);
        UnmatchedLineCount = _allRows.Count(r => r.Line.MatchStatus == BankLineMatchStatus.Unmatched);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void ImportStatement() => ImportRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenFilterModal() =>
        App.BankMatchingModalsViewModel?.OpenFilterModal(FilterStartDate, FilterEndDate, FilterStatus);

    private void OnFiltersApplied(object? sender, BankFilterAppliedEventArgs e)
    {
        FilterStartDate = e.StartDate?.DateTime;
        FilterEndDate = e.EndDate?.DateTime;
        FilterStatus = e.Status;
        CurrentPage = 1;
        ApplyFiltersAndPaginate();
    }

    [RelayCommand]
    private void OpenMissingFilterModal() =>
        App.BankMatchingModalsViewModel?.OpenMissingFilterModal(MissingFilterStartDate, MissingFilterEndDate, MissingFilterType);

    private void OnMissingFiltersApplied(object? sender, MissingFilterAppliedEventArgs e)
    {
        MissingFilterStartDate = e.StartDate?.DateTime;
        MissingFilterEndDate = e.EndDate?.DateTime;
        MissingFilterType = e.Type;
        MissingCurrentPage = 1;
        RefreshMissing();
    }

    /// <summary>Sorts the missing-records table by a column (toggles direction on repeat clicks).</summary>
    [RelayCommand]
    private void MissingSortBy(string? column)
    {
        if (string.IsNullOrEmpty(column)) return;

        if (MissingSortColumn == column)
        {
            MissingSortDirection = MissingSortDirection switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.None,
                _ => SortDirection.Ascending
            };
        }
        else
        {
            MissingSortColumn = column;
            MissingSortDirection = SortDirection.Ascending;
        }

        MissingCurrentPage = 1;
        RefreshMissing();
    }

    [RelayCommand]
    private void MissingGoToPreviousPage()
    {
        if (MissingCurrentPage > 1) MissingCurrentPage--;
    }

    [RelayCommand]
    private void MissingGoToNextPage()
    {
        if (MissingCurrentPage < MissingTotalPages) MissingCurrentPage++;
    }

    [RelayCommand]
    private void MissingGoToPage(int page)
    {
        if (page >= 1 && page <= MissingTotalPages && page != MissingCurrentPage)
            MissingCurrentPage = page;
    }

    [RelayCommand]
    private void AcceptSuggestion(BankLineRow? row)
    {
        if (row?.TopCandidate == null) return;
        ConfirmCandidate(row, row.TopCandidate);
    }

    /// <summary>Marks a line as ignored (e.g., internal transfer). Tracked in undo/redo.</summary>
    [RelayCommand]
    private void IgnoreLine(BankLineRow? row)
    {
        if (row == null) return;
        var line = row.Line;
        SetLineIgnored(line);
        App.UndoRedoManager.RecordAction(new DelegateAction(
            "Ignore bank line".Translate(),
            () => SetLineRestored(line),
            () => SetLineIgnored(line)));
        App.CompanyManager?.MarkAsChanged();
    }

    /// <summary>Restores a previously ignored line, recomputing its candidate matches. Tracked in undo/redo.</summary>
    [RelayCommand]
    private void RestoreLine(BankLineRow? row)
    {
        if (row == null) return;
        var line = row.Line;
        SetLineRestored(line);
        App.UndoRedoManager.RecordAction(new DelegateAction(
            "Restore bank line".Translate(),
            () => SetLineIgnored(line),
            () => SetLineRestored(line)));
        App.CompanyManager?.MarkAsChanged();
    }

    /// <summary>Unlinks a confirmed match, clearing the flag on the book record.</summary>
    [RelayCommand]
    private void UnlinkMatch(BankLineRow? row)
    {
        var data = App.CompanyManager?.CompanyData;
        if (row == null || data == null) return;
        _matcher.UnlinkMatch(row.Line, data);
        row.Refresh(null);
        RecomputeCounts();
        ApplyFiltersAndPaginate();
    }

    /// <summary>Opens the AppShell-hosted picker so the user can choose among candidates.</summary>
    [RelayCommand]
    private void OpenCandidatePicker(BankLineRow? row)
    {
        if (row == null) return;
        var candidates = _candidatesByLineId.TryGetValue(row.Line.Id, out var list) ? list : [];
        App.BankMatchingModalsViewModel?.OpenCandidatePicker(row.Line.Id, row.Description, candidates);
    }

    private void SetLineIgnored(Core.Models.BankMatching.BankStatementLine line)
    {
        line.MatchStatus = BankLineMatchStatus.Ignored;
        _candidatesByLineId.Remove(line.Id);
        RefreshRow(line, null);
        App.CompanyManager?.CompanyData?.MarkAsModified();
        RecomputeCounts();
        ApplyFiltersAndPaginate();
    }

    private void SetLineRestored(Core.Models.BankMatching.BankStatementLine line)
    {
        var data = App.CompanyManager?.CompanyData;
        var candidates = data != null ? _matcher.FindCandidates(line, data, _options) : [];
        if (candidates.Count > 0)
        {
            _candidatesByLineId[line.Id] = candidates;
            line.MatchStatus = BankLineMatchStatus.Suggested;
            RefreshRow(line, null, candidates[0]);
        }
        else
        {
            _candidatesByLineId.Remove(line.Id);
            line.MatchStatus = BankLineMatchStatus.Unmatched;
            RefreshRow(line, null);
        }
        data?.MarkAsModified();
        RecomputeCounts();
        ApplyFiltersAndPaginate();
    }

    private void RefreshRow(Core.Models.BankMatching.BankStatementLine line, BankMatchCandidate? confirmed, BankMatchCandidate? suggested = null)
    {
        var row = _allRows.FirstOrDefault(r => r.Line.Id == line.Id);
        row?.Refresh(confirmed, suggested);
    }

    private void ConfirmCandidate(BankLineRow row, BankMatchCandidate candidate)
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null) return;
        _matcher.ConfirmMatch(row.Line, candidate, data);
        _candidatesByLineId.Remove(row.Line.Id);
        row.Refresh(candidate);
        RecomputeCounts();
        ApplyFiltersAndPaginate();
    }

    #endregion

    #region Events

    public event EventHandler? ImportRequested;

    #endregion
}

/// <summary>Display wrapper for a bank statement line in the table.</summary>
public partial class BankLineRow : ObservableObject
{
    public Core.Models.BankMatching.BankStatementLine Line { get; }

    public BankLineRow(Core.Models.BankMatching.BankStatementLine line, BankMatchCandidate? topCandidate)
    {
        Line = line;
        TopCandidate = topCandidate;
        Refresh(topCandidate);
    }

    [ObservableProperty]
    private BankMatchCandidate? _topCandidate;

    [ObservableProperty]
    private string _matchedDisplay = string.Empty;

    public string DateDisplay => Line.Date == DateTime.MinValue ? "-" : Line.Date.ToString("MMM dd, yyyy");
    public string Description => string.IsNullOrWhiteSpace(Line.Description) ? "-" : Line.Description;
    public string AmountDisplay => Line.Amount.ToString("C2");
    public string AmountColor => Line.Amount < 0 ? AppColors.ExpenseRed : AppColors.Success;

    public string StatusDisplay => Line.MatchStatus switch
    {
        BankLineMatchStatus.Matched => "Matched",
        BankLineMatchStatus.Suggested => "Suggested",
        BankLineMatchStatus.Ignored => "Ignored",
        _ => "Unmatched"
    };

    public string StatusColor => Line.MatchStatus switch
    {
        BankLineMatchStatus.Matched => AppColors.Success,
        BankLineMatchStatus.Suggested => AppColors.Warning,
        BankLineMatchStatus.Ignored => AppColors.GrayMedium,
        _ => AppColors.ExpenseRed
    };

    public bool IsMatched => Line.MatchStatus == BankLineMatchStatus.Matched;
    public bool IsSuggested => Line.MatchStatus == BankLineMatchStatus.Suggested;
    public bool IsUnmatched => Line.MatchStatus == BankLineMatchStatus.Unmatched;
    public bool IsIgnored => Line.MatchStatus == BankLineMatchStatus.Ignored;

    /// <summary>True when the line can still be matched or ignored (not matched, not ignored).</summary>
    public bool IsActionable => IsUnmatched || IsSuggested;

    /// <summary>Recomputes display properties after the line's status changes.</summary>
    public void Refresh(BankMatchCandidate? confirmed, BankMatchCandidate? suggested = null)
    {
        if (confirmed != null) TopCandidate = confirmed;
        else if (suggested != null) TopCandidate = suggested;

        MatchedDisplay = Line.MatchStatus switch
        {
            BankLineMatchStatus.Matched => TopCandidate?.RecordDescription is { Length: > 0 } d ? d : "Matched record",
            BankLineMatchStatus.Suggested => TopCandidate != null
                ? $"{TopCandidate.RecordDescription} ({TopCandidate.Confidence:P0})"
                : "-",
            _ => "-"
        };

        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(IsMatched));
        OnPropertyChanged(nameof(IsSuggested));
        OnPropertyChanged(nameof(IsUnmatched));
        OnPropertyChanged(nameof(IsIgnored));
        OnPropertyChanged(nameof(IsActionable));
    }
}
