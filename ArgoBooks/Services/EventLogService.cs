using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;

namespace ArgoBooks.Services;

/// <summary>
/// Service that manages the audit event log for version history.
/// Records all entity changes, supports selective undo/redo, and persists
/// events to the company file via CompanyData.EventLog.
/// </summary>
/// <remarks>
/// FUTURE MULTI-ACCOUNTANT SUPPORT:
/// When multi-accountant support is added:
/// 1. Set AccountantId/AccountantName on each event from the current session's accountant.
/// 2. Add filtering by accountant to GetEvents/GetGroupedEvents.
/// 3. Enforce permissions before allowing undo of another accountant's actions
///    (e.g., only admins can undo other accountants' changes).
/// 4. For sync, the event log becomes the unit of replication — merge event logs
///    from multiple clients using timestamp ordering and conflict detection.
/// </remarks>
public class EventLogService
{
    private readonly List<AuditEvent> _events = [];
    private readonly Dictionary<string, IUndoableAction> _undoableActions = new();
    private readonly int _maxEventCount;

    /// <summary>
    /// Raised when an event is recorded or modified (for UI updates).
    /// </summary>
    public event EventHandler? EventsChanged;

    /// <summary>
    /// Creates a new EventLogService.
    /// </summary>
    /// <param name="maxEventCount">Maximum number of events to retain. Oldest events are trimmed.</param>
    public EventLogService(int maxEventCount = 10000)
    {
        _maxEventCount = maxEventCount;
    }

