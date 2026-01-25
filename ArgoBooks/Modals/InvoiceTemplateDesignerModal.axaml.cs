using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ArgoBooks.Helpers;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal for designing invoice templates with zoom, pan, and rubber band overscroll.
/// </summary>
public partial class InvoiceTemplateDesignerModal : UserControl
{
    #region Private Fields

    private ScrollViewer? _previewScrollViewer;
    private LayoutTransformControl? _zoomTransformControl;
    private Control? _htmlPreviewPanel;
    private OverscrollHelper? _overscrollHelper;
    private Slider? _zoomSlider;
    private TextBlock? _zoomPercentText;
    private bool _updatingSlider;

    // Zoom settings
    private double _zoomLevel = 1.0;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.1;

    // Panning (right-click drag)
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    #endregion

    public InvoiceTemplateDesignerModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        FindControls();

        // Use Tunnel strategy to intercept wheel events before ScrollViewer handles them
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.AddHandler(PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        // Disable context menu on HtmlLabel by handling the event
        if (_htmlPreviewPanel != null)
        {
            _htmlPreviewPanel.ContextMenu = null;
            _htmlPreviewPanel.AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
        }
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // Prevent the default context menu from showing
        e.Handled = true;
    }

    private void FindControls()
    {
        _previewScrollViewer ??= this.FindControl<ScrollViewer>("PreviewScrollViewer");
        _zoomTransformControl ??= this.FindControl<LayoutTransformControl>("ZoomTransformControl");
        _htmlPreviewPanel ??= this.FindControl<Control>("HtmlPreviewPanel");
        _zoomSlider ??= this.FindControl<Slider>("ZoomSlider");
        _zoomPercentText ??= this.FindControl<TextBlock>("ZoomPercentText");

        if (_zoomTransformControl != null && _overscrollHelper == null)
        {
            _overscrollHelper = new OverscrollHelper(_zoomTransformControl);
        }

        // Initialize slider value
        if (_zoomSlider != null)
        {
            _updatingSlider = true;
            _zoomSlider.Value = _zoomLevel;
            _updatingSlider = false;
        }

        UpdateZoomDisplay();
    }

    #region Zoom

    private void ApplyZoom()
    {
        if (_zoomTransformControl == null) return;
        _zoomTransformControl.LayoutTransform = new ScaleTransform(_zoomLevel, _zoomLevel);
        UpdateZoomDisplay();
    }

    private void UpdateZoomDisplay()
    {
        // Update slider (without triggering the event)
        if (_zoomSlider != null && !_updatingSlider)
        {
            _updatingSlider = true;
            _zoomSlider.Value = _zoomLevel;
            _updatingSlider = false;
        }

        // Update percentage text
        if (_zoomPercentText != null)
        {
            _zoomPercentText.Text = $"{_zoomLevel:P0}";
        }
    }

    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => ZoomTowardsCenter(true);
    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => ZoomTowardsCenter(false);

