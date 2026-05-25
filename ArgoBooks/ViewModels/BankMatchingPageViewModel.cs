using System.Collections.ObjectModel;
using ArgoBooks.Core;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Bank Matching page. Imports a bank statement (via the smart importer) and
/// matches each line against recorded expenses, revenue, invoices and payments.
/// </summary>
public partial class BankMatchingPageViewModel : ViewModelBase
{
    private readonly BankMatchingService _matcher = new();
    private readonly BankMatchingOptions _options = new();
    private BankMatchingResult? _result;

    public BankMatchingPageViewModel()
    {
        Reload();

        if (App.NavigationService != null)
            App.NavigationService.Navigated += OnNavigated;

        // The candidate picker lives in the AppShell-hosted modal; apply the user's choice here.
        if (App.BankMatchingModalsViewModel != null)
            App.BankMatchingModalsViewModel.CandidateChosen += OnCandidateChosen;

        // Filter the view by the shared date range used on the dashboard/analytics pages.
        ChartSettingsService.Instance.DateRangeChanged += OnDateRangeChanged;
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        if (e.PageName == PageNames.BankMatching)
            Reload();
    }

    private void OnDateRangeChanged(object? sender, string e) => RefreshDisplay();

    private void OnCandidateChosen(object? sender, BankMatchChosenEventArgs e)
    {
        var row = Lines.FirstOrDefault(r => r.Line.Id == e.LineId);
        if (row != null)
            ConfirmCandidate(row, e.Candidate);
    }

    #region State

    /// <summary>Rows shown in the table.</summary>
    public ObservableCollection<BankLineRow> Lines { get; } = [];

    /// <summary>Book records that no bank line matched (possibly missing from the statement).</summary>
    public ObservableCollection<BookRecordRef> UnmatchedBookRecords { get; } = [];

    [ObservableProperty]
    private bool _hasSession;

    /// <summary>True when at least one bank line is visible in the current date range.</summary>
    [ObservableProperty]
    private bool _hasLines;

    [ObservableProperty]
    private int _matchedCount;

    [ObservableProperty]
    private int _suggestedCount;

    [ObservableProperty]
    private int _unmatchedLineCount;

    [ObservableProperty]
    private int _unmatchedBookCount;

    [ObservableProperty]
    private bool _isAiBusy;

    /// <summary>True when there are unmatched lines the AI could be asked to suggest matches for.</summary>
    public bool CanRunAi => UnmatchedLineCount > 0 && !IsAiBusy;

    /// <summary>True when there are book records the statement did not account for.</summary>
    public bool HasUnmatchedBook => UnmatchedBookCount > 0;

    partial void OnUnmatchedLineCountChanged(int value) => OnPropertyChanged(nameof(CanRunAi));
    partial void OnIsAiBusyChanged(bool value) => OnPropertyChanged(nameof(CanRunAi));
    partial void OnUnmatchedBookCountChanged(int value) => OnPropertyChanged(nameof(HasUnmatchedBook));

    #endregion

    #region Tabs

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

    /// <summary>
    /// Reloads from all imported sessions (lines accumulate across imports), runs matching,
    /// and rebuilds the view.
    /// </summary>
    public void Reload()
    {
        var data = App.CompanyManager?.CompanyData;
        if (data == null || data.BankImportSessions.Count == 0)
        {
            _result = null;
            HasSession = false;
            HasLines = false;
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

        // Match across every imported statement so previously imported lines are not lost.
        var allLines = data.BankImportSessions.SelectMany(s => s.Lines).ToList();
        _result = _matcher.MatchDeterministic(allLines, data, _options);
        _candidatesByLineId = _result.CandidatesByLineId;

        RefreshDisplay();
    }

    /// <summary>Rebuilds the visible rows from the last match result, filtered by the selected date range.</summary>
    private void RefreshDisplay()
    {
        if (_result == null) return;

        var settings = ChartSettingsService.Instance;
        var start = settings.StartDate.Date;
        var end = settings.EndDate.Date;
        bool InRange(DateTime d) => d == DateTime.MinValue || (d.Date >= start && d.Date <= end);

        Lines.Clear();
        foreach (var line in _result.Lines.Where(l => InRange(l.Date)))
        {
            // Auto-matched lines aren't in CandidatesByLineId, so resolve their record for display.
            var top = ResolveTopCandidate(line)
                      ?? (line.MatchStatus == BankLineMatchStatus.Matched ? BuildMatchedCandidate(line) : null);
            Lines.Add(new BankLineRow(line, top));
        }

        UnmatchedBookRecords.Clear();
        foreach (var r in _result.UnmatchedBookRecords.Where(r => InRange(r.Date)))
            UnmatchedBookRecords.Add(r);

        RecomputeCounts();
        UnmatchedBookCount = UnmatchedBookRecords.Count;
        HasLines = Lines.Count > 0;
    }

    private Dictionary<string, List<BankMatchCandidate>> _candidatesByLineId = [];

    private BankMatchCandidate? ResolveTopCandidate(Core.Models.BankMatching.BankStatementLine line) =>
        _candidatesByLineId.TryGetValue(line.Id, out var list) ? list.FirstOrDefault() : null;

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
    }

