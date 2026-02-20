using System.Collections.ObjectModel;
using ArgoBooks.Core.Models;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Display item for a single audit event in the version history timeline.
/// </summary>
public partial class VersionHistoryItem : ObservableObject
{
    /// <summary>
    /// The underlying audit event.
    /// </summary>
    public AuditEvent Event { get; }

    /// <summary>
    /// Reference to the parent ViewModel for command binding.
    /// </summary>
    public VersionHistoryModalViewModel Parent { get; }

    /// <summary>
    /// The event description.
    /// </summary>
    public string Description => Event.Description;

    /// <summary>
    /// Formatted time string (e.g., "2:41 PM").
    /// </summary>
    public string TimeText => Event.Timestamp.ToLocalTime().ToString("h:mm tt");

    /// <summary>
    /// The entity type label.
    /// </summary>
    public string EntityTypeText => !string.IsNullOrEmpty(Event.EntityType) ? Event.EntityType : "";

    /// <summary>
    /// The action type for visual indicators.
    /// </summary>
    public AuditAction Action => Event.Action;

    /// <summary>
    /// Whether this event has been undone.
    /// </summary>
    [ObservableProperty]
    private bool _isUndone;

    /// <summary>
    /// Whether undo is available for this event (current session only).
    /// </summary>
    [ObservableProperty]
    private bool _canUndo;

    /// <summary>
    /// Whether redo is available for this event (was undone, can be reapplied).
    /// </summary>
    [ObservableProperty]
    private bool _canRedo;

    /// <summary>
    /// Whether the undo/redo button should be visible at all.
    /// </summary>
    public bool ShowUndoRedoButton => CanUndo || CanRedo;

    /// <summary>
    /// Formatted summary of field-level changes for Modified events (e.g., "Name: 'test' → '123test'").
    /// </summary>
    public string? ChangesSummary
    {
        get
        {
            if (Event.Changes == null || Event.Changes.Count == 0)
                return null;
            return string.Join("\n", Event.Changes.Select(c =>
                $"{c.Key}: '{c.Value.OldValue}' → '{c.Value.NewValue}'"));
        }
    }

    /// <summary>
    /// Whether there are field-level changes to display.
    /// </summary>
    public bool HasChanges => Event.Changes is { Count: > 0 };

    /// <summary>
    /// Gets the ChangeType equivalent for reusing existing converters.
    /// </summary>
    public ChangeType ChangeType => Action switch
    {
        AuditAction.Added => ChangeType.Added,
        AuditAction.Deleted => ChangeType.Deleted,
        _ => ChangeType.Modified
    };

    /// <summary>
    /// Nested undo/redo sub-items for this event.
    /// </summary>
    public ObservableCollection<VersionHistorySubItem> SubItems { get; } = [];

    /// <summary>
    /// Whether this event has any sub-items.
    /// </summary>
    public bool HasSubItems => SubItems.Count > 0;

    public VersionHistoryItem(AuditEvent evt, VersionHistoryModalViewModel parent, EventLogService eventLogService)
    {
        Event = evt;
        Parent = parent;
        IsUndone = evt.IsUndone;
        CanUndo = eventLogService.CanUndoEvent(evt);
        CanRedo = eventLogService.CanRedoEvent(evt);
    }

    /// <summary>
    /// Refreshes the undo/redo state from the EventLogService.
    /// </summary>
    public void RefreshUndoRedoState(EventLogService eventLogService)
    {
        IsUndone = Event.IsUndone;
        CanUndo = eventLogService.CanUndoEvent(Event);
        CanRedo = eventLogService.CanRedoEvent(Event);
    }
}

/// <summary>
/// Display item for an undo/redo sub-entry nested under a parent event.
/// </summary>
public class VersionHistorySubItem
{
    /// <summary>
    /// Whether this is an undo (true) or redo (false).
    /// </summary>
    public bool IsUndo { get; init; }

    /// <summary>
    /// Label text ("Undone" or "Redone").
    /// </summary>
    public string Label => IsUndo ? "Undone" : "Redone";

    /// <summary>
    /// Formatted time string (e.g., "3:42 PM").
    /// </summary>
    public string TimeText { get; init; } = string.Empty;
}

/// <summary>
/// Groups version history items by date for the timeline view.
/// </summary>
public class VersionHistoryGroup
{
    /// <summary>
    /// The date label (e.g., "Today, Feb 9", "Yesterday, Feb 8", "Feb 7, 2026").
    /// </summary>
    public string DateLabel { get; set; } = string.Empty;

    /// <summary>
    /// The events for this date, newest first.
    /// </summary>
    public ObservableCollection<VersionHistoryItem> Items { get; } = [];
}

