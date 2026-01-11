using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArgoBooks.Localization;
using ArgoBooks.Services;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a history item in the undo/redo dropdown.
/// </summary>
public partial class HistoryItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets the index of the item in the history.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the description of the action.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets whether this item is highlighted.
    /// </summary>
    [ObservableProperty]
    private bool _isHighlighted;

    /// <summary>
    /// Initializes a new history item.
    /// </summary>
    public HistoryItemViewModel(int index, string description)
    {
        Index = index;
        Description = description;
    }
}

/// <summary>
/// ViewModel for the undo/redo history dropdown panel.
/// </summary>
public partial class UndoRedoHistoryPanelViewModel : ViewModelBase
{
    private UndoRedoManager? _undoRedoManager;
    private bool _isUndoMode;
    private Action? _onActionPerformed;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private Thickness _dropdownMargin;

    [ObservableProperty]
    private string _actionLabel = "";

    [ObservableProperty]
    private int _highlightedCount;

    /// <summary>
    /// Gets the history items.
    /// </summary>
    public ObservableCollection<HistoryItemViewModel> HistoryItems { get; } = [];

    /// <summary>
    /// Gets whether there are items in the history.
    /// </summary>
    public bool HasItems => HistoryItems.Count > 0;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public UndoRedoHistoryPanelViewModel()
    {
    }

    /// <summary>
    /// Shows the dropdown panel for undo history.
    /// </summary>
    /// <param name="manager">The undo/redo manager.</param>
    /// <param name="position">Position to show the dropdown.</param>
    /// <param name="onActionPerformed">Callback when an action is performed.</param>
    public void ShowUndo(UndoRedoManager manager, Point position, Action? onActionPerformed = null)
    {
        Show(manager, position, isUndo: true, onActionPerformed);
    }

    /// <summary>
    /// Shows the dropdown panel for redo history.
    /// </summary>
    /// <param name="manager">The undo/redo manager.</param>
    /// <param name="position">Position to show the dropdown.</param>
    /// <param name="onActionPerformed">Callback when an action is performed.</param>
    public void ShowRedo(UndoRedoManager manager, Point position, Action? onActionPerformed = null)
    {
        Show(manager, position, isUndo: false, onActionPerformed);
    }

    /// <summary>
    /// Shows the dropdown panel.
    /// </summary>
    private void Show(UndoRedoManager manager, Point position, bool isUndo, Action? onActionPerformed)
    {
        _undoRedoManager = manager;
        _isUndoMode = isUndo;
        _onActionPerformed = onActionPerformed;

        DropdownMargin = new Thickness(position.X, position.Y, 0, 0);

        // Populate history items
        HistoryItems.Clear();
        var history = isUndo ? manager.UndoHistory : manager.RedoHistory;
        int index = 0;
        foreach (var action in history)
        {
            HistoryItems.Add(new HistoryItemViewModel(index++, action.Description));
        }

        OnPropertyChanged(nameof(HasItems));

        // Default highlight first item
        HighlightedCount = 0;
        if (HistoryItems.Count > 0)
        {
            HighlightUpTo(HistoryItems[0]);
        }

        IsOpen = true;
    }

    /// <summary>
    /// Highlights items up to and including the specified item.
    /// </summary>
    public void HighlightUpTo(HistoryItemViewModel item)
    {
        HighlightedCount = item.Index + 1;

        foreach (var historyItem in HistoryItems)
        {
            historyItem.IsHighlighted = historyItem.Index <= item.Index;
        }

        UpdateActionLabel();
    }

    /// <summary>
    /// Updates the action label based on highlighted count.
    /// </summary>
    private void UpdateActionLabel()
    {
        if (_isUndoMode)
        {
            ActionLabel = HighlightedCount == 1
                ? "Undo 1 Action".Translate()
                : "Undo {0} Actions".TranslateFormat(HighlightedCount);
        }
        else
        {
            ActionLabel = HighlightedCount == 1
                ? "Redo 1 Action".Translate()
                : "Redo {0} Actions".TranslateFormat(HighlightedCount);
        }
    }

    /// <summary>
    /// Selects an item and performs undo/redo.
    /// </summary>
    [RelayCommand]
    private void SelectItem(HistoryItemViewModel? item)
    {
        if (item == null || _undoRedoManager == null)
            return;

        var count = item.Index + 1;

        if (_isUndoMode)
        {
            _undoRedoManager.UndoMultiple(count);
        }
        else
        {
            _undoRedoManager.RedoMultiple(count);
        }

        Close();
        _onActionPerformed?.Invoke();
    }

    /// <summary>
    /// Closes the dropdown panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        HistoryItems.Clear();
        OnPropertyChanged(nameof(HasItems));
    }
}