    #endregion

    #region Commands

    /// <summary>Asks the host to open a file picker and import a bank statement.</summary>
    [RelayCommand]
    private void ImportStatement() => ImportRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Asks the host to run AI suggestions over the remaining unmatched lines.</summary>
    [RelayCommand]
    private void RunAiSuggestions()
    {
        if (CanRunAi)
            AiSuggestionsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Accepts the top suggestion for a line.</summary>
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
            () => SetLineRestored(line),   // undo: bring the line back
            () => SetLineIgnored(line)));  // redo: ignore again
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
            () => SetLineIgnored(line),    // undo: ignore again
            () => SetLineRestored(line))); // redo: restore again
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
    }

    /// <summary>Opens the AppShell-hosted picker so the user can choose among candidates.</summary>
    [RelayCommand]
    private void OpenCandidatePicker(BankLineRow? row)
    {
        if (row == null) return;
        var candidates = _candidatesByLineId.TryGetValue(row.Line.Id, out var list) ? list : [];
        App.BankMatchingModalsViewModel?.OpenCandidatePicker(row.Line.Id, row.Description, candidates);
    }

    private void SetLineIgnored(BankStatementLine line)
    {
        line.MatchStatus = BankLineMatchStatus.Ignored;
        _candidatesByLineId.Remove(line.Id);
        RefreshRow(line, null);
        App.CompanyManager?.CompanyData?.MarkAsModified();
        RecomputeCounts();
    }

    private void SetLineRestored(BankStatementLine line)
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
    }

    private void RefreshRow(BankStatementLine line, BankMatchCandidate? confirmed, BankMatchCandidate? suggested = null)
    {
        var row = Lines.FirstOrDefault(r => r.Line.Id == line.Id);
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
    }

    private void RecomputeCounts()
    {
        MatchedCount = Lines.Count(l => l.Line.MatchStatus == BankLineMatchStatus.Matched);
        SuggestedCount = Lines.Count(l => l.Line.MatchStatus == BankLineMatchStatus.Suggested);
        UnmatchedLineCount = Lines.Count(l => l.Line.MatchStatus == BankLineMatchStatus.Unmatched);
    }

    /// <summary>
    /// Applies suggestions to the matching lines (never auto-confirmed). Returns the number of
    /// lines that gained a new suggestion.
    /// </summary>
    public int ApplyAiSuggestions(IReadOnlyDictionary<string, List<BankMatchCandidate>> suggestions)
    {
        var applied = 0;
        foreach (var (lineId, candidates) in suggestions)
        {
            if (candidates.Count == 0) continue;
            _candidatesByLineId[lineId] = candidates;
            var row = Lines.FirstOrDefault(r => r.Line.Id == lineId);
            if (row != null && row.Line.MatchStatus == BankLineMatchStatus.Unmatched)
            {
                row.Line.MatchStatus = BankLineMatchStatus.Suggested;
                row.Refresh(null, candidates.First());
                applied++;
            }
        }
        RecomputeCounts();
        return applied;
    }

    /// <summary>The lines still unmatched after deterministic matching (for the AI step).</summary>
    public IReadOnlyList<Core.Models.BankMatching.BankStatementLine> GetUnmatchedLines() =>
        Lines.Where(r => r.Line.MatchStatus == BankLineMatchStatus.Unmatched).Select(r => r.Line).ToList();

    public BankMatchingOptions Options => _options;

    #endregion

    #region Events

    /// <summary>Raised when the user clicks Import statement.</summary>
    public event EventHandler? ImportRequested;

    /// <summary>Raised when the user clicks Run AI suggestions.</summary>
    public event EventHandler? AiSuggestionsRequested;

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
