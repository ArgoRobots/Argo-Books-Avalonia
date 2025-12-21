using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Services;

/// <summary>
/// Interface for undoable actions in the report layout designer.
/// </summary>
public interface IReportUndoableAction
{
    string Description { get; }
    void Undo();
    void Redo();
}

/// <summary>
/// Manages undo/redo operations for the report layout designer.
/// </summary>
public class ReportUndoRedoManager
{
    private readonly Stack<IReportUndoableAction> _undoStack = new();
    private readonly Stack<IReportUndoableAction> _redoStack = new();
    private readonly int _maxStackSize;
    private bool _isUndoingOrRedoing;

    /// <summary>
    /// Fired when the undo/redo state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    public ReportUndoRedoManager(int maxStackSize = 100)
    {
        _maxStackSize = maxStackSize;
    }

    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether redo is available.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the description of the next undo action.
    /// </summary>
    public string? UndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Gets the description of the next redo action.
    /// </summary>
    public string? RedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Gets the undo history.
    /// </summary>
    public IEnumerable<string> UndoHistory => _undoStack.Select(a => a.Description);

    /// <summary>
    /// Gets the redo history.
    /// </summary>
    public IEnumerable<string> RedoHistory => _redoStack.Select(a => a.Description);

    /// <summary>
    /// Records an action for undo/redo.
    /// </summary>
    public void RecordAction(IReportUndoableAction action)
    {
        if (_isUndoingOrRedoing)
            return;

        _undoStack.Push(action);
        _redoStack.Clear();

        // Enforce max stack size
        while (_undoStack.Count > _maxStackSize)
        {
            var temp = new Stack<IReportUndoableAction>();
            while (_undoStack.Count > 1)
            {
                temp.Push(_undoStack.Pop());
            }
            _undoStack.Pop(); // Remove oldest
            while (temp.Count > 0)
            {
                _undoStack.Push(temp.Pop());
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Performs undo operation.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo || _isUndoingOrRedoing)
            return;

        _isUndoingOrRedoing = true;
        try
        {
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isUndoingOrRedoing = false;
        }
    }

    /// <summary>
    /// Performs redo operation.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo || _isUndoingOrRedoing)
            return;

        _isUndoingOrRedoing = true;
        try
        {
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isUndoingOrRedoing = false;
        }
    }

    /// <summary>
    /// Clears all undo/redo history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Action for adding an element.
/// </summary>
public class AddElementAction : IReportUndoableAction
{
    private readonly ReportConfiguration _config;
    private readonly ReportElementBase _element;

    public string Description => $"Add {_element.DisplayName}";

    public AddElementAction(ReportConfiguration config, ReportElementBase element)
    {
        _config = config;
        _element = element;
    }

    public void Undo()
    {
        _config.RemoveElement(_element.Id);
    }

    public void Redo()
    {
        _config.AddElement(_element);
    }
}

/// <summary>
/// Action for removing an element.
/// </summary>
public class RemoveElementAction : IReportUndoableAction
{
    private readonly ReportConfiguration _config;
    private readonly ReportElementBase _element;
    private readonly int _originalZOrder;

    public string Description => $"Remove {_element.DisplayName}";

    public RemoveElementAction(ReportConfiguration config, ReportElementBase element)
    {
        _config = config;
        _element = element.Clone();
        _originalZOrder = element.ZOrder;
    }

    public void Undo()
    {
        _element.ZOrder = _originalZOrder;
        _config.AddElement(_element);
    }

    public void Redo()
    {
        _config.RemoveElement(_element.Id);
    }
}

/// <summary>
/// Action for moving/resizing an element.
/// </summary>
public class MoveResizeElementAction : IReportUndoableAction
{
    private readonly ReportConfiguration _config;
    private readonly string _elementId;
    private readonly (double X, double Y, double Width, double Height) _oldBounds;
    private readonly (double X, double Y, double Width, double Height) _newBounds;
    private readonly bool _isResize;

    public string Description => _isResize ? "Resize element" : "Move element";

