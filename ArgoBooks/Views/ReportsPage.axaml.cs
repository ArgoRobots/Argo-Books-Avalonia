using System.Linq;
using ArgoBooks.Controls.Reports;
using ArgoBooks.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ArgoBooks.Views;

public partial class ReportsPage : UserControl
{
    private ReportDesignCanvas? _designCanvas;
    private ScrollViewer? _previewScrollViewer;
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    public ReportsPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _designCanvas = this.FindControl<ReportDesignCanvas>("DesignCanvas");
        _previewScrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");

        // Wire up zoom, pan, and selection for the design canvas
        if (_designCanvas != null)
        {
            _designCanvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
            _designCanvas.PointerPressed += OnCanvasPointerPressed;
            _designCanvas.PointerMoved += OnCanvasPointerMoved;
            _designCanvas.PointerReleased += OnCanvasPointerReleased;
            _designCanvas.SelectionChanged += OnCanvasSelectionChanged;
        }

        // Wire up CTRL+scroll zoom and right-click pan for the preview canvas
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.PointerWheelChanged += OnPreviewPointerWheelChanged;
            _previewScrollViewer.PointerPressed += OnPreviewPointerPressed;
            _previewScrollViewer.PointerMoved += OnPreviewPointerMoved;
            _previewScrollViewer.PointerReleased += OnPreviewPointerReleased;
        }

        // Subscribe to ViewModel property changes to sync canvas elements
        if (DataContext is ReportsPageViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            // Initial sync in case elements were already added
            _designCanvas?.SyncElements();
        }
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
            _previewScrollViewer.PointerWheelChanged -= OnPreviewPointerWheelChanged;
            _previewScrollViewer.PointerPressed -= OnPreviewPointerPressed;
            _previewScrollViewer.PointerMoved -= OnPreviewPointerMoved;
            _previewScrollViewer.PointerReleased -= OnPreviewPointerReleased;
        }

        if (DataContext is ReportsPageViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When Configuration changes, sync the canvas elements
        if (e.PropertyName == nameof(ReportsPageViewModel.Configuration))
        {
            _designCanvas?.SyncElements();
        }
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
        // Always zoom with scroll wheel (no CTRL required)
        if (DataContext is ReportsPageViewModel vm)
        {
            if (e.Delta.Y > 0)
                vm.PreviewZoomInCommand.Execute(null);
            else if (e.Delta.Y < 0)
                vm.PreviewZoomOutCommand.Execute(null);

            e.Handled = true;
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

    // Right-click pan for preview scroll viewer
    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_previewScrollViewer);
        if (point.Properties.IsRightButtonPressed && _previewScrollViewer != null)
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(_previewScrollViewer);
            _panStartOffset = new Vector(_previewScrollViewer.Offset.X, _previewScrollViewer.Offset.Y);
            e.Pointer.Capture(_previewScrollViewer);
            _previewScrollViewer.Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
        }
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _previewScrollViewer != null)
        {
            var currentPoint = e.GetPosition(_previewScrollViewer);
            var delta = _panStartPoint - currentPoint;
            _previewScrollViewer.Offset = new Vector(
                _panStartOffset.X + delta.X,
                _panStartOffset.Y + delta.Y);
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
            e.Handled = true;
        }
    }
}
