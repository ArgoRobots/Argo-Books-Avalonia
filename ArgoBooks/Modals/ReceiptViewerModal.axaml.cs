using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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

    // Zoom settings
    private double _zoomLevel = 1.0;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.25;

    // Panning (right-click or middle-click drag)
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    // Rubber band overscroll effect
    private Vector _overscroll;
    private const double OverscrollResistance = 0.3;
    private const double OverscrollMaxDistance = 100;

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
    }

    private void FindControls()
    {
        _imageScrollViewer ??= this.FindControl<ScrollViewer>("ImageScrollViewer");
        _zoomTransformControl ??= this.FindControl<LayoutTransformControl>("ZoomTransformControl");
        _receiptImage ??= this.FindControl<Image>("ReceiptImage");
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
            }
        }
    }

    #endregion

    #region Zoom

    private void ApplyZoom()
    {
        if (_zoomTransformControl == null) return;
        _zoomTransformControl.LayoutTransform = new ScaleTransform(_zoomLevel, _zoomLevel);
    }

    private void ZoomIn() => ZoomTowardsCenter(true);
    private void ZoomOut() => ZoomTowardsCenter(false);

    private void ResetZoom()
    {
        _zoomLevel = 1.0;
        ApplyZoom();
    }

    private void ZoomTowardsCenter(bool zoomIn)
    {
        if (_imageScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = _zoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        var viewportCenterX = _imageScrollViewer.Viewport.Width / 2;
        var viewportCenterY = _imageScrollViewer.Viewport.Height / 2;

        var contentCenterX = (_imageScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_imageScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        _zoomLevel = newZoom;
        ApplyZoom();
        _zoomTransformControl.UpdateLayout();

        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;

        var maxX = Math.Max(0, _imageScrollViewer.Extent.Width - _imageScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _imageScrollViewer.Extent.Height - _imageScrollViewer.Viewport.Height);

        _imageScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
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

        var unscaledX = scaledContentPoint.X / oldZoom;
        var unscaledY = scaledContentPoint.Y / oldZoom;

        _zoomLevel = newZoom;
        ApplyZoom();
        _zoomTransformControl.UpdateLayout();

        var newOffsetX = unscaledX * newZoom - viewportPoint.X;
        var newOffsetY = unscaledY * newZoom - viewportPoint.Y;

        var maxX = Math.Max(0, _imageScrollViewer.Extent.Width - _imageScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _imageScrollViewer.Extent.Height - _imageScrollViewer.Viewport.Height);

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

        if (_isPanning && _imageScrollViewer != null)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _panStartPoint - currentPoint;

            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            var maxX = Math.Max(0, _imageScrollViewer.Extent.Width - _imageScrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _imageScrollViewer.Extent.Height - _imageScrollViewer.Viewport.Height);

            double overscrollX = 0;
            double overscrollY = 0;
            double clampedX = desiredX;
            double clampedY = desiredY;

            if (desiredX < 0)
            {
                overscrollX = desiredX * OverscrollResistance;
                overscrollX = Math.Max(overscrollX, -OverscrollMaxDistance);
                clampedX = 0;
            }
            else if (desiredX > maxX)
            {
                overscrollX = (desiredX - maxX) * OverscrollResistance;
                overscrollX = Math.Min(overscrollX, OverscrollMaxDistance);
                clampedX = maxX;
            }

            if (desiredY < 0)
            {
                overscrollY = desiredY * OverscrollResistance;
                overscrollY = Math.Max(overscrollY, -OverscrollMaxDistance);
                clampedY = 0;
            }
            else if (desiredY > maxY)
            {
                overscrollY = (desiredY - maxY) * OverscrollResistance;
                overscrollY = Math.Min(overscrollY, OverscrollMaxDistance);
                clampedY = maxY;
            }

            _imageScrollViewer.Offset = new Vector(clampedX, clampedY);

            _overscroll = new Vector(overscrollX, overscrollY);
            ApplyOverscrollTransform();

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

            if (_overscroll.X != 0 || _overscroll.Y != 0)
            {
                AnimateOverscrollSnapBack();
            }

            e.Handled = true;
        }
    }

    private void ApplyOverscrollTransform()
    {
        if (_zoomTransformControl == null) return;
        var translateTransform = new TranslateTransform(-_overscroll.X, -_overscroll.Y);
        _zoomTransformControl.RenderTransform = translateTransform;
    }

    private async void AnimateOverscrollSnapBack()
    {
        const int steps = 12;
        const int delayMs = 16;

        var startOverscroll = _overscroll;

        for (int i = 1; i <= steps; i++)
        {
            double t = i / (double)steps;
            double easeOut = 1 - Math.Pow(1 - t, 3);

            _overscroll = new Vector(
                startOverscroll.X * (1 - easeOut),
                startOverscroll.Y * (1 - easeOut)
            );

            ApplyOverscrollTransform();
            await Task.Delay(delayMs);
        }

        _overscroll = new Vector(0, 0);
        ApplyOverscrollTransform();
    }

    #endregion
}
