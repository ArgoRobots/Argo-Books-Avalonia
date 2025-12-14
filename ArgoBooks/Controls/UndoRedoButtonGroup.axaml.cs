using Avalonia;
using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Controls;

public partial class UndoRedoButtonGroup : UserControl
{
    public UndoRedoButtonGroup()
    {
        InitializeComponent();
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
}
