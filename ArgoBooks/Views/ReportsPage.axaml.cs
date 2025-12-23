using System.Linq;
using ArgoBooks.Controls.Reports;
using ArgoBooks.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ArgoBooks.Views;

public partial class ReportsPage : UserControl
{
    private ReportDesignCanvas? _designCanvas;
    private ScrollViewer? _previewScrollViewer;
    private LayoutTransformControl? _previewZoomTransformControl;
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    // Preview zoom level (managed here since we're not using binding anymore)
    private double _previewZoomLevel = 1.0;

    // Rubberband overscroll effect for preview
    private Vector _previewOverscroll;
    private const double OverscrollResistance = 0.3;
    private const double OverscrollMaxDistance = 100;

    public ReportsPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _designCanvas = this.FindControl<ReportDesignCanvas>("DesignCanvas");
        _previewScrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");
        _previewZoomTransformControl = this.FindControl<LayoutTransformControl>("PreviewZoomTransformControl");

        // Wire up zoom, pan, and selection for the design canvas
        if (_designCanvas != null)
        {
            _designCanvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
            _designCanvas.PointerPressed += OnCanvasPointerPressed;
            _designCanvas.PointerMoved += OnCanvasPointerMoved;
            _designCanvas.PointerReleased += OnCanvasPointerReleased;
            _designCanvas.SelectionChanged += OnCanvasSelectionChanged;
        }

        // Wire up zoom-to-cursor and right-click pan with rubberband for the preview
        if (_previewScrollViewer != null)
        {
            // Use tunnel routing to intercept before ScrollViewer handles it
            _previewScrollViewer.AddHandler(PointerWheelChangedEvent, OnPreviewPointerWheelChanged, RoutingStrategies.Tunnel);
            _previewScrollViewer.PointerPressed += OnPreviewPointerPressed;
            _previewScrollViewer.PointerMoved += OnPreviewPointerMoved;
            _previewScrollViewer.PointerReleased += OnPreviewPointerReleased;
        }

        // Apply initial zoom
        ApplyPreviewZoom();

