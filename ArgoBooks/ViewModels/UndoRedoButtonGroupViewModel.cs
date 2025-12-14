using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArgoBooks.Services;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the undo/redo button group control.
/// </summary>
public partial class UndoRedoButtonGroupViewModel : ViewModelBase
{
    private UndoRedoManager? _undoRedoManager;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _undoTooltip = "Undo";

    [ObservableProperty]
    private string _redoTooltip = "Redo";

    /// <summary>
    /// Event raised when the undo dropdown should be shown.
    /// </summary>
    public event EventHandler<Point>? ShowUndoDropdownRequested;

    /// <summary>
    /// Event raised when the redo dropdown should be shown.
    /// </summary>
    public event EventHandler<Point>? ShowRedoDropdownRequested;

    /// <summary>
    /// Event raised when an action is performed.
    /// </summary>
    public event EventHandler? ActionPerformed;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public UndoRedoButtonGroupViewModel()
    {
    }

    /// <summary>
    /// Constructor with UndoRedoManager.
    /// </summary>
    public UndoRedoButtonGroupViewModel(UndoRedoManager manager)
    {
        SetUndoRedoManager(manager);
    }

    /// <summary>
    /// Sets the undo/redo manager.
    /// </summary>
    public void SetUndoRedoManager(UndoRedoManager manager)
    {
        if (_undoRedoManager != null)
        {
            _undoRedoManager.StateChanged -= OnManagerStateChanged;
        }

        _undoRedoManager = manager;
        _undoRedoManager.StateChanged += OnManagerStateChanged;
        UpdateState();
    }

    private void OnManagerStateChanged(object? sender, EventArgs e)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        if (_undoRedoManager == null)
        {
            CanUndo = false;
            CanRedo = false;
            UndoTooltip = "Undo";
            RedoTooltip = "Redo";
            return;
        }

        CanUndo = _undoRedoManager.CanUndo;
        CanRedo = _undoRedoManager.CanRedo;

        UndoTooltip = _undoRedoManager.UndoDescription != null
            ? $"Undo {_undoRedoManager.UndoDescription}"
            : "Undo";

        RedoTooltip = _undoRedoManager.RedoDescription != null
            ? $"Redo {_undoRedoManager.RedoDescription}"
            : "Redo";
    }

    /// <summary>
    /// Performs an undo operation.
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        if (_undoRedoManager?.Undo() == true)
        {
            ActionPerformed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Performs a redo operation.
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        if (_undoRedoManager?.Redo() == true)
        {
            ActionPerformed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Toggles the undo dropdown.
    /// </summary>
    [RelayCommand]
    private void ToggleUndoDropdown()
    {
        // The position will be calculated by the control and passed via event
        ShowUndoDropdownRequested?.Invoke(this, new Point(0, 0));
    }

    /// <summary>
    /// Toggles the redo dropdown.
    /// </summary>
    [RelayCommand]
    private void ToggleRedoDropdown()
    {
        // The position will be calculated by the control and passed via event
        ShowRedoDropdownRequested?.Invoke(this, new Point(0, 0));
    }

    /// <summary>
    /// Gets the undo/redo manager.
    /// </summary>
    public UndoRedoManager? Manager => _undoRedoManager;
}
