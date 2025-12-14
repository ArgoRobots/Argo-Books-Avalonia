using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Controls;

public partial class UndoRedoButtonGroup : UserControl
{
    private Popup? _undoPopup;
    private Popup? _redoPopup;

    public UndoRedoButtonGroup()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _undoPopup = this.FindControl<Popup>("UndoPopup");
        _redoPopup = this.FindControl<Popup>("RedoPopup");
    }

    private void UndoDropdownButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_undoPopup != null && DataContext is UndoRedoButtonGroupViewModel vm)
        {
            vm.RefreshHistory();
            _redoPopup?.Close();
            _undoPopup.IsOpen = !_undoPopup.IsOpen;
        }
    }

    private void RedoDropdownButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_redoPopup != null && DataContext is UndoRedoButtonGroupViewModel vm)
        {
            vm.RefreshHistory();
            _undoPopup?.Close();
            _redoPopup.IsOpen = !_redoPopup.IsOpen;
        }
    }

    /// <summary>
    /// Gets the position for the undo dropdown.
    /// </summary>
    public Point GetUndoDropdownPosition()
    {
        var button = this.FindControl<Button>("UndoDropdownButton");
        if (button != null)
        {
            var position = button.TranslatePoint(new Point(0, button.Bounds.Height), TopLevel.GetTopLevel(this));
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
        if (button != null)
        {
            var position = button.TranslatePoint(new Point(0, button.Bounds.Height), TopLevel.GetTopLevel(this));
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