/// <summary>
/// ViewModel for the version history modal.
/// Displays a chronological timeline of all changes with search, filtering, and selective undo/redo.
/// </summary>
public partial class VersionHistoryModalViewModel : ViewModelBase
{
    private EventLogService? _eventLogService;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string? _selectedEntityTypeFilter;

    [ObservableProperty]
    private string? _selectedActionFilter;

    [ObservableProperty]
    private int _totalEventCount;

    [ObservableProperty]
    private int _filteredEventCount;

    [ObservableProperty]
    private bool _hasEvents;

    [ObservableProperty]
    private bool _isFiltered;

    [ObservableProperty]
    private bool _showNoResults;

    /// <summary>
    /// Grouped timeline items (by date).
    /// </summary>
    public ObservableCollection<VersionHistoryGroup> Groups { get; } = [];

    /// <summary>
    /// Available entity type filters.
    /// </summary>
    public ObservableCollection<string> EntityTypeFilters { get; } = [];

    /// <summary>
    /// Available action type filters.
    /// </summary>
    public ObservableCollection<string> ActionFilters { get; } =
    [
        "All",
        "Added",
        "Modified",
        "Deleted",
        "Undone"
    ];

    /// <summary>
    /// Sets the event log service reference. Called during initialization.
    /// </summary>
    public void SetEventLogService(EventLogService eventLogService)
    {
        if (_eventLogService != null)
            _eventLogService.EventsChanged -= OnEventsChanged;

        _eventLogService = eventLogService;
        _eventLogService.EventsChanged += OnEventsChanged;
    }

    /// <summary>
    /// Opens the modal and refreshes the event list.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        RefreshEvents();
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Undoes a specific history item.
    /// </summary>
    [RelayCommand]
    private void UndoItem(VersionHistoryItem? item)
    {
        if (item == null || _eventLogService == null)
            return;

        if (_eventLogService.UndoEvent(item.Event))
        {
            App.CompanyManager?.MarkAsChanged();

            // Refresh the current page so the UI reflects the undone change
            App.NavigationService?.RefreshCurrentPage();
        }
    }

    /// <summary>
    /// Redoes a previously undone history item.
    /// </summary>
    [RelayCommand]
    private void RedoItem(VersionHistoryItem? item)
    {
        if (item == null || _eventLogService == null)
            return;

        if (_eventLogService.RedoEvent(item.Event))
        {
            App.CompanyManager?.MarkAsChanged();

            // Refresh the current page so the UI reflects the redone change
            App.NavigationService?.RefreshCurrentPage();
        }
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        SelectedEntityTypeFilter = null;
        SelectedActionFilter = null;
    }

    partial void OnSearchQueryChanged(string value) => RefreshEvents();
    partial void OnSelectedEntityTypeFilterChanged(string? value) => RefreshEvents();
    partial void OnSelectedActionFilterChanged(string? value) => RefreshEvents();

    private void OnEventsChanged(object? sender, EventArgs e)
    {
        // Re-render the timeline when events change (new event recorded, undo/redo)
        if (IsOpen)
        {
            RefreshEvents();
        }
    }

