using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArgoBooks.Services;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the undo/redo button group control in the Reports page.
/// Works with ReportUndoRedoManager instead of the general UndoRedoManager.
/// </summary>
public partial class ReportsUndoRedoButtonGroupViewModel : ViewModelBase, IUndoRedoButtonGroupViewModel
{
    private ReportUndoRedoManager? _undoRedoManager;

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
    /// Event raised when an action is performed.
    /// </summary>
    public event EventHandler? ActionPerformed;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ReportsUndoRedoButtonGroupViewModel()
    {
    }

    /// <summary>
    /// Constructor with ReportUndoRedoManager.
    /// </summary>
    public ReportsUndoRedoButtonGroupViewModel(ReportUndoRedoManager manager)
    {
        SetUndoRedoManager(manager);
    }

    /// <summary>
    /// Sets the undo/redo manager.
    /// </summary>
    public void SetUndoRedoManager(ReportUndoRedoManager manager)
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

        int i = 0;
        foreach (var description in _undoRedoManager.UndoHistory)
        {
            UndoHistory.Add(new UndoRedoHistoryItem
            {
                Index = i++,
                Description = description
            });
        }

        i = 0;
        foreach (var description in _undoRedoManager.RedoHistory)
        {
            RedoHistory.Add(new UndoRedoHistoryItem
            {
                Index = i++,
                Description = description
            });
        }
    }

    /// <summary>
    /// Performs an undo operation.
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        if (_undoRedoManager?.CanUndo == true)
        {
            _undoRedoManager.Undo();
            ActionPerformed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Performs a redo operation.
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        if (_undoRedoManager?.CanRedo == true)
        {
            _undoRedoManager.Redo();
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
    /// Gets the undo/redo manager.
    /// </summary>
    public ReportUndoRedoManager? Manager => _undoRedoManager;

    // Explicit interface implementation for ICommand properties
    System.Windows.Input.ICommand IUndoRedoButtonGroupViewModel.UndoCommand => UndoCommand;
    System.Windows.Input.ICommand IUndoRedoButtonGroupViewModel.RedoCommand => RedoCommand;
    System.Windows.Input.ICommand IUndoRedoButtonGroupViewModel.UndoToCommand => UndoToCommand;
    System.Windows.Input.ICommand IUndoRedoButtonGroupViewModel.RedoToCommand => RedoToCommand;
}
