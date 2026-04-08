using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ArgoBooks.Controls.Dashboard;

public class DashboardDragDropManager
{
    private readonly DashboardFlowPanel _panel;
    private readonly ScrollViewer _scrollViewer;
    private readonly Action<int, int> _onMoveWidget;

    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _dragOffset;
    private int _dragSourceIndex = -1;
    private int _currentDropIndex = -1;
    private Border? _dragGhost;
    private Control? _dragSourceControl;

    private const double DragDeadZone = 5;
    private const double AutoScrollMargin = 50;
    private const double AutoScrollSpeed = 8;

    public DashboardDragDropManager(DashboardFlowPanel panel, ScrollViewer scrollViewer, Action<int, int> onMoveWidget)
    {
        _panel = panel;
        _scrollViewer = scrollViewer;
        _onMoveWidget = onMoveWidget;
    }

    public void AttachDragHandle(Control dragHandle, int widgetIndex)
    {
        dragHandle.PointerPressed += (s, e) => OnPointerPressed(e, widgetIndex, dragHandle);
        dragHandle.PointerMoved += (s, e) => OnPointerMoved(e);
        dragHandle.PointerReleased += (s, e) => OnPointerReleased(e);
        dragHandle.PointerCaptureLost += (s, e) => CancelDrag();
    }

    private void OnPointerPressed(PointerPressedEventArgs e, int widgetIndex, Control dragHandle)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        _dragStartPoint = e.GetPosition(_panel);
        _dragSourceIndex = widgetIndex;

        // Find the WidgetHost control for this drag handle
        if (widgetIndex >= 0 && widgetIndex < _panel.Children.Count)
            _dragSourceControl = _panel.Children[widgetIndex];

        // Calculate offset from pointer to widget top-left for ghost positioning
        if (_dragSourceControl != null)
        {
            var widgetPos = _dragSourceControl.Bounds.Position;
            _dragOffset = _dragStartPoint - widgetPos;
        }

        e.Pointer.Capture(dragHandle);
        e.Handled = true;
    }

    private void OnPointerMoved(PointerEventArgs e)
    {
        if (_dragSourceIndex < 0) return;
        var position = e.GetPosition(_panel);

        if (!_isDragging)
        {
            var delta = position - _dragStartPoint;
            if (Math.Abs(delta.X) < DragDeadZone && Math.Abs(delta.Y) < DragDeadZone) return;
            StartDrag();
        }

        UpdateDrag(position);
        HandleAutoScroll(e.GetPosition(_scrollViewer));
    }

    private void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // Final position is already set via live rearrangement — just clean up
        CancelDrag();
        e.Pointer.Capture(null);
    }

    private void StartDrag()
    {
        _isDragging = true;
        _currentDropIndex = _dragSourceIndex;

        // Make the source widget semi-transparent
        if (_dragSourceControl != null)
            _dragSourceControl.Opacity = 0.4;

        // Create a ghost outline that follows the cursor
        var sourceWidth = _dragSourceControl?.Bounds.Width ?? 200;
        var sourceHeight = _dragSourceControl?.Bounds.Height ?? 80;

        _dragGhost = new Border
        {
            Width = sourceWidth,
            Height = sourceHeight,
            Background = new SolidColorBrush(Color.FromArgb(30, 59, 130, 246)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            IsHitTestVisible = false,
            IsVisible = true,
            Opacity = 0.7
        };

        if (_panel.Parent is Panel parent)
            parent.Children.Add(_dragGhost);
    }

    private void UpdateDrag(Point position)
    {
        // Move ghost to follow cursor
        if (_dragGhost != null)
        {
            var ghostX = position.X - _dragOffset.X;
            var ghostY = position.Y - _dragOffset.Y;
            _dragGhost.Margin = new Thickness(ghostX, ghostY, 0, 0);
            _dragGhost.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            _dragGhost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        }

        // Calculate where the widget would drop
        var bounds = _panel.GetChildBounds();
        var newDropIndex = CalculateDropIndex(position, bounds);

        // Live rearrange: move the widget in the collection so the layout updates in real time
        if (newDropIndex >= 0 && newDropIndex != _currentDropIndex && newDropIndex != _dragSourceIndex)
        {
            _onMoveWidget(_dragSourceIndex, newDropIndex);

            // After move, the source index changes to the new position
            _dragSourceIndex = newDropIndex > _dragSourceIndex
                ? newDropIndex - 1
                : newDropIndex;
            _currentDropIndex = _dragSourceIndex;

            // Re-find the source control after rearrangement
            if (_dragSourceIndex >= 0 && _dragSourceIndex < _panel.Children.Count)
            {
                _dragSourceControl = _panel.Children[_dragSourceIndex];
                _dragSourceControl.Opacity = 0.4;
            }
        }
    }

    private int CalculateDropIndex(Point position, IReadOnlyList<Rect> bounds)
    {
        if (bounds.Count == 0) return 0;

        var bestIndex = 0;
        var bestDistance = double.MaxValue;

        for (int i = 0; i <= bounds.Count; i++)
        {
            double gapY, gapX;
            if (i == 0)
            {
                gapY = bounds[0].Top;
                gapX = bounds[0].Left;
            }
            else if (i == bounds.Count)
            {
                gapY = bounds[^1].Bottom;
                gapX = bounds[^1].Left;
            }
            else
            {
                var prev = bounds[i - 1];
                var curr = bounds[i];
                if (Math.Abs(prev.Top - curr.Top) < 1) // same row
                {
                    gapY = prev.Top + prev.Height / 2;
                    gapX = (prev.Right + curr.Left) / 2;
                }
                else // different rows
                {
                    gapY = (prev.Bottom + curr.Top) / 2;
                    gapX = curr.Left;
                }
            }

            var dist = Math.Sqrt(Math.Pow(position.X - gapX, 2) + Math.Pow(position.Y - gapY, 2));
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private void HandleAutoScroll(Point posInScrollViewer)
    {
        if (posInScrollViewer.Y < AutoScrollMargin)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, Math.Max(0, _scrollViewer.Offset.Y - AutoScrollSpeed));
        else if (posInScrollViewer.Y > _scrollViewer.Bounds.Height - AutoScrollMargin)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y + AutoScrollSpeed);
    }

    private void CancelDrag()
    {
        // Restore source widget opacity
        if (_dragSourceControl != null)
        {
            _dragSourceControl.Opacity = 1.0;
            _dragSourceControl = null;
        }

        _isDragging = false;
        _dragSourceIndex = -1;
        _currentDropIndex = -1;

        if (_panel.Parent is Panel parent)
        {
            if (_dragGhost != null)
                parent.Children.Remove(_dragGhost);
        }
        _dragGhost = null;
    }
}
