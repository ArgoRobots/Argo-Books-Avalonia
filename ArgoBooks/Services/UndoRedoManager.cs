using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Services;

/// <summary>
/// Common interface for undo/redo managers.
/// </summary>
public interface IUndoRedoManager
{
    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether redo is available.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Gets the description of the next undo action.
    /// </summary>
    string? UndoDescription { get; }

    /// <summary>
    /// Gets the description of the next redo action.
    /// </summary>
    string? RedoDescription { get; }

    /// <summary>
    /// Gets the undo history descriptions.
    /// </summary>
    IEnumerable<string> GetUndoHistory();

    /// <summary>
    /// Gets the redo history descriptions.
    /// </summary>
    IEnumerable<string> GetRedoHistory();

    /// <summary>
    /// Performs undo operation.
    /// </summary>
    void Undo();

    /// <summary>
    /// Performs redo operation.
    /// </summary>
    void Redo();

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    event EventHandler? StateChanged;
}

/// <summary>
/// Interface for undoable actions.
/// </summary>
public interface IUndoableAction
{
    /// <summary>
    /// Gets the description of the action.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Undoes the action.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the action.
    /// </summary>
    void Redo();
}

/// <summary>
/// Interface for actions that can be coalesced with subsequent actions of the same kind.
/// When rapid sequential changes occur (e.g., color picker dragging), the undo manager
/// merges them into a single undo entry instead of creating separate entries.
/// </summary>
public interface ICoalescingUndoableAction : IUndoableAction
{
    /// <summary>
    /// Key identifying actions that can be coalesced together.
    /// Actions with the same key within the time window will be merged.
    /// </summary>
    string CoalescingKey { get; }

    /// <summary>
    /// Updates this action's "new state" to match the newer action's state.
    /// The original "old state" is preserved so undo returns to the initial state.
    /// </summary>
    void UpdateToNewState(ICoalescingUndoableAction newerAction);
}

