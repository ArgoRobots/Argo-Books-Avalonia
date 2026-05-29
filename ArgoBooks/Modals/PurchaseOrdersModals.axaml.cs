using System.ComponentModel;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Purchase Orders modals.
/// ESC key handling is managed by ModalOverlay.
/// </summary>
public partial class PurchaseOrdersModals : UserControl
{
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.25;

    private double _sendZoomLevel = 1.0;
    private bool _updatingSendSlider;
    private OverscrollHelper? _sendOverscrollHelper;

    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;
    private ScrollViewer? _panScrollViewer;
    private OverscrollHelper? _activePanOverscroll;

    private PurchaseOrdersModalsViewModel? _subscribedVm;

    public PurchaseOrdersModals()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (SendPdfPreviewScrollViewer != null)
        {
            SendPdfPreviewScrollViewer.AddHandler(PointerWheelChangedEvent, OnSendPdfPreviewPointerWheelChanged, RoutingStrategies.Tunnel);
            SendPdfPreviewScrollViewer.AddHandler(PointerPressedEvent, OnSendPdfPreviewPointerPressed, RoutingStrategies.Tunnel);
            SendPdfPreviewScrollViewer.AddHandler(PointerMovedEvent, OnSendPdfPreviewPointerMoved, RoutingStrategies.Tunnel);
            SendPdfPreviewScrollViewer.AddHandler(PointerReleasedEvent, OnSendPdfPreviewPointerReleased, RoutingStrategies.Tunnel);
        }

        if (SendPdfPreviewZoomTransform != null && _sendOverscrollHelper == null)
        {
            _sendOverscrollHelper = new OverscrollHelper(SendPdfPreviewZoomTransform);
        }

        if (SendPdfPreviewZoomSlider != null)
        {
            _updatingSendSlider = true;
            SendPdfPreviewZoomSlider.Value = _sendZoomLevel;
            _updatingSendSlider = false;
        }