    private void ZoomSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingSlider) return;
        ZoomToLevel(e.NewValue);
    }

    /// <summary>
    /// Zooms to a specific level while maintaining center focus.
    /// </summary>
    private void ZoomToLevel(double newZoom)
    {
        if (_previewScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = _zoomLevel;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Get the center of the viewport in content coordinates
        var viewportCenterX = _previewScrollViewer.Viewport.Width / 2;
        var viewportCenterY = _previewScrollViewer.Viewport.Height / 2;

        var contentCenterX = (_previewScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_previewScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        _zoomLevel = newZoom;
        ApplyZoom();
        _zoomTransformControl.UpdateLayout();

        // Calculate new offset to keep the same content point at center
        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;

        var maxX = Math.Max(0, _previewScrollViewer.Extent.Width - _previewScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _previewScrollViewer.Extent.Height - _previewScrollViewer.Viewport.Height);

        _previewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    private void FitToWindow_Click(object? sender, RoutedEventArgs e)
    {
        ZoomToFit();
    }

    /// <summary>
    /// Zooms to fit the entire preview in the viewport.
    /// </summary>
    private void ZoomToFit()
    {
        if (_previewScrollViewer == null || _zoomTransformControl == null) return;

        // First reset to 1.0 zoom to get accurate content dimensions
        _zoomLevel = 1.0;
        ApplyZoom();
        _zoomTransformControl.UpdateLayout();

        // Get content dimensions from the scroll extent (this gives us the actual content size)
        var contentWidth = _previewScrollViewer.Extent.Width;
        var contentHeight = _previewScrollViewer.Extent.Height;

        // Fallback to reasonable defaults if extent is not yet calculated
        if (contentWidth <= 0) contentWidth = 700;
        if (contentHeight <= 0) contentHeight = 900;

        // Get viewport size (accounting for margin)
        var viewportWidth = _previewScrollViewer.Bounds.Width - 60;
        var viewportHeight = _previewScrollViewer.Bounds.Height - 60;

        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        // Calculate zoom to fit
        var scaleX = viewportWidth / contentWidth;
        var scaleY = viewportHeight / contentHeight;

        _zoomLevel = Math.Clamp(Math.Min(scaleX, scaleY), MinZoom, MaxZoom);
        ApplyZoom();
    }

    private void ZoomTowardsCenter(bool zoomIn)
    {
        var newZoom = zoomIn
            ? Math.Min(_zoomLevel + ZoomStep, MaxZoom)
            : Math.Max(_zoomLevel - ZoomStep, MinZoom);

        ZoomToLevel(newZoom);
    }

    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Only zoom when Ctrl is pressed
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        var delta = e.Delta.Y;
        if (delta != 0 && _zoomTransformControl != null)
        {
            var viewportPoint = e.GetPosition(_previewScrollViewer);
            var contentPoint = e.GetPosition(_zoomTransformControl);
            ZoomAtPoint(delta > 0, viewportPoint, contentPoint);
        }
        e.Handled = true;
    }

    private void ZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (_previewScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = _zoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Account for centering when content is smaller than viewport
        var oldExtent = _previewScrollViewer.Extent;
        var viewportWidth = _previewScrollViewer.Viewport.Width;
        var viewportHeight = _previewScrollViewer.Viewport.Height;
        var oldOffset = _previewScrollViewer.Offset;

        var centeringOffsetX = oldExtent.Width < viewportWidth
            ? (viewportWidth - oldExtent.Width) / 2
            : 0;
        var centeringOffsetY = oldExtent.Height < viewportHeight
            ? (viewportHeight - oldExtent.Height) / 2
            : 0;

        // Calculate mouse position in content space
        var contentX = viewportPoint.X + oldOffset.X - centeringOffsetX;
        var contentY = viewportPoint.Y + oldOffset.Y - centeringOffsetY;

        // Convert to unscaled coordinates
        var unscaledX = contentX / oldZoom;
        var unscaledY = contentY / oldZoom;

        _zoomLevel = newZoom;
        ApplyZoom();
        _zoomTransformControl.UpdateLayout();

        // Calculate new offset to keep mouse position at same content point
        var newExtent = _previewScrollViewer.Extent;
        var newCenteringOffsetX = newExtent.Width < viewportWidth
            ? (viewportWidth - newExtent.Width) / 2
            : 0;
        var newCenteringOffsetY = newExtent.Height < viewportHeight
            ? (viewportHeight - newExtent.Height) / 2
            : 0;

        var newContentX = unscaledX * newZoom;
        var newContentY = unscaledY * newZoom;

        var newOffsetX = newContentX - viewportPoint.X + newCenteringOffsetX;
        var newOffsetY = newContentY - viewportPoint.Y + newCenteringOffsetY;

        var maxX = Math.Max(0, newExtent.Width - viewportWidth);
        var maxY = Math.Max(0, newExtent.Height - viewportHeight);

        _previewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    #endregion

    #region Panning and Overscroll

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);

        // Start panning with right mouse button or middle mouse button
        if (point.Properties.IsRightButtonPressed || point.Properties.IsMiddleButtonPressed)
        {
            // Only pan if we're over the preview area
            if (_previewScrollViewer != null)
            {
                var pos = e.GetPosition(_previewScrollViewer);
                if (pos.X >= 0 && pos.Y >= 0 &&
                    pos.X <= _previewScrollViewer.Bounds.Width &&
                    pos.Y <= _previewScrollViewer.Bounds.Height)
                {
                    _isPanning = true;
                    _panStartPoint = e.GetPosition(this);
                    _panStartOffset = new Vector(_previewScrollViewer.Offset.X, _previewScrollViewer.Offset.Y);
                    e.Pointer.Capture(this);
                    Cursor = new Cursor(StandardCursorType.Hand);
                    e.Handled = true;
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning && _previewScrollViewer != null && _overscrollHelper != null)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _panStartPoint - currentPoint;

            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            var maxX = Math.Max(0, _previewScrollViewer.Extent.Width - _previewScrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _previewScrollViewer.Extent.Height - _previewScrollViewer.Viewport.Height);

            var (clampedX, clampedY, overscrollX, overscrollY) =
                _overscrollHelper.CalculateOverscroll(desiredX, desiredY, maxX, maxY);

            _previewScrollViewer.Offset = new Vector(clampedX, clampedY);
            _overscrollHelper.ApplyOverscroll(overscrollX, overscrollY);

            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            Cursor = Cursor.Default;

            if (_overscrollHelper?.HasOverscroll == true)
            {
                _ = _overscrollHelper.AnimateSnapBackAsync();
            }

            e.Handled = true;
        }
    }

    #endregion
}
