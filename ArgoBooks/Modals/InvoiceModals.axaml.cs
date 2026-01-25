using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ArgoBooks.Helpers;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating, editing, and filtering invoices.
/// Includes zoom, pan, and rubber band functionality for the preview modal.
/// </summary>
public partial class InvoiceModals : UserControl
{
    #region Private Fields

    private ScrollViewer? _previewScrollViewer;
    private LayoutTransformControl? _previewZoomTransformControl;
    private Control? _invoiceHtmlPreview;
    private OverscrollHelper? _previewOverscrollHelper;
    private Slider? _previewZoomSlider;
    private TextBlock? _previewZoomPercentText;
    private bool _updatingSlider;

    // Zoom settings
    private double _previewZoomLevel = 1.0;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.1;

    // Panning (right-click drag)
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    #endregion

    public InvoiceModals()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        FindPreviewControls();

        // Use Tunnel strategy to intercept wheel events before ScrollViewer handles them
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.AddHandler(PointerWheelChangedEvent, OnPreviewScrollViewerPointerWheelChanged, RoutingStrategies.Tunnel);
            // Intercept right-click at tunnel level to prevent HtmlLabel's context menu
            _previewScrollViewer.AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        }

        // Disable context menu on the preview area
        if (_invoiceHtmlPreview != null)
        {
            _invoiceHtmlPreview.ContextMenu = null;
            _invoiceHtmlPreview.AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
        }
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_previewScrollViewer);

        // Intercept right-click to prevent context menu and enable panning
        if (point.Properties.IsRightButtonPressed || point.Properties.IsMiddleButtonPressed)
        {
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
                    e.Pointer.Capture(_previewScrollViewer);
                    _previewScrollViewer.Cursor = new Cursor(StandardCursorType.Hand);
                    e.Handled = true; // Prevent HtmlLabel from receiving the event
                }
            }
        }
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // Prevent the default context menu from showing
        e.Handled = true;
    }

    private void FindPreviewControls()
    {
        _previewScrollViewer ??= this.FindControl<ScrollViewer>("PreviewScrollViewer");
        _previewZoomTransformControl ??= this.FindControl<LayoutTransformControl>("PreviewZoomTransformControl");
        _invoiceHtmlPreview ??= this.FindControl<Control>("InvoiceHtmlPreview");
        _previewZoomSlider ??= this.FindControl<Slider>("PreviewZoomSlider");
        _previewZoomPercentText ??= this.FindControl<TextBlock>("PreviewZoomPercentText");

        if (_previewZoomTransformControl != null && _previewOverscrollHelper == null)
        {
            _previewOverscrollHelper = new OverscrollHelper(_previewZoomTransformControl);
        }

        // Initialize slider value
        if (_previewZoomSlider != null)
        {
            _updatingSlider = true;
            _previewZoomSlider.Value = _previewZoomLevel;
            _updatingSlider = false;
        }

        UpdatePreviewZoomDisplay();
    }

    #region Preview Zoom

    private void ApplyPreviewZoom()
    {
        if (_previewZoomTransformControl == null) return;
        _previewZoomTransformControl.LayoutTransform = new ScaleTransform(_previewZoomLevel, _previewZoomLevel);
        UpdatePreviewZoomDisplay();
    }

    private void UpdatePreviewZoomDisplay()
    {
        // Update slider (without triggering the event)
        if (_previewZoomSlider != null && !_updatingSlider)
        {
            _updatingSlider = true;
            _previewZoomSlider.Value = _previewZoomLevel;
            _updatingSlider = false;
        }

        // Update percentage text
        if (_previewZoomPercentText != null)
        {
            _previewZoomPercentText.Text = $"{_previewZoomLevel:P0}";
        }
    }

    private void PreviewZoomIn_Click(object? sender, RoutedEventArgs e) => PreviewZoomTowardsCenter(true);
    private void PreviewZoomOut_Click(object? sender, RoutedEventArgs e) => PreviewZoomTowardsCenter(false);

    private void PreviewZoomSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingSlider) return;
        PreviewZoomToLevel(e.NewValue);
    }

    private void PreviewZoomToLevel(double newZoom)
    {
        if (_previewScrollViewer == null || _previewZoomTransformControl == null) return;

        var oldZoom = _previewZoomLevel;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Get the center of the viewport in content coordinates
        var viewportCenterX = _previewScrollViewer.Viewport.Width / 2;
        var viewportCenterY = _previewScrollViewer.Viewport.Height / 2;

        var contentCenterX = (_previewScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_previewScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        _previewZoomLevel = newZoom;
        ApplyPreviewZoom();
        _previewZoomTransformControl.UpdateLayout();

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

    private void PreviewFitToWindow_Click(object? sender, RoutedEventArgs e)
    {
        PreviewZoomToFit();
    }

    private void PreviewZoomToFit()
    {
        if (_previewScrollViewer == null || _previewZoomTransformControl == null) return;

        // First reset to 1.0 zoom to get accurate content dimensions
        _previewZoomLevel = 1.0;
        ApplyPreviewZoom();
        _previewZoomTransformControl.UpdateLayout();

        // Get content dimensions from the scroll extent
        var contentWidth = _previewScrollViewer.Extent.Width;
        var contentHeight = _previewScrollViewer.Extent.Height;

        // Fallback to reasonable defaults if extent is not yet calculated
        if (contentWidth <= 0) contentWidth = 700;
        if (contentHeight <= 0) contentHeight = 900;

        // Get viewport size (use full viewport, no margin subtraction)
        var viewportWidth = _previewScrollViewer.Bounds.Width;
        var viewportHeight = _previewScrollViewer.Bounds.Height;

        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        // Calculate zoom to fit
        var scaleX = viewportWidth / contentWidth;
        var scaleY = viewportHeight / contentHeight;

        _previewZoomLevel = Math.Clamp(Math.Min(scaleX, scaleY), MinZoom, MaxZoom);
        ApplyPreviewZoom();
    }

    private void PreviewZoomTowardsCenter(bool zoomIn)
    {
        var newZoom = zoomIn
            ? Math.Min(_previewZoomLevel + ZoomStep, MaxZoom)
            : Math.Max(_previewZoomLevel - ZoomStep, MinZoom);

        PreviewZoomToLevel(newZoom);
    }

    private void OnPreviewScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Only zoom when Ctrl is pressed
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        var delta = e.Delta.Y;
        if (delta != 0 && _previewZoomTransformControl != null)
        {
            var viewportPoint = e.GetPosition(_previewScrollViewer);
            var contentPoint = e.GetPosition(_previewZoomTransformControl);
            PreviewZoomAtPoint(delta > 0, viewportPoint, contentPoint);
        }
        e.Handled = true;
    }

    private void PreviewZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (_previewScrollViewer == null || _previewZoomTransformControl == null) return;

        var oldZoom = _previewZoomLevel;
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

        _previewZoomLevel = newZoom;
        ApplyPreviewZoom();
        _previewZoomTransformControl.UpdateLayout();

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

    #region Preview Panning and Overscroll

    // Note: Panning is initiated in OnPreviewPointerPressed (Tunnel handler) to intercept
    // right-click before HtmlLabel's context menu can appear

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning && _previewScrollViewer != null && _previewOverscrollHelper != null)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _panStartPoint - currentPoint;

            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            var maxX = Math.Max(0, _previewScrollViewer.Extent.Width - _previewScrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _previewScrollViewer.Extent.Height - _previewScrollViewer.Viewport.Height);

            var (clampedX, clampedY, overscrollX, overscrollY) =
                _previewOverscrollHelper.CalculateOverscroll(desiredX, desiredY, maxX, maxY);

            _previewScrollViewer.Offset = new Vector(clampedX, clampedY);
            _previewOverscrollHelper.ApplyOverscroll(overscrollX, overscrollY);

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
            if (_previewScrollViewer != null)
            {
                _previewScrollViewer.Cursor = Cursor.Default;
            }

            if (_previewOverscrollHelper?.HasOverscroll == true)
            {
                _ = _previewOverscrollHelper.AnimateSnapBackAsync();
            }

            e.Handled = true;
        }
    }

    #endregion
}