/// <summary>
/// Manages undo and redo operations with history tracking.
/// </summary>
public class UndoRedoManager : ObservableObject, IUndoRedoManager
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private readonly int _maxHistorySize;
    private IUndoableAction? _savedState;
    private bool _isExecutingUndoRedo;
    private DateTime _lastRecordTime;
    private const int CoalesceThresholdMs = 500;

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Event raised when a new action is recorded.
    /// The EventLogService subscribes to this to create audit events automatically.
    /// </summary>
    public event EventHandler<ActionRecordedEventArgs>? ActionRecorded;

    /// <summary>
    /// Event raised after a linear undo is performed (Ctrl+Z).
    /// The EventLogService subscribes to sync the audit trail.
    /// </summary>
    public event EventHandler<ActionRecordedEventArgs>? ActionUndone;

    /// <summary>
    /// Event raised after a linear redo is performed (Ctrl+Y).
    /// The EventLogService subscribes to sync the audit trail.
    /// </summary>
    public event EventHandler<ActionRecordedEventArgs>? ActionRedone;

    /// <summary>
    /// Initializes a new instance of the UndoRedoManager.
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of actions to keep in history.</param>
    public UndoRedoManager(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Gets the number of actions that can be undone.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Gets the number of actions that can be redone.
    /// </summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Gets whether there are actions that can be undone.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether there are actions that can be redone.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the description of the next action to undo.
    /// </summary>
    public string? UndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Gets the description of the next action to redo.
    /// </summary>
    public string? RedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Gets whether the current state is the saved state.
    /// </summary>
    public bool IsAtSavedState => (_undoStack.Count == 0 && _savedState == null) ||
                                   (_undoStack.Count > 0 && _undoStack.Peek() == _savedState);

    /// <summary>
    /// Gets the undo history as a read-only collection.
    /// </summary>
    public IReadOnlyList<IUndoableAction> UndoHistory => _undoStack.ToList();

    /// <summary>
    /// Gets the redo history as a read-only collection.
    /// </summary>
    public IReadOnlyList<IUndoableAction> RedoHistory => _redoStack.ToList();

    /// <summary>
    /// Gets the descriptions of all undo actions.
    /// </summary>
    public List<string> GetUndoDescriptions() => _undoStack.Select(a => a.Description).ToList();

    /// <summary>
    /// Gets the descriptions of all redo actions.
    /// </summary>
    public List<string> GetRedoDescriptions() => _redoStack.Select(a => a.Description).ToList();

    /// <inheritdoc />
    IEnumerable<string> IUndoRedoManager.GetUndoHistory() => GetUndoDescriptions();

    /// <inheritdoc />
    IEnumerable<string> IUndoRedoManager.GetRedoHistory() => GetRedoDescriptions();

    /// <inheritdoc />
    void IUndoRedoManager.Undo() => Undo();

    /// <inheritdoc />
    void IUndoRedoManager.Redo() => Redo();

    /// <summary>
    /// Records an action for undo/redo.
    /// Also emits the <see cref="ActionRecorded"/> event so the EventLogService
    /// can create an audit trail entry automatically.
    /// </summary>
    /// <param name="action">The action to record.</param>
    public void RecordAction(IUndoableAction action)
    {
        if (_isExecutingUndoRedo)
            return;

        var now = DateTime.UtcNow;

        // Coalesce with the most recent action if it's the same kind of operation
        // within a short time window. This prevents flooding the undo stack when
        // using color pickers or other rapid-change controls.
        if (action is ICoalescingUndoableAction newCoalescing &&
            _undoStack.Count > 0 &&
            _undoStack.Peek() is ICoalescingUndoableAction topCoalescing &&
            newCoalescing.CoalescingKey == topCoalescing.CoalescingKey &&
            (now - _lastRecordTime).TotalMilliseconds < CoalesceThresholdMs)
        {
            topCoalescing.UpdateToNewState(newCoalescing);
            _lastRecordTime = now;
            _redoStack.Clear();
            OnStateChanged();
            return;
        }

        _undoStack.Push(action);
        _lastRecordTime = now;
        _redoStack.Clear();

        // Trim history if it exceeds max size
        if (_undoStack.Count > _maxHistorySize)
        {
            var tempStack = new Stack<IUndoableAction>();
            for (int i = 0; i < _maxHistorySize; i++)
            {
                tempStack.Push(_undoStack.Pop());
            }
            _undoStack.Clear();
            while (tempStack.Count > 0)
            {
                _undoStack.Push(tempStack.Pop());
            }
        }

        // Notify the EventLogService to create an audit event
        ActionRecorded?.Invoke(this, new ActionRecordedEventArgs(action));

        OnStateChanged();
    }

    /// <summary>
    /// Removes a specific action from the undo stack without executing it.
    /// Used by EventLogService for selective undo (the service calls action.Undo() itself).
    /// </summary>
    /// <param name="action">The action to remove.</param>
    /// <returns>True if the action was found and removed.</returns>
    public bool RemoveFromUndoStack(IUndoableAction action)
    {
        if (!_undoStack.Contains(action))
            return false;

        var tempList = _undoStack.ToList();
        tempList.Remove(action);

        _undoStack.Clear();
        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            _undoStack.Push(tempList[i]);
        }

        OnStateChanged();
        return true;
    }

    /// <summary>
    /// Removes a specific action from the redo stack without executing it.
    /// Used by EventLogService for selective redo (the service calls action.Redo() itself).
    /// </summary>
    /// <param name="action">The action to remove.</param>
    /// <returns>True if the action was found and removed.</returns>
    public bool RemoveFromRedoStack(IUndoableAction action)
    {
        if (!_redoStack.Contains(action))
            return false;

        var tempList = _redoStack.ToList();
        tempList.Remove(action);

        _redoStack.Clear();
        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            _redoStack.Push(tempList[i]);
        }

        OnStateChanged();
        return true;
    }

    /// <summary>
    /// Removes a specific action from both undo and redo stacks without executing it.
    /// Used by EventLogService to prevent double-execution when it handles undo/redo itself.
    /// </summary>
    public void RemoveFromBothStacks(IUndoableAction action)
    {
        RemoveFromUndoStack(action);
        RemoveFromRedoStack(action);
    }

    /// <summary>
    /// Undoes the last action.
    /// </summary>
    /// <returns>True if an action was undone.</returns>
    public bool Undo()
    {
        if (!CanUndo || _isExecutingUndoRedo)
            return false;

        _isExecutingUndoRedo = true;
        try
        {
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            OnStateChanged();
            ActionUndone?.Invoke(this, new ActionRecordedEventArgs(action));
            return true;
        }
        finally
        {
            _isExecutingUndoRedo = false;
        }
    }

    /// <summary>
    /// Redoes the last undone action.
    /// </summary>
    /// <returns>True if an action was redone.</returns>
    public bool Redo()
    {
        if (!CanRedo || _isExecutingUndoRedo)
            return false;

        _isExecutingUndoRedo = true;
        try
        {
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
            OnStateChanged();
            ActionRedone?.Invoke(this, new ActionRecordedEventArgs(action));
            return true;
        }
        finally
        {
            _isExecutingUndoRedo = false;
        }
    }

    /// <summary>
    /// Undoes multiple actions up to and including the specified index.
    /// </summary>
    /// <param name="count">Number of actions to undo.</param>
    public void UndoMultiple(int count)
    {
        for (int i = 0; i < count && CanUndo; i++)
        {
            Undo();
        }
    }

    /// <summary>
    /// Redoes multiple actions up to and including the specified index.
    /// </summary>
    /// <param name="count">Number of actions to redo.</param>
    public void RedoMultiple(int count)
    {
        for (int i = 0; i < count && CanRedo; i++)
        {
            Redo();
        }
    }

    /// <summary>
    /// Marks the current state as saved.
    /// </summary>
    public void MarkSaved()
    {
        _savedState = _undoStack.Count > 0 ? _undoStack.Peek() : null;
        OnStateChanged();
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _savedState = null;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoCount));
        OnPropertyChanged(nameof(RedoCount));
        OnPropertyChanged(nameof(UndoDescription));
        OnPropertyChanged(nameof(RedoDescription));
        OnPropertyChanged(nameof(IsAtSavedState));
        OnPropertyChanged(nameof(UndoHistory));
        OnPropertyChanged(nameof(RedoHistory));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// A composite action that groups multiple actions together.
