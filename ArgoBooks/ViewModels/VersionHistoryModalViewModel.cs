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
    /// Hidden for undo/redo meta-events.
    /// </summary>
    public bool ShowUndoRedoButton => Action is not (AuditAction.Undone or AuditAction.Redone);

    /// <summary>
    /// Gets the ChangeType equivalent for reusing existing converters.
    /// </summary>
    public ChangeType ChangeType => Action switch
    {
        AuditAction.Added => ChangeType.Added,
        AuditAction.Deleted => ChangeType.Deleted,
        AuditAction.Undone => ChangeType.Deleted,
        AuditAction.Redone => ChangeType.Added,
        _ => ChangeType.Modified
    };

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
            // Also remove the action from the UndoRedoManager's stack
            // so the standard Ctrl+Z doesn't re-undo it
            var action = GetUndoableActionForEvent(item.Event);
            if (action != null)
                App.UndoRedoManager.RemoveFromUndoStack(action);

            App.CompanyManager?.MarkAsChanged();
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
    /// </summary>
    private void RefreshEvents()
    {
        if (_eventLogService == null)
            return;

        // Parse filter
        AuditAction? actionFilter = SelectedActionFilter switch
        {
            "Added" => AuditAction.Added,
            "Modified" => AuditAction.Modified,
            "Deleted" => AuditAction.Deleted,
            "Undone" => AuditAction.Undone,
            _ => null
        };

        var entityTypeFilter = SelectedEntityTypeFilter is "All" or null
            ? null
            : SelectedEntityTypeFilter;

        // Get events filtered by action/entity type (no text search — we handle that with Levenshtein)
        var events = _eventLogService.GetFilteredEvents(
            searchQuery: null,
            actionFilter: actionFilter,
            entityTypeFilter: entityTypeFilter)
            .ToList();

        // Apply fuzzy search using Levenshtein scoring
        List<AuditEvent> filteredEvents;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filteredEvents = events
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
            filteredEvents = events
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        TotalEventCount = _eventLogService.EventCount;
        FilteredEventCount = filteredEvents.Count;
        HasEvents = TotalEventCount > 0;
        IsFiltered = actionFilter.HasValue
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
                historyGroup.Items.Add(new VersionHistoryItem(evt, this, _eventLogService));
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

    private IUndoableAction? GetUndoableActionForEvent(AuditEvent evt)
    {
        // The EventLogService maintains the mapping internally.
        // For RemoveFromUndoStack, we need to search by description match
        // since the action reference is inside the service.
        // This is a best-effort approach — if the action isn't in the stack, it's a no-op.
        var undoHistory = App.UndoRedoManager.UndoHistory;
        return undoHistory.FirstOrDefault(a => a.Description == evt.Description);
    }
}
