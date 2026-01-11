using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// Control for displaying a preview of the generated report.
/// Supports zooming, paging, and refreshing.
/// </summary>
public partial class ReportPreviewControl : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<ReportConfiguration?> ConfigurationProperty =
        AvaloniaProperty.Register<ReportPreviewControl, ReportConfiguration?>(nameof(Configuration));

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<ReportPreviewControl, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<ReportPreviewControl, int>(nameof(CurrentPage), 1);

    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<ReportPreviewControl, int>(nameof(TotalPages), 1);

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<ReportPreviewControl, bool>(nameof(IsLoading));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ReportPreviewControl, bool>(nameof(HasError));

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<ReportPreviewControl, string?>(nameof(ErrorMessage));

    #endregion

    #region Properties

    /// <summary>
    /// The report configuration to preview.
    /// </summary>
    public ReportConfiguration? Configuration
    {
        get => GetValue(ConfigurationProperty);
        set => SetValue(ConfigurationProperty, value);
    }

    /// <summary>
    /// Current zoom level (1.0 = 100%).
    /// </summary>
    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, Math.Clamp(value, SkiaReportDesignCanvas.MinZoom, SkiaReportDesignCanvas.MaxZoom));
    }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, Math.Clamp(value, 1, TotalPages));
    }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, Math.Max(1, value));
    }

    /// <summary>
    /// Whether a preview is being generated.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Whether an error occurred during preview generation.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    /// <summary>
    /// Error message if preview generation failed.
    /// </summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the preview needs to be refreshed.
    /// </summary>
    public event EventHandler? RefreshRequested;

    #endregion

    #region Private Fields

    private Image? _previewImage;
    private Border? _pageBorder;
    private Border? _zoomContainer;
    private Border? _loadingOverlay;
    private Border? _errorOverlay;
    private TextBlock? _errorText;
    private TextBlock? _totalPagesText;
    private NumericUpDown? _pageNumberInput;
    private ComboBox? _zoomComboBox;
    private ScrollViewer? _previewScrollViewer;
    private LayoutTransformControl? _zoomTransformControl;

    private Bitmap? _currentBitmap;

    private double _pageWidth;
    private double _pageHeight;

    private bool _pendingZoomToFit;

    // Right-click panning
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    // Rubberband overscroll effect
    private Vector _overscroll;
    private const double OverscrollResistance = 0.3; // How much resistance when overscrolling (0-1)
    private const double OverscrollMaxDistance = 100; // Maximum overscroll distance in pixels

    #endregion

    public ReportPreviewControl()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _previewImage = this.FindControl<Image>("PreviewImage");
        _pageBorder = this.FindControl<Border>("PageBorder");
        _zoomContainer = this.FindControl<Border>("ZoomContainer");
        _loadingOverlay = this.FindControl<Border>("LoadingOverlay");
        _errorOverlay = this.FindControl<Border>("ErrorOverlay");
        _errorText = this.FindControl<TextBlock>("ErrorText");
        _totalPagesText = this.FindControl<TextBlock>("TotalPagesText");
        _pageNumberInput = this.FindControl<NumericUpDown>("PageNumberInput");
        _zoomComboBox = this.FindControl<ComboBox>("ZoomComboBox");
        _previewScrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");
        _zoomTransformControl = this.FindControl<LayoutTransformControl>("ZoomTransformControl");

        // Subscribe to layout updated to handle deferred zoom
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;
            // Intercept wheel events on the ScrollViewer to zoom instead of scroll
            _previewScrollViewer.AddHandler(PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, RoutingStrategies.Tunnel);
        }
    }

    private void OnScrollViewerLayoutUpdated(object? sender, EventArgs e)
    {
        if (_pendingZoomToFit && _previewScrollViewer is { Bounds: { Width: > 0, Height: > 0 } })
        {
            _pendingZoomToFit = false;
            ZoomToFitPage();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ConfigurationProperty)
        {
            OnConfigurationChanged();
        }
        else if (change.Property == ZoomLevelProperty)
        {
            ApplyZoom();
        }
        else if (change.Property == CurrentPageProperty)
        {
            UpdatePageDisplay();
        }
        else if (change.Property == TotalPagesProperty)
        {
            UpdatePageInfo();
        }
        else if (change.Property == IsLoadingProperty)
        {
            UpdateLoadingState();
        }
        else if (change.Property == HasErrorProperty || change.Property == ErrorMessageProperty)
        {
            UpdateErrorState();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Configuration != null)
        {
            GeneratePreviewAsync();
        }
    }

    #region Configuration Handling

    private void OnConfigurationChanged()
    {
        if (Configuration == null)
        {
            ClearPreview();
            return;
        }

        // Update page dimensions
        var (width, height) = PageDimensions.GetDimensions(
            Configuration.PageSize,
            Configuration.PageOrientation);
        _pageWidth = width;
        _pageHeight = height;

        UpdatePageSize();
        GeneratePreviewAsync();
    }

    private void UpdatePageSize()
    {
        if (_pageBorder == null) return;

        _pageBorder.Width = _pageWidth;
        _pageBorder.Height = _pageHeight;
    }

    #endregion

    #region Preview Generation

    /// <summary>
    /// Generates the preview asynchronously.
    /// </summary>
    public async void GeneratePreviewAsync()
    {
        if (Configuration == null) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            var config = Configuration;
            var width = (int)_pageWidth;
            var height = (int)_pageHeight;
            var companyData = App.CompanyManager?.CompanyData;

            await Task.Run(() =>
            {
                // Generate preview bitmap using SkiaSharp with company data for chart rendering
                using var renderer = new ReportRenderer(config!, companyData, 1f, LanguageServiceTranslationProvider.Instance);
                using var skBitmap = renderer.CreatePreview(width, height);

                // Convert SKBitmap to Avalonia Bitmap
                using var stream = new MemoryStream();
                using var image = SkiaSharp.SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                data.SaveTo(stream);
                stream.Position = 0;

                var avaloniaBitmap = new Bitmap(stream);

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SetPreviewBitmap(avaloniaBitmap);
                    IsLoading = false;
                    // Fit to page after preview is generated
                    ZoomToFitPage();
                });
            });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            IsLoading = false;
        }
    }

    private void SetPreviewBitmap(Bitmap? bitmap)
    {
        // Dispose of previous bitmap
        _currentBitmap?.Dispose();
        _currentBitmap = bitmap;

        _previewImage?.Source = bitmap;
    }

    private void ClearPreview()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;

        _previewImage?.Source = null;
    }

    #endregion

    #region Zoom

    private void ApplyZoom()
    {
        if (_zoomTransformControl == null) return;

        _zoomTransformControl.LayoutTransform = new ScaleTransform(ZoomLevel, ZoomLevel);

        UpdateZoomComboBox();
    }

    private void UpdateZoomComboBox()
    {
        if (_zoomComboBox == null) return;

        // Update selection based on zoom level
        var index = ZoomLevel switch
        {
            0.5 => 0,
            0.75 => 1,
            1.0 => 2,
            1.5 => 5,
            2.0 => 6,
            _ => -1
        };

        if (index >= 0)
        {
            _zoomComboBox.SelectedIndex = index;
        }
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        ZoomTowardsCenter(true);
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        ZoomTowardsCenter(false);
    }

    /// <summary>
    /// Zooms towards the center of the viewport.
    /// </summary>
    /// <param name="zoomIn">True to zoom in, false to zoom out.</param>
    private void ZoomTowardsCenter(bool zoomIn)
    {
        if (_previewScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = ZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + SkiaReportDesignCanvas.ZoomStep, SkiaReportDesignCanvas.MaxZoom)
            : Math.Max(oldZoom - SkiaReportDesignCanvas.ZoomStep, SkiaReportDesignCanvas.MinZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Get the center point of the viewport
        var viewportCenterX = _previewScrollViewer.Viewport.Width / 2;
        var viewportCenterY = _previewScrollViewer.Viewport.Height / 2;

        // Calculate the content point at the center of the viewport
        var contentCenterX = (_previewScrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_previewScrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        // Apply the zoom
        ZoomLevel = newZoom;

        // Force layout to update so we get accurate extent/viewport values
        _zoomTransformControl.UpdateLayout();

        // Calculate new offset to keep the same content point at center
        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;

        // Use actual extent and viewport after layout update
        var maxX = Math.Max(0, _previewScrollViewer.Extent.Width - _previewScrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _previewScrollViewer.Extent.Height - _previewScrollViewer.Viewport.Height);

        _previewScrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    public void OnZoomSelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_zoomComboBox?.SelectedIndex is not int index) return;

        switch (index)
        {
            case 0: ZoomLevel = 0.5; break;
            case 1: ZoomLevel = 0.75; break;
            case 2: ZoomLevel = 1.0; break;
            case 3: ZoomToFitWidth(); break;
            case 4: ZoomToFitPage(); break;
            case 5: ZoomLevel = 1.5; break;
            case 6: ZoomLevel = 2.0; break;
        }
    }

    /// <summary>
    /// Zooms to fit the page width in the viewport.
    /// </summary>
    public void ZoomToFitWidth()
    {
        if (_previewScrollViewer == null || _pageWidth <= 0) return;

        var viewportWidth = _previewScrollViewer.Bounds.Width - 80; // Account for padding
        ZoomLevel = viewportWidth / _pageWidth;
    }

    /// <summary>
    /// Zooms to fit the entire page in the viewport.
    /// </summary>
    public void ZoomToFitPage()
    {
        if (_previewScrollViewer == null || _pageWidth <= 0 || _pageHeight <= 0) return;

        var viewportWidth = _previewScrollViewer.Bounds.Width - 80;
        var viewportHeight = _previewScrollViewer.Bounds.Height - 80;

        // If bounds aren't ready yet, defer the zoom
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            _pendingZoomToFit = true;
            return;
        }

        var scaleX = viewportWidth / _pageWidth;
        var scaleY = viewportHeight / _pageHeight;

        ZoomLevel = Math.Min(scaleX, scaleY);
    }

    /// <summary>
    /// Handles mouse wheel events to zoom at the cursor position.
    /// </summary>
    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Intercept wheel events to zoom instead of scroll
        var delta = e.Delta.Y;
        if (delta != 0 && _zoomTransformControl != null)
        {
            // Get cursor position relative to the scroll viewer's viewport
            var viewportPoint = e.GetPosition(_previewScrollViewer);
            // Get cursor position relative to the scaled content
            var contentPoint = e.GetPosition(_zoomTransformControl);
            ZoomAtPoint(delta > 0, viewportPoint, contentPoint);
        }
        e.Handled = true;
    }

    /// <summary>
    /// Zooms at a specific point, keeping that point fixed on screen.
    /// </summary>
    /// <param name="zoomIn">True to zoom in, false to zoom out.</param>
    /// <param name="viewportPoint">The cursor position relative to the scroll viewer.</param>
    /// <param name="scaledContentPoint">The cursor position relative to the scaled content.</param>
    private void ZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (_previewScrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = ZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + SkiaReportDesignCanvas.ZoomStep, SkiaReportDesignCanvas.MaxZoom)
            : Math.Max(oldZoom - SkiaReportDesignCanvas.ZoomStep, SkiaReportDesignCanvas.MinZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Convert scaled content point to unscaled coordinates
        var unscaledX = scaledContentPoint.X / oldZoom;
        var unscaledY = scaledContentPoint.Y / oldZoom;

        // Apply the zoom
        ZoomLevel = newZoom;

        // Force layout to update so we get accurate extent/viewport values
        _zoomTransformControl.UpdateLayout();

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
    }

    #endregion

    #region Paging

    private void OnPreviousPageClick(object? sender, RoutedEventArgs e)
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    private void OnNextPageClick(object? sender, RoutedEventArgs e)
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    private void OnPageNumberChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (e.NewValue.HasValue)
        {
            CurrentPage = (int)e.NewValue.Value;
        }
    }

    private void UpdatePageDisplay()
    {
        _pageNumberInput?.Value = CurrentPage;

        // In the future, this would render a different page
        // For now, we only support single-page reports
    }

    private void UpdatePageInfo()
    {
        _totalPagesText?.Text = $"of {TotalPages}";

        _pageNumberInput?.Maximum = TotalPages;
    }

    #endregion

    #region State Management

    private void UpdateLoadingState()
    {
        _loadingOverlay?.IsVisible = IsLoading;

        if (_errorOverlay != null && IsLoading)
        {
            _errorOverlay.IsVisible = false;
        }
    }

    private void UpdateErrorState()
    {
        _errorOverlay?.IsVisible = HasError && !IsLoading;

        _errorText?.Text = ErrorMessage ?? "Failed to generate preview";
    }

    #endregion

    #region Event Handlers

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        GeneratePreviewAsync();
    }

    #endregion

    #region Panning and Overscroll

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsRightButtonPressed)
        {
            // Start panning with right mouse button
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartOffset = new Vector(_previewScrollViewer?.Offset.X ?? 0, _previewScrollViewer?.Offset.Y ?? 0);
            e.Pointer.Capture(this);
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning && _previewScrollViewer != null)
        {
            var currentPoint = e.GetPosition(this);
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

            // Animate overscroll back to zero (rubberband snap-back)
            if (_overscroll.X != 0 || _overscroll.Y != 0)
            {
                AnimateOverscrollSnapBack();
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Applies the current overscroll as a visual transform.
    /// </summary>
    private void ApplyOverscrollTransform()
    {
        if (_zoomTransformControl == null) return;

        // Apply translation to show overscroll effect
        // The overscroll is inverted because dragging right should show content from left
        var translateTransform = new TranslateTransform(-_overscroll.X, -_overscroll.Y);
        _zoomTransformControl.RenderTransform = translateTransform;
    }

    /// <summary>
    /// Animates the overscroll back to zero with a spring-like effect.
    /// </summary>
    private async void AnimateOverscrollSnapBack()
    {
        const int steps = 12;
        const int delayMs = 16; // ~60fps

        var startOverscroll = _overscroll;

        for (int i = 1; i <= steps; i++)
        {
            // Ease-out curve for smooth deceleration
            double t = i / (double)steps;
            double easeOut = 1 - Math.Pow(1 - t, 3); // Cubic ease-out

            _overscroll = new Vector(
                startOverscroll.X * (1 - easeOut),
                startOverscroll.Y * (1 - easeOut)
            );

            ApplyOverscrollTransform();

            await Task.Delay(delayMs);
        }

        // Ensure we end at exactly zero
        _overscroll = new Vector(0, 0);
        ApplyOverscrollTransform();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Refreshes the preview.
    /// </summary>
    public void Refresh()
    {
        GeneratePreviewAsync();
    }

    /// <summary>
    /// Gets the current preview bitmap.
    /// </summary>
    public Bitmap? GetPreviewBitmap()
    {
        return _currentBitmap;
    }

    #endregion

    #region Cleanup

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        ClearPreview();

        // Unsubscribe from layout updated
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.LayoutUpdated -= OnScrollViewerLayoutUpdated;
        }
    }

    #endregion
}
