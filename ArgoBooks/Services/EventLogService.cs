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
    private CompanyData? _companyData;

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
    /// Reconstructs undoable actions for persisted events that have entity snapshots
    /// or whose entities can be found in CompanyData.
    /// </summary>
    public void Initialize(List<AuditEvent> persistedEvents, CompanyData? companyData = null)
    {
        _events.Clear();
        _undoableActions.Clear();
        _companyData = companyData;

        if (persistedEvents.Count > _maxEventCount)
        {
            _events.AddRange(persistedEvents.Skip(persistedEvents.Count - _maxEventCount));
        }
        else
        {
            _events.AddRange(persistedEvents);
        }

        // Reconstruct undoable actions for persisted events
        if (companyData != null)
        {
            ReconstructActions(companyData);
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
        _companyData = null;
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
    /// <param name="entitySnapshot">Optional JSON snapshot of the entity for persistence.</param>
    /// <returns>The created audit event.</returns>
    public AuditEvent RecordEvent(
        IUndoableAction action,
        string description,
        AuditAction auditAction,
        string entityType = "",
        string entityId = "",
        string entityName = "",
        Dictionary<string, FieldChange>? changes = null,
        string? entitySnapshot = null)
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
            Changes = changes,
            EntitySnapshot = entitySnapshot
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
    /// Also captures entity ID and snapshot from CompanyData for future reconstruction.
    /// </summary>
    public AuditEvent RecordFromAction(IUndoableAction action)
    {
        var (auditAction, entityType, entityName) = ParseActionDescription(action.Description);

        string entityId = "";
        string? entitySnapshot = null;

        // Try to resolve entity ID and capture snapshot for persistence
        if (_companyData != null && !string.IsNullOrEmpty(entityType))
        {
            entityId = EntityCollectionHelper.FindEntityIdByName(_companyData, entityType, entityName) ?? "";

            if (!string.IsNullOrEmpty(entityId))
            {
                // Capture entity snapshot for Added events (entity just added, state is correct).
                // For Deleted events, the entity is already removed so FindAndSerialize returns null.
                // For Modified events, the entity has the new values — not useful for undo
                // (we'd need pre-modification state). Modified undo requires future enhancement.
                if (auditAction == AuditAction.Added)
                {
                    entitySnapshot = EntityCollectionHelper.FindAndSerializeEntity(
                        _companyData, entityType, entityId);
                }
            }
        }

        return RecordEvent(
            action,
            action.Description,
            auditAction,
            entityType,
            entityId: entityId,
            entityName: entityName,
            entitySnapshot: entitySnapshot);
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

    /// <summary>
    /// Reconstructs undoable actions for persisted events from a previous session.
    /// Uses EntitySnapshot (if available) or looks up entities in CompanyData.
    /// </summary>
    private void ReconstructActions(CompanyData companyData)
    {
        foreach (var evt in _events)
        {
            // Skip meta-events (Undone/Redone) — they don't have their own undo actions
            if (evt.Action is AuditAction.Undone or AuditAction.Redone)
                continue;

            // Skip if we already have an action (shouldn't happen on fresh load, but be safe)
            if (_undoableActions.ContainsKey(evt.Id))
                continue;

            var action = TryReconstructAction(evt, companyData);
            if (action != null)
                _undoableActions[evt.Id] = action;
        }
    }

    /// <summary>
    /// Attempts to reconstruct an undoable action for a single persisted event.
    /// </summary>
    private static IUndoableAction? TryReconstructAction(AuditEvent evt, CompanyData companyData)
    {
        if (string.IsNullOrEmpty(evt.EntityType))
            return null;

        // Try to resolve EntityId if it wasn't stored
        var entityId = evt.EntityId;
        if (string.IsNullOrEmpty(entityId) && !string.IsNullOrEmpty(evt.EntityName))
        {
            entityId = EntityCollectionHelper.FindEntityIdByName(companyData, evt.EntityType, evt.EntityName) ?? "";
        }

        return evt.Action switch
        {
            AuditAction.Added => ReconstructAddedAction(evt, companyData, entityId),
            AuditAction.Deleted => ReconstructDeletedAction(evt, companyData),
            AuditAction.Modified => ReconstructModifiedAction(evt, companyData, entityId),
            _ => null
        };
    }

    /// <summary>
    /// Reconstructs an undo action for an "Added" event.
    /// Undo = remove the entity from CompanyData. Redo = add it back from snapshot.
    /// </summary>
    private static IUndoableAction? ReconstructAddedAction(AuditEvent evt, CompanyData companyData, string entityId)
    {
        if (string.IsNullOrEmpty(entityId))
            return null;

        // Get a snapshot: prefer persisted snapshot, fall back to current entity in CompanyData
        var snapshot = evt.EntitySnapshot
                       ?? EntityCollectionHelper.FindAndSerializeEntity(companyData, evt.EntityType, entityId);

        if (string.IsNullOrEmpty(snapshot))
            return null;

        // For undone Added events: entity was removed, so we need snapshot for redo
        // For non-undone Added events: entity exists, undo will remove it
        string? capturedSnapshot = snapshot;
        var entityType = evt.EntityType;

        return new DelegateAction(
            evt.Description,
            () =>
            {
                // Undo: serialize current state (may have been modified since add), then remove
                var currentSnapshot = EntityCollectionHelper.FindAndSerializeEntity(companyData, entityType, entityId);
                if (currentSnapshot != null)
                    capturedSnapshot = currentSnapshot;
                EntityCollectionHelper.RemoveEntity(companyData, entityType, entityId);
                companyData.MarkAsModified();
            },
            () =>
            {
                // Redo: add back from the captured snapshot
                EntityCollectionHelper.AddEntityFromSnapshot(companyData, entityType, capturedSnapshot);
                companyData.MarkAsModified();
            });
    }

    /// <summary>
    /// Reconstructs an undo action for a "Deleted" event.
    /// Requires EntitySnapshot to restore the deleted entity.
    /// Undo = add entity back from snapshot. Redo = remove it again.
    /// </summary>
    private static IUndoableAction? ReconstructDeletedAction(AuditEvent evt, CompanyData companyData)
    {
        // Without a snapshot, we can't restore the deleted entity
        if (string.IsNullOrEmpty(evt.EntitySnapshot) || string.IsNullOrEmpty(evt.EntityType))
            return null;

        var snapshot = evt.EntitySnapshot;
        var entityType = evt.EntityType;
        var entityId = evt.EntityId;

        return new DelegateAction(
            evt.Description,
            () =>
            {
                // Undo: restore the deleted entity from snapshot
                EntityCollectionHelper.AddEntityFromSnapshot(companyData, entityType, snapshot);
                companyData.MarkAsModified();
            },
            () =>
            {
                // Redo: remove it again
                if (!string.IsNullOrEmpty(entityId))
                    EntityCollectionHelper.RemoveEntity(companyData, entityType, entityId);
                companyData.MarkAsModified();
            });
    }

    /// <summary>
    /// Reconstructs an undo action for a "Modified" event.
    /// Requires EntitySnapshot (pre-modification state) to revert changes.
    /// Undo = replace entity with pre-modification snapshot. Redo = restore post-modification state.
    /// </summary>
    private static IUndoableAction? ReconstructModifiedAction(AuditEvent evt, CompanyData companyData, string entityId)
    {
        // Without a snapshot of the pre-modification state, we can't undo
        if (string.IsNullOrEmpty(evt.EntitySnapshot) || string.IsNullOrEmpty(entityId))
            return null;

        var preModSnapshot = evt.EntitySnapshot;
        var entityType = evt.EntityType;

        // Capture the current (post-modification) state for redo
        var postModSnapshot = EntityCollectionHelper.FindAndSerializeEntity(companyData, entityType, entityId);
        if (string.IsNullOrEmpty(postModSnapshot))
            return null;

        return new DelegateAction(
            evt.Description,
            () =>
            {
                // Undo: replace current entity with pre-modification snapshot
                EntityCollectionHelper.RemoveEntity(companyData, entityType, entityId);
                EntityCollectionHelper.AddEntityFromSnapshot(companyData, entityType, preModSnapshot);
                companyData.MarkAsModified();
            },
            () =>
            {
                // Redo: replace with post-modification snapshot
                EntityCollectionHelper.RemoveEntity(companyData, entityType, entityId);
                EntityCollectionHelper.AddEntityFromSnapshot(companyData, entityType, postModSnapshot);
                companyData.MarkAsModified();
            });
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