/// </summary>
public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> _actions;

    /// <summary>
    /// Gets the description of the composite action.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new composite action.
    /// </summary>
    /// <param name="description">Description of the action.</param>
    /// <param name="actions">Actions to group together.</param>
    public CompositeAction(string description, IEnumerable<IUndoableAction> actions)
    {
        Description = description;
        _actions = actions.ToList();
    }

    /// <summary>
    /// Undoes all actions in reverse order.
    /// </summary>
    public void Undo()
    {
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo();
        }
    }

    /// <summary>
    /// Redoes all actions in order.
    /// </summary>
    public void Redo()
    {
        foreach (var action in _actions)
        {
            action.Redo();
        }
    }
}

/// <summary>
/// A generic property change action.
/// </summary>
/// <typeparam name="T">Type of the property value.</typeparam>
public class PropertyChangeAction<T> : IUndoableAction
{
    private readonly Action<T> _setter;
    private readonly T _oldValue;
    private readonly T _newValue;

    /// <summary>
    /// Gets the description of the property change.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new property change action.
    /// </summary>
    /// <param name="description">Description of the change.</param>
    /// <param name="setter">Action to set the property value.</param>
    /// <param name="oldValue">The old property value.</param>
    /// <param name="newValue">The new property value.</param>
    public PropertyChangeAction(string description, Action<T> setter, T oldValue, T newValue)
    {
        Description = description;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    /// <summary>
    /// Undoes the property change.
    /// </summary>
    public void Undo() => _setter(_oldValue);

    /// <summary>
    /// Redoes the property change.
    /// </summary>
    public void Redo() => _setter(_newValue);
}

/// <summary>
/// A generic property change action that supports coalescing rapid changes.
/// </summary>
/// <typeparam name="T">Type of the property value.</typeparam>
public class CoalescingPropertyChangeAction<T> : ICoalescingUndoableAction
{
    private readonly Action<T> _setter;
    private readonly T _oldValue;
    private T _newValue;

    /// <summary>
    /// Gets the description of the property change.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the coalescing key for this action.
    /// </summary>
    public string CoalescingKey { get; }

    public CoalescingPropertyChangeAction(string description, string coalescingKey, Action<T> setter, T oldValue, T newValue)
    {
        Description = description;
        CoalescingKey = coalescingKey;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    /// <inheritdoc />
    public void UpdateToNewState(ICoalescingUndoableAction newerAction)
    {
        if (newerAction is CoalescingPropertyChangeAction<T> newer)
            _newValue = newer._newValue;
    }

    /// <summary>
    /// Undoes the property change.
    /// </summary>
    public void Undo() => _setter(_oldValue);

    /// <summary>
    /// Redoes the property change.
    /// </summary>
    public void Redo() => _setter(_newValue);
}

/// <summary>
/// A simple undoable action that uses delegate functions for undo and redo.
/// </summary>
public class DelegateAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    /// <summary>
    /// Gets the description of the action.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new delegate-based action.
    /// </summary>
    /// <param name="description">Description of the action.</param>
    /// <param name="undoAction">Action to perform on undo.</param>
    /// <param name="redoAction">Action to perform on redo.</param>
    public DelegateAction(string description, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    /// <summary>
    /// Undoes the action.
    /// </summary>
    public void Undo() => _undoAction();

    /// <summary>
    /// Redoes the action.
    /// </summary>
    public void Redo() => _redoAction();
}

/// <summary>
/// Event args for when an action is recorded in the UndoRedoManager.
/// </summary>
public class ActionRecordedEventArgs(IUndoableAction action) : EventArgs
{
    /// <summary>
    /// The action that was recorded.
    /// </summary>
    public IUndoableAction Action { get; } = action;
}
