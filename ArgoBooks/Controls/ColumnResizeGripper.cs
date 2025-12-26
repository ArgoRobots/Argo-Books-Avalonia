using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ArgoBooks.Controls;

/// <summary>
/// A control for resizing table columns via drag.
/// </summary>
public class ColumnResizeGripper : Border
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private double _originalWidth;

    public static readonly StyledProperty<string> ColumnNameProperty =
        AvaloniaProperty.Register<ColumnResizeGripper, string>(nameof(ColumnName), string.Empty);

    public static readonly StyledProperty<TableColumnWidths?> ColumnWidthsProperty =
        AvaloniaProperty.Register<ColumnResizeGripper, TableColumnWidths?>(nameof(ColumnWidths));

    /// <summary>
    /// Gets or sets the name of the column this gripper controls.
    /// </summary>
    public string ColumnName
    {
        get => GetValue(ColumnNameProperty);
        set => SetValue(ColumnNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the TableColumnWidths instance.
    /// </summary>
    public TableColumnWidths? ColumnWidths
    {
        get => GetValue(ColumnWidthsProperty);
        set => SetValue(ColumnWidthsProperty, value);
    }

    public ColumnResizeGripper()
    {
        Width = 8;
        MinHeight = 20;
        Cursor = new Cursor(StandardCursorType.SizeWestEast);
        Background = Brushes.Transparent;
        Margin = new Thickness(-4, 0, -4, 0);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!_isDragging)
        {
            Background = new SolidColorBrush(Color.FromArgb(80, 59, 130, 246));
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!_isDragging)
        {
            Background = Brushes.Transparent;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this.GetVisualRoot() as Visual);
            _originalWidth = GetCurrentColumnWidth();
            Background = new SolidColorBrush(Color.FromArgb(120, 59, 130, 246));
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this.GetVisualRoot() as Visual);
            var delta = currentPoint.X - _dragStartPoint.X;
            var newWidth = Math.Max(ColumnWidths?.GetMinWidth(ColumnName) ?? 50, _originalWidth + delta);

            ColumnWidths?.SetColumnWidth(ColumnName, newWidth);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            _isDragging = false;
            Background = Brushes.Transparent;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private double GetCurrentColumnWidth()
    {
        return ColumnWidths?.GetColumnWidth(ColumnName) ?? 100;
    }
}
