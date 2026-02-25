using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using ArgoBooks.Helpers;
using ArgoBooks.Services;
using SkiaSharp;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// SkiaSharp-based design canvas for the report layout editor.
/// Uses a single SkiaSharp renderer for both design and preview consistency.
/// </summary>
public partial class SkiaReportDesignCanvas : UserControl
{
    #region Constants

    public const double MinZoom = 0.25;
    public const double MaxZoom = 2.0;
    public const double ZoomStep = 0.25;

    private const double HandleSize = 10;
    private const double HandleHitArea = 14;
    private const double SelectionBorderWidth = 2;
    private const int PageGap = 24; // Gap between pages in pixels

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the page dimensions in pixels based on the current configuration.
    /// </summary>
    private (int Width, int Height) GetPageDimensions()
    {
        if (Configuration == null)
            return (800, 1000);

        return PageDimensions.GetDimensions(Configuration.PageSize, Configuration.PageOrientation);
    }

    /// <summary>
    /// Gets the total number of pages to render.
    /// </summary>
    private int GetPageCount() => Math.Max(1, Configuration?.PageCount ?? 1);

    /// <summary>
    /// Gets the total canvas height for all pages stacked vertically.
    /// </summary>
    private int GetTotalCanvasHeight()
    {
        var (_, pageHeight) = GetPageDimensions();
        var pageCount = GetPageCount();
        return pageCount * pageHeight + (pageCount - 1) * PageGap;
    }

    /// <summary>
    /// Gets the Y offset for a given page number (1-based) in the stacked layout.
    /// </summary>
    private int GetPageYOffset(int pageNumber)
    {
        var (_, pageHeight) = GetPageDimensions();
        return (pageNumber - 1) * (pageHeight + PageGap);
    }

    /// <summary>
    /// Determines which page a canvas Y coordinate falls on, and the local Y within that page.
    /// Returns (pageNumber, localY). pageNumber is 0 if the point is in a gap.
    /// </summary>
    private (int PageNumber, double LocalY) GetPageAtCanvasY(double canvasY)
    {
        var (_, pageHeight) = GetPageDimensions();
        var pageCount = GetPageCount();
        var stride = pageHeight + PageGap;

        for (int page = 1; page <= pageCount; page++)
        {
            var pageTop = GetPageYOffset(page);
            var pageBottom = pageTop + pageHeight;
            if (canvasY >= pageTop && canvasY < pageBottom)
                return (page, canvasY - pageTop);
        }

        return (0, 0); // In a gap between pages
    }

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<ReportConfiguration?> ConfigurationProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, ReportConfiguration?>(nameof(Configuration));

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, bool>(nameof(ShowGrid), true);

    public static readonly StyledProperty<double> GridSizeProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, double>(nameof(GridSize), 20);

    public static readonly StyledProperty<bool> SnapToGridProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, bool>(nameof(SnapToGrid), true);

    public static readonly StyledProperty<bool> ShowHeaderFooterProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, bool>(nameof(ShowHeaderFooter), true);

    public static readonly StyledProperty<int> CurrentDesignerPageProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, int>(nameof(CurrentDesignerPage), 1);

    public static readonly StyledProperty<ReportUndoRedoManager?> UndoRedoManagerProperty =
        AvaloniaProperty.Register<SkiaReportDesignCanvas, ReportUndoRedoManager?>(nameof(UndoRedoManager));

    #endregion

    #region Properties

    public ReportConfiguration? Configuration
    {
        get => GetValue(ConfigurationProperty);
        set => SetValue(ConfigurationProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, Math.Clamp(value, MinZoom, MaxZoom));
    }

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    public bool SnapToGrid
    {
        get => GetValue(SnapToGridProperty);
        set => SetValue(SnapToGridProperty, value);
    }

    public bool ShowHeaderFooter
    {
        get => GetValue(ShowHeaderFooterProperty);
        set => SetValue(ShowHeaderFooterProperty, value);
    }

    /// <summary>
    /// The currently displayed page in the designer (1-based).
    /// </summary>
    public int CurrentDesignerPage
    {
        get => GetValue(CurrentDesignerPageProperty);
        set => SetValue(CurrentDesignerPageProperty, Math.Max(1, value));
    }

    public ReportUndoRedoManager? UndoRedoManager
    {
        get => GetValue(UndoRedoManagerProperty);
        set => SetValue(UndoRedoManagerProperty, value);
    }

    #endregion

    #region Private Fields

    private Image? _canvasImage;
    private Rectangle? _selectionRectangle;
    private ScaleTransform? _zoomTransform;

    // Selection state
    private readonly List<ReportElementBase> _selectedElements = [];
    private ReportElementBase? _hoveredElement;

    // Interaction state
    private enum InteractionMode { None, Selecting, Dragging, Resizing, Panning }
    private InteractionMode _interactionMode = InteractionMode.None;
    private Point _interactionStartPoint;
    private Point _elementStartPosition;
    private Size _elementStartSize;
    private ResizeHandle _activeResizeHandle = ResizeHandle.None;
    private readonly Dictionary<string, (Point Position, Size Size)> _multiDragStartBounds = new();

    // Panning state
    private Point _panStartPoint;
    private Vector _panStartOffset;
    private ReportElementBase? _rightClickedElement;
    private const double PanThreshold = 5; // Minimum pixels to move before panning starts

    // Overscroll (rubber-band) state
    private LayoutTransformControl? _zoomTransformControl;
    private OverscrollHelper? _overscrollHelper;

    // Zoom state tracking
    private double _previousZoomLevel = 1.0;
    private bool _zoomingFromWheel;

    // Render state
    private SKBitmap? _renderBitmap;
    private bool _needsRender = true;

    #endregion

    #region Resize Handle Enum

    private enum ResizeHandle
    {
        None,
        TopLeft, Top, TopRight,
        Left, Right,
        BottomLeft, Bottom, BottomRight
    }

    #endregion

    #region Events

    public event EventHandler<ReportElementBase>? ElementSelected;
    public event EventHandler? SelectionCleared;
    public event EventHandler<ElementAddedEventArgs>? ElementAdded;
    public event EventHandler<ReportElementBase>? ElementRemoved;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested;

