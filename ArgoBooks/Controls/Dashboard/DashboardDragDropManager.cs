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
    private DragGridOverlay? _dragGrid;
    private Control? _dragSourceControl;

    // Preview state: no collection changes until mouse release
    private int _previewTargetIndex = -1;
    private DashboardRowPanel? _crossRowTargetPanel;

    private const double DragDeadZone = 5;
    private const double AutoScrollMargin = 50;
    private const double AutoScrollSpeed = 8;

    private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(59, 130, 246));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(239, 68, 68));

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
        FinalizeDrag();
    }

    private void StartDrag()
    {
        _isDragging = true;
        _previewTargetIndex = _dragSourceIndex;
        _crossRowTargetPanel = null;

        if (_dragSourceControl != null)
            _dragSourceControl.Opacity = 0;

        var sourceWidth = _dragSourceControl?.Bounds.Width ?? 200;
        var sourceHeight = _dragSourceControl?.Bounds.Height ?? 80;

        _dragGhost = new Border
        {
            Width = sourceWidth,
            Height = sourceHeight,
            Background = new SolidColorBrush(Color.FromArgb(30, 59, 130, 246)),
            BorderBrush = BlueBrush,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            IsHitTestVisible = false,
            Opacity = 0.7,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        if (_rowsContainer.Parent is Panel parent)
        {
            _dragGrid = new DragGridOverlay(_rowsContainer);
            parent.Children.Add(_dragGrid);
            parent.Children.Add(_dragGhost);
        }
    }

    private void UpdateDrag(Point position)
    {
        if (_dragGhost == null || _sourceRowPanel == null) return;

        var ghostWidth = _dragGhost.Width;
        var ghostHeight = _dragGhost.Height;
        var ghostX = position.X - _dragOffset.X;
        var ghostY = position.Y - _dragOffset.Y;

        _dragGhost.Margin = new Thickness(ghostX, ghostY, 0, 0);
        _dragGrid?.InvalidateVisual();

        var ghostRect = new Rect(ghostX, ghostY, ghostWidth, ghostHeight);
        var targetRowPanel = FindRowPanelAtPosition(ghostRect.Center);

        if (targetRowPanel == _sourceRowPanel)
        {
            _dragGhost.BorderBrush = BlueBrush;
            _crossRowTargetPanel = null;
            UpdateSameRowPreview(ghostRect);
        }
        else if (targetRowPanel != null)
        {
            ResetPreviewTransforms();
            _previewTargetIndex = _dragSourceIndex;
            UpdateCrossRowPreview(targetRowPanel);
        }
        else
        {
            _dragGhost.BorderBrush = BlueBrush;
            _crossRowTargetPanel = null;
            ResetPreviewTransforms();
            _previewTargetIndex = _dragSourceIndex;
        }
    }

    private void UpdateSameRowPreview(Rect ghostRect)
    {
        if (_sourceRowPanel == null || _dragSourceIndex < 0) return;

        var children = _sourceRowPanel.Children;
        int count = children.Count;
        if (count <= 1) return;

        int sourceIdx = _dragSourceIndex;
        var sourceMargin = children[sourceIdx].Margin;
        double sourceWidth = children[sourceIdx].Bounds.Width + sourceMargin.Left + sourceMargin.Right;

        // Ghost center X in row-panel-local coordinates
        var panelPos = _sourceRowPanel.TranslatePoint(new Point(0, 0), _rowsContainer) ?? new Point();
        double localGhostCenterX = ghostRect.Center.X - panelPos.X;

        // Determine target: compare ghost center against midpoints of non-source widgets
        int targetIdx = sourceIdx;

        for (int i = sourceIdx + 1; i < count; i++)
        {
            var midX = children[i].Bounds.Left + children[i].Bounds.Width / 2;
            if (localGhostCenterX > midX)
                targetIdx = i;
        }

        for (int i = sourceIdx - 1; i >= 0; i--)
        {
            var midX = children[i].Bounds.Left + children[i].Bounds.Width / 2;
            if (localGhostCenterX < midX)
                targetIdx = i;
        }

        _previewTargetIndex = targetIdx;

        // Apply transforms: shift widgets between source and target by source's width
        for (int i = 0; i < count; i++)
        {
            if (i == sourceIdx) continue;

            double dx = 0;
            if (targetIdx > sourceIdx && i > sourceIdx && i <= targetIdx)
                dx = -sourceWidth;
            else if (targetIdx < sourceIdx && i >= targetIdx && i < sourceIdx)
                dx = sourceWidth;

            children[i].RenderTransform = Math.Abs(dx) > 0.5
                ? new TranslateTransform(dx, 0)
                : null;
        }
    }

    private void UpdateCrossRowPreview(DashboardRowPanel targetRowPanel)
    {
        if (_sourceRowPanel == null || _dragSourceIndex < 0 || _dragGhost == null) return;

        var sourceRowVm = FindRowVm(_sourceRowPanel);
        var targetRowVm = FindRowVm(targetRowPanel);
        if (sourceRowVm == null || targetRowVm == null) return;
        if (_dragSourceIndex >= sourceRowVm.Widgets.Count) return;

        if (!targetRowVm.CanFit(sourceRowVm.Widgets[_dragSourceIndex].Size))
        {
            _dragGhost.BorderBrush = RedBrush;
            _crossRowTargetPanel = null;
            return;
        }

        _dragGhost.BorderBrush = BlueBrush;
        _crossRowTargetPanel = targetRowPanel;
    }

    private void FinalizeDrag()
    {
        // Capture move info before resetting state
        var sourceRowPanel = _sourceRowPanel;
        int sourceIndex = _dragSourceIndex;
        int targetIndex = _previewTargetIndex;
        var crossRowTarget = _crossRowTargetPanel;

        // Reset all visual state first (before any collection changes trigger rebuilds)
        ResetPreviewTransforms();

        if (_dragSourceControl != null)
        {
            _dragSourceControl.Opacity = 1.0;
            _dragSourceControl = null;
        }

        if (_rowsContainer.Parent is Panel parent)
        {
            if (_dragGrid != null)
                parent.Children.Remove(_dragGrid);
            if (_dragGhost != null)
                parent.Children.Remove(_dragGhost);
        }
        _dragGrid = null;
        _dragGhost = null;

        _isDragging = false;
        _dragSourceIndex = -1;
        _previewTargetIndex = -1;
        _crossRowTargetPanel = null;
        _sourceRowPanel = null;

        // Now perform the actual move (may trigger RebuildRows — that's fine, state is clean)
        if (crossRowTarget != null && sourceRowPanel != null)
        {
            var sourceRowVm = FindRowVm(sourceRowPanel);
            var targetRowVm = FindRowVm(crossRowTarget);
            if (sourceRowVm != null && targetRowVm != null
                && sourceIndex >= 0 && sourceIndex < sourceRowVm.Widgets.Count
                && targetRowVm.CanFit(sourceRowVm.Widgets[sourceIndex].Size))
            {
                _layoutVm.MoveWidgetToRow(sourceRowVm, sourceIndex, targetRowVm);
            }
        }
        else if (sourceRowPanel != null && targetIndex >= 0 && targetIndex != sourceIndex)
        {
            var rowVm = FindRowVm(sourceRowPanel);
            if (rowVm != null && sourceIndex >= 0 && sourceIndex < rowVm.Widgets.Count
                && targetIndex < rowVm.Widgets.Count)
            {
                rowVm.Widgets.Move(sourceIndex, targetIndex);
            }
        }
    }

    private void ResetPreviewTransforms()
    {
        if (_sourceRowPanel == null) return;
        foreach (var child in _sourceRowPanel.Children)
            child.RenderTransform = null;
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

    private void HandleAutoScroll(Point posInScrollViewer)
    {
        if (posInScrollViewer.Y < AutoScrollMargin)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X,
                Math.Max(0, _scrollViewer.Offset.Y - AutoScrollSpeed));
        else if (posInScrollViewer.Y > _scrollViewer.Bounds.Height - AutoScrollMargin)
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X,
                _scrollViewer.Offset.Y + AutoScrollSpeed);
    }
}