        // Subscribe to ViewModel property changes to sync canvas elements
        if (DataContext is ReportsPageViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.ElementPropertyChanged += OnElementPropertyChanged;
            vm.PageSettingsRefreshRequested += OnPageSettingsRefreshRequested;
            vm.TemplateLoaded += OnTemplateLoaded;
            // Initial sync in case elements were already added
            _designCanvas?.SyncElements();

            // Trigger initial fit-to-window (template was already loaded in ViewModel constructor)
            TriggerInitialZoomToFit();
        }
    }

    private async void TriggerInitialZoomToFit()
    {
        if (_designCanvas == null) return;

        // Wait for layout to stabilize - the ScrollViewer inside the canvas
        // needs time for its Viewport to be calculated
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

        void OnLayoutUpdated(object? sender, EventArgs args)
        {
            if (_designCanvas.Bounds.Width > 0 && _designCanvas.Bounds.Height > 0)
            {
                _designCanvas.LayoutUpdated -= OnLayoutUpdated;
                tcs.TrySetResult(true);
            }
        }

        if (_designCanvas.Bounds.Width > 0 && _designCanvas.Bounds.Height > 0)
        {
            tcs.TrySetResult(true);
        }
        else
        {
            _designCanvas.LayoutUpdated += OnLayoutUpdated;
        }

        // Wait for layout to complete
        await tcs.Task;

        // Additional delay to ensure ScrollViewer's Viewport is calculated
        await Task.Delay(50);

        _designCanvas?.ZoomToFit();
    }

    private async void OnTemplateLoaded(object? sender, EventArgs e)
    {
        // Wait a frame for layout to complete before fitting to window
        await Task.Delay(50);
        _designCanvas?.ZoomToFit();
    }

    private void OnPageSettingsRefreshRequested(object? sender, EventArgs e)
    {
        _designCanvas?.RefreshPageSettings();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // Unsubscribe from events
        if (_designCanvas != null)
        {
            _designCanvas.PointerWheelChanged -= OnCanvasPointerWheelChanged;
            _designCanvas.PointerPressed -= OnCanvasPointerPressed;
            _designCanvas.PointerMoved -= OnCanvasPointerMoved;
            _designCanvas.PointerReleased -= OnCanvasPointerReleased;
            _designCanvas.SelectionChanged -= OnCanvasSelectionChanged;
        }

        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.RemoveHandler(PointerWheelChangedEvent, OnPreviewPointerWheelChanged);
            _previewScrollViewer.PointerPressed -= OnPreviewPointerPressed;
            _previewScrollViewer.PointerMoved -= OnPreviewPointerMoved;
            _previewScrollViewer.PointerReleased -= OnPreviewPointerReleased;
        }

        if (DataContext is ReportsPageViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.ElementPropertyChanged -= OnElementPropertyChanged;
            vm.PageSettingsRefreshRequested -= OnPageSettingsRefreshRequested;
            vm.TemplateLoaded -= OnTemplateLoaded;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When Configuration changes, sync the canvas elements
        if (e.PropertyName == nameof(ReportsPageViewModel.Configuration))
        {
            _designCanvas?.SyncElements();
            _designCanvas?.RefreshAllElements();
        }
        // When PreviewZoom changes from slider/buttons, sync to our local zoom
        else if (e.PropertyName == nameof(ReportsPageViewModel.PreviewZoom))
        {
            SyncPreviewZoomFromViewModel();
        }
    }

    private void OnElementPropertyChanged(object? sender, ArgoBooks.Core.Models.Reports.ReportElementBase element)
    {
        // Refresh the specific element's content when its properties change
        _designCanvas?.RefreshElementContent(element);
    }

    private void OnCanvasSelectionChanged(object? sender, Controls.Reports.SelectionChangedEventArgs e)
    {
        // Sync canvas selection to ViewModel
        if (DataContext is ReportsPageViewModel vm)
        {
            vm.SyncSelection(e.SelectedElements.ToList());
        }
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Always zoom with scroll wheel (no CTRL required)
        if (DataContext is ReportsPageViewModel vm)
        {
            if (e.Delta.Y > 0)
                vm.ZoomInCommand.Execute(null);
            else if (e.Delta.Y < 0)
                vm.ZoomOutCommand.Execute(null);

            e.Handled = true;
        }
    }

    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Zoom at cursor position
        if (_previewScrollViewer != null && _previewZoomTransformControl != null)
        {
            var viewportPoint = e.GetPosition(_previewScrollViewer);
            var contentPoint = e.GetPosition(_previewZoomTransformControl);
            PreviewZoomAtPoint(e.Delta.Y > 0, viewportPoint, contentPoint);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Zooms the preview at a specific point, keeping that point fixed on screen.
    /// </summary>
    private void PreviewZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (_previewScrollViewer == null || _previewZoomTransformControl == null) return;

        var oldZoom = _previewZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + 0.25, 4.0)
            : Math.Max(oldZoom - 0.25, 0.25);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Convert scaled content point to unscaled coordinates
        var unscaledX = scaledContentPoint.X / oldZoom;
        var unscaledY = scaledContentPoint.Y / oldZoom;

        // Apply the zoom
        _previewZoomLevel = newZoom;
        ApplyPreviewZoom();

        // Force layout to update so we get accurate extent/viewport values
        _previewZoomTransformControl.UpdateLayout();

        // Now calculate offset with actual post-zoom values
        var newOffsetX = unscaledX * newZoom - viewportPoint.X;
        var newOffsetY = unscaledY * newZoom - viewportPoint.Y;

        // Use actual extent and viewport after layout update
        var maxX = Math.Max(0, _previewScrollViewer.Extent.Width - _previewScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _previewScrollViewer.Extent.Height - _previewScrollViewer.Viewport.Height);

        _previewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );

        // Update the ViewModel's PreviewZoom to keep slider in sync
        if (DataContext is ReportsPageViewModel vm)
        {
            vm.PreviewZoom = newZoom;
        }
    }

    /// <summary>
    /// Applies the current zoom level to the preview.
    /// </summary>
    private void ApplyPreviewZoom()
    {
        if (_previewZoomTransformControl != null)
        {
            _previewZoomTransformControl.LayoutTransform = new ScaleTransform(_previewZoomLevel, _previewZoomLevel);
        }
    }

    // Right-click pan for design canvas
    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_designCanvas);
        if (point.Properties.IsRightButtonPressed && _designCanvas != null)
        {
            // Find the ScrollViewer inside the canvas
            var scrollViewer = _designCanvas.FindControl<ScrollViewer>("CanvasScrollViewer");
            if (scrollViewer != null)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(scrollViewer);
                _panStartOffset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y);
                e.Pointer.Capture(_designCanvas);
                _designCanvas.Cursor = new Cursor(StandardCursorType.Hand);
                e.Handled = true;
            }
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _designCanvas != null)
        {
            var scrollViewer = _designCanvas.FindControl<ScrollViewer>("CanvasScrollViewer");
            if (scrollViewer != null)
            {
                var currentPoint = e.GetPosition(scrollViewer);
                var delta = _panStartPoint - currentPoint;
                scrollViewer.Offset = new Vector(
                    _panStartOffset.X + delta.X,
                    _panStartOffset.Y + delta.Y);
                e.Handled = true;
            }
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning && _designCanvas != null)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            _designCanvas.Cursor = new Cursor(StandardCursorType.Arrow);
            e.Handled = true;
        }
    }

    // Right-click pan for preview scroll viewer with rubber band effect
    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_previewScrollViewer);
        if (point.Properties.IsRightButtonPressed && _previewScrollViewer != null)
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(_previewScrollViewer);
            _panStartOffset = new Vector(_previewScrollViewer.Offset.X, _previewScrollViewer.Offset.Y);
            e.Pointer.Capture(_previewScrollViewer);
            _previewScrollViewer.Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Handled = true;
        }
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _previewScrollViewer != null)
        {
            var currentPoint = e.GetPosition(_previewScrollViewer);
            var delta = _panStartPoint - currentPoint;

            // Calculate desired offset
            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            // Calculate bounds
            var maxX = Math.Max(0, _previewScrollViewer.Extent.Width - _previewScrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _previewScrollViewer.Extent.Height - _previewScrollViewer.Viewport.Height);

            // Calculate overscroll with resistance
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

            // Apply clamped scroll offset
            _previewScrollViewer.Offset = new Vector(clampedX, clampedY);

            // Apply overscroll visual effect
            _previewOverscroll = new Vector(overscrollX, overscrollY);
            ApplyPreviewOverscrollTransform();

            e.Handled = true;
        }
    }

    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning && _previewScrollViewer != null)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            _previewScrollViewer.Cursor = new Cursor(StandardCursorType.Arrow);

            // Animate overscroll back to zero (rubberband snap-back)
            if (_previewOverscroll.X != 0 || _previewOverscroll.Y != 0)
            {
                AnimatePreviewOverscrollSnapBack();
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Applies the current overscroll as a visual transform on the preview.
    /// </summary>
    private void ApplyPreviewOverscrollTransform()
    {
        if (_previewZoomTransformControl == null) return;

        // Apply translation to show overscroll effect
        // The overscroll is inverted because dragging right should show content from left
        var translateTransform = new TranslateTransform(-_previewOverscroll.X, -_previewOverscroll.Y);
        _previewZoomTransformControl.RenderTransform = translateTransform;
    }

    /// <summary>
    /// Animates the preview overscroll back to zero with a spring-like effect.
    /// </summary>
    private async void AnimatePreviewOverscrollSnapBack()
    {
        const int steps = 12;
        const int delayMs = 16; // ~60fps

        var startOverscroll = _previewOverscroll;

        for (int i = 1; i <= steps; i++)
        {
            // Ease-out curve for smooth deceleration
            double t = i / (double)steps;
            double easeOut = 1 - Math.Pow(1 - t, 3); // Cubic ease-out

            _previewOverscroll = new Vector(
                startOverscroll.X * (1 - easeOut),
                startOverscroll.Y * (1 - easeOut)
            );

            ApplyPreviewOverscrollTransform();

            await Task.Delay(delayMs);
        }

        // Ensure we end at exactly zero
        _previewOverscroll = new Vector(0, 0);
        ApplyPreviewOverscrollTransform();
    }

    /// <summary>
    /// Handles the Fit to Window button click for the design canvas.
    /// </summary>
    public void OnZoomFitClick(object? sender, RoutedEventArgs e)
    {
        _designCanvas?.ZoomToFit();
    }

    /// <summary>
    /// Handles the Fit to Window button click for the preview.
    /// </summary>
    public void OnPreviewZoomFitClick(object? sender, RoutedEventArgs e)
    {
        PreviewZoomToFit();
    }

    /// <summary>
    /// Zooms the preview to fit the entire content in the viewport.
    /// </summary>
    private void PreviewZoomToFit()
    {
        if (_previewScrollViewer == null || _previewZoomTransformControl == null) return;
        if (DataContext is not ReportsPageViewModel vm) return;

        // Use the display dimensions (original page size, not the 2x rendered size)
        var imageWidth = vm.PreviewDisplayWidth;
        var imageHeight = vm.PreviewDisplayHeight;

        if (imageWidth <= 0 || imageHeight <= 0) return;

        var viewportWidth = _previewScrollViewer.Bounds.Width - 40; // Account for padding
        var viewportHeight = _previewScrollViewer.Bounds.Height - 40;

        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        var scaleX = viewportWidth / imageWidth;
        var scaleY = viewportHeight / imageHeight;

        _previewZoomLevel = Math.Min(scaleX, scaleY);
        _previewZoomLevel = Math.Clamp(_previewZoomLevel, 0.25, 4.0);
        ApplyPreviewZoom();

        // Update ViewModel
        vm.PreviewZoom = _previewZoomLevel;
    }

    /// <summary>
    /// Syncs our local zoom level from the ViewModel (when slider changes).
    /// </summary>
    private void SyncPreviewZoomFromViewModel()
    {
        if (DataContext is ReportsPageViewModel vm)
        {
            if (Math.Abs(_previewZoomLevel - vm.PreviewZoom) > 0.001)
            {
                _previewZoomLevel = vm.PreviewZoom;
                ApplyPreviewZoom();
            }
        }
    }
}