    /// <summary>
    /// Notifies listeners about selection changes.
    /// </summary>
    private void NotifySelectionChanged()
    {
        var args = new SelectionChangedEventArgs(
            _selectedElements.FirstOrDefault(),
            _selectedElements.ToList().AsReadOnly()
        );
        SelectionChanged?.Invoke(this, args);

        if (_selectedElements.Count > 0)
            ElementSelected?.Invoke(this, _selectedElements[0]);
        else
            SelectionCleared?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Constructor

    public SkiaReportDesignCanvas()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private ScrollViewer? _scrollViewer;

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _canvasImage = this.FindControl<Image>("CanvasImage");
        _selectionRectangle = this.FindControl<Rectangle>("SelectionRectangle");
        _scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");

        // Get the ScaleTransform from the LayoutTransformControl
        _zoomTransformControl = this.FindControl<LayoutTransformControl>("ZoomTransformControl");
        _zoomTransform = _zoomTransformControl?.LayoutTransform as ScaleTransform;

        if (_zoomTransformControl != null)
        {
            _overscrollHelper = new OverscrollHelper(_zoomTransformControl);
        }

        // Wire up pointer events on the canvas image
        if (_canvasImage != null)
        {
            _canvasImage.PointerPressed += OnCanvasPointerPressed;
            _canvasImage.PointerMoved += OnCanvasPointerMoved;
            _canvasImage.PointerReleased += OnCanvasPointerReleased;
        }

        // Wire up pointer wheel for zoom-to-cursor (only fires when not already handled by parent)
        _scrollViewer?.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, Avalonia.Interactivity.RoutingStrategies.Bubble);

        // Wire up keyboard events
        KeyDown += OnKeyDown;

