using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace ArgoBooks.Modals;

public partial class InvoiceTemplateDesignerModal : UserControl
{
    private ScrollViewer? _previewScrollViewer;
    private LayoutTransformControl? _zoomTransformControl;
    private Slider? _zoomSlider;
    private TextBlock? _zoomPercentText;
    private double _zoomLevel = 1.0;

    private const double MinZoom = 0.25;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.1;

    public InvoiceTemplateDesignerModal()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _previewScrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");
        _zoomTransformControl = this.FindControl<LayoutTransformControl>("ZoomTransformControl");
        _zoomSlider = this.FindControl<Slider>("ZoomSlider");
        _zoomPercentText = this.FindControl<TextBlock>("ZoomPercentText");

        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
        }

        ApplyZoom();
    }

    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var delta = e.Delta.Y;
            if (delta != 0)
            {
                if (delta > 0)
                    _zoomLevel = Math.Min(_zoomLevel + ZoomStep, MaxZoom);
                else
                    _zoomLevel = Math.Max(_zoomLevel - ZoomStep, MinZoom);

                ApplyZoom();
                e.Handled = true;
            }
        }
    }

    private void ZoomIn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoomLevel = Math.Min(_zoomLevel + ZoomStep, MaxZoom);
        ApplyZoom();
    }

    private void ZoomOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoomLevel = Math.Max(_zoomLevel - ZoomStep, MinZoom);
        ApplyZoom();
    }

    private void ResetZoom_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        ApplyZoom();
    }

    private void ZoomSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_zoomSlider != null && Math.Abs(_zoomLevel - _zoomSlider.Value) > 0.001)
        {
            _zoomLevel = _zoomSlider.Value;
            ApplyZoom();
        }
    }

    private void ApplyZoom()
    {
        if (_zoomTransformControl != null)
        {
            _zoomTransformControl.LayoutTransform = new ScaleTransform(_zoomLevel, _zoomLevel);
        }

        if (_zoomSlider != null && Math.Abs(_zoomSlider.Value - _zoomLevel) > 0.001)
        {
            _zoomSlider.Value = _zoomLevel;
        }

        if (_zoomPercentText != null)
        {
            _zoomPercentText.Text = $"{(int)(_zoomLevel * 100)}%";
        }
    }
}
