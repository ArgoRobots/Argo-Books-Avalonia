using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.ViewModels.Dashboard;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

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
    private DragGridOverlay? _dragGrid;
    private Control? _dragSourceControl;

    private const double DragDeadZone = 5;
    private const double AutoScrollMargin = 50;
    private const double AutoScrollSpeed = 8;
    private bool _swapSettling;

    public DashboardDragDropManager(DashboardFlowPanel panel, ScrollViewer scrollViewer, Action<int, int> onMoveWidget)
    {
        _panel = panel;
        _scrollViewer = scrollViewer;
        _onMoveWidget = onMoveWidget;

        _scrollViewer.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
        _scrollViewer.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
    }

    /// <summary>
    /// Attach a drag handle. The widget index is determined dynamically at press time
    /// by finding the parent WidgetHost's position in the panel, so handles never go stale.
    /// </summary>
    public void AttachDragHandle(Control dragHandle)
    {
        dragHandle.PointerPressed += OnDragHandlePressed;
    }

    private void OnDragHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;

        // Walk up from the drag handle to find the WidgetHost, then find its index
        var widgetHost = (sender as Control)?.FindAncestorOfType<WidgetHost>();
        if (widgetHost == null) return;

        var widgetIndex = _panel.Children.IndexOf(widgetHost);
        if (widgetIndex < 0) return;

        _dragStartPoint = e.GetPosition(_panel);
        _dragSourceIndex = widgetIndex;
        _dragSourceControl = widgetHost;

        // Calculate offset from pointer to widget top-left for ghost positioning
        var widgetPos = _dragSourceControl.Bounds.Position;
        _dragOffset = _dragStartPoint - widgetPos;

        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
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
        _currentDropIndex = _dragSourceIndex;
        _swapSettling = false;

        // Hide the source widget — the ghost is the visual representation.
        // Using Opacity 0 (not IsVisible=false) so it still occupies layout space.
        if (_dragSourceControl != null)
            _dragSourceControl.Opacity = 0;

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
            Opacity = 0.7,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        if (_panel.Parent is Panel parent)
        {
            // Grid overlay behind the ghost
            _dragGrid = new DragGridOverlay
            {
                IsHitTestVisible = false,
                IsVisible = true
            };
            parent.Children.Add(_dragGrid);
            UpdateGridOverlay();

            parent.Children.Add(_dragGhost);
        }
    }

    private void UpdateDrag(Point position)
    {
        // Move ghost to follow cursor
        var ghostWidth = _dragGhost?.Width ?? _dragSourceControl?.Bounds.Width ?? 0;
        var ghostHeight = _dragGhost?.Height ?? _dragSourceControl?.Bounds.Height ?? 0;
        var ghostX = position.X - _dragOffset.X;
        var ghostY = position.Y - _dragOffset.Y;

        if (_dragGhost != null)
            _dragGhost.Margin = new Thickness(ghostX, ghostY, 0, 0);

        var ghostRect = new Rect(ghostX, ghostY, ghostWidth, ghostHeight);
        var allBounds = _panel.GetChildBounds();

        // After a swap, block further swaps until the ghost reaches the source's new slot.
        // This prevents ping-pong when different-sized widgets swap (the source slot jumps
        // far from the ghost) or during cross-row reflows.
        if (_swapSettling && _dragSourceIndex >= 0 && _dragSourceIndex < allBounds.Count)
        {
            var src = allBounds[_dragSourceIndex];
            var ghostCX = ghostX + ghostWidth / 2;
            var ghostCY = ghostY + ghostHeight / 2;
            bool inSourceRow = ghostCY >= src.Top && ghostCY <= src.Bottom;
            if (!inSourceRow)
                // Ghost left the source's row entirely — user wants a new cross-row move
                _swapSettling = false;
            else if (ghostCX >= src.Left && ghostCX <= src.Right)
                // Ghost center is within the source's slot — settled
                _swapSettling = false;
        }

        var newDropIndex = _swapSettling
            ? _dragSourceIndex // still settling — no swap
            : CalculateDropIndex(ghostRect, allBounds, _dragSourceIndex);

        // Live rearrange: move the widget in the collection so the layout updates in real time.
        if (newDropIndex >= 0 && newDropIndex != _currentDropIndex && newDropIndex != _dragSourceIndex)
        {
            _onMoveWidget(_dragSourceIndex, newDropIndex);
            _swapSettling = true;

            // After move, the source index changes to the new position
            _dragSourceIndex = newDropIndex > _dragSourceIndex
                ? newDropIndex - 1
                : newDropIndex;
            _currentDropIndex = _dragSourceIndex;

            // Re-find the source control after rearrangement
            if (_dragSourceIndex >= 0 && _dragSourceIndex < _panel.Children.Count)
            {
                _dragSourceControl = _panel.Children[_dragSourceIndex];
                _dragSourceControl.Opacity = 0;
            }

            // Update ghost size and grid to match the new layout
            UpdateGhostSize();
            UpdateGridOverlay();
        }

        // Handle repositioning within a partial row — snap to grid columns
        UpdateRowStartOffset(ghostRect);
    }

    private void UpdateGhostSize()
    {
        if (_dragGhost == null || _dragSourceControl == null) return;

        // Force layout so bounds reflect the new position
        _panel.UpdateLayout();

        var w = _dragSourceControl.Bounds.Width;
        var h = _dragSourceControl.Bounds.Height;
        if (w > 0 && h > 0)
        {
            _dragGhost.Width = w;
            _dragGhost.Height = h;
        }
    }

    private int CalculateDropIndex(Rect ghostRect, IReadOnlyList<Rect> allBounds, int excludeIndex)
    {
        // Build filtered bounds list, skipping the dragged widget.
        var filtered = new List<(Rect bounds, int originalIndex)>();
        for (int i = 0; i < allBounds.Count; i++)
        {
            if (i != excludeIndex)
                filtered.Add((allBounds[i], i));
        }
        if (filtered.Count == 0) return excludeIndex;

        var sourceBounds = allBounds[excludeIndex];

        // Is the ghost in the same row as the source? (ghost center within source row's vertical extent)
        bool sameRow = ghostRect.Center.Y >= sourceBounds.Top && ghostRect.Center.Y <= sourceBounds.Bottom;

        if (sameRow)
            return CalculateSameRowDrop(ghostRect, filtered, sourceBounds, excludeIndex);
        else
            return CalculateCrossRowDrop(ghostRect, filtered, sourceBounds, excludeIndex, allBounds.Count);
    }

    /// <summary>
    /// Same-row: swap with the widget the ghost overlaps more than its own slot.
    /// </summary>
    private int CalculateSameRowDrop(Rect ghostRect, List<(Rect bounds, int originalIndex)> filtered,
        Rect sourceBounds, int excludeIndex)
    {
        double sourceOverlap = RectOverlapArea(ghostRect, sourceBounds);

        // Only consider widgets in the same row as the source
        int swapTarget = -1;
        double maxOverlap = 0;
        for (int i = 0; i < filtered.Count; i++)
        {
            if (Math.Abs(filtered[i].bounds.Top - sourceBounds.Top) > 1) continue;
            double overlap = RectOverlapArea(ghostRect, filtered[i].bounds);
            if (overlap > maxOverlap)
            {
                maxOverlap = overlap;
                swapTarget = i;
            }
        }

        if (swapTarget < 0 || maxOverlap <= sourceOverlap)
            return excludeIndex;

        int targetOrigIdx = filtered[swapTarget].originalIndex;
        return targetOrigIdx > excludeIndex ? targetOrigIdx + 1 : targetOrigIdx;
    }

    /// <summary>
    /// Cross-row: direction-aware insertion.
    /// Moving DOWN → insert after the last widget of the target row (don't split the row).
    /// Moving UP   → insert before the first widget of the target row.
    /// Also blocks moves that would go the wrong direction (prevents ping-pong).
    /// </summary>
    private int CalculateCrossRowDrop(Rect ghostRect, List<(Rect bounds, int originalIndex)> filtered,
        Rect sourceBounds, int excludeIndex, int totalCount)
    {
        // Group filtered widgets into rows by similar Y position
        var rows = new List<(int start, int end, double top, double bottom)>();
        int rowStart = 0;
        for (int i = 1; i < filtered.Count; i++)
        {
            if (Math.Abs(filtered[i].bounds.Top - filtered[rowStart].bounds.Top) > 1)
            {
                rows.Add((rowStart, i - 1, filtered[rowStart].bounds.Top, filtered[i - 1].bounds.Bottom));
                rowStart = i;
            }
        }
        rows.Add((rowStart, filtered.Count - 1, filtered[rowStart].bounds.Top, filtered[^1].bounds.Bottom));

        // Find the row closest to the ghost's center Y
        double ghostCenterY = ghostRect.Center.Y;
        int bestRow = 0;
        double bestDist = double.MaxValue;
        for (int r = 0; r < rows.Count; r++)
        {
            var (_, _, top, bottom) = rows[r];
            double dist = ghostCenterY < top ? top - ghostCenterY
                        : ghostCenterY > bottom ? ghostCenterY - bottom
                        : 0;
            if (dist < bestDist) { bestDist = dist; bestRow = r; }
        }

        var (rStart, rEnd, _, _) = rows[bestRow];

        bool movingDown = ghostRect.Center.Y > sourceBounds.Center.Y;

        int insertIndex;
        if (movingDown)
            // Insert after the last widget of the target row — avoids splitting the row
            insertIndex = filtered[rEnd].originalIndex + 1;
        else
            // Insert before the first widget of the target row
            insertIndex = filtered[rStart].originalIndex;

        // Guard: no-op check
        if (insertIndex == excludeIndex || insertIndex == excludeIndex + 1)
        {
            // Try the opposite end of the row as fallback
            int fallback = movingDown ? filtered[rStart].originalIndex : filtered[rEnd].originalIndex + 1;
            if (fallback != excludeIndex && fallback != excludeIndex + 1)
                insertIndex = fallback;
            else
                return excludeIndex;
        }

        // Direction check: don't move the widget the wrong way (prevents ping-pong)
        if (movingDown && insertIndex < excludeIndex)
            return excludeIndex;
        if (!movingDown && insertIndex > excludeIndex)
            return excludeIndex;

        return insertIndex;
    }

    /// <summary>
    /// When the source widget is in a partial row (total fractions &lt; 1.0), allow horizontal
    /// repositioning by snapping the RowStartOffset to the nearest grid column (0.25 increments).
    /// </summary>
    private void UpdateRowStartOffset(Rect ghostRect)
    {
        if (_dragSourceIndex < 0 || _dragSourceControl == null) return;
        if (_dragSourceControl.DataContext is not WidgetHostViewModel hostVm) return;

        var panelWidth = _panel.Bounds.Width;
        if (panelWidth <= 0) return;

        var fraction = hostVm.Size.ToFraction();

        // Find what row the source is in and calculate total row fraction
        var allBounds = _panel.GetChildBounds();
        if (_dragSourceIndex >= allBounds.Count) return;
        var sourceBounds = allBounds[_dragSourceIndex];
        double rowFractionSum = 0;
        for (int i = 0; i < allBounds.Count; i++)
        {
            if (Math.Abs(allBounds[i].Top - sourceBounds.Top) <= 1)
            {
                if (_panel.Children[i] is Control c)
                    rowFractionSum += DashboardFlowPanel.GetWidgetFraction(c);
            }
        }

        // Only reposition in partial rows
        if (rowFractionSum >= 0.999) return;

        // Snap ghost center X to nearest valid offset (0.25 increments)
        var ghostCenterFraction = ghostRect.Center.X / panelWidth;
        var widgetHalfFraction = fraction / 2;

        // Calculate the left edge fraction the widget would need
        var desiredStart = ghostCenterFraction - widgetHalfFraction;

        // Snap to 0.25 grid
        var snapped = Math.Round(desiredStart * 4) / 4;
        snapped = Math.Max(0, Math.Min(snapped, 1.0 - fraction));

        // Only update if changed
        if (Math.Abs(hostVm.RowStartOffset - snapped) < 0.01) return;

        hostVm.RowStartOffset = snapped;
        DashboardFlowPanel.SetRowStartOffset(_dragSourceControl, snapped);
        _panel.InvalidateMeasure();
        _panel.InvalidateArrange();
        UpdateGridOverlay();
    }

    private void UpdateGridOverlay()
    {
        if (_dragGrid == null) return;
        var bounds = _panel.GetChildBounds();
        _dragGrid.Update(bounds, _panel.Bounds.Width, _panel.Bounds.Height);
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
            if (_dragGrid != null)
                parent.Children.Remove(_dragGrid);
        }
        _dragGhost = null;
        _dragGrid = null;
    }
}