        InvalidateCanvas();
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _renderBitmap?.Dispose();
        _renderBitmap = null;
    }

    #endregion

    #region Property Changed

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ConfigurationProperty)
        {
            _selectedElements.Clear();
            InvalidateCanvas();
        }
        else if (change.Property == ZoomLevelProperty)
        {
            UpdateZoomTransform();
        }
        else if (change.Property == ShowGridProperty ||
                 change.Property == GridSizeProperty ||
                 change.Property == ShowHeaderFooterProperty)
        {
            InvalidateCanvas();
        }
        else if (change.Property == CurrentDesignerPageProperty)
        {
            // Scroll to the target page
            ScrollToPage(CurrentDesignerPage);
        }
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Marks the canvas as needing a re-render.
    /// </summary>
    public void InvalidateCanvas()
    {
        _needsRender = true;
        Dispatcher.UIThread.Post(RenderCanvas, DispatcherPriority.Render);
    }

    // Render at 2x resolution for better quality when zoomed
    private const float RenderScale = 2.0f;

    private void RenderCanvas()
    {
        if (!_needsRender || _canvasImage == null || Configuration == null) return;
        _needsRender = false;

        var (baseWidth, baseHeight) = GetPageDimensions();

        if (baseWidth <= 0 || baseHeight <= 0) return;

        var pageCount = GetPageCount();
        var totalHeight = GetTotalCanvasHeight();

        // Render at higher resolution for better quality
        var renderWidth = (int)(baseWidth * RenderScale);
        var renderHeight = (int)(totalHeight * RenderScale);

        // Create or resize bitmap
        if (_renderBitmap == null || _renderBitmap.Width != renderWidth || _renderBitmap.Height != renderHeight)
        {
            _renderBitmap?.Dispose();
            _renderBitmap = new SKBitmap(renderWidth, renderHeight);
        }

        using var canvas = new SKCanvas(_renderBitmap);

        // Scale canvas for high-resolution rendering
        canvas.Scale(RenderScale, RenderScale);

        // Clear with gap background color
        canvas.Clear(new SKColor(200, 200, 200)); // Neutral gray for gaps

        var companyData = App.CompanyManager?.CompanyData;
        Configuration.Use24HourFormat = TimeZoneService.Is24HourFormat;
        using var renderer = new ReportRenderer(Configuration, companyData, 1f, LanguageServiceTranslationProvider.Instance);

        // Render each page stacked vertically
        for (int page = 1; page <= pageCount; page++)
        {
            var pageYOffset = GetPageYOffset(page);

            canvas.Save();
            canvas.Translate(0, pageYOffset);

            // Draw page background
            var bgColor = SKColor.Parse(Configuration.BackgroundColor);
            canvas.DrawRect(0, 0, baseWidth, baseHeight, new SKPaint { Color = bgColor, Style = SKPaintStyle.Fill });

            // Draw grid if enabled
            if (ShowGrid)
            {
                DrawGrid(canvas, baseWidth, baseHeight);
            }

            // Draw header/footer
            if (Configuration.ShowHeader)
            {
                DrawHeader(canvas, baseWidth);
            }
            if (Configuration.ShowFooter)
            {
                DrawFooter(canvas, baseWidth, baseHeight, page, pageCount);
            }

            // Render elements for this page
            renderer.RenderElementsToCanvas(canvas, page);

            canvas.Restore();
        }

        // Draw hover highlight and selection visuals (need page offset translation)
        if (_hoveredElement != null && !_selectedElements.Contains(_hoveredElement))
        {
            var pageOffset = GetPageYOffset(_hoveredElement.PageNumber);
            canvas.Save();
            canvas.Translate(0, pageOffset);
            DrawHoverHighlightClipped(canvas, _hoveredElement);
            canvas.Restore();
        }

        DrawSelectionVisuals(canvas);

        // Convert SKBitmap to Avalonia Bitmap
        UpdateCanvasImage(baseWidth, totalHeight);
    }

    private void DrawGrid(SKCanvas canvas, int width, int height)
    {
        var gridSize = (float)GridSize;
        using var gridPaint = new SKPaint();
        gridPaint.Color = new SKColor(200, 200, 200, 100);
        gridPaint.Style = SKPaintStyle.Stroke;
        gridPaint.StrokeWidth = 1;
        gridPaint.IsAntialias = false;

        // Vertical lines
        for (float x = gridSize; x < width; x += gridSize)
        {
            canvas.DrawLine(x, 0, x, height, gridPaint);
        }

        // Horizontal lines
        for (float y = gridSize; y < height; y += gridSize)
        {
            canvas.DrawLine(0, y, width, y, gridPaint);
        }
    }

    private void DrawHeader(SKCanvas canvas, int width)
    {
        var headerHeight = (float)PageDimensions.HeaderHeight;

        using var separatorPaint = new SKPaint();
        separatorPaint.Color = SKColors.LightGray;
        separatorPaint.Style = SKPaintStyle.Stroke;
        separatorPaint.StrokeWidth = 1;

        canvas.DrawLine(40, headerHeight, width - 40, headerHeight, separatorPaint);

        var titleFontSize = (float)(Configuration?.TitleFontSize ?? 18);
        using var titleFont = new SKFont(SKTypeface.Default, titleFontSize);
        using var titlePaint = new SKPaint();
        titlePaint.Color = SKColors.Black;
        titlePaint.IsAntialias = true;
        var title = Configuration?.Title ?? "Report Title";
        canvas.DrawText(title, width / 2f, 35, SKTextAlign.Center, titleFont, titlePaint);
    }

    private void DrawFooter(SKCanvas canvas, int width, int height, int pageNumber = 0, int totalPages = 0)
    {
        if (pageNumber == 0) pageNumber = CurrentDesignerPage;
        if (totalPages == 0) totalPages = Configuration?.PageCount ?? 1;

        var footerHeight = (float)PageDimensions.FooterHeight;

        using var separatorPaint = new SKPaint();
        separatorPaint.Color = SKColors.LightGray;
        separatorPaint.Style = SKPaintStyle.Stroke;
        separatorPaint.StrokeWidth = 1;

        canvas.DrawLine(40, height - footerHeight, width - 40, height - footerHeight, separatorPaint);

        using var footerFont = new SKFont(SKTypeface.Default, 11);
        using var footerPaint = new SKPaint();
        footerPaint.Color = SKColors.Gray;
        footerPaint.IsAntialias = true;
        canvas.DrawText("Generated: [Date/Time]", 40, height - 15, SKTextAlign.Left, footerFont, footerPaint);
        var pageText = totalPages > 1
            ? $"Page {pageNumber} of {totalPages}"
            : $"Page {pageNumber}";
        canvas.DrawText(pageText, width - 40, height - 15, SKTextAlign.Right, footerFont, footerPaint);
    }

    private void DrawSelectionVisuals(SKCanvas canvas)
    {
        foreach (var element in _selectedElements)
        {
            var pageOffset = GetPageYOffset(element.PageNumber);

            // Clip out higher Z-order elements so selection visuals
            // don't render on top of elements that are above this one
            canvas.Save();
            canvas.Translate(0, pageOffset);
            ClipHigherZOrderElements(canvas, element);

            DrawSelectionBorder(canvas, element);
            DrawResizeHandles(canvas, element);
            DrawElementTypeIndicator(canvas, element);

            canvas.Restore();
        }
    }

    private void ClipHigherZOrderElements(SKCanvas canvas, ReportElementBase element)
    {
        if (Configuration == null) return;

        foreach (var other in Configuration.Elements)
        {
            if (other.PageNumber != element.PageNumber || other.ZOrder <= element.ZOrder || !other.IsVisible) continue;

            var rect = new SKRect(
                (float)other.X,
                (float)other.Y,
                (float)(other.X + other.Width),
                (float)(other.Y + other.Height)
            );

            canvas.ClipRect(rect, SKClipOperation.Difference);
        }
    }

    private void DrawSelectionBorder(SKCanvas canvas, ReportElementBase element)
    {
        var rect = new SKRect(
            (float)element.X - 1,
            (float)element.Y - 1,
            (float)(element.X + element.Width) + 1,
            (float)(element.Y + element.Height) + 1
        );

        using var borderPaint = new SKPaint();
        borderPaint.Color = new SKColor(59, 130, 246); // Primary blue
        borderPaint.Style = SKPaintStyle.Stroke;
        borderPaint.StrokeWidth = (float)SelectionBorderWidth;
        borderPaint.IsAntialias = true;

        canvas.DrawRect(rect, borderPaint);
    }

    private void DrawResizeHandles(SKCanvas canvas, ReportElementBase element)
    {
        var handles = GetResizeHandlePositions(element);

        using var fillPaint = new SKPaint();
        fillPaint.Color = SKColors.White;
        fillPaint.Style = SKPaintStyle.Fill;
        fillPaint.IsAntialias = true;

        using var borderPaint = new SKPaint();
        borderPaint.Color = new SKColor(59, 130, 246);
        borderPaint.Style = SKPaintStyle.Stroke;
        borderPaint.StrokeWidth = 1;
        borderPaint.IsAntialias = true;

        foreach (var handle in handles.Values)
        {
            var handleRect = new SKRect(
                (float)(handle.X - HandleSize / 2),
                (float)(handle.Y - HandleSize / 2),
                (float)(handle.X + HandleSize / 2),
                (float)(handle.Y + HandleSize / 2)
            );

            canvas.DrawRect(handleRect, fillPaint);
            canvas.DrawRect(handleRect, borderPaint);
        }
    }

    private void DrawElementTypeIndicator(SKCanvas canvas, ReportElementBase element)
    {
        var typeName = element.DisplayName;
        using var font = new SKFont(SKTypeface.Default, 10);
        var textWidth = font.MeasureText(typeName);

        var badgeRect = new SKRect(
            (float)element.X - 1,
            (float)element.Y - 18,
            (float)element.X + textWidth + 10,
            (float)element.Y - 1
        );

        using var bgPaint = new SKPaint();
        bgPaint.Color = new SKColor(59, 130, 246);
        bgPaint.Style = SKPaintStyle.Fill;

        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.White;
        textPaint.IsAntialias = true;

        canvas.DrawRect(badgeRect, bgPaint);
        canvas.DrawText(typeName, (float)element.X + 5, (float)element.Y - 5, SKTextAlign.Left, font, textPaint);
    }

    private void DrawHoverHighlightClipped(SKCanvas canvas, ReportElementBase element)
    {
        if (Configuration == null) return;

        // Draw the hover highlight OUTSIDE the element bounds
        // Offset by 1 pixel to avoid overlapping with the element's own border
        const float offset = 1f;
        var rect = new SKRect(
            (float)element.X - offset,
            (float)element.Y - offset,
            (float)(element.X + element.Width) + offset,
            (float)(element.Y + element.Height) + offset
        );

        canvas.Save();
        ClipHigherZOrderElements(canvas, element);

        using var borderPaint = new SKPaint();
        borderPaint.Color = new SKColor(59, 130, 246, 180); // Semi-transparent blue
        borderPaint.Style = SKPaintStyle.Stroke;
        borderPaint.StrokeWidth = 2;
        borderPaint.IsAntialias = true;

        canvas.DrawRect(rect, borderPaint);

        canvas.Restore();
    }

    private Dictionary<ResizeHandle, Point> GetResizeHandlePositions(ReportElementBase element)
    {
        var x = element.X;
        var y = element.Y;
        var w = element.Width;
        var h = element.Height;

        return new Dictionary<ResizeHandle, Point>
        {
            [ResizeHandle.TopLeft] = new(x, y),
            [ResizeHandle.Top] = new(x + w / 2, y),
            [ResizeHandle.TopRight] = new(x + w, y),
            [ResizeHandle.Left] = new(x, y + h / 2),
            [ResizeHandle.Right] = new(x + w, y + h / 2),
            [ResizeHandle.BottomLeft] = new(x, y + h),
            [ResizeHandle.Bottom] = new(x + w / 2, y + h),
            [ResizeHandle.BottomRight] = new(x + w, y + h)
        };
    }

    private void UpdateCanvasImage(int displayWidth, int displayHeight)
    {
        if (_renderBitmap == null || _canvasImage == null) return;

        // Convert SKBitmap to Avalonia WriteableBitmap directly (faster than PNG encoding)
        var info = _renderBitmap.Info;
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(info.Width, info.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var frameBuffer = writeableBitmap.Lock())
        {
            var srcPtr = _renderBitmap.GetPixels();
            var dstPtr = frameBuffer.Address;
            var srcRowBytes = info.RowBytes;
            var dstRowBytes = frameBuffer.RowBytes;

            // Use a row buffer to copy without unsafe code
            var rowBuffer = new byte[srcRowBytes];
            for (int y = 0; y < info.Height; y++)
            {
                // Copy from source to buffer, then buffer to destination
                System.Runtime.InteropServices.Marshal.Copy(srcPtr + y * srcRowBytes, rowBuffer, 0, srcRowBytes);
                System.Runtime.InteropServices.Marshal.Copy(rowBuffer, 0, dstPtr + y * dstRowBytes, srcRowBytes);
            }
        }

        _canvasImage.Source = writeableBitmap;

        // Set display size to base dimensions (zoom transform will scale it)
        _canvasImage.Width = displayWidth;
        _canvasImage.Height = displayHeight;
    }

    #endregion

    #region Zoom

    public void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + ZoomStep, MaxZoom);
    public void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - ZoomStep, MinZoom);
    public void ResetZoom() => ZoomLevel = 1.0;

    private void UpdateZoomTransform()
    {
        if (_zoomTransform == null) return;

        var oldZoom = _previousZoomLevel;
        var newZoom = ZoomLevel;
        _previousZoomLevel = newZoom;

        _zoomTransform.ScaleX = newZoom;
        _zoomTransform.ScaleY = newZoom;

        // If zooming from wheel, the wheel handler manages the scroll offset
        if (_zoomingFromWheel || _scrollViewer == null)
        {
            _zoomingFromWheel = false;
            return;
        }

        // Maintain center focus for slider/button zoom changes
        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        var viewportCenterX = _scrollViewer.Viewport.Width / 2;
        var viewportCenterY = _scrollViewer.Viewport.Height / 2;

        var contentCenterX = (_scrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_scrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer == null) return;

            var newOffsetX = contentCenterX * newZoom - viewportCenterX;
            var newOffsetY = contentCenterY * newZoom - viewportCenterY;

            var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);

            _scrollViewer.Offset = new Vector(
                Math.Clamp(newOffsetX, 0, maxX),
                Math.Clamp(newOffsetY, 0, maxY)
            );
        }, DispatcherPriority.Render);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Zoom with scroll wheel (no modifier key required)
        if (_scrollViewer == null || _canvasImage == null)
            return;

        e.Handled = true;

        // Get mouse position relative to the scroll viewer (viewport coordinates)
        var mousePosInViewport = e.GetPosition(_scrollViewer);

        // Get current scroll offset, extent, and zoom
        var oldOffset = _scrollViewer.Offset;
        var oldExtent = _scrollViewer.Extent;
        var viewportWidth = _scrollViewer.Viewport.Width;
        var viewportHeight = _scrollViewer.Viewport.Height;
        var oldZoom = ZoomLevel;

        // Apply zoom change
        var delta = e.Delta.Y > 0 ? ZoomStep : -ZoomStep;
        var newZoom = Math.Clamp(ZoomLevel + delta, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - oldZoom) < 0.001)
            return;

        // When content is smaller than viewport, it's centered (due to HorizontalAlignment/VerticalAlignment="Center")
        // We need to account for this centering offset when converting viewport coords to content coords
        var centeringOffsetX = oldExtent.Width < viewportWidth
            ? (viewportWidth - oldExtent.Width) / 2
            : 0;
        var centeringOffsetY = oldExtent.Height < viewportHeight
            ? (viewportHeight - oldExtent.Height) / 2
            : 0;

        // Calculate the mouse position in content space
        // When scrolled: add scroll offset
        // When centered: subtract centering offset (scroll offset is 0)
        var mousePosInContent = new Point(
            mousePosInViewport.X + oldOffset.X - centeringOffsetX,
            mousePosInViewport.Y + oldOffset.Y - centeringOffsetY
        );

        // Convert content position to canvas coordinates (accounting for margin and zoom)
        const double margin = 10;
        var canvasX = (mousePosInContent.X - margin) / oldZoom;
        var canvasY = (mousePosInContent.Y - margin) / oldZoom;

        // Mark that we're zooming from wheel (so UpdateZoomTransform doesn't also adjust offset)
        _zoomingFromWheel = true;

        // Apply the new zoom level
        ZoomLevel = newZoom;

        // Get canvas dimensions (page width, total stacked height)
        var (pageWidth, _) = GetPageDimensions();
        var totalHeight = GetTotalCanvasHeight();

        // Schedule the scroll adjustment after layout is updated
        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer == null) return;

            var newViewportWidth = _scrollViewer.Viewport.Width;
            var newViewportHeight = _scrollViewer.Viewport.Height;
            var newExtentWidth = pageWidth * newZoom + margin * 2;
            var newExtentHeight = totalHeight * newZoom + margin * 2;

            // Calculate where the canvas point is in the new content space
            var newContentX = canvasX * newZoom + margin;
            var newContentY = canvasY * newZoom + margin;

            // Calculate new centering offset (after zoom)
            var newCenteringOffsetX = newExtentWidth < newViewportWidth
                ? (newViewportWidth - newExtentWidth) / 2
                : 0;
            var newCenteringOffsetY = newExtentHeight < newViewportHeight
                ? (newViewportHeight - newExtentHeight) / 2
                : 0;

            // If content will be centered after zoom, no scroll adjustment needed
            if (newExtentWidth <= newViewportWidth && newExtentHeight <= newViewportHeight)
            {
                return;
            }

            // New scroll offset = new content position - (viewport position - new centering offset)
            // But when there's a new centering offset, scroll offset should be 0 for that axis
            var newScrollX = newCenteringOffsetX > 0 ? 0 : newContentX - mousePosInViewport.X;
            var newScrollY = newCenteringOffsetY > 0 ? 0 : newContentY - mousePosInViewport.Y;

            // Clamp to valid scroll range
            var maxScrollX = Math.Max(0, newExtentWidth - newViewportWidth);
            var maxScrollY = Math.Max(0, newExtentHeight - newViewportHeight);

            newScrollX = Math.Clamp(newScrollX, 0, maxScrollX);
            newScrollY = Math.Clamp(newScrollY, 0, maxScrollY);

            _scrollViewer.Offset = new Vector(newScrollX, newScrollY);
        }, DispatcherPriority.Render);
    }

    #endregion

    #region Hit Testing

    private ReportElementBase? GetElementAtPoint(Point canvasPoint)
    {
        if (Configuration == null) return null;

        // Determine which page this point falls on
        var (pageNumber, localY) = GetPageAtCanvasY(canvasPoint.Y);
        if (pageNumber == 0) return null; // In a gap

        var localPoint = new Point(canvasPoint.X, localY);

        // Check in reverse Z-order (topmost first), filtered to the page under the cursor
        foreach (var element in Configuration.Elements
            .Where(e => e.PageNumber == pageNumber)
            .OrderByDescending(e => e.ZOrder))
        {
            var rect = new Rect(element.X, element.Y, element.Width, element.Height);
            if (rect.Contains(localPoint))
            {
                return element;
            }
        }

        return null;
    }

    private ResizeHandle GetResizeHandleAtPoint(Point canvasPoint, ReportElementBase element)
    {
        // Convert canvas point to page-local coordinates for this element's page
        var pageOffset = GetPageYOffset(element.PageNumber);
        var localPoint = new Point(canvasPoint.X, canvasPoint.Y - pageOffset);

        var handles = GetResizeHandlePositions(element);

        foreach (var kvp in handles)
        {
            var handleCenter = kvp.Value;
            var handleRect = new Rect(
                handleCenter.X - HandleHitArea / 2,
                handleCenter.Y - HandleHitArea / 2,
                HandleHitArea,
                HandleHitArea
            );

            if (handleRect.Contains(localPoint))
            {
                return kvp.Key;
            }
        }

        return ResizeHandle.None;
    }

    private Point GetCanvasPoint(PointerEventArgs e)
    {
        if (_canvasImage == null) return new Point();
        var point = e.GetPosition(_canvasImage);
        // No need to adjust for zoom since we're getting position relative to the image
        return point;
    }

    #endregion

    #region Pointer Events

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Configuration == null) return;

        var point = GetCanvasPoint(e);
        var props = e.GetCurrentPoint(_canvasImage).Properties;

        // Handle right-click - start potential panning (context menu shown on release if no drag)
        if (props.IsRightButtonPressed)
        {
            var element = GetElementAtPoint(point);
            _rightClickedElement = element;

            // Select the element if clicking on one and not already selected
            if (element != null && !_selectedElements.Contains(element))
            {
                _selectedElements.Clear();
                _selectedElements.Add(element);
                NotifySelectionChanged();
                InvalidateCanvas();
            }

            // Start tracking for potential pan - store starting position
            _interactionMode = InteractionMode.None; // Will switch to Panning if user drags
            _interactionStartPoint = point;
            if (_scrollViewer != null)
            {
                _panStartPoint = e.GetPosition(_scrollViewer);
                _panStartOffset = new Vector(_scrollViewer.Offset.X, _scrollViewer.Offset.Y);
            }
            e.Pointer.Capture(_canvasImage);
            e.Handled = true;
            return;
        }

        if (props.IsLeftButtonPressed)
        {
            // Check if clicking on a resize handle of a selected element
            foreach (var selected in _selectedElements)
            {
                var handle = GetResizeHandleAtPoint(point, selected);
                if (handle != ResizeHandle.None)
                {
                    StartResize(selected, handle, point);
                    e.Handled = true;
                    return;
                }
            }

            // Check if clicking on an element
            var element = GetElementAtPoint(point);

            if (element != null)
            {
                var isMultiSelect = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                                    e.KeyModifiers.HasFlag(KeyModifiers.Shift);

                if (isMultiSelect)
                {
                    // Toggle selection
                    if (_selectedElements.Contains(element))
                        _selectedElements.Remove(element);
                    else
                        _selectedElements.Add(element);
                }
                else if (!_selectedElements.Contains(element))
                {
                    // Single select
                    _selectedElements.Clear();
                    _selectedElements.Add(element);
                }

                // Start drag
                StartDrag(point);
                NotifySelectionChanged();
            }
            else
            {
                // Clicked on empty space - start selection rectangle
                _selectedElements.Clear();
                NotifySelectionChanged();
                StartSelectionRectangle(point);
            }

            InvalidateCanvas();
            e.Handled = true;
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Configuration == null) return;

        var point = GetCanvasPoint(e);
        var props = e.GetCurrentPoint(_canvasImage).Properties;

        // Handle right-button drag for panning
        if (props.IsRightButtonPressed && _scrollViewer != null)
        {
            var currentPoint = e.GetPosition(_scrollViewer);
            var delta = _panStartPoint - currentPoint;
            var distance = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

            // Start panning if we've moved past the threshold
            if (_interactionMode != InteractionMode.Panning && distance > PanThreshold)
            {
                _interactionMode = InteractionMode.Panning;
                Cursor = new Cursor(StandardCursorType.SizeAll);
            }

            // Update scroll position while panning with overscroll effect
            if (_interactionMode == InteractionMode.Panning && _overscrollHelper != null)
            {
                // Calculate desired offset
                var desiredX = _panStartOffset.X + delta.X;
                var desiredY = _panStartOffset.Y + delta.Y;

                // Calculate bounds
                var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
                var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);

                var (clampedX, clampedY, overscrollX, overscrollY) =
                    _overscrollHelper.CalculateOverscroll(desiredX, desiredY, maxX, maxY);

                // Apply clamped scroll offset
                _scrollViewer.Offset = new Vector(clampedX, clampedY);

                // Apply overscroll visual effect
                _overscrollHelper.ApplyOverscroll(overscrollX, overscrollY);

                e.Handled = true;
                return;
            }
        }

        switch (_interactionMode)
        {
            case InteractionMode.Dragging:
                HandleDrag(point);
                break;

            case InteractionMode.Resizing:
                HandleResize(point);
                break;

            case InteractionMode.Selecting:
                UpdateSelectionRectangle(point);
                break;

            case InteractionMode.Panning:
                // Handled above
                break;

            case InteractionMode.None:
                // Update hover state
                var element = GetElementAtPoint(point);
                if (element != _hoveredElement)
                {
                    _hoveredElement = element;
                    UpdateCursor(point);
                    InvalidateCanvas();
                }
                else
                {
                    UpdateCursor(point);
                }
                break;
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Handle right-click release - show context menu if we didn't pan
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            e.Pointer.Capture(null);

            if (_interactionMode == InteractionMode.Panning)
            {
                // We were panning, reset cursor and animate overscroll snap-back
                Cursor = Cursor.Default;

                // Animate overscroll back to zero (rubberband snap-back)
                if (_overscrollHelper?.HasOverscroll == true)
                {
                    _ = _overscrollHelper.AnimateSnapBackAsync();
                }
            }
            else if (_rightClickedElement != null)
            {
                // No panning occurred, show context menu
                var screenPoint = e.GetPosition(this);
                ContextMenuRequested?.Invoke(this, new ContextMenuRequestedEventArgs(screenPoint.X, screenPoint.Y, _rightClickedElement));
            }

            _interactionMode = InteractionMode.None;
            _rightClickedElement = null;
            e.Handled = true;
            return;
        }

        switch (_interactionMode)
        {
            case InteractionMode.Dragging:
                EndDrag();
                break;

            case InteractionMode.Resizing:
                EndResize();
                break;

            case InteractionMode.Selecting:
                EndSelectionRectangle();
                break;

            case InteractionMode.Panning:
                // Already handled above for right-click
                break;
        }

        _interactionMode = InteractionMode.None;
        InvalidateCanvas();
    }

    #endregion

    #region Dragging

    private void StartDrag(Point point)
    {
        _interactionMode = InteractionMode.Dragging;
        _interactionStartPoint = point;

        // Suppress property change undo recording during drag - EndDrag records the final action
        if (UndoRedoManager != null)
            UndoRedoManager.SuppressRecording = true;

        // Store start bounds for all selected elements
        _multiDragStartBounds.Clear();
        foreach (var element in _selectedElements)
        {
            _multiDragStartBounds[element.Id] = (new Point(element.X, element.Y), new Size(element.Width, element.Height));
        }
    }

    private void HandleDrag(Point canvasPoint)
    {
        // Use canvas-space delta (same offset applies regardless of page)
        var delta = canvasPoint - _interactionStartPoint;

        foreach (var element in _selectedElements)
        {
            if (_multiDragStartBounds.TryGetValue(element.Id, out var startBounds))
            {
                var newX = startBounds.Position.X + delta.X;
                var newY = startBounds.Position.Y + delta.Y;

                // Apply grid snapping only when grid is visible
                if (SnapToGrid && ShowGrid)
                {
                    newX = Math.Round(newX / GridSize) * GridSize;
                    newY = Math.Round(newY / GridSize) * GridSize;
                }

                // Constrain to page bounds
                var (pageWidth, pageHeight) = GetPageDimensions();
                newX = Math.Max(0, Math.Min(newX, pageWidth - element.Width));
                newY = Math.Max(0, Math.Min(newY, pageHeight - element.Height));

                element.X = newX;
                element.Y = newY;
            }
        }

        // Mark as manually positioned
        Configuration?.HasManualChartLayout = true;

        InvalidateCanvas();
    }

    private void EndDrag()
    {
        // Re-enable undo recording after drag
        if (UndoRedoManager != null)
            UndoRedoManager.SuppressRecording = false;

        // Record undo action for moved elements (as a single batch action)
        if (UndoRedoManager != null && Configuration != null && _selectedElements.Count > 0)
        {
            var oldBounds = new Dictionary<string, (double X, double Y, double Width, double Height)>();
            var newBounds = new Dictionary<string, (double X, double Y, double Width, double Height)>();
            var hasMoved = false;

            foreach (var element in _selectedElements)
            {
                if (_multiDragStartBounds.TryGetValue(element.Id, out var startBounds))
                {
                    var old = (startBounds.Position.X, startBounds.Position.Y, startBounds.Size.Width, startBounds.Size.Height);
                    var current = (element.X, element.Y, element.Width, element.Height);

                    // Check if position actually changed
                    if (Math.Abs(old.Item1 - current.Item1) > 0.1 || Math.Abs(old.Item2 - current.Item2) > 0.1)
                    {
                        oldBounds[element.Id] = old;
                        newBounds[element.Id] = current;
                        hasMoved = true;
                    }
                }
            }

            if (hasMoved)
            {
                var description = _selectedElements.Count > 1 ? $"Move {_selectedElements.Count} elements" : "Move element";
                var action = new BatchMoveResizeAction(Configuration, oldBounds, newBounds, description);
                UndoRedoManager.RecordAction(action);
            }
        }

        _multiDragStartBounds.Clear();
    }

    #endregion

    #region Resizing

    private void StartResize(ReportElementBase element, ResizeHandle handle, Point point)
    {
        _interactionMode = InteractionMode.Resizing;
        _activeResizeHandle = handle;
        _interactionStartPoint = point;
        _elementStartPosition = new Point(element.X, element.Y);
        _elementStartSize = new Size(element.Width, element.Height);

        // Suppress property change undo recording during resize - EndResize records the final action
        if (UndoRedoManager != null)
            UndoRedoManager.SuppressRecording = true;

        // Ensure only this element is selected during resize
        _selectedElements.Clear();
        _selectedElements.Add(element);
    }

    private void HandleResize(Point point)
    {
        if (_selectedElements.Count != 1) return;

        var element = _selectedElements[0];
        var delta = point - _interactionStartPoint;

        var newX = _elementStartPosition.X;
        var newY = _elementStartPosition.Y;
        var newWidth = _elementStartSize.Width;
        var newHeight = _elementStartSize.Height;

        var minSize = element.MinimumSize;

        switch (_activeResizeHandle)
        {
            case ResizeHandle.TopLeft:
                newX = _elementStartPosition.X + delta.X;
                newY = _elementStartPosition.Y + delta.Y;
                newWidth = _elementStartSize.Width - delta.X;
                newHeight = _elementStartSize.Height - delta.Y;
                break;

            case ResizeHandle.Top:
                newY = _elementStartPosition.Y + delta.Y;
                newHeight = _elementStartSize.Height - delta.Y;
                break;

            case ResizeHandle.TopRight:
                newY = _elementStartPosition.Y + delta.Y;
                newWidth = _elementStartSize.Width + delta.X;
                newHeight = _elementStartSize.Height - delta.Y;
                break;

            case ResizeHandle.Left:
                newX = _elementStartPosition.X + delta.X;
                newWidth = _elementStartSize.Width - delta.X;
                break;

            case ResizeHandle.Right:
                newWidth = _elementStartSize.Width + delta.X;
                break;

            case ResizeHandle.BottomLeft:
                newX = _elementStartPosition.X + delta.X;
                newWidth = _elementStartSize.Width - delta.X;
                newHeight = _elementStartSize.Height + delta.Y;
                break;

            case ResizeHandle.Bottom:
                newHeight = _elementStartSize.Height + delta.Y;
                break;

            case ResizeHandle.BottomRight:
                newWidth = _elementStartSize.Width + delta.X;
                newHeight = _elementStartSize.Height + delta.Y;
                break;
        }

        // Apply minimum size constraints
        if (newWidth < minSize)
        {
            if (_activeResizeHandle is ResizeHandle.TopLeft or ResizeHandle.Left or ResizeHandle.BottomLeft)
                newX = _elementStartPosition.X + _elementStartSize.Width - minSize;
            newWidth = minSize;
        }

        if (newHeight < minSize)
        {
            if (_activeResizeHandle is ResizeHandle.TopLeft or ResizeHandle.Top or ResizeHandle.TopRight)
                newY = _elementStartPosition.Y + _elementStartSize.Height - minSize;
            newHeight = minSize;
        }

        // Apply grid snapping only when grid is visible
        if (SnapToGrid && ShowGrid)
        {
            newX = Math.Round(newX / GridSize) * GridSize;
            newY = Math.Round(newY / GridSize) * GridSize;
            newWidth = Math.Round(newWidth / GridSize) * GridSize;
            newHeight = Math.Round(newHeight / GridSize) * GridSize;
        }

        element.X = newX;
        element.Y = newY;
        element.Width = Math.Max(newWidth, minSize);
        element.Height = Math.Max(newHeight, minSize);

        // Mark as manually positioned
        Configuration?.HasManualChartLayout = true;

        InvalidateCanvas();
    }

    private void EndResize()
    {
        // Re-enable undo recording after resize
        if (UndoRedoManager != null)
            UndoRedoManager.SuppressRecording = false;

        // Record undo action for resized element
        if (UndoRedoManager != null && Configuration != null && _selectedElements.Count == 1)
        {
            var element = _selectedElements[0];
            var oldBounds = (_elementStartPosition.X, _elementStartPosition.Y, _elementStartSize.Width, _elementStartSize.Height);
            var newBounds = (element.X, element.Y, element.Width, element.Height);

            // Only record if bounds actually changed
            if (Math.Abs(oldBounds.Item1 - newBounds.Item1) > 0.1 ||
                Math.Abs(oldBounds.Item2 - newBounds.Item2) > 0.1 ||
                Math.Abs(oldBounds.Item3 - newBounds.Item3) > 0.1 ||
                Math.Abs(oldBounds.Item4 - newBounds.Item4) > 0.1)
            {
                var action = new MoveResizeElementAction(Configuration, element.Id, oldBounds, newBounds, isResize: true);
                UndoRedoManager.RecordAction(action);
            }
        }

        _activeResizeHandle = ResizeHandle.None;
    }

    #endregion

    #region Selection Rectangle

    private void StartSelectionRectangle(Point point)
    {
        _interactionMode = InteractionMode.Selecting;
        _interactionStartPoint = point;

        if (_selectionRectangle != null)
        {
            Canvas.SetLeft(_selectionRectangle, point.X);
            Canvas.SetTop(_selectionRectangle, point.Y);
            _selectionRectangle.Width = 0;
            _selectionRectangle.Height = 0;
            _selectionRectangle.IsVisible = true;
        }
    }

    private void UpdateSelectionRectangle(Point canvasPoint)
    {
        if (_selectionRectangle == null) return;

        var x = Math.Min(_interactionStartPoint.X, canvasPoint.X);
        var y = Math.Min(_interactionStartPoint.Y, canvasPoint.Y);
        var width = Math.Abs(canvasPoint.X - _interactionStartPoint.X);
        var height = Math.Abs(canvasPoint.Y - _interactionStartPoint.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;

        // Update selection based on rectangle (check all pages)
        if (Configuration != null)
        {
            var selectionRect = new Rect(x, y, width, height);
            _selectedElements.Clear();

            var pageCount = GetPageCount();
            for (int page = 1; page <= pageCount; page++)
            {
                var pageOffset = GetPageYOffset(page);
                foreach (var element in Configuration.Elements.Where(e => e.PageNumber == page))
                {
                    // Convert element rect to canvas space by adding page offset
                    var elementRect = new Rect(element.X, element.Y + pageOffset, element.Width, element.Height);
                    if (selectionRect.Intersects(elementRect))
                    {
                        _selectedElements.Add(element);
                    }
                }
            }
        }

        InvalidateCanvas();
    }

    private void EndSelectionRectangle()
    {
        _selectionRectangle?.IsVisible = false;

        NotifySelectionChanged();
    }

    #endregion

    #region Cursor

    private void UpdateCursor(Point point)
    {
        // Check resize handles first
        foreach (var selected in _selectedElements)
        {
            var handle = GetResizeHandleAtPoint(point, selected);
            if (handle != ResizeHandle.None)
            {
                Cursor = GetResizeCursor(handle);
                return;
            }
        }

        // Check if over an element
        var element = GetElementAtPoint(point);
        Cursor = element != null ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    }

    private Cursor GetResizeCursor(ResizeHandle handle)
    {
        return handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => new Cursor(StandardCursorType.TopLeftCorner),
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => new Cursor(StandardCursorType.TopRightCorner),
            ResizeHandle.Top or ResizeHandle.Bottom => new Cursor(StandardCursorType.SizeNorthSouth),
            ResizeHandle.Left or ResizeHandle.Right => new Cursor(StandardCursorType.SizeWestEast),
            _ => Cursor.Default
        };
    }

    #endregion

    #region Keyboard

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_selectedElements.Count == 0) return;

        switch (e.Key)
        {
            case Key.Delete:
            case Key.Back:
                DeleteSelectedElements();
                e.Handled = true;
                break;

            case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                SelectAll();
                e.Handled = true;
                break;

            case Key.Escape:
                ClearSelection();
                e.Handled = true;
                break;

            // Arrow key movement
            case Key.Up:
            case Key.Down:
            case Key.Left:
            case Key.Right:
                MoveSelectedElements(e.Key, e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1);
                e.Handled = true;
                break;
        }
    }

    #endregion

    #region Public Methods

    public void SelectAll()
    {
        if (Configuration == null) return;

        _selectedElements.Clear();
        _selectedElements.AddRange(Configuration.Elements.Where(e => e.PageNumber == CurrentDesignerPage));
        InvalidateCanvas();

        if (_selectedElements.Count > 0)
        {
            ElementSelected?.Invoke(this, _selectedElements[0]);
        }
    }

    public void ClearSelection()
    {
        _selectedElements.Clear();
        _hoveredElement = null;
        NotifySelectionChanged();
        InvalidateCanvas();
    }

    public void DeleteSelectedElements()
    {
        if (Configuration == null) return;

        foreach (var element in _selectedElements.ToList())
        {
            Configuration.Elements.Remove(element);
            ElementRemoved?.Invoke(this, element);
        }

        _selectedElements.Clear();
        _hoveredElement = null;
        NotifySelectionChanged();
        InvalidateCanvas();
    }

    public void MoveSelectedElements(Key direction, double distance)
    {
        var (pageWidth, pageHeight) = GetPageDimensions();

        foreach (var element in _selectedElements)
        {
            switch (direction)
            {
                case Key.Up:
                    element.Y = Math.Max(0, element.Y - distance);
                    break;
                case Key.Down:
                    element.Y = Math.Min(pageHeight - element.Height, element.Y + distance);
                    break;
                case Key.Left:
                    element.X = Math.Max(0, element.X - distance);
                    break;
                case Key.Right:
                    element.X = Math.Min(pageWidth - element.Width, element.X + distance);
                    break;
            }
        }

        Configuration?.HasManualChartLayout = true;

        InvalidateCanvas();
    }

    public void AddElement(ReportElementBase element)
    {
        if (Configuration == null) return;

        element.PageNumber = CurrentDesignerPage;
        Configuration.AddElement(element);
        _selectedElements.Clear();
        _selectedElements.Add(element);

        ElementAdded?.Invoke(this, new ElementAddedEventArgs(element));
        InvalidateCanvas();
    }

    public ReportElementBase? GetSelectedElement()
    {
        return _selectedElements.FirstOrDefault();
    }

    public IReadOnlyList<ReportElementBase> GetSelectedElements()
    {
        return _selectedElements.AsReadOnly();
    }

    public void SelectElement(ReportElementBase element, bool addToSelection = false)
    {
        if (!addToSelection)
        {
            _selectedElements.Clear();
        }

        if (!_selectedElements.Contains(element))
        {
            _selectedElements.Add(element);
        }

        ElementSelected?.Invoke(this, element);
        InvalidateCanvas();
    }

    /// <summary>
    /// Refreshes the canvas display.
    /// </summary>
    public void Refresh()
    {
        InvalidateCanvas();
    }

    /// <summary>
    /// Zooms to fit the page width within the available viewport.
    /// </summary>
    public void ZoomToFit()
    {
        if (Configuration == null) return;

        var scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
        if (scrollViewer == null) return;

        // Use Bounds if Viewport is not yet calculated (layout not complete)
        var viewportWidth = scrollViewer.Viewport.Width > 0 ? scrollViewer.Viewport.Width : scrollViewer.Bounds.Width;

        // Account for margins - the LayoutTransformControl has 10px margin on each side (20px total)
        viewportWidth -= 20;

        if (viewportWidth <= 0) return;

        var (pageWidth, _) = GetPageDimensions();

        ZoomLevel = Math.Clamp(viewportWidth / pageWidth, MinZoom, MaxZoom);

        // Scroll to top after fitting
        Dispatcher.UIThread.Post(() =>
        {
            if (scrollViewer != null)
                scrollViewer.Offset = new Vector(0, 0);
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Scrolls the canvas to bring a specific page into view.
    /// </summary>
    private void ScrollToPage(int pageNumber)
    {
        if (_scrollViewer == null) return;

        var pageYOffset = GetPageYOffset(pageNumber);
        var zoom = ZoomLevel;
        const double margin = 10;

        // Calculate the scroll offset to show this page at the top
        var scrollY = pageYOffset * zoom;

        Dispatcher.UIThread.Post(() =>
        {
            if (_scrollViewer == null) return;
            var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, Math.Clamp(scrollY, 0, maxY));
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Syncs elements with the current configuration.
    /// Validates selection by removing any elements that no longer exist in the configuration.
    /// </summary>
    public void SyncElements()
    {
        ValidateSelection();
        InvalidateCanvas();
    }

    /// <summary>
    /// Validates the current selection by removing any elements that no longer exist in Configuration.Elements.
    /// </summary>
    private void ValidateSelection()
    {
        if (Configuration == null)
        {
            if (_selectedElements.Count > 0)
            {
                _selectedElements.Clear();
                _hoveredElement = null;
                NotifySelectionChanged();
            }
            return;
        }

        // Get the set of valid element IDs
        var validIds = Configuration.Elements.Select(e => e.Id).ToHashSet();

        // Remove any selected elements that are no longer in the configuration
        var staleElements = _selectedElements.Where(e => !validIds.Contains(e.Id)).ToList();
        if (staleElements.Count > 0)
        {
            foreach (var stale in staleElements)
            {
                _selectedElements.Remove(stale);
            }

            // Clear hovered element if it was deleted
            if (_hoveredElement != null && !validIds.Contains(_hoveredElement.Id))
            {
                _hoveredElement = null;
            }

            NotifySelectionChanged();
        }
    }

    /// <summary>
    /// Refreshes all elements (compatibility method - just refreshes).
    /// </summary>
    public void RefreshAllElements()
    {
        InvalidateCanvas();
    }

    /// <summary>
    /// Refreshes page settings (compatibility method - just refreshes).
    /// </summary>
    public void RefreshPageSettings()
    {
        InvalidateCanvas();
    }

    /// <summary>
    /// Refreshes a specific element's content (compatibility method - just refreshes).
    /// </summary>
    public void RefreshElementContent(ReportElementBase element)
    {
        InvalidateCanvas();
    }

    #endregion
}

/// <summary>
/// Event args for selection changes.
/// </summary>
public class SelectionChangedEventArgs(
    ReportElementBase? selectedElement,
    IReadOnlyList<ReportElementBase> selectedElements)
    : EventArgs
{
    public ReportElementBase? SelectedElement { get; } = selectedElement;
    public IReadOnlyList<ReportElementBase> SelectedElements { get; } = selectedElements;
}

/// <summary>
/// Event args for when an element is added.
/// </summary>
public class ElementAddedEventArgs(ReportElementBase element) : EventArgs
{
    public ReportElementBase Element { get; } = element;
}

/// <summary>
/// Event args for when a context menu is requested.
/// </summary>
public class ContextMenuRequestedEventArgs(double x, double y, ReportElementBase element) : EventArgs
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public ReportElementBase Element { get; } = element;
}
