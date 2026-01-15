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
/// Modal for viewing receipt images with pan, zoom, and rubber band overscroll.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class ReceiptViewerModal : UserControl
{
    #region Private Fields

    private ScrollViewer? _imageScrollViewer;
    private LayoutTransformControl? _zoomTransformControl;
    private Image? _receiptImage;
    private OverscrollHelper? _overscrollHelper;
    private Slider? _zoomSlider;
    private TextBlock? _zoomPercentText;
    private bool _eventsSubscribed;
    private bool _updatingSlider;

    // Zoom settings
    private double _zoomLevel = 1.0;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.25;

    // Panning (right-click or middle-click drag)
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    #endregion

    public ReceiptViewerModal()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        FindControls();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        FindControls();

        if (_imageScrollViewer != null)
        {
            _imageScrollViewer.AddHandler(PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        // Subscribe to ViewModel property changes
        if (DataContext is ReceiptViewerModalViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ReceiptViewerModalViewModel vm) return;

        // Fit to window when fullscreen is toggled or modal opens
        if (e.PropertyName == nameof(ReceiptViewerModalViewModel.IsFullscreen))
        {
            // Delay to allow layout to update after fullscreen change
            _ = FitToWindowAfterLayoutAsync();
        }
        else if (e.PropertyName == nameof(ReceiptViewerModalViewModel.IsOpen) && vm.IsOpen)
        {
            // Delay to allow layout to update and image to load
            _ = FitToWindowAfterLayoutAsync();
        }
    }

    private async Task FitToWindowAfterLayoutAsync()
    {
        // Wait for layout to update
        await Task.Delay(100);
        ZoomToFit();
    }

    private void FindControls()
    {
        _imageScrollViewer ??= this.FindControl<ScrollViewer>("ImageScrollViewer");
        _zoomTransformControl ??= this.FindControl<LayoutTransformControl>("ZoomTransformControl");
        _receiptImage ??= this.FindControl<Image>("ReceiptImage");
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

    #region Event Handlers

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ReceiptViewerModalViewModel vm)
            vm.CloseCommand.Execute(null);
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ReceiptViewerModalViewModel vm)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    vm.CloseCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F:
                    vm.ToggleFullscreenCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Add:
                case Key.OemPlus:
                    ZoomIn();
                    e.Handled = true;
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ZoomOut();
                    e.Handled = true;
                    break;
                case Key.D0:
                case Key.NumPad0:
                    ResetZoom();
                    e.Handled = true;
                    break;
                case Key.W:
                    ZoomToFit();
                    e.Handled = true;
                    break;
            }
        }
    }

    #endregion

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

    private void ZoomIn() => ZoomTowardsCenter(true);
    private void ZoomOut() => ZoomTowardsCenter(false);

    private void ZoomIn_Click(object? sender, RoutedEventArgs e) => ZoomIn();
    private void ZoomOut_Click(object? sender, RoutedEventArgs e) => ZoomOut();

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
        if (_imageScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = _zoomLevel;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Get the center of the viewport in content coordinates
        var viewportCenterX = _imageScrollViewer.Viewport.Width / 2;
        var viewportCenterY = _imageScrollViewer.Viewport.Height / 2;

        var contentCenterX = (_imageScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_imageScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        _zoomLevel = newZoom;
        ApplyZoom();
        _zoomTransformControl.UpdateLayout();

        // Calculate new offset to keep the same content point at center
        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;

        var maxX = Math.Max(0, _imageScrollViewer.Extent.Width - _imageScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _imageScrollViewer.Extent.Height - _imageScrollViewer.Viewport.Height);

        _imageScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    private void ResetZoom()
    {
        _zoomLevel = 1.0;
        ApplyZoom();
    }

    private void FitToWindow_Click(object? sender, RoutedEventArgs e)
    {
        ZoomToFit();
    }

    /// <summary>
    /// Zooms to fit the entire image in the viewport.
    /// </summary>
    private void ZoomToFit()
    {
        if (_imageScrollViewer == null || _receiptImage?.Source == null) return;

        // Get the actual image dimensions
        var imageSource = _receiptImage.Source;
        double imageWidth = 0;
        double imageHeight = 0;

        if (imageSource is Bitmap bitmap)
        {
            imageWidth = bitmap.PixelSize.Width;
            imageHeight = bitmap.PixelSize.Height;
        }

        if (imageWidth <= 0 || imageHeight <= 0) return;

        // Get viewport size (accounting for padding)
        var viewportWidth = _imageScrollViewer.Bounds.Width - 32; // 16px padding on each side
        var viewportHeight = _imageScrollViewer.Bounds.Height - 32;

        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        // Calculate zoom to fit
        var scaleX = viewportWidth / imageWidth;
        var scaleY = viewportHeight / imageHeight;

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
        var delta = e.Delta.Y;
        if (delta != 0 && _zoomTransformControl != null)
        {
            var viewportPoint = e.GetPosition(_imageScrollViewer);
            var contentPoint = e.GetPosition(_zoomTransformControl);
            ZoomAtPoint(delta > 0, viewportPoint, contentPoint);
        }
        e.Handled = true;
    }

    private void ZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (_imageScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = _zoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Account for centering when content is smaller than viewport
        var oldExtent = _imageScrollViewer.Extent;
        var viewportWidth = _imageScrollViewer.Viewport.Width;
        var viewportHeight = _imageScrollViewer.Viewport.Height;
        var oldOffset = _imageScrollViewer.Offset;

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
        var newExtent = _imageScrollViewer.Extent;
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

        _imageScrollViewer.Offset = new Vector(
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
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartOffset = new Vector(_imageScrollViewer?.Offset.X ?? 0, _imageScrollViewer?.Offset.Y ?? 0);
            e.Pointer.Capture(this);
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning && _imageScrollViewer != null && _overscrollHelper != null)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _panStartPoint - currentPoint;

            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            var maxX = Math.Max(0, _imageScrollViewer.Extent.Width - _imageScrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _imageScrollViewer.Extent.Height - _imageScrollViewer.Viewport.Height);

            var (clampedX, clampedY, overscrollX, overscrollY) =
                _overscrollHelper.CalculateOverscroll(desiredX, desiredY, maxX, maxY);

            _imageScrollViewer.Offset = new Vector(clampedX, clampedY);
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
