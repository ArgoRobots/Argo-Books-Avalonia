using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.BankMatching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Bank Matching modals (the "choose matching book entry" picker).
/// Hosted at the AppShell level so it overlays the whole app, like the other modals.
/// </summary>
public partial class BankMatchingModalsViewModel : ViewModelBase
{
    /// <summary>Raised when the user picks a candidate. The page applies the match.</summary>
    public event EventHandler<BankMatchChosenEventArgs>? CandidateChosen;

    [ObservableProperty]
    private bool _isCandidateModalOpen;

    [ObservableProperty]
    private bool _hasCandidates;

    [ObservableProperty]
    private string _lineDescription = string.Empty;

    /// <summary>True when there are records available to manually pick from.</summary>
    [ObservableProperty]
    private bool _hasManualOptions;

    [ObservableProperty]
    private string? _manualSearchQuery;

    private string? _lineId;

    /// <summary>Suggested (auto-detected) candidates.</summary>
    public ObservableCollection<BankMatchCandidate> CandidateOptions { get; } = [];

    /// <summary>All records the user can manually pick from (direction-filtered), before search.</summary>
    private readonly List<BankMatchCandidate> _allManualOptions = [];

    /// <summary>Manual-pick records currently shown (filtered by the search box).</summary>
    public ObservableCollection<BankMatchCandidate> ManualOptions { get; } = [];

    partial void OnManualSearchQueryChanged(string? value) => FilterManualOptions();

    /// <summary>Opens the candidate picker for a bank line, with suggestions and a full manual list.</summary>
    public void OpenCandidatePicker(string lineId, string lineDescription,
        IEnumerable<BankMatchCandidate> candidates, IEnumerable<BankMatchCandidate> manualOptions)
    {
        _lineId = lineId;
        LineDescription = lineDescription;

        CandidateOptions.Clear();
        foreach (var c in candidates)
            CandidateOptions.Add(c);
        HasCandidates = CandidateOptions.Count > 0;

        _allManualOptions.Clear();
        _allManualOptions.AddRange(manualOptions);
        HasManualOptions = _allManualOptions.Count > 0;
        ManualSearchQuery = null;
        FilterManualOptions();

        IsCandidateModalOpen = true;
    }

    private void FilterManualOptions()
    {
        ManualOptions.Clear();
        IEnumerable<BankMatchCandidate> query = _allManualOptions;
        if (!string.IsNullOrWhiteSpace(ManualSearchQuery))
        {
            var q = ManualSearchQuery.Trim();
            query = query.Where(c =>
                c.RecordDescription.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.RecordAmount.ToString("C2").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.RecordType.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var c in query)
            ManualOptions.Add(c);
    }

    [RelayCommand]
    private void CloseCandidateModal() => IsCandidateModalOpen = false;

    [RelayCommand]
    private void ChooseCandidate(BankMatchCandidate? candidate)
    {
        if (_lineId == null || candidate == null) return;
        IsCandidateModalOpen = false;
        CandidateChosen?.Invoke(this, new BankMatchChosenEventArgs(_lineId, candidate));
    }

    #region Filter modal

    /// <summary>Raised when the user applies filters from the filter modal.</summary>
    public event EventHandler<BankFilterAppliedEventArgs>? FiltersApplied;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private DateTimeOffset? _filterStartDate;

    [ObservableProperty]
    private DateTimeOffset? _filterEndDate;

    [ObservableProperty]
    private string _filterStatus = "All";

    /// <summary>Status filter options for the dropdown.</summary>
    public ObservableCollection<string> StatusOptions { get; } =
        ["All", "Matched", "Suggested", "Unmatched", "Ignored"];

    /// <summary>Opens the filter modal seeded with the current filter values.</summary>
    public void OpenFilterModal(DateTime? startDate, DateTime? endDate, string status)
    {
        FilterStartDate = startDate.HasValue ? new DateTimeOffset(startDate.Value) : null;
        FilterEndDate = endDate.HasValue ? new DateTimeOffset(endDate.Value) : null;
        FilterStatus = string.IsNullOrEmpty(status) ? "All" : status;
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    private void CloseFilterModal() => IsFilterModalOpen = false;

    [RelayCommand]
    private void ApplyFilters()
    {
        IsFilterModalOpen = false;
        FiltersApplied?.Invoke(this, new BankFilterAppliedEventArgs(FilterStartDate, FilterEndDate, FilterStatus));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterStartDate = null;
        FilterEndDate = null;
        FilterStatus = "All";
        IsFilterModalOpen = false;
        FiltersApplied?.Invoke(this, new BankFilterAppliedEventArgs(null, null, "All"));
    }

    #endregion

    #region Missing-records filter modal

    /// <summary>Raised when the user applies filters from the missing-records filter modal.</summary>
    public event EventHandler<MissingFilterAppliedEventArgs>? MissingFiltersApplied;

    [ObservableProperty]
    private bool _isMissingFilterModalOpen;

    [ObservableProperty]
    private DateTimeOffset? _missingFilterStartDate;

    [ObservableProperty]
    private DateTimeOffset? _missingFilterEndDate;

    [ObservableProperty]
    private string _missingFilterType = "All";

    /// <summary>Record-type filter options for the missing-records dropdown.</summary>
    public ObservableCollection<string> TypeOptions { get; } =
        ["All", "Expense", "Revenue"];

    public void OpenMissingFilterModal(DateTime? startDate, DateTime? endDate, string type)
    {
        MissingFilterStartDate = startDate.HasValue ? new DateTimeOffset(startDate.Value) : null;
        MissingFilterEndDate = endDate.HasValue ? new DateTimeOffset(endDate.Value) : null;
        MissingFilterType = string.IsNullOrEmpty(type) ? "All" : type;
        IsMissingFilterModalOpen = true;
    }

    [RelayCommand]
    private void CloseMissingFilterModal() => IsMissingFilterModalOpen = false;

    [RelayCommand]
    private void ApplyMissingFilters()
    {
        IsMissingFilterModalOpen = false;
        MissingFiltersApplied?.Invoke(this, new MissingFilterAppliedEventArgs(MissingFilterStartDate, MissingFilterEndDate, MissingFilterType));
    }

    [RelayCommand]
    private void ClearMissingFilters()
    {
        MissingFilterStartDate = null;
        MissingFilterEndDate = null;
        MissingFilterType = "All";
        IsMissingFilterModalOpen = false;
        MissingFiltersApplied?.Invoke(this, new MissingFilterAppliedEventArgs(null, null, "All"));
    }

    #endregion
}

/// <summary>Event args carrying the applied bank-line filters.</summary>
public class BankFilterAppliedEventArgs(DateTimeOffset? startDate, DateTimeOffset? endDate, string status) : EventArgs
{
    public DateTimeOffset? StartDate { get; } = startDate;
    public DateTimeOffset? EndDate { get; } = endDate;
    public string Status { get; } = status;
}

/// <summary>Event args carrying the applied missing-records filters.</summary>
public class MissingFilterAppliedEventArgs(DateTimeOffset? startDate, DateTimeOffset? endDate, string type) : EventArgs
{
    public DateTimeOffset? StartDate { get; } = startDate;
    public DateTimeOffset? EndDate { get; } = endDate;
    public string Type { get; } = type;
}

/// <summary>Event args carrying the user's chosen candidate for a bank line.</summary>
public class BankMatchChosenEventArgs(string lineId, BankMatchCandidate candidate) : EventArgs
{
    public string LineId { get; } = lineId;
    public BankMatchCandidate Candidate { get; } = candidate;
}
