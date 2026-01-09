using ArgoBooks.ViewModels;

namespace ArgoBooks.Services;

/// <summary>
/// Interface for components that can track and report their changes.
/// </summary>
public interface IChangeTracker
{
    /// <summary>
    /// Gets whether this component has unsaved changes.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Gets the category name for grouping changes.
    /// </summary>
    string CategoryName { get; }

    /// <summary>
    /// Gets the icon name for the category.
    /// </summary>
    string CategoryIcon { get; }

    /// <summary>
    /// Gets the list of individual changes.
    /// </summary>
    IEnumerable<ChangeItem> GetChanges();

    /// <summary>
    /// Clears all tracked changes (after save).
    /// </summary>
    void ClearChanges();
}

/// <summary>
/// Service that aggregates changes from multiple sources and provides
/// a unified view of all unsaved changes in the application.
/// </summary>
public class ChangeTrackingService
{
    private readonly List<IChangeTracker> _trackers = [];
    private readonly List<ChangeItem> _globalChanges = [];
    private string _globalCategoryName = "General";
    private string _globalCategoryIcon = "Cog";

    /// <summary>
    /// Event raised when the overall change state changes.
    /// </summary>
    public event EventHandler? ChangeStateChanged;

    /// <summary>
    /// Gets whether there are any unsaved changes across all trackers.
    /// </summary>
    public bool HasChanges => _globalChanges.Count > 0 || _trackers.Any(t => t.HasChanges);

    /// <summary>
    /// Registers a change tracker to be aggregated.
    /// </summary>
    /// <param name="tracker">The tracker to register.</param>
    public void RegisterTracker(IChangeTracker tracker)
    {
        if (!_trackers.Contains(tracker))
        {
            _trackers.Add(tracker);
        }
    }

    /// <summary>
    /// Unregisters a change tracker.
    /// </summary>
    /// <param name="tracker">The tracker to unregister.</param>
    public void UnregisterTracker(IChangeTracker tracker)
    {
        _trackers.Remove(tracker);
    }

    /// <summary>
    /// Records a global change that isn't tied to a specific tracker.
    /// </summary>
    /// <param name="description">Description of the change.</param>
    /// <param name="changeType">Type of change.</param>
    public void RecordChange(string description, ChangeType changeType = ChangeType.Modified)
    {
        _globalChanges.Add(new ChangeItem
        {
            Description = description,
            ChangeType = changeType
        });
        OnChangeStateChanged();
    }

    /// <summary>
    /// Records a change with the specified category.
    /// </summary>
    /// <param name="categoryName">Name of the category.</param>
    /// <param name="description">Description of the change.</param>
    /// <param name="changeType">Type of change.</param>
    public void RecordChange(string categoryName, string description, ChangeType changeType = ChangeType.Modified)
    {
        _globalCategoryName = categoryName;
        RecordChange(description, changeType);
    }

    /// <summary>
    /// Sets the global category name and icon.
    /// </summary>
    /// <param name="name">Category name.</param>
    /// <param name="icon">Icon name.</param>
    public void SetGlobalCategory(string name, string icon = "Cog")
    {
        _globalCategoryName = name;
        _globalCategoryIcon = icon;
    }

    /// <summary>
    /// Clears all global changes.
    /// </summary>
    public void ClearGlobalChanges()
    {
        _globalChanges.Clear();
        OnChangeStateChanged();
    }

    /// <summary>
    /// Clears all changes from all trackers.
    /// </summary>
    public void ClearAllChanges()
    {
        _globalChanges.Clear();
        foreach (var tracker in _trackers)
        {
            tracker.ClearChanges();
        }
        OnChangeStateChanged();
    }

    /// <summary>
    /// Gets all change categories with their changes.
    /// </summary>
    /// <returns>Collection of change categories.</returns>
    public IEnumerable<ChangeCategory> GetAllChangeCategories()
    {
        var categories = new List<ChangeCategory>();

        // Add global changes if any
        if (_globalChanges.Count > 0)
        {
            var globalCategory = new ChangeCategory
            {
                Name = _globalCategoryName,
                IconName = _globalCategoryIcon,
                IsExpanded = true
            };
            foreach (var change in _globalChanges)
            {
                globalCategory.Changes.Add(change);
            }
            categories.Add(globalCategory);
        }

        // Add changes from registered trackers
        foreach (var tracker in _trackers.Where(t => t.HasChanges))
        {
            var category = new ChangeCategory
            {
                Name = tracker.CategoryName,
                IconName = tracker.CategoryIcon,
                IsExpanded = true
            };
            foreach (var change in tracker.GetChanges())
            {
                category.Changes.Add(change);
            }
            categories.Add(category);
        }

        return categories;
    }

    /// <summary>
    /// Gets the total count of all changes.
    /// </summary>
    public int TotalChangeCount
    {
        get
        {
            var count = _globalChanges.Count;
            foreach (var tracker in _trackers.Where(t => t.HasChanges))
            {
                count += tracker.GetChanges().Count();
            }
            return count;
        }
    }

    /// <summary>
    /// Notifies that change state has changed.
    /// </summary>
    public void NotifyChangeStateChanged()
    {
        OnChangeStateChanged();
    }

    private void OnChangeStateChanged()
    {
        ChangeStateChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Simple change tracker for basic scenarios.
/// </summary>
public class SimpleChangeTracker : IChangeTracker
{
    private readonly List<ChangeItem> _changes = [];

    public string CategoryName { get; set; } = "Changes";
    public string CategoryIcon { get; set; } = "Folder";
    public bool HasChanges => _changes.Count > 0;

    public void AddChange(string description, ChangeType changeType = ChangeType.Modified)
    {
        _changes.Add(new ChangeItem
        {
            Description = description,
            ChangeType = changeType
        });
    }

    public void AddChange(ChangeItem change)
    {
        _changes.Add(change);
    }

    public IEnumerable<ChangeItem> GetChanges() => _changes;

    public void ClearChanges() => _changes.Clear();
}
