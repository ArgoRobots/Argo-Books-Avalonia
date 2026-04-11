using ArgoBooks.ViewModels.Dashboard;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ArgoBooks.Controls.Dashboard;

public class DashboardDragDropManager
{
    private readonly List<DashboardRowPanel> _rowPanels;
    private readonly StackPanel _rowsContainer;
    private readonly ScrollViewer _scrollViewer;
    private readonly DashboardLayoutViewModel _layoutVm;

    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _dragOffset;
    private DashboardRowPanel? _sourceRowPanel;
    private int _dragSourceIndex = -1;
    private Border? _dragGhost;
    private Control? _dragSourceControl;
    private bool _swapSettling;

    private const double DragDeadZone = 5;
    private const double AutoScrollMargin = 50;
    private const double AutoScrollSpeed = 8;

    public DashboardDragDropManager(
        List<DashboardRowPanel> rowPanels,
        StackPanel rowsContainer,
        ScrollViewer scrollViewer,
        DashboardLayoutViewModel layoutVm)
    {
        _rowPanels = rowPanels;
        _rowsContainer = rowsContainer;
        _scrollViewer = scrollViewer;
        _layoutVm = layoutVm;

        _scrollViewer.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
        _scrollViewer.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
    }

    public void AttachDragHandle(Control dragHandle)
    {
        dragHandle.PointerPressed += OnDragHandlePressed;
    }

    private void OnDragHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;

        var widgetHost = (sender as Control)?.FindAncestorOfType<WidgetHost>();
        if (widgetHost == null) return;

        foreach (var rowPanel in _rowPanels)
        {
            var idx = rowPanel.Children.IndexOf(widgetHost);
            if (idx >= 0)
            {
                _sourceRowPanel = rowPanel;
                _dragSourceIndex = idx;
                _dragSourceControl = widgetHost;
                _dragStartPoint = e.GetPosition(_rowsContainer);
                var widgetPos = widgetHost.TranslatePoint(new Point(0, 0), _rowsContainer) ?? new Point();
                _dragOffset = _dragStartPoint - widgetPos;
                e.Handled = true;
                return;
            }
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSourceIndex < 0 || _sourceRowPanel == null) return;
        var position = e.GetPosition(_rowsContainer);

        if (!_isDragging)
        {
            var delta = position - _dragStartPoint;
            if (Math.Abs(delta.X) < DragDeadZone && Math.Abs(delta.Y) < DragDeadZone) return;
            StartDrag();
        }