    /// <summary>
    /// Rebuilds the grouped timeline from the event log.
    /// Undo/redo meta-events are nested as sub-items under their parent events.
    /// </summary>
    private void RefreshEvents()
    {
        if (_eventLogService == null)
            return;

        // Parse filter — "Undone" means "show events that have been undone"
        AuditAction? actionFilter = SelectedActionFilter switch
        {
            "Added" => AuditAction.Added,
            "Modified" => AuditAction.Modified,
            "Deleted" => AuditAction.Deleted,
            _ => null
        };
        var filterUndoneOnly = SelectedActionFilter == "Undone";

        var entityTypeFilter = SelectedEntityTypeFilter is "All" or null
            ? null
            : SelectedEntityTypeFilter;

        // Get all events (no action filter yet — we need meta-events for nesting)
        var allEvents = _eventLogService.GetFilteredEvents(
            searchQuery: null,
            actionFilter: null,
            entityTypeFilter: entityTypeFilter)
            .ToList();

        // Meta-events (Undone/Redone) are always included as sub-items (even unsaved)
        // so that clicking undo in the modal immediately shows the nested entry
        var metaEvents = allEvents
            .Where(e => e.Action is AuditAction.Undone or AuditAction.Redone)
            .ToList();

        // Primary events must be saved to appear in the timeline
        var allPrimaryEvents = allEvents
            .Where(e => e.IsSaved && e.Action is not (AuditAction.Undone or AuditAction.Redone))
            .ToList();
        var primaryEvents = allPrimaryEvents.ToList();

        // Apply action filter to primary events only
        if (actionFilter.HasValue)
            primaryEvents = primaryEvents.Where(e => e.Action == actionFilter.Value).ToList();

        // Apply "Undone" filter — show only events that have been undone
        if (filterUndoneOnly)
            primaryEvents = primaryEvents.Where(e => e.IsUndone).ToList();

        // Compute per-entity sequential undo/redo constraints.
        // For each entity, only the most recent non-undone event can be undone,
        // and only the oldest undone event can be redone. This prevents nonsensical
        // operations like undoing an "Add" when the entity was subsequently deleted.
        var undoableEventIds = new HashSet<string>();
        var redoableEventIds = new HashSet<string>();

        var eventsByEntity = allPrimaryEvents
            .Where(e => !string.IsNullOrEmpty(e.EntityType) && !string.IsNullOrEmpty(e.EntityId))
            .GroupBy(e => (e.EntityType, e.EntityId));

        foreach (var entityGroup in eventsByEntity)
        {
            var ordered = entityGroup.OrderByDescending(e => e.Timestamp).ToList();

            // Most recent non-undone event can be undone
            var latestActive = ordered.FirstOrDefault(e => !e.IsUndone);
            if (latestActive != null)
                undoableEventIds.Add(latestActive.Id);

            // Oldest undone event can be redone (forward chronological order)
            var oldestUndone = ordered.LastOrDefault(e => e.IsUndone);
            if (oldestUndone != null)
                redoableEventIds.Add(oldestUndone.Id);
        }

        // Build lookup: parent event ID → list of meta-events
        var metaByParent = metaEvents
            .Where(e => !string.IsNullOrEmpty(e.RelatedEventId))
            .GroupBy(e => e.RelatedEventId!)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

        // Apply fuzzy search using Levenshtein scoring
        List<AuditEvent> filteredEvents;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filteredEvents = primaryEvents
                .Select(e => new
                {
                    Event = e,
                    Score = new[] { e.Description, e.EntityName, e.EntityType }
                        .Where(f => !string.IsNullOrEmpty(f))
                        .Select(f => LevenshteinDistance.ComputeSearchScore(SearchQuery, f))
                        .DefaultIfEmpty(-1)
                        .Max()
                })
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Event.Timestamp)
                .Select(x => x.Event)
                .ToList();
        }
        else
        {
            filteredEvents = primaryEvents
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        // Count only primary events (meta-events are sub-items, not counted separately)
        var totalPrimaryCount = _eventLogService.GetFilteredEvents(searchQuery: null)
            .Count(e => e.IsSaved && e.Action is not (AuditAction.Undone or AuditAction.Redone));

        TotalEventCount = totalPrimaryCount;
        FilteredEventCount = filteredEvents.Count;
        HasEvents = TotalEventCount > 0;
        IsFiltered = actionFilter.HasValue
                     || filterUndoneOnly
                     || !string.IsNullOrWhiteSpace(entityTypeFilter)
                     || !string.IsNullOrWhiteSpace(SearchQuery);
        ShowNoResults = HasEvents && IsFiltered && FilteredEventCount == 0;

        // Group by date
        Groups.Clear();
        var today = DateTime.Now.Date;
        var yesterday = today.AddDays(-1);

        var grouped = filteredEvents.GroupBy(e => e.Timestamp.ToLocalTime().Date);
        foreach (var group in grouped)
        {
            var dateLabel = group.Key == today
                ? "Today".Translate() + ", " + group.Key.ToString("MMM d")
                : group.Key == yesterday
                    ? "Yesterday".Translate() + ", " + group.Key.ToString("MMM d")
                    : group.Key.ToString("MMM d, yyyy");

            var historyGroup = new VersionHistoryGroup { DateLabel = dateLabel };
            foreach (var evt in group)
            {
                var item = new VersionHistoryItem(evt, this, _eventLogService);

                // Apply per-entity sequential constraints
                if (item.CanUndo && !undoableEventIds.Contains(evt.Id))
                    item.CanUndo = false;
                if (item.CanRedo && !redoableEventIds.Contains(evt.Id))
                    item.CanRedo = false;

                // Attach undo/redo meta-events as sub-items
                if (metaByParent.TryGetValue(evt.Id, out var relatedMeta))
                {
                    foreach (var meta in relatedMeta)
                    {
                        item.SubItems.Add(new VersionHistorySubItem
                        {
                            IsUndo = meta.Action == AuditAction.Undone,
                            TimeText = meta.Timestamp.ToLocalTime().ToString("h:mm tt")
                        });
                    }

                }

                historyGroup.Items.Add(item);
            }
            Groups.Add(historyGroup);
        }

        // Refresh entity type filters
        var currentTypes = _eventLogService.GetEntityTypes().ToList();
        EntityTypeFilters.Clear();
        EntityTypeFilters.Add("All");
        foreach (var type in currentTypes)
        {
            EntityTypeFilters.Add(type);
        }
    }

}
