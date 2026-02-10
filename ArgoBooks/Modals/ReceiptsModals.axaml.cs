using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Receipts modals.
/// </summary>
public partial class ReceiptsModals : UserControl
{
    // Zoom settings for scan preview
    private double _scanZoomLevel = 1.0;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.25;
    private bool _updatingSlider;

    // Panning (right-click or middle-click drag)
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;
    private OverscrollHelper? _overscrollHelper;

    public ReceiptsModals()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (ScanPreviewScrollViewer != null)
        {
            ScanPreviewScrollViewer.AddHandler(PointerWheelChangedEvent, OnScanPreviewPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        if (ScanPreviewZoomTransform != null && _overscrollHelper == null)
        {
            _overscrollHelper = new OverscrollHelper(ScanPreviewZoomTransform);
        }

        if (ScanPreviewZoomSlider != null)
        {
            _updatingSlider = true;
            ScanPreviewZoomSlider.Value = _scanZoomLevel;
            _updatingSlider = false;
        }

        // Subscribe to ViewModel to fit image when scan results arrive
        if (DataContext is ReceiptsModalsViewModel vm)
        {
            vm.PropertyChanged += OnScanViewModelPropertyChanged;
        }
    }

    private void OnScanViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReceiptsModalsViewModel.HasScanResult) &&
            sender is ReceiptsModalsViewModel { HasScanResult: true })
        {
            // Reset zoom and fit when results first appear
            _scanZoomLevel = 1.0;
            _ = FitScanPreviewAfterLayoutAsync();
        }
    }

    private async Task FitScanPreviewAfterLayoutAsync()
    {
        // Wait for layout to settle after results state becomes visible
        await Task.Delay(150);
        ScanPreviewFitToWindow();
    }

    #region Scan Preview Zoom

    private void ApplyScanZoom()
    {
        if (ScanPreviewZoomTransform == null) return;
        ScanPreviewZoomTransform.LayoutTransform = new ScaleTransform(_scanZoomLevel, _scanZoomLevel);
        UpdateScanZoomDisplay();
    }

    private void UpdateScanZoomDisplay()
    {
        if (ScanPreviewZoomSlider != null && !_updatingSlider)
        {
            _updatingSlider = true;
            ScanPreviewZoomSlider.Value = _scanZoomLevel;
            _updatingSlider = false;
        }

        if (ScanPreviewZoomText != null)
        {
            ScanPreviewZoomText.Text = $"{_scanZoomLevel:P0}";
        }
    }

    private void ScanPreviewZoomIn_Click(object? sender, RoutedEventArgs e) => ScanPreviewZoomTowardsCenter(true);
    private void ScanPreviewZoomOut_Click(object? sender, RoutedEventArgs e) => ScanPreviewZoomTowardsCenter(false);

    private void ScanPreviewZoomSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingSlider) return;
        ScanPreviewZoomToLevel(e.NewValue);
    }

    private void ScanPreviewZoomToLevel(double newZoom)
    {
        if (ScanPreviewScrollViewer == null || ScanPreviewZoomTransform == null) return;

        var oldZoom = _scanZoomLevel;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);
        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        var viewportCenterX = ScanPreviewScrollViewer.Viewport.Width / 2;
        var viewportCenterY = ScanPreviewScrollViewer.Viewport.Height / 2;
        var contentCenterX = (ScanPreviewScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (ScanPreviewScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        _scanZoomLevel = newZoom;
        ApplyScanZoom();
        ScanPreviewZoomTransform.UpdateLayout();

        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;
        var maxX = Math.Max(0, ScanPreviewScrollViewer.Extent.Width - ScanPreviewScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, ScanPreviewScrollViewer.Extent.Height - ScanPreviewScrollViewer.Viewport.Height);

        ScanPreviewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    private void ScanPreviewFitToWindow_Click(object? sender, RoutedEventArgs e) => ScanPreviewFitToWindow();

    private void ScanPreviewFitToWindow()
    {
        if (ScanPreviewScrollViewer == null || ScanPreviewImage?.Source == null) return;

        double imageWidth = 0, imageHeight = 0;
        if (ScanPreviewImage.Source is Bitmap bitmap)
        {
            imageWidth = bitmap.PixelSize.Width;
            imageHeight = bitmap.PixelSize.Height;
        }

        if (imageWidth <= 0 || imageHeight <= 0) return;

        var viewportWidth = ScanPreviewScrollViewer.Bounds.Width;
        var viewportHeight = ScanPreviewScrollViewer.Bounds.Height;
        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        var scaleX = viewportWidth / imageWidth;
        var scaleY = viewportHeight / imageHeight;

        _scanZoomLevel = Math.Clamp(Math.Min(scaleX, scaleY), MinZoom, MaxZoom);
        ApplyScanZoom();
    }

    private void ScanPreviewZoomTowardsCenter(bool zoomIn)
    {
        var newZoom = zoomIn
            ? Math.Min(_scanZoomLevel + ZoomStep, MaxZoom)
            : Math.Max(_scanZoomLevel - ZoomStep, MinZoom);
        ScanPreviewZoomToLevel(newZoom);
    }

    private void OnScanPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        if (delta != 0 && ScanPreviewZoomTransform != null)
        {
            var viewportPoint = e.GetPosition(ScanPreviewScrollViewer);
            var contentPoint = e.GetPosition(ScanPreviewZoomTransform);
            ScanPreviewZoomAtPoint(delta > 0, viewportPoint, contentPoint);
        }
        e.Handled = true;
    }

    private void ScanPreviewZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (ScanPreviewScrollViewer == null || ScanPreviewZoomTransform == null) return;

        var oldZoom = _scanZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);
        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        var oldExtent = ScanPreviewScrollViewer.Extent;
        var viewportWidth = ScanPreviewScrollViewer.Viewport.Width;
        var viewportHeight = ScanPreviewScrollViewer.Viewport.Height;
        var oldOffset = ScanPreviewScrollViewer.Offset;

        var centeringOffsetX = oldExtent.Width < viewportWidth ? (viewportWidth - oldExtent.Width) / 2 : 0;
        var centeringOffsetY = oldExtent.Height < viewportHeight ? (viewportHeight - oldExtent.Height) / 2 : 0;

        var contentX = viewportPoint.X + oldOffset.X - centeringOffsetX;
        var contentY = viewportPoint.Y + oldOffset.Y - centeringOffsetY;
        var unscaledX = contentX / oldZoom;
        var unscaledY = contentY / oldZoom;

        _scanZoomLevel = newZoom;
        ApplyScanZoom();
        ScanPreviewZoomTransform.UpdateLayout();

        var newExtent = ScanPreviewScrollViewer.Extent;
        var newCenteringOffsetX = newExtent.Width < viewportWidth ? (viewportWidth - newExtent.Width) / 2 : 0;
        var newCenteringOffsetY = newExtent.Height < viewportHeight ? (viewportHeight - newExtent.Height) / 2 : 0;

        var newOffsetX = unscaledX * newZoom - viewportPoint.X + newCenteringOffsetX;
        var newOffsetY = unscaledY * newZoom - viewportPoint.Y + newCenteringOffsetY;

        var maxX = Math.Max(0, newExtent.Width - viewportWidth);
        var maxY = Math.Max(0, newExtent.Height - viewportHeight);

        ScanPreviewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    #endregion

    #region Scan Preview Panning

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed || point.Properties.IsMiddleButtonPressed)
        {
            if (ScanPreviewScrollViewer != null)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(this);
                _panStartOffset = new Vector(ScanPreviewScrollViewer.Offset.X, ScanPreviewScrollViewer.Offset.Y);
                e.Pointer.Capture(this);
                Cursor = new Cursor(StandardCursorType.SizeAll);
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning && ScanPreviewScrollViewer != null && _overscrollHelper != null)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _panStartPoint - currentPoint;

            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            var maxX = Math.Max(0, ScanPreviewScrollViewer.Extent.Width - ScanPreviewScrollViewer.Viewport.Width);
            var maxY = Math.Max(0, ScanPreviewScrollViewer.Extent.Height - ScanPreviewScrollViewer.Viewport.Height);

            var (clampedX, clampedY, overscrollX, overscrollY) =
                _overscrollHelper.CalculateOverscroll(desiredX, desiredY, maxX, maxY);

            ScanPreviewScrollViewer.Offset = new Vector(clampedX, clampedY);
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
