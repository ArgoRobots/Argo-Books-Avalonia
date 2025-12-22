using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArgoBooks.Services;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents an item in the undo/redo history.
/// </summary>
public class UndoRedoHistoryItem
{
    public int Index { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Interface for undo/redo button group ViewModels.
/// </summary>
public interface IUndoRedoButtonGroupViewModel
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    string UndoTooltip { get; }
    string RedoTooltip { get; }
    ObservableCollection<UndoRedoHistoryItem> UndoHistory { get; }
    ObservableCollection<UndoRedoHistoryItem> RedoHistory { get; }
    ICommand UndoCommand { get; }
    ICommand RedoCommand { get; }
    ICommand UndoToCommand { get; }
    ICommand RedoToCommand { get; }
    void RefreshHistory();
}

/// <summary>
/// ViewModel for the undo/redo button group control.
/// </summary>
public partial class UndoRedoButtonGroupViewModel : ViewModelBase, IUndoRedoButtonGroupViewModel
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
    /// Undo history items.
    /// </summary>
    public ObservableCollection<UndoRedoHistoryItem> UndoHistory { get; } = new();

    /// <summary>
    /// Redo history items.
    /// </summary>
    public ObservableCollection<UndoRedoHistoryItem> RedoHistory { get; } = new();

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
    /// Refreshes the history collections from the manager.
    /// </summary>
    public void RefreshHistory()
    {
        UndoHistory.Clear();
        RedoHistory.Clear();

        if (_undoRedoManager == null) return;

        var undoDescriptions = _undoRedoManager.GetUndoDescriptions();
        for (int i = 0; i < undoDescriptions.Count; i++)
        {
            UndoHistory.Add(new UndoRedoHistoryItem
            {
                Index = i,
                Description = undoDescriptions[i]
            });
        }

        var redoDescriptions = _undoRedoManager.GetRedoDescriptions();
        for (int i = 0; i < redoDescriptions.Count; i++)
        {
            RedoHistory.Add(new UndoRedoHistoryItem
            {
                Index = i,
                Description = redoDescriptions[i]
            });
        }
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
    /// Undoes to a specific index in the history.
    /// </summary>
    [RelayCommand]
    private void UndoTo(int index)
    {
        if (_undoRedoManager == null) return;

        // Undo (index + 1) times to reach the selected state
        for (int i = 0; i <= index; i++)
        {
            _undoRedoManager.Undo();
        }

        ActionPerformed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Redoes to a specific index in the history.
    /// </summary>
    [RelayCommand]
    private void RedoTo(int index)
    {
        if (_undoRedoManager == null) return;

        // Redo (index + 1) times to reach the selected state
        for (int i = 0; i <= index; i++)
        {
            _undoRedoManager.Redo();
        }

        ActionPerformed?.Invoke(this, EventArgs.Empty);
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

    // Explicit interface implementation for ICommand properties
    ICommand IUndoRedoButtonGroupViewModel.UndoCommand => UndoCommand;
    ICommand IUndoRedoButtonGroupViewModel.RedoCommand => RedoCommand;
    ICommand IUndoRedoButtonGroupViewModel.UndoToCommand => UndoToCommand;
    ICommand IUndoRedoButtonGroupViewModel.RedoToCommand => RedoToCommand;
}