    public MoveResizeElementAction(
        ReportConfiguration config,
        string elementId,
        (double X, double Y, double Width, double Height) oldBounds,
        (double X, double Y, double Width, double Height) newBounds,
        bool isResize = false)
    {
        _config = config;
        _elementId = elementId;
        _oldBounds = oldBounds;
        _newBounds = newBounds;
        _isResize = isResize;
    }

    public void Undo()
    {
        var element = _config.GetElementById(_elementId);
        if (element != null)
        {
            element.Bounds = _oldBounds;
        }
    }

    public void Redo()
    {
        var element = _config.GetElementById(_elementId);
        if (element != null)
        {
            element.Bounds = _newBounds;
        }
    }
}

/// <summary>
/// Action for changing element Z-order.
/// </summary>
public class ZOrderChangeAction : IReportUndoableAction
{
    private readonly ReportConfiguration _config;
    private readonly Dictionary<string, int> _oldZOrders;
    private readonly Dictionary<string, int> _newZOrders;
    private readonly string _description;

    public string Description => _description;

    public ZOrderChangeAction(
        ReportConfiguration config,
        Dictionary<string, int> oldZOrders,
        Dictionary<string, int> newZOrders,
        string description)
    {
        _config = config;
        _oldZOrders = new Dictionary<string, int>(oldZOrders);
        _newZOrders = new Dictionary<string, int>(newZOrders);
        _description = description;
    }

    public void Undo()
    {
        foreach (var kvp in _oldZOrders)
        {
            var element = _config.GetElementById(kvp.Key);
            if (element != null)
            {
                element.ZOrder = kvp.Value;
            }
        }
    }

    public void Redo()
    {
        foreach (var kvp in _newZOrders)
        {
            var element = _config.GetElementById(kvp.Key);
            if (element != null)
            {
                element.ZOrder = kvp.Value;
            }
        }
    }
}

/// <summary>
/// Action for changing a property value.
/// </summary>
public class PropertyChangeAction<T> : IReportUndoableAction
{
    private readonly Action<T> _setter;
    private readonly T _oldValue;
    private readonly T _newValue;
    private readonly string _propertyName;
    private readonly Action? _onChanged;

    public string Description => $"Change {_propertyName}";

    public PropertyChangeAction(
        string propertyName,
        T oldValue,
        T newValue,
        Action<T> setter,
        Action? onChanged = null)
    {
        _propertyName = propertyName;
        _oldValue = oldValue;
        _newValue = newValue;
        _setter = setter;
        _onChanged = onChanged;
    }

    public void Undo()
    {
        _setter(_oldValue);
        _onChanged?.Invoke();
    }

    public void Redo()
    {
        _setter(_newValue);
        _onChanged?.Invoke();
    }
}

/// <summary>
/// Action for batch operations (alignment, distribution, sizing).
/// </summary>
public class BatchMoveResizeAction : IReportUndoableAction
{
    private readonly ReportConfiguration _config;
    private readonly Dictionary<string, (double X, double Y, double Width, double Height)> _oldBounds;
    private readonly Dictionary<string, (double X, double Y, double Width, double Height)> _newBounds;
    private readonly string _description;

    public string Description => _description;

    public BatchMoveResizeAction(
        ReportConfiguration config,
        Dictionary<string, (double X, double Y, double Width, double Height)> oldBounds,
        Dictionary<string, (double X, double Y, double Width, double Height)> newBounds,
        string description)
    {
        _config = config;
        _oldBounds = new Dictionary<string, (double, double, double, double)>(oldBounds);
        _newBounds = new Dictionary<string, (double, double, double, double)>(newBounds);
        _description = description;
    }

    public void Undo()
    {
        foreach (var kvp in _oldBounds)
        {
            var element = _config.GetElementById(kvp.Key);
            if (element != null)
            {
                element.Bounds = kvp.Value;
            }
        }
    }

    public void Redo()
    {
        foreach (var kvp in _newBounds)
        {
            var element = _config.GetElementById(kvp.Key);
            if (element != null)
            {
                element.Bounds = kvp.Value;
            }
        }
    }
}