        UpdateDrag(position);
        HandleAutoScroll(e.GetPosition(_scrollViewer));
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSourceIndex < 0) return;
        CancelDrag();
    }

    private void StartDrag()
    {
        _isDragging = true;
        _swapSettling = false;

        if (_dragSourceControl != null)
            _dragSourceControl.Opacity = 0;

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
            Opacity = 0.7,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        if (_rowsContainer.Parent is Panel parent)
            parent.Children.Add(_dragGhost);
    }

    private void UpdateDrag(Point position)
    {
        if (_dragGhost == null || _sourceRowPanel == null) return;

        var ghostWidth = _dragGhost.Width;
        var ghostHeight = _dragGhost.Height;
        var ghostX = position.X - _dragOffset.X;
        var ghostY = position.Y - _dragOffset.Y;

        _dragGhost.Margin = new Thickness(ghostX, ghostY, 0, 0);

        var ghostRect = new Rect(ghostX, ghostY, ghostWidth, ghostHeight);

        if (_swapSettling)
        {
            if (_dragSourceIndex >= 0 && _dragSourceIndex < _sourceRowPanel.Children.Count)
            {
                var srcControl = _sourceRowPanel.Children[_dragSourceIndex];
                var srcPos = srcControl.TranslatePoint(new Point(0, 0), _rowsContainer) ?? new Point();
                var srcRect = new Rect(srcPos, srcControl.Bounds.Size);
                var ghostCenter = ghostRect.Center;
                if (ghostCenter.X >= srcRect.Left && ghostCenter.X <= srcRect.Right
                    && ghostCenter.Y >= srcRect.Top && ghostCenter.Y <= srcRect.Bottom)
                    _swapSettling = false;
            }
            else
            {
                _swapSettling = false;
            }

            if (_swapSettling) return;
        }

        var targetRowPanel = FindRowPanelAtPosition(ghostRect.Center);

        if (targetRowPanel == _sourceRowPanel)
        {
            // Reset ghost color for same-row
            _dragGhost.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            TrySameRowSwap(ghostRect);
        }
        else if (targetRowPanel != null)
        {
            TryCrossRowMove(targetRowPanel);
        }
        else
        {
            // Not over any row — reset ghost color
            _dragGhost.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        }
    }

    private void TrySameRowSwap(Rect ghostRect)
    {
        if (_sourceRowPanel == null || _dragSourceIndex < 0) return;

        int bestTarget = -1;
        double bestOverlap = 0;
        double sourceOverlap = 0;

        for (int i = 0; i < _sourceRowPanel.Children.Count; i++)
        {
            var child = _sourceRowPanel.Children[i];
            var childPos = child.TranslatePoint(new Point(0, 0), _rowsContainer) ?? new Point();
            var childRect = new Rect(childPos, child.Bounds.Size);
            var overlap = RectOverlapArea(ghostRect, childRect);

            if (i == _dragSourceIndex)
            {
                sourceOverlap = overlap;
                continue;
            }

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                bestTarget = i;
            }
        }

        if (bestTarget < 0 || bestOverlap <= sourceOverlap) return;

        var rowVm = FindRowVm(_sourceRowPanel);
        if (rowVm == null) return;

        _layoutVm.SwapWidgetsInRow(rowVm, _dragSourceIndex, bestTarget);
        _swapSettling = true;
        _dragSourceIndex = bestTarget;

        if (_dragSourceIndex >= 0 && _dragSourceIndex < _sourceRowPanel.Children.Count)
        {
            _dragSourceControl = _sourceRowPanel.Children[_dragSourceIndex];
            _dragSourceControl.Opacity = 0;
        }
    }

    private void TryCrossRowMove(DashboardRowPanel targetRowPanel)
    {
        if (_sourceRowPanel == null || _dragSourceIndex < 0) return;

        var sourceRowVm = FindRowVm(_sourceRowPanel);
        var targetRowVm = FindRowVm(targetRowPanel);
        if (sourceRowVm == null || targetRowVm == null) return;

        if (_dragSourceIndex >= sourceRowVm.Widgets.Count) return;

        if (!targetRowVm.CanFit(sourceRowVm.Widgets[_dragSourceIndex].Size))
        {
            if (_dragGhost != null)
                _dragGhost.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            return;
        }

        if (_dragGhost != null)
            _dragGhost.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));

        if (_layoutVm.MoveWidgetToRow(sourceRowVm, _dragSourceIndex, targetRowVm))
        {
            CancelDrag();
        }
    }

    private DashboardRowPanel? FindRowPanelAtPosition(Point position)
    {
        foreach (var rowPanel in _rowPanels)
        {
            var rowHost = rowPanel.FindAncestorOfType<DashboardRowHost>();
            if (rowHost == null) continue;

            var topLeft = rowHost.TranslatePoint(new Point(0, 0), _rowsContainer) ?? new Point();
            var bounds = new Rect(topLeft, rowHost.Bounds.Size);
            if (bounds.Contains(position))
                return rowPanel;
        }
        return null;
    }

    private DashboardRowViewModel? FindRowVm(DashboardRowPanel panel)
    {
        var rowHost = panel.FindAncestorOfType<DashboardRowHost>();
        return rowHost?.DataContext as DashboardRowViewModel;
    }

    private static double RectOverlapArea(Rect a, Rect b)
    {
        double x = Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left));
        double y = Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top));
        return x * y;
    }

    private void HandleAutoScroll(Point posInScrollViewer)
    {
        if (posInScrollViewer.Y < AutoScrollMargin)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X,
                Math.Max(0, _scrollViewer.Offset.Y - AutoScrollSpeed));
        else if (posInScrollViewer.Y > _scrollViewer.Bounds.Height - AutoScrollMargin)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X,
                _scrollViewer.Offset.Y + AutoScrollSpeed);
    }

    private void CancelDrag()
    {
        if (_dragSourceControl != null)
        {
            _dragSourceControl.Opacity = 1.0;
            _dragSourceControl = null;
        }

        _isDragging = false;
        _dragSourceIndex = -1;
        _sourceRowPanel = null;

        if (_rowsContainer.Parent is Panel parent && _dragGhost != null)
            parent.Children.Remove(_dragGhost);

        _dragGhost = null;
    }
}