        if (DataContext is PurchaseOrdersModalsViewModel vm)
        {
            _subscribedVm = vm;
            vm.PropertyChanged += OnSendViewModelPropertyChanged;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnSendViewModelPropertyChanged;
            _subscribedVm = null;
        }
    }

    private void OnSendViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PurchaseOrdersModalsViewModel vm) return;

        if (e.PropertyName == nameof(PurchaseOrdersModalsViewModel.SendPdfPreview) && vm.SendPdfPreview != null)
        {
            _sendZoomLevel = 1.0;
            _ = FitSendPreviewAfterLayoutAsync();
        }
        else if (e.PropertyName == nameof(PurchaseOrdersModalsViewModel.IsSendModalOpen) && vm.IsSendModalOpen)
        {
            _sendZoomLevel = 1.0;
            _ = FitSendPreviewAfterLayoutAsync();
        }
        else if (e.PropertyName == nameof(PurchaseOrdersModalsViewModel.IsSendFullscreen))
        {
            _ = FitSendPreviewAfterLayoutAsync();
        }
    }

    private async Task FitSendPreviewAfterLayoutAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        SendPdfPreviewFitToWindow();
    }

    #region Send PDF Preview Zoom

    private void ApplySendZoom()
    {
        if (SendPdfPreviewZoomTransform == null) return;
        SendPdfPreviewZoomTransform.LayoutTransform = new ScaleTransform(_sendZoomLevel, _sendZoomLevel);
        UpdateSendZoomDisplay();
    }

    private void UpdateSendZoomDisplay()
    {
        if (SendPdfPreviewZoomSlider != null && !_updatingSendSlider)
        {
            _updatingSendSlider = true;
            SendPdfPreviewZoomSlider.Value = _sendZoomLevel;
            _updatingSendSlider = false;
        }

        if (SendPdfPreviewZoomText != null)
        {
            SendPdfPreviewZoomText.Text = $"{_sendZoomLevel:P0}";
        }
    }

    private void SendPdfPreviewZoomIn_Click(object? sender, RoutedEventArgs e) => SendPreviewZoomTowardsCenter(true);
    private void SendPdfPreviewZoomOut_Click(object? sender, RoutedEventArgs e) => SendPreviewZoomTowardsCenter(false);

    private void SendPdfPreviewZoomSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_updatingSendSlider) return;
        SendPreviewZoomToLevel(e.NewValue);
    }

    private void SendPreviewZoomToLevel(double newZoom)
    {
        if (SendPdfPreviewScrollViewer == null || SendPdfPreviewZoomTransform == null) return;

        var oldZoom = _sendZoomLevel;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);
        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        var viewportCenterX = SendPdfPreviewScrollViewer.Viewport.Width / 2;
        var viewportCenterY = SendPdfPreviewScrollViewer.Viewport.Height / 2;
        var contentCenterX = (SendPdfPreviewScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (SendPdfPreviewScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        _sendZoomLevel = newZoom;
        ApplySendZoom();
        SendPdfPreviewZoomTransform.UpdateLayout();

        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;
        var maxX = Math.Max(0, SendPdfPreviewScrollViewer.Extent.Width - SendPdfPreviewScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, SendPdfPreviewScrollViewer.Extent.Height - SendPdfPreviewScrollViewer.Viewport.Height);

        SendPdfPreviewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    private void SendPdfPreviewFitToWindow_Click(object? sender, RoutedEventArgs e) => SendPdfPreviewFitToWindow();

    private void SendPdfPreviewFitToWindow()
    {
        if (SendPdfPreviewScrollViewer == null || SendPdfPreviewCard == null) return;

        var imageWidth = SendPdfPreviewCard.Bounds.Width;
        var imageHeight = SendPdfPreviewCard.Bounds.Height;
        if (imageWidth <= 0 || imageHeight <= 0) return;

        var viewportWidth = SendPdfPreviewScrollViewer.Bounds.Width;
        var viewportHeight = SendPdfPreviewScrollViewer.Bounds.Height;
        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        var scaleX = viewportWidth / imageWidth;
        var scaleY = viewportHeight / imageHeight;

        _sendZoomLevel = Math.Clamp(Math.Min(scaleX, scaleY), 0.01, MaxZoom);
        ApplySendZoom();
    }

    private void SendPreviewZoomTowardsCenter(bool zoomIn)
    {
        var newZoom = zoomIn
            ? Math.Min(_sendZoomLevel + ZoomStep, MaxZoom)
            : Math.Max(_sendZoomLevel - ZoomStep, MinZoom);
        SendPreviewZoomToLevel(newZoom);
    }

    private void OnSendPdfPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        if (delta != 0 && SendPdfPreviewZoomTransform != null)
        {
            var viewportPoint = e.GetPosition(SendPdfPreviewScrollViewer);
            SendPreviewZoomAtPoint(delta > 0, viewportPoint);
        }
        e.Handled = true;
    }

    private void SendPreviewZoomAtPoint(bool zoomIn, Point viewportPoint)
    {
        if (SendPdfPreviewScrollViewer == null || SendPdfPreviewZoomTransform == null) return;

        var oldZoom = _sendZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);
        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        var oldExtent = SendPdfPreviewScrollViewer.Extent;
        var viewportWidth = SendPdfPreviewScrollViewer.Viewport.Width;
        var viewportHeight = SendPdfPreviewScrollViewer.Viewport.Height;
        var oldOffset = SendPdfPreviewScrollViewer.Offset;

        var centeringOffsetX = oldExtent.Width < viewportWidth ? (viewportWidth - oldExtent.Width) / 2 : 0;
        var centeringOffsetY = oldExtent.Height < viewportHeight ? (viewportHeight - oldExtent.Height) / 2 : 0;

        var contentX = viewportPoint.X + oldOffset.X - centeringOffsetX;
        var contentY = viewportPoint.Y + oldOffset.Y - centeringOffsetY;
        var unscaledX = contentX / oldZoom;
        var unscaledY = contentY / oldZoom;

        _sendZoomLevel = newZoom;
        ApplySendZoom();
        SendPdfPreviewZoomTransform.UpdateLayout();

        var newExtent = SendPdfPreviewScrollViewer.Extent;
        var newCenteringOffsetX = newExtent.Width < viewportWidth ? (viewportWidth - newExtent.Width) / 2 : 0;
        var newCenteringOffsetY = newExtent.Height < viewportHeight ? (viewportHeight - newExtent.Height) / 2 : 0;

        var newOffsetX = unscaledX * newZoom - viewportPoint.X + newCenteringOffsetX;
        var newOffsetY = unscaledY * newZoom - viewportPoint.Y + newCenteringOffsetY;

        var maxX = Math.Max(0, newExtent.Width - viewportWidth);
        var maxY = Math.Max(0, newExtent.Height - viewportHeight);

        SendPdfPreviewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    #endregion

    #region Send Preview Panning

    private void OnSendPdfPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        if (IsOnScrollBar(e.Source)) return;

        var point = e.GetCurrentPoint(scrollViewer);
        if (point.Properties.IsLeftButtonPressed
            || point.Properties.IsRightButtonPressed
            || point.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panScrollViewer = scrollViewer;
            _activePanOverscroll = _sendOverscrollHelper;
            _panStartPoint = e.GetPosition(this);
            _panStartOffset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y);
            e.Pointer.Capture(scrollViewer);
            scrollViewer.Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
        }
    }

    private void OnSendPdfPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || _panScrollViewer == null) return;

        var currentPoint = e.GetPosition(this);
        var delta = _panStartPoint - currentPoint;

        var desiredX = _panStartOffset.X + delta.X;
        var desiredY = _panStartOffset.Y + delta.Y;

        var maxX = Math.Max(0, _panScrollViewer.Extent.Width - _panScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _panScrollViewer.Extent.Height - _panScrollViewer.Viewport.Height);

        if (_activePanOverscroll != null)
        {
            var (clampedX, clampedY, overscrollX, overscrollY) =
                _activePanOverscroll.CalculateOverscroll(desiredX, desiredY, maxX, maxY);

            _panScrollViewer.Offset = new Vector(clampedX, clampedY);
            _activePanOverscroll.ApplyOverscroll(overscrollX, overscrollY);
        }
        else
        {
            _panScrollViewer.Offset = new Vector(
                Math.Clamp(desiredX, 0, maxX),
                Math.Clamp(desiredY, 0, maxY));
        }

        e.Handled = true;
    }

    private void OnSendPdfPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanning) return;

        var scrollViewer = _panScrollViewer;
        _isPanning = false;
        _panScrollViewer = null;
        e.Pointer.Capture(null);

        if (scrollViewer != null)
            scrollViewer.Cursor = new Cursor(StandardCursorType.Hand);

        if (_activePanOverscroll?.HasOverscroll == true)
        {
            _ = _activePanOverscroll.AnimateSnapBackAsync();
        }

        _activePanOverscroll = null;
        e.Handled = true;
    }

    private static bool IsOnScrollBar(object? source)
    {
        var current = source as Visual;
        while (current != null)
        {
            if (current is ScrollBar) return true;
            current = current.GetVisualParent();
        }
        return false;
    }

    #endregion
}
