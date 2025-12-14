using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Services;

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
/// Manages undo and redo operations with history tracking.
/// </summary>
public partial class UndoRedoManager : ObservableObject
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private readonly int _maxHistorySize;
    private IUndoableAction? _savedState;
    private bool _isExecutingUndoRedo;

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    public event EventHandler? StateChanged;

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
    /// Records an action for undo/redo.
    /// </summary>
    /// <param name="action">The action to record.</param>
    public void RecordAction(IUndoableAction action)
    {
        if (_isExecutingUndoRedo)
            return;

        _undoStack.Push(action);
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

        OnStateChanged();
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