    /// <summary>
    /// Initializes the service with persisted events from a loaded company file.
    /// Previous-session events are view-only (no undoable action available).
    /// </summary>
    public void Initialize(List<AuditEvent> persistedEvents)
    {
        _events.Clear();
        _undoableActions.Clear();

        if (persistedEvents.Count > _maxEventCount)
        {
            _events.AddRange(persistedEvents.Skip(persistedEvents.Count - _maxEventCount));
        }
        else
        {
            _events.AddRange(persistedEvents);
        }

        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears all events and action mappings (called when closing a company).
    /// </summary>
    public void Clear()
    {
        _events.Clear();
        _undoableActions.Clear();
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Records a new audit event linked to an undoable action.
    /// </summary>
    /// <param name="action">The undoable action (for selective undo support).</param>
    /// <param name="description">Human-readable description of the change.</param>
    /// <param name="auditAction">The type of action (Added, Modified, Deleted).</param>
    /// <param name="entityType">The entity type (e.g., "Customer", "Expense").</param>
    /// <param name="entityId">The entity's unique ID.</param>
    /// <param name="entityName">The entity's display name.</param>
    /// <param name="changes">Optional field-level changes for edit operations.</param>
    /// <returns>The created audit event.</returns>
    public AuditEvent RecordEvent(
        IUndoableAction action,
        string description,
        AuditAction auditAction,
        string entityType = "",
        string entityId = "",
        string entityName = "",
        Dictionary<string, FieldChange>? changes = null)
    {
        var evt = new AuditEvent
        {
            Id = GenerateEventId(),
            Timestamp = DateTime.UtcNow,
            Action = auditAction,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Description = description,
            Changes = changes
        };

        _events.Add(evt);
        _undoableActions[evt.Id] = action;

        TrimIfNeeded();
        EventsChanged?.Invoke(this, EventArgs.Empty);

        return evt;
    }

    /// <summary>
    /// Records an audit event automatically from an IUndoableAction's description.
    /// Parses the description to extract action type, entity type, and entity name.
    /// Used when no explicit audit metadata is provided.
    /// </summary>
    public AuditEvent RecordFromAction(IUndoableAction action)
    {
        var (auditAction, entityType, entityName) = ParseActionDescription(action.Description);

        return RecordEvent(
            action,
            action.Description,
            auditAction,
            entityType,
            entityName: entityName);
    }

    /// <summary>
    /// Gets whether a specific event can be undone (action is still available and event hasn't been undone).
    /// </summary>
    public bool CanUndoEvent(AuditEvent evt)
    {
        return !evt.IsUndone
               && evt.Action is not (AuditAction.Undone or AuditAction.Redone)
               && _undoableActions.ContainsKey(evt.Id);
    }

    /// <summary>
    /// Gets whether a specific undone event can be redone.
    /// </summary>
    public bool CanRedoEvent(AuditEvent evt)
    {
        return evt.IsUndone
               && evt.Action is not (AuditAction.Undone or AuditAction.Redone)
               && _undoableActions.ContainsKey(evt.Id);
    }

    /// <summary>
    /// Selectively undoes a specific event without affecting other events.
    /// Creates a new "Undone" event in the log recording the undo.
    /// </summary>
    /// <returns>True if the undo succeeded.</returns>
    public bool UndoEvent(AuditEvent evt)
    {
        if (!CanUndoEvent(evt))
            return false;

        var action = _undoableActions[evt.Id];

        try
        {
            action.Undo();
            evt.IsUndone = true;

            // Record the undo as a new event
            var undoEvent = new AuditEvent
            {
                Id = GenerateEventId(),
                Timestamp = DateTime.UtcNow,
                Action = AuditAction.Undone,
                EntityType = evt.EntityType,
                EntityId = evt.EntityId,
                EntityName = evt.EntityName,
                Description = $"Undo: {evt.Description}",
                RelatedEventId = evt.Id
            };
            _events.Add(undoEvent);

            TrimIfNeeded();
            EventsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Selectively redoes a previously undone event.
    /// Creates a new "Redone" event in the log recording the redo.
    /// </summary>
    /// <returns>True if the redo succeeded.</returns>
    public bool RedoEvent(AuditEvent evt)
    {
        if (!CanRedoEvent(evt))
            return false;

        var action = _undoableActions[evt.Id];

        try
        {
            action.Redo();
            evt.IsUndone = false;

            // Record the redo as a new event
            var redoEvent = new AuditEvent
            {
                Id = GenerateEventId(),
                Timestamp = DateTime.UtcNow,
                Action = AuditAction.Redone,
                EntityType = evt.EntityType,
                EntityId = evt.EntityId,
                EntityName = evt.EntityName,
                Description = $"Redo: {evt.Description}",
                RelatedEventId = evt.Id
            };
            _events.Add(redoEvent);

            TrimIfNeeded();
            EventsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all events, newest first.
    /// </summary>
    public IReadOnlyList<AuditEvent> GetEvents()
    {
        return _events.AsReadOnly();
    }

    /// <summary>
    /// Gets events filtered by criteria, newest first.
    /// </summary>
    public IEnumerable<AuditEvent> GetFilteredEvents(
        string? searchQuery = null,
        AuditAction? actionFilter = null,
        string? entityTypeFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        IEnumerable<AuditEvent> results = _events;

        if (actionFilter.HasValue)
            results = results.Where(e => e.Action == actionFilter.Value);

        if (!string.IsNullOrWhiteSpace(entityTypeFilter))
            results = results.Where(e => string.Equals(e.EntityType, entityTypeFilter, StringComparison.OrdinalIgnoreCase));

        if (fromDate.HasValue)
            results = results.Where(e => e.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            results = results.Where(e => e.Timestamp <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var query = searchQuery.Trim();
            results = results.Where(e =>
                e.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.EntityName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.EntityType.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return results;
    }

    /// <summary>
    /// Gets all distinct entity types present in the event log (for filter dropdown).
    /// </summary>
    public IEnumerable<string> GetEntityTypes()
    {
        return _events
            .Where(e => !string.IsNullOrEmpty(e.EntityType))
            .Select(e => e.EntityType)
            .Distinct()
            .OrderBy(t => t);
    }

    /// <summary>
    /// Gets the total number of events.
    /// </summary>
    public int EventCount => _events.Count;

    /// <summary>
    /// Syncs the in-memory event list back to the CompanyData for persistence.
    /// Call this before saving the company file.
    /// </summary>
    public void SyncToCompanyData(CompanyData companyData)
    {
        companyData.EventLog.Clear();
        companyData.EventLog.AddRange(_events);
    }

    /// <summary>
    /// Generates a batch of realistic test events for UI testing.
    /// Call after Initialize() to populate the history timeline.
    /// Remove or disable this method before shipping.
    /// </summary>
    public void GenerateTestEvents(int count = 80)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var now = DateTime.UtcNow;

        var entityTypes = new[] { "Customer", "Supplier", "Product", "Expense", "Revenue", "Invoice", "Payment", "Employee", "Category", "Inventory" };

        var customerNames = new[] { "Acme Corp", "TechStart Inc", "Global Trade LLC", "City Bakery", "Metro Supplies", "Peak Fitness", "River Café", "Sunrise Solar", "Nordic Design", "Atlas Logistics" };
        var productNames = new[] { "Widget A", "Premium Service Plan", "Consulting Hours", "Office Chair", "Laptop Stand", "USB Hub", "Webcam HD", "Desk Lamp", "Keyboard Pro", "Monitor 27\"" };
        var supplierNames = new[] { "Office Depot", "Amazon Business", "Staples Direct", "Dell Technologies", "Costco Wholesale", "Grainger", "Uline Shipping", "Home Depot Pro", "Best Buy Business", "CDW" };
        var employeeNames = new[] { "John Smith", "Maria Garcia", "David Chen", "Sarah Johnson", "Michael Brown", "Emily Davis", "James Wilson", "Lisa Anderson", "Robert Taylor", "Jennifer Martinez" };

        var actions = new[] { AuditAction.Added, AuditAction.Modified, AuditAction.Deleted };
        var actionWeights = new[] { 0.4, 0.85, 1.0 }; // 40% add, 45% modify, 15% delete

        for (int i = 0; i < count; i++)
        {
            // Spread events across the last 14 days
            var daysAgo = random.NextDouble() * 14;
            var hoursOffset = random.NextDouble() * 10 + 8; // Between 8am-6pm
            var timestamp = now.AddDays(-daysAgo).Date.AddHours(hoursOffset);

            var entityType = entityTypes[random.Next(entityTypes.Length)];

            // Pick a name based on entity type
            var entityName = entityType switch
            {
                "Customer" => customerNames[random.Next(customerNames.Length)],
                "Supplier" => supplierNames[random.Next(supplierNames.Length)],
                "Product" => productNames[random.Next(productNames.Length)],
                "Employee" => employeeNames[random.Next(employeeNames.Length)],
                "Expense" => $"PUR-2026-{random.Next(1, 500):D5}",
                "Revenue" => $"SAL-2026-{random.Next(1, 500):D5}",
                "Invoice" => $"INV-2026-{random.Next(1, 200):D5}",
                "Payment" => $"PAY-2026-{random.Next(1, 300):D5}",
                "Category" => new[] { "Electronics", "Office Supplies", "Services", "Food & Beverage", "Software" }[random.Next(5)],
                "Inventory" => productNames[random.Next(productNames.Length)],
                _ => "Item"
            };

            // Pick action with weighted distribution
            var roll = random.NextDouble();
            var action = AuditAction.Added;
            for (int j = 0; j < actionWeights.Length; j++)
            {
                if (roll < actionWeights[j])
                {
                    action = actions[j];
                    break;
                }
            }

            var actionVerb = action switch
            {
                AuditAction.Added => "Add",
                AuditAction.Modified => "Edit",
                AuditAction.Deleted => "Delete",
                _ => "Update"
            };

            var description = $"{actionVerb} {entityType.ToLower()} '{entityName}'";

            var evt = new AuditEvent
            {
                Id = GenerateEventId(),
                Timestamp = timestamp,
                Action = action,
                EntityType = entityType,
                EntityId = $"{random.Next(1, 1000)}",
                EntityName = entityName,
                Description = description
            };

            _events.Add(evt);

            // Register a no-op undoable action so undo buttons appear in the UI
            _undoableActions[evt.Id] = new NoOpUndoableAction(description);
        }

        // Sort by timestamp
        _events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        TrimIfNeeded();
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrimIfNeeded()
    {
        if (_events.Count > _maxEventCount)
        {
            var excess = _events.Count - _maxEventCount;
            var removedIds = _events.Take(excess).Select(e => e.Id).ToHashSet();
            _events.RemoveRange(0, excess);

            // Clean up action references for trimmed events
            foreach (var id in removedIds)
            {
                _undoableActions.Remove(id);
            }
        }
    }

    private static string GenerateEventId()
    {
        return $"evt_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Parses an IUndoableAction.Description string to extract audit metadata.
    /// Handles patterns like "Add customer 'Acme Corp'", "Edit product 'Widget A'",
    /// "Delete expense PUR-2026-00042".
    /// </summary>
    private static (AuditAction action, string entityType, string entityName) ParseActionDescription(string description)
    {
        var auditAction = AuditAction.Modified;
        var entityType = "";
        var entityName = "";

        if (string.IsNullOrWhiteSpace(description))
            return (auditAction, entityType, entityName);

        var desc = description.Trim();

        // Determine action type from prefix
        if (desc.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
        {
            auditAction = AuditAction.Added;
            desc = desc[4..];
        }
        else if (desc.StartsWith("Edit ", StringComparison.OrdinalIgnoreCase))
        {
            auditAction = AuditAction.Modified;
            desc = desc[5..];
        }
        else if (desc.StartsWith("Delete ", StringComparison.OrdinalIgnoreCase))
        {
            auditAction = AuditAction.Deleted;
            desc = desc[7..];
        }
        else if (desc.StartsWith("Update ", StringComparison.OrdinalIgnoreCase))
        {
            auditAction = AuditAction.Modified;
            desc = desc[7..];
        }

        // Extract entity type (first word after action)
        var spaceIndex = desc.IndexOf(' ');
        if (spaceIndex > 0)
        {
            entityType = CapitalizeFirst(desc[..spaceIndex]);
            var remainder = desc[(spaceIndex + 1)..].Trim();

            // Extract entity name from quotes or remainder
            var singleQuoteStart = remainder.IndexOf('\'');
            if (singleQuoteStart >= 0)
            {
                var singleQuoteEnd = remainder.IndexOf('\'', singleQuoteStart + 1);
                if (singleQuoteEnd > singleQuoteStart)
                {
                    entityName = remainder[(singleQuoteStart + 1)..singleQuoteEnd];
                }
            }
            else
            {
                entityName = remainder;
            }
        }
        else
        {
            entityType = CapitalizeFirst(desc);
        }

        return (auditAction, entityType, entityName);
    }

    private static string CapitalizeFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s[1..];
    }

    /// <summary>
    /// A no-op undoable action used for test events.
    /// Undo/Redo are no-ops since the data isn't real.
    /// Remove or disable this class before shipping.
    /// </summary>
    private class NoOpUndoableAction(string description) : IUndoableAction
    {
        public string Description => description;
        public void Undo() { }
        public void Redo() { }
    }
}
