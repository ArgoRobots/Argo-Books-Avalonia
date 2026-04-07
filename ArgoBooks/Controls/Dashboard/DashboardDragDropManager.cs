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
    private int _dragSourceIndex = -1;
    private int _currentDropIndex = -1;
    private DragDropIndicator? _dropIndicator;

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
        dragHandle.PointerPressed += (s, e) => OnPointerPressed(e, widgetIndex);
        dragHandle.PointerMoved += (s, e) => OnPointerMoved(e);
        dragHandle.PointerReleased += (s, e) => OnPointerReleased(e);
        dragHandle.PointerCaptureLost += (s, e) => CancelDrag();
    }

    private void OnPointerPressed(PointerPressedEventArgs e, int widgetIndex)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        _dragStartPoint = e.GetPosition(_panel);
        _dragSourceIndex = widgetIndex;
        e.Pointer.Capture((Control)e.Source!);
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
        if (_isDragging && _currentDropIndex >= 0 && _currentDropIndex != _dragSourceIndex)
            _onMoveWidget(_dragSourceIndex, _currentDropIndex);
        CancelDrag();
        e.Pointer.Capture(null);
    }

    private void StartDrag()
    {
        _isDragging = true;
        _dropIndicator = new DragDropIndicator
        {
            IsVisible = false,
            IndicatorBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            IsHitTestVisible = false
        };
        if (_panel.Parent is Panel parent)
            parent.Children.Add(_dropIndicator);
    }

    private void UpdateDrag(Point position)
    {
        var bounds = _panel.GetChildBounds();
        _currentDropIndex = CalculateDropIndex(position, bounds);

        if (_dropIndicator != null && _currentDropIndex >= 0 && bounds.Count > 0)
        {
            _dropIndicator.IsVisible = true;
            PositionIndicator(_currentDropIndex, bounds);
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

    private void PositionIndicator(int index, IReadOnlyList<Rect> bounds)
    {
        if (_dropIndicator == null || bounds.Count == 0) return;

        double y;
        if (index == 0) y = bounds[0].Top - 6;
        else if (index >= bounds.Count) y = bounds[^1].Bottom + 2;
        else y = (bounds[index - 1].Bottom + bounds[index].Top) / 2 - 6;

        // Position relative to panel within the parent
        var panelBounds = _panel.Bounds;
        Canvas.SetLeft(_dropIndicator, panelBounds.Left);
        Canvas.SetTop(_dropIndicator, panelBounds.Top + y);
        _dropIndicator.Width = panelBounds.Width;
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
        _isDragging = false;
        _dragSourceIndex = -1;
        _currentDropIndex = -1;
        if (_dropIndicator != null)
        {
            if (_panel.Parent is Panel parent)
                parent.Children.Remove(_dropIndicator);
            _dropIndicator = null;
        }
    }
}
