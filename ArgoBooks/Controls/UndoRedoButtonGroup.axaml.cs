using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Controls;

public partial class UndoRedoButtonGroup : UserControl
{
    private Popup? _undoPopup;
    private Popup? _redoPopup;
    private ItemsControl? _undoHistoryList;
    private ItemsControl? _redoHistoryList;
    private TextBlock? _undoCountLabel;
    private TextBlock? _redoCountLabel;
    private int _currentUndoHoverIndex = -1;
    private int _currentRedoHoverIndex = -1;

    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromArgb(40, 59, 130, 246));
    private static readonly IBrush TransparentBrush = Brushes.Transparent;

    public UndoRedoButtonGroup()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _undoPopup = this.FindControl<Popup>("UndoPopup");
        _redoPopup = this.FindControl<Popup>("RedoPopup");
        _undoHistoryList = this.FindControl<ItemsControl>("UndoHistoryList");
        _redoHistoryList = this.FindControl<ItemsControl>("RedoHistoryList");
        _undoCountLabel = this.FindControl<TextBlock>("UndoCountLabel");
        _redoCountLabel = this.FindControl<TextBlock>("RedoCountLabel");
    }

    private void UndoDropdownButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_undoPopup != null && DataContext is UndoRedoButtonGroupViewModel vm)
        {
            vm.RefreshHistory();
            _redoPopup?.Close();
            _currentUndoHoverIndex = -1;
            UpdateUndoCountLabel();
            _undoPopup.IsOpen = !_undoPopup.IsOpen;
        }
    }

    private void RedoDropdownButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_redoPopup != null && DataContext is UndoRedoButtonGroupViewModel vm)
        {
            vm.RefreshHistory();
            _undoPopup?.Close();
            _currentRedoHoverIndex = -1;
            UpdateRedoCountLabel();
            _redoPopup.IsOpen = !_redoPopup.IsOpen;
        }
    }

    private void UndoItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.Tag is int index)
        {
            _currentUndoHoverIndex = index;
            UpdateUndoHighlighting();
            UpdateUndoCountLabel();
        }
    }

    private void UndoItem_PointerExited(object? sender, PointerEventArgs e)
    {
        // Don't clear immediately - only clear when leaving the whole list
    }

    private void UndoItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is int index && DataContext is UndoRedoButtonGroupViewModel vm)
        {
            vm.UndoToCommand.Execute(index);
            _undoPopup?.Close();
            _currentUndoHoverIndex = -1;
        }
    }

    private void RedoItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.Tag is int index)
        {
            _currentRedoHoverIndex = index;
            UpdateRedoHighlighting();
            UpdateRedoCountLabel();
        }
    }

    private void RedoItem_PointerExited(object? sender, PointerEventArgs e)
    {
        // Don't clear immediately - only clear when leaving the whole list
    }

    private void RedoItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is int index && DataContext is UndoRedoButtonGroupViewModel vm)
        {
            vm.RedoToCommand.Execute(index);
            _redoPopup?.Close();
            _currentRedoHoverIndex = -1;
        }
    }

    private void UpdateUndoHighlighting()
    {
        if (_undoHistoryList == null) return;

        for (int i = 0; i < _undoHistoryList.Items.Count; i++)
        {
            var container = _undoHistoryList.ContainerFromIndex(i);
            if (container is ContentPresenter cp && cp.Child is Border border)
            {
                // Highlight items from 0 to hoverIndex (inclusive)
                border.Background = i <= _currentUndoHoverIndex ? HighlightBrush : TransparentBrush;
            }
        }
    }

    private void UpdateRedoHighlighting()
    {
        if (_redoHistoryList == null) return;

        for (int i = 0; i < _redoHistoryList.Items.Count; i++)
        {
            var container = _redoHistoryList.ContainerFromIndex(i);
            if (container is ContentPresenter cp && cp.Child is Border border)
            {
                // Highlight items from 0 to hoverIndex (inclusive)
                border.Background = i <= _currentRedoHoverIndex ? HighlightBrush : TransparentBrush;
            }
        }
    }

    private void UpdateUndoCountLabel()
    {
        if (_undoCountLabel == null) return;

        if (_currentUndoHoverIndex < 0)
        {
            _undoCountLabel.Text = "Hover to select actions";
        }
        else
        {
            int count = _currentUndoHoverIndex + 1;
            _undoCountLabel.Text = count == 1 ? "Undo 1 action" : $"Undo {count} actions";
        }
    }

    private void UpdateRedoCountLabel()
    {
        if (_redoCountLabel == null) return;

        if (_currentRedoHoverIndex < 0)
        {
            _redoCountLabel.Text = "Hover to select actions";
        }
        else
        {
            int count = _currentRedoHoverIndex + 1;
            _redoCountLabel.Text = count == 1 ? "Redo 1 action" : $"Redo {count} actions";
        }
    }

    /// <summary>
    /// Gets the position for the undo dropdown.
    /// </summary>
    public Point GetUndoDropdownPosition()
    {
        var button = this.FindControl<Button>("UndoDropdownButton");
        var topLevel = TopLevel.GetTopLevel(this);
        if (button != null && topLevel != null)
        {
            var position = button.TranslatePoint(new Point(0, button.Bounds.Height), topLevel);
            return position ?? new Point(0, 0);
        }
        return new Point(0, 0);
    }

    /// <summary>
    /// Gets the position for the redo dropdown.
    /// </summary>
    public Point GetRedoDropdownPosition()
    {
        var button = this.FindControl<Button>("RedoDropdownButton");
        var topLevel = TopLevel.GetTopLevel(this);
        if (button != null && topLevel != null)
        {
            var position = button.TranslatePoint(new Point(0, button.Bounds.Height), topLevel);
            return position ?? new Point(0, 0);
        }
        return new Point(0, 0);
    }

    /// <summary>
    /// Closes all popups.
    /// </summary>
    public void ClosePopups()
    {
        _undoPopup?.Close();
        _redoPopup?.Close();
    }
}
