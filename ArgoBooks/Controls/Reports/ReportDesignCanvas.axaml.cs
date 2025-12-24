using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Services;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// Main design canvas for the report layout editor.
/// Manages element placement, selection, and visual guides.
/// </summary>
public partial class ReportDesignCanvas : UserControl
{
    #region Zoom Constants

    /// <summary>
    /// Minimum zoom level (25%).
    /// </summary>
    public const double MinZoom = 0.25;

    /// <summary>
    /// Maximum zoom level (200%).
    /// </summary>
    public const double MaxZoom = 2.0;

    /// <summary>
    /// Zoom step size for incremental zoom operations.
    /// </summary>
    public const double ZoomStep = 0.25;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<ReportConfiguration?> ConfigurationProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, ReportConfiguration?>(nameof(Configuration));

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, bool>(nameof(ShowGrid), true);

    public static readonly StyledProperty<double> GridSizeProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, double>(nameof(GridSize), 20);

    public static readonly StyledProperty<bool> SnapToGridProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, bool>(nameof(SnapToGrid), true);

    public static readonly StyledProperty<bool> ShowMarginGuidesProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, bool>(nameof(ShowMarginGuides), true);

    public static readonly StyledProperty<bool> ShowHeaderFooterProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, bool>(nameof(ShowHeaderFooter), true);

    public static readonly StyledProperty<IBrush?> PageBackgroundBrushProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, IBrush?>(nameof(PageBackgroundBrush), Brushes.White);

    public static readonly StyledProperty<ReportUndoRedoManager?> UndoRedoManagerProperty =
        AvaloniaProperty.Register<ReportDesignCanvas, ReportUndoRedoManager?>(nameof(UndoRedoManager));

    #endregion

    #region Properties

    /// <summary>
    /// The report configuration being designed.
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
        set => SetValue(ZoomLevelProperty, Math.Clamp(value, MinZoom, MaxZoom));
    }

    /// <summary>
    /// Whether to show grid lines.
    /// </summary>
    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// Grid cell size in pixels.
    /// </summary>
    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    /// <summary>
    /// Whether elements snap to grid.
    /// </summary>
    public bool SnapToGrid
    {
        get => GetValue(SnapToGridProperty);
        set => SetValue(SnapToGridProperty, value);
    }

    /// <summary>
    /// Whether to show margin guides.
    /// </summary>
    public bool ShowMarginGuides
    {
        get => GetValue(ShowMarginGuidesProperty);
        set => SetValue(ShowMarginGuidesProperty, value);
    }

    /// <summary>
    /// Whether to show header/footer areas.
    /// </summary>
    public bool ShowHeaderFooter
    {
        get => GetValue(ShowHeaderFooterProperty);
        set => SetValue(ShowHeaderFooterProperty, value);
    }

    /// <summary>
    /// Page background color brush.
    /// </summary>
    public IBrush? PageBackgroundBrush
    {
        get => GetValue(PageBackgroundBrushProperty);
        set => SetValue(PageBackgroundBrushProperty, value);
    }

    /// <summary>
    /// Undo/redo manager for tracking changes.
    /// </summary>
    public ReportUndoRedoManager? UndoRedoManager
    {
        get => GetValue(UndoRedoManagerProperty);
        set => SetValue(UndoRedoManagerProperty, value);
    }

    /// <summary>
    /// Currently selected elements.
    /// </summary>
    public IReadOnlyList<ReportElementBase> SelectedElements => _selectedElements.AsReadOnly();

    /// <summary>
    /// All element controls on the canvas.
    /// </summary>
    public IReadOnlyList<CanvasElementControl> ElementControls => _elementControls.AsReadOnly();

    #endregion

    #region Events

    /// <summary>
    /// Raised when the selection changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Raised when an element is added.
    /// </summary>
    public event EventHandler<ElementAddedEventArgs>? ElementAdded;

    /// <summary>
    /// Raised when an element is removed.
    /// </summary>
    public event EventHandler<ElementRemovedEventArgs>? ElementRemoved;

    /// <summary>
    /// Raised when elements are modified (position, size, etc.).
    /// </summary>
    public event EventHandler<ElementsModifiedEventArgs>? ElementsModified;

    #endregion

    #region Private Fields

    private readonly List<CanvasElementControl> _elementControls = [];
    private readonly List<ReportElementBase> _selectedElements = [];
    private readonly Dictionary<string, CanvasElementControl> _elementControlMap = [];

    private ScrollViewer? _scrollViewer;
    private LayoutTransformControl? _zoomTransformControl;
    private Canvas? _elementsCanvas;
    private Canvas? _gridLinesCanvas;
    private Rectangle? _marginGuide;
    private Border? _headerArea;
    private Border? _footerArea;
    private Rectangle? _selectionRectangle;
    private Border? _pageBackground;
    private Border? _dropIndicator;

    private bool _isMultiSelecting;
    private Point _selectionStartPoint;

    // Right-click panning
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

    // Rubberband overscroll effect
    private Vector _overscroll;
    private const double OverscrollResistance = 0.3; // How much resistance when overscrolling (0-1)
    private const double OverscrollMaxDistance = 100; // Maximum overscroll distance in pixels

    private double _pageWidth;
    private double _pageHeight;

    // Clipboard storage
    private List<ReportElementBase>? _clipboardElements;

    #endregion

    public ReportDesignCanvas()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
        _zoomTransformControl = this.FindControl<LayoutTransformControl>("ZoomTransformControl");
        _elementsCanvas = this.FindControl<Canvas>("ElementsCanvas");
        _gridLinesCanvas = this.FindControl<Canvas>("GridLinesCanvas");
        _marginGuide = this.FindControl<Rectangle>("MarginGuide");
        _headerArea = this.FindControl<Border>("HeaderArea");
        _footerArea = this.FindControl<Border>("FooterArea");
        _selectionRectangle = this.FindControl<Rectangle>("SelectionRectangle");
        _pageBackground = this.FindControl<Border>("PageBackground");
        _dropIndicator = this.FindControl<Border>("DropIndicator");

        // Intercept wheel events on the ScrollViewer to prevent scrolling
        if (_scrollViewer != null)
        {
            _scrollViewer.AddHandler(PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        UpdateLayout();

        // If Configuration was set before template was applied, process it now
        if (Configuration != null && _elementsCanvas != null && _elementControls.Count == 0)
        {
            OnConfigurationChanged();
        }
    }

    private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Intercept wheel events to zoom instead of scroll
        var delta = e.Delta.Y;
        if (delta != 0 && _zoomTransformControl != null)
        {
            // Get cursor position relative to the scroll viewer's viewport
            var viewportPoint = e.GetPosition(_scrollViewer);
            // Get cursor position relative to the scaled content
            var contentPoint = e.GetPosition(_zoomTransformControl);
            ZoomAtPoint(delta > 0, viewportPoint, contentPoint);
        }
        e.Handled = true;
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
        else if (change.Property == ShowGridProperty || change.Property == GridSizeProperty)
        {
            DrawGrid();
            UpdateElementsSnapGridSize();
        }
        else if (change.Property == SnapToGridProperty)
        {
            UpdateElementsSnapGridSize();
        }
        else if (change.Property == ShowMarginGuidesProperty)
        {
            UpdateMarginGuides();
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        EnsureElementsLoaded();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // This is called when control becomes visible in the visual tree
        // Important for cases where Configuration was set while the control was hidden
        EnsureElementsLoaded();
    }

    private void EnsureElementsLoaded()
    {
        UpdateLayout();
        DrawGrid();

        // Ensure elements are loaded if Configuration was set before loading/becoming visible
        // Check both that we have a configuration AND that elements haven't been added to canvas yet
        var elementsNeedLoading = Configuration != null && _elementsCanvas != null &&
            ((_elementControls.Count == 0 && Configuration.Elements.Count > 0) ||
             (_elementControls.Count > 0 && _elementsCanvas.Children.Count == 0));

        if (elementsNeedLoading)
        {
            OnConfigurationChanged();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        // When the control gets a non-zero size (becomes visible), ensure elements are loaded
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0 && Configuration != null && _elementsCanvas != null)
        {
            // Check if elements exist in config but aren't on canvas
            var elementsNeedLoading = (_elementControls.Count == 0 && Configuration.Elements.Count > 0) ||
                                       (_elementControls.Count > 0 && _elementsCanvas.Children.Count == 0);
            if (elementsNeedLoading)
            {
                OnConfigurationChanged();
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // Mouse wheel controls zoom instead of scroll
        var delta = e.Delta.Y;
        if (delta > 0)
        {
            ZoomIn();
        }
        else if (delta < 0)
        {
            ZoomOut();
        }

        // Prevent scrolling - mark the event as handled
        e.Handled = true;
        base.OnPointerWheelChanged(e);
    }

    #region Configuration Handling

    private void OnConfigurationChanged()
    {
        ClearAllElements();

        if (Configuration == null) return;

        // Update page dimensions
        var (width, height) = PageDimensions.GetDimensions(
            Configuration.PageSize,
            Configuration.PageOrientation);
        _pageWidth = width;
        _pageHeight = height;

        // Update page background
        if (Color.TryParse(Configuration.BackgroundColor, out var color))
        {
            PageBackgroundBrush = new SolidColorBrush(color);
        }

        UpdateLayout();

        // Add element controls for each element
        foreach (var element in Configuration.Elements)
        {
            AddElementControl(element);
        }
    }

    private new void UpdateLayout()
    {
        if (_pageBackground == null || Configuration == null) return;

        _pageBackground.Width = _pageWidth;
        _pageBackground.Height = _pageHeight;

        if (_elementsCanvas != null)
        {
            _elementsCanvas.Width = _pageWidth;
            _elementsCanvas.Height = _pageHeight;
        }

        // Set explicit dimensions on grid canvas to match page size
        if (_gridLinesCanvas != null)
        {
            _gridLinesCanvas.Width = _pageWidth;
            _gridLinesCanvas.Height = _pageHeight;
        }

        UpdateMarginGuides();
        UpdateHeaderFooterAreas();
        DrawGrid();
        ApplyZoom();
    }

    private void UpdateMarginGuides()
    {
        if (_marginGuide == null || Configuration == null) return;

        var margins = Configuration.PageMargins;
        _marginGuide.Margin = new Thickness(margins.Left, margins.Top, margins.Right, margins.Bottom);
        _marginGuide.Width = _pageWidth - margins.Left - margins.Right;
        _marginGuide.Height = _pageHeight - margins.Top - margins.Bottom;
    }

    private void UpdateElementsSnapGridSize()
    {
        var snapSize = (SnapToGrid && ShowGrid) ? GridSize : 0;
        foreach (var control in _elementControls)
        {
            control.SnapGridSize = snapSize;
        }
    }

    private void UpdateHeaderFooterAreas()
    {
        if (Configuration == null) return;

        if (_headerArea != null && Configuration.ShowHeader)
        {
            _headerArea.Height = PageDimensions.HeaderHeight;
            _headerArea.IsVisible = ShowHeaderFooter;
        }

        if (_footerArea != null && Configuration.ShowFooter)
        {
            _footerArea.Height = PageDimensions.FooterHeight;
            _footerArea.IsVisible = ShowHeaderFooter;
        }
    }

    #endregion

    #region Grid Drawing

    private void DrawGrid()
    {
        if (_gridLinesCanvas == null || !ShowGrid) return;

        _gridLinesCanvas.Children.Clear();

        if (GridSize <= 0) return;

        var width = _pageWidth;
        var height = _pageHeight;

        // Draw edge lines (border frame) at page boundaries
        // Top edge
        _gridLinesCanvas.Children.Add(new Line
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(width, 0),
            Classes = { "grid-line-major" }
        });
        // Bottom edge
        _gridLinesCanvas.Children.Add(new Line
        {
            StartPoint = new Point(0, height),
            EndPoint = new Point(width, height),
            Classes = { "grid-line-major" }
        });
        // Left edge
        _gridLinesCanvas.Children.Add(new Line
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, height),
            Classes = { "grid-line-major" }
        });
        // Right edge
        _gridLinesCanvas.Children.Add(new Line
        {
            StartPoint = new Point(width, 0),
            EndPoint = new Point(width, height),
            Classes = { "grid-line-major" }
        });

        // Draw vertical lines
        for (double x = GridSize; x < width; x += GridSize)
        {
            var line = new Line
            {
                StartPoint = new Point(x, 0),
                EndPoint = new Point(x, height),
                Classes = { x % (GridSize * 5) == 0 ? "grid-line-major" : "grid-line" }
            };
            _gridLinesCanvas.Children.Add(line);
        }

        // Draw horizontal lines
        for (double y = GridSize; y < height; y += GridSize)
        {
            var line = new Line
            {
                StartPoint = new Point(0, y),
                EndPoint = new Point(width, y),
                Classes = { y % (GridSize * 5) == 0 ? "grid-line-major" : "grid-line" }
            };
            _gridLinesCanvas.Children.Add(line);
        }
    }

    #endregion

    #region Zoom

    private void ApplyZoom()
    {
        if (_zoomTransformControl != null)
        {
            _zoomTransformControl.LayoutTransform = new ScaleTransform(ZoomLevel, ZoomLevel);
        }
    }

    /// <summary>
    /// Zooms in by step amount, centering the view.
    /// </summary>
    public void ZoomIn()
    {
        ZoomTowardsCenter(true);
    }

    /// <summary>
    /// Zooms out by step amount, centering the view.
    /// </summary>
    public void ZoomOut()
    {
        ZoomTowardsCenter(false);
    }

    /// <summary>
    /// Zooms towards the center of the viewport.
    /// </summary>
    /// <param name="zoomIn">True to zoom in, false to zoom out.</param>
    private void ZoomTowardsCenter(bool zoomIn)
    {
        if (_scrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = ZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);

        if (Math.Abs(oldZoom - newZoom) < 0.001) return;

        // Get the center point of the viewport
        var viewportCenterX = _scrollViewer.Viewport.Width / 2;
        var viewportCenterY = _scrollViewer.Viewport.Height / 2;

        // Calculate the content point at the center of the viewport
        var contentCenterX = (_scrollViewer.Offset.X + viewportCenterX) / oldZoom;
        var contentCenterY = (_scrollViewer.Offset.Y + viewportCenterY) / oldZoom;

        // Apply the zoom
        ZoomLevel = newZoom;

        // Force layout to update so we get accurate extent/viewport values
        _zoomTransformControl.UpdateLayout();

        // Calculate new offset to keep the same content point at center
        var newOffsetX = contentCenterX * newZoom - viewportCenterX;
        var newOffsetY = contentCenterY * newZoom - viewportCenterY;

        // Use actual extent and viewport after layout update
        var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);

        _scrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    /// <summary>
    /// Zooms at a specific point, keeping that point fixed on screen.
    /// </summary>
    /// <param name="zoomIn">True to zoom in, false to zoom out.</param>
    /// <param name="viewportPoint">The cursor position relative to the scroll viewer.</param>
    /// <param name="scaledContentPoint">The cursor position relative to the scaled content.</param>
    private void ZoomAtPoint(bool zoomIn, Point viewportPoint, Point scaledContentPoint)
    {
        if (_scrollViewer == null || _zoomTransformControl == null) return;

        var oldZoom = ZoomLevel;
        var newZoom = zoomIn
            ? Math.Min(oldZoom + ZoomStep, MaxZoom)
            : Math.Max(oldZoom - ZoomStep, MinZoom);

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
        var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);

        _scrollViewer.Offset = new Vector(
            Math.Clamp(newOffsetX, 0, maxX),
            Math.Clamp(newOffsetY, 0, maxY)
        );
    }

    /// <summary>
    /// Resets zoom to 100%.
    /// </summary>
    public void ZoomReset()
    {
        ZoomLevel = 1.0;
    }

    /// <summary>
    /// Fits the canvas to the viewport.
    /// </summary>
    public void ZoomToFit()
    {
        // Use ScrollViewer's viewport for accurate fit calculation
        var viewportWidth = _scrollViewer?.Viewport.Width ?? Bounds.Width;
        var viewportHeight = _scrollViewer?.Viewport.Height ?? Bounds.Height;

        if (viewportWidth <= 0 || viewportHeight <= 0 || _pageWidth <= 0 || _pageHeight <= 0) return;

        // Account for padding (40px on each side from ZoomContainer)
        var availableWidth = viewportWidth - 80;
        var availableHeight = viewportHeight - 80;

        var scaleX = availableWidth / _pageWidth;
        var scaleY = availableHeight / _pageHeight;
        ZoomLevel = Math.Min(scaleX, scaleY);

        // Center the view after zooming
        CenterView();
    }

    /// <summary>
    /// Centers the view on the page.
    /// </summary>
    public void CenterView()
    {
        if (_scrollViewer == null) return;

        // Get the extent (total scrollable content size) and viewport
        var extentWidth = _scrollViewer.Extent.Width;
        var extentHeight = _scrollViewer.Extent.Height;
        var viewportWidth = _scrollViewer.Viewport.Width;
        var viewportHeight = _scrollViewer.Viewport.Height;

        // Calculate offset to center the content
        double offsetX = Math.Max(0, (extentWidth - viewportWidth) / 2);
        double offsetY = Math.Max(0, (extentHeight - viewportHeight) / 2);

        _scrollViewer.Offset = new Vector(offsetX, offsetY);
    }

    #endregion

    #region Overscroll/Rubberband Effect

    /// <summary>
    /// Applies the current overscroll as a visual transform.
    /// </summary>
    private void ApplyOverscrollTransform()
    {
        if (_zoomTransformControl == null) return;

        // Apply translation to show overscroll effect
        // The overscroll is inverted because dragging right should show content from left
        var translateTransform = new TranslateTransform(-_overscroll.X, -_overscroll.Y);

        // Combine with existing layout transform
        if (_zoomTransformControl.LayoutTransform is ScaleTransform scaleTransform)
        {
            _zoomTransformControl.RenderTransform = translateTransform;
        }
        else
        {
            _zoomTransformControl.RenderTransform = translateTransform;
        }
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

    #region Element Management

    /// <summary>
    /// Adds an element to the canvas.
    /// </summary>
    public void AddElement(ReportElementBase element)
    {
        if (Configuration == null || element == null) return;

        Configuration.AddElement(element);
        var control = AddElementControl(element);

        // Record for undo
        UndoRedoManager?.RecordAction(new AddElementAction(Configuration, element));

        ElementAdded?.Invoke(this, new ElementAddedEventArgs(element));

        // Select the new element
        SelectElement(element, false);
    }

    /// <summary>
    /// Removes an element from the canvas.
    /// </summary>
    public void RemoveElement(ReportElementBase element)
    {
        if (Configuration == null || element == null) return;

        if (_elementControlMap.TryGetValue(element.Id, out var control))
        {
            RemoveElementControl(control);
        }

        Configuration.RemoveElement(element.Id);

        // Record for undo
        UndoRedoManager?.RecordAction(new RemoveElementAction(Configuration, element));

        _selectedElements.Remove(element);
        ElementRemoved?.Invoke(this, new ElementRemovedEventArgs(element));
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectedElements));
    }

    /// <summary>
    /// Removes all selected elements.
    /// </summary>
    public void RemoveSelectedElements()
    {
        var elementsToRemove = _selectedElements.ToList();
        foreach (var element in elementsToRemove)
        {
            RemoveElement(element);
        }
    }

    private CanvasElementControl? AddElementControl(ReportElementBase element)
    {
        // Don't add if canvas isn't ready
        if (_elementsCanvas == null) return null;

        var control = new CanvasElementControl
        {
            Element = element,
            Width = element.Width,
            Height = element.Height,
            SnapGridSize = (SnapToGrid && ShowGrid) ? GridSize : 0
        };

        Canvas.SetLeft(control, element.X);
        Canvas.SetTop(control, element.Y);
        control.ZIndex = element.ZOrder;

        // Wire up events
        control.ElementSelected += OnElementSelected;
        control.PositionChanged += OnElementPositionChanged;
        control.SizeChanged += OnElementSizeChanged;
        control.DragStarted += OnElementDragStarted;
        control.DragEnded += OnElementDragEnded;
        control.DeleteRequested += OnElementDeleteRequested;

        // Subscribe to element property changes for real-time rendering
        element.PropertyChanged += OnElementPropertyChanged;

        // Set content based on element type
        control.ElementContent = CreateElementContent(element);

        _elementsCanvas.Children.Add(control);
        _elementControls.Add(control);
        _elementControlMap[element.Id] = control;

        return control;
    }

    private void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ReportElementBase element && _elementControlMap.TryGetValue(element.Id, out var control))
        {
            // Update position and size if those properties changed
            if (e.PropertyName is nameof(ReportElementBase.X) or nameof(ReportElementBase.Y) or
                nameof(ReportElementBase.Width) or nameof(ReportElementBase.Height) or nameof(ReportElementBase.ZOrder))
            {
                control.SyncFromElement();
            }

            // Refresh the element's visual content when properties change
            control.ElementContent = CreateElementContent(element);
        }
    }

    private void RemoveElementControl(CanvasElementControl control)
    {
        control.ElementSelected -= OnElementSelected;
        control.PositionChanged -= OnElementPositionChanged;
        control.SizeChanged -= OnElementSizeChanged;
        control.DragStarted -= OnElementDragStarted;
        control.DragEnded -= OnElementDragEnded;
        control.DeleteRequested -= OnElementDeleteRequested;

        // Unsubscribe from element property changes
        if (control.Element != null)
        {
            control.Element.PropertyChanged -= OnElementPropertyChanged;
        }

        _elementsCanvas?.Children.Remove(control);
        _elementControls.Remove(control);

        if (control.Element != null)
        {
            _elementControlMap.Remove(control.Element.Id);
        }
    }

    private void ClearAllElements()
    {
        foreach (var control in _elementControls.ToList())
        {
            RemoveElementControl(control);
        }

        _elementControls.Clear();
        _elementControlMap.Clear();
        _selectedElements.Clear();
    }

    private Control CreateElementContent(ReportElementBase element)
    {
        return element.GetElementType() switch
        {
            ReportElementType.Chart => CreateChartPreview(element as ChartReportElement),
            ReportElementType.Table => CreateTablePreview(element as TableReportElement),
            ReportElementType.Label => CreateLabelPreview(element as LabelReportElement),
            ReportElementType.Image => CreateImagePreview(element as ImageReportElement),
            ReportElementType.DateRange => CreateDateRangePreview(element as DateRangeReportElement),
            ReportElementType.Summary => CreateSummaryPreview(element as SummaryReportElement),
            _ => new TextBlock { Text = "Unknown Element", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center }
        };
    }

    private static Control CreateChartPreview(ChartReportElement? element)
    {
        var title = GetChartTitle(element?.ChartType ?? ChartDataType.TotalRevenue);
        var borderThickness = element?.BorderThickness ?? 0;
        var showTitle = element?.ShowTitle ?? true;
        var titleFontSize = element?.TitleFontSize ?? 14;

        var grid = new Grid
        {
            RowDefinitions = showTitle
                ? new RowDefinitions("Auto,*")
                : new RowDefinitions("*")
        };

        // Title row
        if (showTitle)
        {
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = titleFontSize,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 4)
            };
            Grid.SetRow(titleBlock, 0);
            grid.Children.Add(titleBlock);
        }

        // Chart area placeholder
        var chartArea = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E8E8E8")),
            Margin = new Thickness(10, showTitle ? 4 : 10, 10, 10),
            Child = new TextBlock
            {
                Text = $"[{element?.ChartType}]",
                Foreground = Brushes.Gray,
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };
        Grid.SetRow(chartArea, showTitle ? 1 : 0);
        grid.Children.Add(chartArea);

        IBrush borderBrush = borderThickness > 0 && element?.BorderColor != null
            ? new SolidColorBrush(Color.Parse(element.BorderColor))
            : Brushes.Gray;

        return new Border
        {
            Background = Brushes.White,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(borderThickness > 0 ? borderThickness : 1),
            Child = grid
        };
    }

    private static string GetChartTitle(ChartDataType chartType)
    {
        return chartType switch
        {
            ChartDataType.TotalRevenue => "Total Revenue",
            ChartDataType.RevenueDistribution => "Revenue Distribution",
            ChartDataType.TotalExpenses => "Total Expenses",
            ChartDataType.ExpensesDistribution => "Expense Distribution",
            ChartDataType.TotalProfits => "Total Profits",
            ChartDataType.SalesVsExpenses => "Sales vs Expenses",
            ChartDataType.GrowthRates => "Growth Rates",
            ChartDataType.AverageTransactionValue => "Average Transaction Value",
            ChartDataType.TotalTransactions => "Total Transactions",
            ChartDataType.AverageShippingCosts => "Average Shipping Costs",
            ChartDataType.WorldMap => "Geographic Distribution",
            ChartDataType.CountriesOfOrigin => "Countries of Origin",
            ChartDataType.CountriesOfDestination => "Countries of Destination",
            ChartDataType.CompaniesOfOrigin => "Companies of Origin",
            ChartDataType.AccountantsTransactions => "Transactions by Accountant",
            ChartDataType.ReturnsOverTime => "Returns Over Time",
            ChartDataType.ReturnReasons => "Return Reasons",
            ChartDataType.ReturnFinancialImpact => "Return Financial Impact",
            ChartDataType.ReturnsByCategory => "Returns by Category",
            ChartDataType.ReturnsByProduct => "Returns by Product",
            ChartDataType.PurchaseVsSaleReturns => "Purchase vs Sale Returns",
            ChartDataType.LossesOverTime => "Losses Over Time",
            ChartDataType.LossReasons => "Loss Reasons",
            ChartDataType.LossFinancialImpact => "Loss Financial Impact",
            ChartDataType.LossesByCategory => "Losses by Category",
            ChartDataType.LossesByProduct => "Losses by Product",
            ChartDataType.PurchaseVsSaleLosses => "Purchase vs Sale Losses",
            _ => "Chart"
        };
    }

    private Control CreateTablePreview(TableReportElement? element)
    {
        var showHeaders = element?.ShowHeaders ?? true;
        var headerBgColor = element?.HeaderBackgroundColor ?? "#E0E0E0";
        var headerTextColor = element?.HeaderTextColor ?? "#000000";
        var dataTextColor = element?.DataRowTextColor ?? "#000000";
        var fontSize = element?.FontSize ?? 12;
        var showGridLines = element?.ShowGridLines ?? true;
        var gridLineColor = element?.GridLineColor ?? "#CCCCCC";
        var headerRowHeight = element?.HeaderRowHeight ?? 25;
        var dataRowHeight = element?.DataRowHeight ?? 20;
        var alternateRowColors = element?.AlternateRowColors ?? true;
        var baseRowColor = element?.BaseRowColor ?? "#FFFFFF";
        var alternateRowColor = element?.AlternateRowColor ?? "#F8F8F8";

        // Check if company data is available
        var companyData = App.CompanyManager?.CompanyData;
        var hasData = companyData != null && (companyData.Sales.Count > 0 || companyData.Purchases.Count > 0);

        if (!hasData)
        {
            // Show "No data" message
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse(gridLineColor)),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = "No data available",
                    FontSize = fontSize,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(10)
                }
            };
        }

        var grid = new Grid();

        // Get visible columns
        var columns = GetVisibleTableColumns(element);
        var columnCount = Math.Max(columns.Count, 1);

        // Get transaction data based on type
        var tableData = GetTableData(element, companyData!, columns);
        var previewRowCount = Math.Min(tableData.Count, element?.MaxRows ?? 10);

        // Create column definitions
        for (int i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }

        // Create row definitions with proper heights
        if (showHeaders)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(headerRowHeight) });
        }
        for (int i = 0; i < previewRowCount; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(dataRowHeight) });
        }

        // Add header row
        if (showHeaders)
        {
            for (int col = 0; col < columnCount; col++)
            {
                var headerCell = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(headerBgColor)),
                    BorderBrush = showGridLines ? new SolidColorBrush(Color.Parse(gridLineColor)) : null,
                    BorderThickness = showGridLines ? new Thickness(0, 0, 1, 1) : new Thickness(0),
                    Padding = new Thickness(4, 0),
                    Child = new TextBlock
                    {
                        Text = col < columns.Count ? columns[col] : $"Col {col + 1}",
                        FontSize = fontSize,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse(headerTextColor)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    }
                };
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, col);
                grid.Children.Add(headerCell);
            }
        }

        // Add data rows with real data
        var startRow = showHeaders ? 1 : 0;
        for (int dataIndex = 0; dataIndex < previewRowCount; dataIndex++)
        {
            var rowData = tableData[dataIndex];
            var rowIndex = startRow + dataIndex;
            var isAlternate = dataIndex % 2 == 1;
            var rowBgColor = alternateRowColors && isAlternate ? alternateRowColor : baseRowColor;

            for (int col = 0; col < columnCount; col++)
            {
                var cellText = col < rowData.Count ? rowData[col] : "";
                var dataCell = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(rowBgColor)),
                    BorderBrush = showGridLines ? new SolidColorBrush(Color.Parse(gridLineColor)) : null,
                    BorderThickness = showGridLines ? new Thickness(0, 0, 1, 1) : new Thickness(0),
                    Padding = new Thickness(4, 0),
                    Child = new TextBlock
                    {
                        Text = cellText,
                        FontSize = fontSize,
                        Foreground = new SolidColorBrush(Color.Parse(dataTextColor)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                    }
                };
                Grid.SetRow(dataCell, rowIndex);
                Grid.SetColumn(dataCell, col);
                grid.Children.Add(dataCell);
            }
        }

        return new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse(gridLineColor)),
            BorderThickness = new Thickness(1),
            Child = grid
        };
    }

    private static List<List<string>> GetTableData(TableReportElement? element, Core.Data.CompanyData companyData, List<string> columns)
    {
        var result = new List<List<string>>();
        var transactionType = element?.TransactionType ?? TransactionType.Both;

        // Build a list of transaction records (date, id, company, product, qty, unitPrice, total, status, accountant, shipping)
        var transactions = new List<(DateTime Date, string Id, string Company, string Product, decimal Qty, decimal UnitPrice, decimal Total, string Status, string Accountant, decimal Shipping)>();

        // Get sales
        if (transactionType == TransactionType.Sales || transactionType == TransactionType.Both)
        {
            foreach (var sale in companyData.Sales)
            {
                var customerName = companyData.Customers.FirstOrDefault(c => c.Id == sale.CustomerId)?.Name ?? "N/A";
                var productName = sale.LineItems.FirstOrDefault()?.ProductName ?? sale.Description;
                var accountantName = companyData.Accountants.FirstOrDefault(a => a.Id == sale.AccountantId)?.Name ?? "";
                transactions.Add((sale.Date, sale.Id, customerName, productName, sale.Quantity, sale.UnitPrice, sale.Total, sale.PaymentStatus, accountantName, sale.ShippingCost));
            }
        }

        // Get purchases
        if (transactionType == TransactionType.Purchases || transactionType == TransactionType.Both)
        {
            foreach (var purchase in companyData.Purchases)
            {
                var supplierName = companyData.Suppliers.FirstOrDefault(s => s.Id == purchase.SupplierId)?.Name ?? "N/A";
                var productName = purchase.LineItems.FirstOrDefault()?.ProductName ?? purchase.Description;
                var accountantName = companyData.Accountants.FirstOrDefault(a => a.Id == purchase.AccountantId)?.Name ?? "";
                transactions.Add((purchase.Date, purchase.Id, supplierName, productName, purchase.Quantity, purchase.UnitPrice, purchase.Total, purchase.PaymentStatus, accountantName, purchase.ShippingCost));
            }
        }

        // Sort transactions
        var sortOrder = element?.SortOrder ?? TableSortOrder.DateDescending;
        transactions = sortOrder switch
        {
            TableSortOrder.DateAscending => transactions.OrderBy(t => t.Date).ToList(),
            TableSortOrder.DateDescending => transactions.OrderByDescending(t => t.Date).ToList(),
            TableSortOrder.AmountAscending => transactions.OrderBy(t => t.Total).ToList(),
            TableSortOrder.AmountDescending => transactions.OrderByDescending(t => t.Total).ToList(),
            _ => transactions.OrderByDescending(t => t.Date).ToList()
        };

        // Convert to row data based on visible columns
        foreach (var trans in transactions)
        {
            var row = new List<string>();
            foreach (var col in columns)
            {
                var value = col switch
                {
                    "Date" => trans.Date.ToString("MM/dd/yyyy"),
                    "ID" => trans.Id,
                    "Company" => trans.Company,
                    "Product" => trans.Product,
                    "Qty" => trans.Qty.ToString("N0"),
                    "Unit Price" => trans.UnitPrice.ToString("C2"),
                    "Total" => trans.Total.ToString("C2"),
                    "Status" => trans.Status,
                    "Accountant" => trans.Accountant,
                    "Shipping" => trans.Shipping.ToString("C2"),
                    _ => ""
                };
                row.Add(value);
            }
            result.Add(row);
        }

        return result;
    }

    private static List<string> GetVisibleTableColumns(TableReportElement? table)
    {
        var columns = new List<string>();
        if (table == null) return ["Date", "Description", "Amount"];

        if (table.ShowDateColumn) columns.Add("Date");
        if (table.ShowTransactionIdColumn) columns.Add("ID");
        if (table.ShowCompanyColumn) columns.Add("Company");
        if (table.ShowProductColumn) columns.Add("Product");
        if (table.ShowQuantityColumn) columns.Add("Qty");
        if (table.ShowUnitPriceColumn) columns.Add("Unit Price");
        if (table.ShowTotalColumn) columns.Add("Total");
        if (table.ShowStatusColumn) columns.Add("Status");
        if (table.ShowAccountantColumn) columns.Add("Accountant");
        if (table.ShowShippingColumn) columns.Add("Shipping");

        return columns.Count > 0 ? columns : ["Date", "Description", "Amount"];
    }

    private static Control CreateLabelPreview(LabelReportElement? element)
    {
        var text = element?.Text ?? "Label";
        var fontFamily = element?.FontFamily ?? "Segoe UI";
        var fontSize = element?.FontSize ?? 14;
        var isBold = element?.IsBold ?? false;
        var isItalic = element?.IsItalic ?? false;
        var isUnderline = element?.IsUnderline ?? false;
        var textColor = element?.TextColor ?? "#000000";
        var hAlign = element?.HorizontalAlignment ?? HorizontalTextAlignment.Left;
        var vAlign = element?.VerticalAlignment ?? VerticalTextAlignment.Top;

        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(fontFamily),
            FontSize = fontSize,
            FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
            FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal,
            TextDecorations = isUnderline ? TextDecorations.Underline : null,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse(textColor)),
            HorizontalAlignment = hAlign switch
            {
                HorizontalTextAlignment.Center => Avalonia.Layout.HorizontalAlignment.Center,
                HorizontalTextAlignment.Right => Avalonia.Layout.HorizontalAlignment.Right,
                _ => Avalonia.Layout.HorizontalAlignment.Left
            },
            VerticalAlignment = vAlign switch
            {
                VerticalTextAlignment.Center => Avalonia.Layout.VerticalAlignment.Center,
                VerticalTextAlignment.Bottom => Avalonia.Layout.VerticalAlignment.Bottom,
                _ => Avalonia.Layout.VerticalAlignment.Top
            }
        };

        return new Border
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(4),
            Child = textBlock
        };
    }

    private static Control CreateImagePreview(ImageReportElement? element)
    {
        var bgColor = element?.BackgroundColor ?? "#F0F0F0";
        var borderColor = element?.BorderColor ?? "#00FFFFFF";
        var borderThickness = element?.BorderThickness ?? 0;
        var opacity = (element?.Opacity ?? 100) / 100.0;

        // Check if background is transparent
        bool isTransparentBg = bgColor == "#00FFFFFF";
        IBrush background = !isTransparentBg
            ? new SolidColorBrush(Color.Parse(bgColor))
            : Brushes.Transparent;

        Control content;

        if (!string.IsNullOrEmpty(element?.ImagePath))
        {
            try
            {
                // Try to load the image
                var imagePath = element.ImagePath;
                if (System.IO.File.Exists(imagePath))
                {
                    content = new Image
                    {
                        Source = new Avalonia.Media.Imaging.Bitmap(imagePath),
                        Stretch = element.ScaleMode switch
                        {
                            ImageScaleMode.Stretch => Stretch.Fill,
                            ImageScaleMode.Fit => Stretch.Uniform,
                            ImageScaleMode.Fill => Stretch.UniformToFill,
                            ImageScaleMode.Center => Stretch.None,
                            _ => Stretch.Uniform
                        }
                        // Opacity is applied to the parent Border
                    };
                }
                else
                {
                    // For placeholder, always use a visible background
                    content = CreateImagePlaceholder("Image not found");
                }
            }
            catch
            {
                // For placeholder, always use a visible background
                content = CreateImagePlaceholder("Error loading image");
            }
        }
        else
        {
            // For placeholder, always use a visible background
            content = CreateImagePlaceholder("No image selected");
        }

        IBrush? border = borderThickness > 0 && borderColor != "#00FFFFFF"
            ? new SolidColorBrush(Color.Parse(borderColor))
            : null;

        // Use user's background color if set, otherwise use default gray for placeholders
        bool hasActualImage = !string.IsNullOrEmpty(element?.ImagePath) && System.IO.File.Exists(element.ImagePath);
        IBrush effectiveBackground;
        if (hasActualImage)
        {
            effectiveBackground = background;
        }
        else
        {
            // For placeholders, use user's background if set (not transparent), otherwise default gray
            effectiveBackground = !isTransparentBg ? background : new SolidColorBrush(Color.Parse("#F0F0F0"));
        }

        return new Border
        {
            Background = effectiveBackground,
            BorderBrush = border,
            BorderThickness = new Thickness(borderThickness),
            Child = content,
            Opacity = opacity // Apply opacity to entire element including background
        };
    }

    private static Control CreateImagePlaceholder(string message)
    {
        return new Border
        {
            Background = Brushes.Transparent,
            Child = new TextBlock
            {
                Text = message,
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };
    }

    private static Control CreateDateRangePreview(DateRangeReportElement? element)
    {
        var dateFormat = element?.DateFormat ?? "MMM dd, yyyy";
        var fontFamily = element?.FontFamily ?? "Segoe UI";
        var fontSize = element?.FontSize ?? 12;
        var textColor = element?.TextColor ?? "#000000";
        var isBold = element?.IsBold ?? false;
        var isItalic = element?.IsItalic ?? false;
        var isUnderline = element?.IsUnderline ?? false;
        var hAlign = element?.HorizontalAlignment ?? HorizontalTextAlignment.Center;
        var vAlign = element?.VerticalAlignment ?? VerticalTextAlignment.Center;

        // Create sample date range text
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        var text = $"Period: {startDate.ToString(dateFormat)} to {endDate.ToString(dateFormat)}";

        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(fontFamily),
            FontSize = fontSize,
            FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
            FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal,
            TextDecorations = isUnderline ? TextDecorations.Underline : null,
            Foreground = new SolidColorBrush(Color.Parse(textColor)),
            HorizontalAlignment = hAlign switch
            {
                HorizontalTextAlignment.Center => Avalonia.Layout.HorizontalAlignment.Center,
                HorizontalTextAlignment.Right => Avalonia.Layout.HorizontalAlignment.Right,
                _ => Avalonia.Layout.HorizontalAlignment.Left
            },
            VerticalAlignment = vAlign switch
            {
                VerticalTextAlignment.Center => Avalonia.Layout.VerticalAlignment.Center,
                VerticalTextAlignment.Bottom => Avalonia.Layout.VerticalAlignment.Bottom,
                _ => Avalonia.Layout.VerticalAlignment.Top
            }
        };

        return new Border
        {
            Background = Brushes.Transparent,
            Child = textBlock
        };
    }

    private Control CreateSummaryPreview(SummaryReportElement? element)
    {
        var bgColor = element?.BackgroundColor ?? "#F5F5F5";
        var borderColor = element?.BorderColor ?? "#CCCCCC";
        var borderThickness = element?.BorderThickness ?? 1;
        var fontSize = element?.FontSize ?? 12;
        var hAlign = element?.HorizontalAlignment ?? HorizontalTextAlignment.Left;
        var vAlign = element?.VerticalAlignment ?? VerticalTextAlignment.Top;
        var transactionType = element?.TransactionType ?? TransactionType.Revenue;

        // Get real data from company
        var companyData = App.CompanyManager?.CompanyData;
        var hasData = companyData != null;

        // Calculate real values if data is available
        decimal total = 0;
        int count = 0;
        if (transactionType == TransactionType.Expenses)
        {
            var purchases = companyData?.Purchases ?? [];
            total = purchases.Sum(t => t.Total);
            count = purchases.Count;
        }
        else
        {
            var sales = companyData?.Sales ?? [];
            total = sales.Sum(t => t.Total);
            count = sales.Count;
        }
        var average = count > 0 ? total / count : 0;

        var lines = new List<string>();

        if (!hasData || count == 0)
        {
            lines.Add("No data available");
        }
        else
        {
            if (element?.ShowTotalSales ?? true)
            {
                var label = transactionType == TransactionType.Expenses ? "Total Expenses" : "Total Revenue";
                lines.Add($"{label}: ${total:N2}");
            }
            if (element?.ShowTotalTransactions ?? true)
            {
                lines.Add($"Transactions: {count}");
            }
            if (element?.ShowAverageValue ?? true)
            {
                lines.Add($"Average Value: ${average:N2}");
            }
            if (element?.ShowGrowthRate ?? true)
            {
                lines.Add("Growth Rate: N/A"); // Would need historical data
            }
        }

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(10),
            HorizontalAlignment = hAlign switch
            {
                HorizontalTextAlignment.Center => Avalonia.Layout.HorizontalAlignment.Center,
                HorizontalTextAlignment.Right => Avalonia.Layout.HorizontalAlignment.Right,
                _ => Avalonia.Layout.HorizontalAlignment.Left
            },
            VerticalAlignment = vAlign switch
            {
                VerticalTextAlignment.Center => Avalonia.Layout.VerticalAlignment.Center,
                VerticalTextAlignment.Bottom => Avalonia.Layout.VerticalAlignment.Bottom,
                _ => Avalonia.Layout.VerticalAlignment.Top
            }
        };

        foreach (var line in lines)
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = line,
                FontSize = fontSize,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 2),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap
            });
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse(bgColor)),
            BorderBrush = borderThickness > 0 ? new SolidColorBrush(Color.Parse(borderColor)) : null,
            BorderThickness = new Thickness(borderThickness),
            ClipToBounds = true,
            Child = stackPanel
        };
    }

    /// <summary>
    /// Refreshes the content of a specific element.
    /// </summary>
    public void RefreshElementContent(ReportElementBase element)
    {
        if (_elementControlMap.TryGetValue(element.Id, out var control))
        {
            control.ElementContent = CreateElementContent(element);
        }
    }

    /// <summary>
    /// Refreshes all element controls from their elements.
    /// </summary>
    /// <summary>
    /// Refreshes the page settings (size, orientation, margins, background color).
    /// Call this when page settings are changed outside of Configuration property assignment.
    /// </summary>
    public void RefreshPageSettings()
    {
        if (Configuration == null) return;

        // Update page dimensions
        var (width, height) = PageDimensions.GetDimensions(
            Configuration.PageSize,
            Configuration.PageOrientation);
        _pageWidth = width;
        _pageHeight = height;

        // Update page background
        if (Color.TryParse(Configuration.BackgroundColor, out var color))
        {
            PageBackgroundBrush = new SolidColorBrush(color);
        }

        // Update layout
        UpdateLayout();
    }

    public void RefreshAllElements()
    {
        foreach (var control in _elementControls)
        {
            control.SyncFromElement();
            if (control.Element != null)
            {
                control.ElementContent = CreateElementContent(control.Element);
            }
        }
    }

    /// <summary>
    /// Synchronizes the canvas with the Configuration's elements.
    /// Adds new elements and removes deleted ones without clearing everything.
    /// </summary>
    public void SyncElements()
    {
        if (Configuration == null) return;

        var existingIds = _elementControlMap.Keys.ToHashSet();
        var configIds = Configuration.Elements.Select(e => e.Id).ToHashSet();

        // Add new elements that are in Configuration but not on canvas
        foreach (var element in Configuration.Elements)
        {
            if (!existingIds.Contains(element.Id))
            {
                var control = AddElementControl(element);
                // Select the newly added element
                SelectElement(element, false);
            }
        }

        // Remove elements that are on canvas but not in Configuration
        foreach (var id in existingIds.Except(configIds).ToList())
        {
            if (_elementControlMap.TryGetValue(id, out var control))
            {
                if (control.Element != null)
                {
                    _selectedElements.Remove(control.Element);
                }
                RemoveElementControl(control);
            }
        }
    }

    #endregion

    #region Selection

    /// <summary>
    /// Selects an element.
    /// </summary>
    public void SelectElement(ReportElementBase element, bool addToSelection)
    {
        if (!addToSelection)
        {
            ClearSelection();
        }

        if (!_selectedElements.Contains(element))
        {
            _selectedElements.Add(element);

            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                control.IsSelected = true;
            }
        }

        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectedElements));
    }

    /// <summary>
    /// Deselects an element.
    /// </summary>
    public void DeselectElement(ReportElementBase element)
    {
        if (_selectedElements.Remove(element))
        {
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                control.IsSelected = false;
            }

            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectedElements));
        }
    }

    /// <summary>
    /// Clears all selection.
    /// </summary>
    public void ClearSelection()
    {
        foreach (var element in _selectedElements)
        {
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                control.IsSelected = false;
            }
        }

        _selectedElements.Clear();
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectedElements));
    }

    /// <summary>
    /// Selects all elements.
    /// </summary>
    public void SelectAll()
    {
        _selectedElements.Clear();

        foreach (var control in _elementControls)
        {
            control.IsSelected = true;
            if (control.Element != null)
            {
                _selectedElements.Add(control.Element);
            }
        }

        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectedElements));
    }

    #endregion

    #region Element Event Handlers

    private void OnElementSelected(object? sender, ElementSelectedEventArgs e)
    {
        if (e.Element == null) return;

        if (e.IsMultiSelect)
        {
            if (_selectedElements.Contains(e.Element))
            {
                DeselectElement(e.Element);
            }
            else
            {
                SelectElement(e.Element, true);
            }
        }
        else
        {
            SelectElement(e.Element, false);
        }
    }

    private Dictionary<string, (Point Position, Size Size)>? _dragStartStates;

    private void OnElementDragStarted(object? sender, ElementDragEventArgs e)
    {
        // Capture initial states for all selected elements
        _dragStartStates = [];
        foreach (var element in _selectedElements)
        {
            _dragStartStates[element.Id] = (new Point(element.X, element.Y), new Size(element.Width, element.Height));
        }
    }

    private void OnElementDragEnded(object? sender, ElementDragEventArgs e)
    {
        if (e.WasCancelled || _dragStartStates == null) return;

        // Record batch move action for undo
        var oldBounds = new Dictionary<string, (double X, double Y, double Width, double Height)>();
        var newBounds = new Dictionary<string, (double X, double Y, double Width, double Height)>();

        foreach (var element in _selectedElements)
        {
            if (_dragStartStates.TryGetValue(element.Id, out var startState))
            {
                var newPosition = new Point(element.X, element.Y);
                if (startState.Position != newPosition)
                {
                    oldBounds[element.Id] = (startState.Position.X, startState.Position.Y, startState.Size.Width, startState.Size.Height);
                    newBounds[element.Id] = (element.X, element.Y, element.Width, element.Height);
                }
            }
        }

        if (oldBounds.Count > 0 && Configuration != null)
        {
            UndoRedoManager?.RecordAction(new BatchMoveResizeAction(Configuration, oldBounds, newBounds, "Move elements"));
            Configuration.HasManualChartLayout = true;
        }

        _dragStartStates = null;
        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    private void OnElementPositionChanged(object? sender, ElementPositionChangedEventArgs e)
    {
        // Update other selected elements if this is part of a multi-selection drag
        if (_selectedElements.Count > 1 && e.Element != null && !e.IsComplete)
        {
            var delta = e.NewPosition - e.OldPosition;

            foreach (var element in _selectedElements.Where(el => el.Id != e.Element.Id))
            {
                element.X += delta.X;
                element.Y += delta.Y;

                if (_elementControlMap.TryGetValue(element.Id, out var control))
                {
                    Canvas.SetLeft(control, element.X);
                    Canvas.SetTop(control, element.Y);
                }
            }
        }
    }

    private void OnElementSizeChanged(object? sender, ElementSizeChangedEventArgs e)
    {
        if (e.IsComplete && e.Element != null && Configuration != null)
        {
            UndoRedoManager?.RecordAction(new MoveResizeElementAction(
                Configuration,
                e.Element.Id,
                (e.Element.X, e.Element.Y, e.OldSize.Width, e.OldSize.Height),
                (e.Element.X, e.Element.Y, e.NewSize.Width, e.NewSize.Height),
                true));

            Configuration.HasManualChartLayout = true;
            ElementsModified?.Invoke(this, new ElementsModifiedEventArgs([e.Element]));
        }
    }

    private void OnElementDeleteRequested(object? sender, ElementDeleteRequestedEventArgs e)
    {
        RemoveSelectedElements();
    }

    #endregion

    #region Canvas Pointer Events

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsRightButtonPressed)
        {
            // Start panning with right mouse button
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartOffset = new Vector(_scrollViewer?.Offset.X ?? 0, _scrollViewer?.Offset.Y ?? 0);
            e.Pointer.Capture(this);
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeAll);
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            // Check if we clicked on an element
            var hitElement = GetElementAtPoint(e.GetPosition(_elementsCanvas));

            if (hitElement == null)
            {
                // Start multi-select rectangle
                ClearSelection();
                _isMultiSelecting = true;
                _selectionStartPoint = e.GetPosition(_elementsCanvas);
                e.Pointer.Capture(this);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning && _scrollViewer != null)
        {
            var currentPoint = e.GetPosition(this);
            var delta = _panStartPoint - currentPoint;

            // Calculate desired offset
            var desiredX = _panStartOffset.X + delta.X;
            var desiredY = _panStartOffset.Y + delta.Y;

            // Calculate bounds
            var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
            var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);

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
            _scrollViewer.Offset = new Vector(clampedX, clampedY);

            // Apply overscroll visual effect
            _overscroll = new Vector(overscrollX, overscrollY);
            ApplyOverscrollTransform();

            e.Handled = true;
        }
        else if (_isMultiSelecting && _selectionRectangle != null)
        {
            var currentPoint = e.GetPosition(_elementsCanvas);
            UpdateSelectionRectangle(_selectionStartPoint, currentPoint);

            // Highlight elements that would be selected in real-time
            if (_selectionRectangle.IsVisible)
            {
                var selectionRect = new Rect(
                    Canvas.GetLeft(_selectionRectangle),
                    Canvas.GetTop(_selectionRectangle),
                    _selectionRectangle.Width,
                    _selectionRectangle.Height);

                foreach (var control in _elementControls)
                {
                    if (control.Element == null) continue;

                    var elementRect = new Rect(
                        control.Element.X,
                        control.Element.Y,
                        control.Element.Width,
                        control.Element.Height);

                    // Select elements that intersect with the selection rectangle
                    var shouldBeSelected = selectionRect.Intersects(elementRect);
                    if (shouldBeSelected != control.IsSelected)
                    {
                        control.IsSelected = shouldBeSelected;
                    }
                }
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            Cursor = Avalonia.Input.Cursor.Default;

            // Animate overscroll back to zero (rubberband snap-back)
            if (_overscroll.X != 0 || _overscroll.Y != 0)
            {
                AnimateOverscrollSnapBack();
            }

            e.Handled = true;
        }
        else if (_isMultiSelecting)
        {
            _isMultiSelecting = false;
            e.Pointer.Capture(null);

            // Select all elements within the rectangle
            if (_selectionRectangle?.IsVisible == true)
            {
                var rect = new Rect(
                    Canvas.GetLeft(_selectionRectangle),
                    Canvas.GetTop(_selectionRectangle),
                    _selectionRectangle.Width,
                    _selectionRectangle.Height);

                foreach (var control in _elementControls)
                {
                    if (control.Element == null) continue;

                    var elementRect = new Rect(control.Element.X, control.Element.Y, control.Element.Width, control.Element.Height);
                    if (rect.Intersects(elementRect))
                    {
                        SelectElement(control.Element, true);
                    }
                }

                _selectionRectangle.IsVisible = false;
            }
        }
    }

    private void UpdateSelectionRectangle(Point start, Point end)
    {
        if (_selectionRectangle == null) return;

        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;
        _selectionRectangle.IsVisible = width > 5 || height > 5;
    }

    private CanvasElementControl? GetElementAtPoint(Point? point)
    {
        if (point == null || _elementsCanvas == null) return null;

        foreach (var control in _elementControls.OrderByDescending(c => c.ZIndex))
        {
            if (control.Element == null) continue;

            var rect = new Rect(control.Element.X, control.Element.Y, control.Element.Width, control.Element.Height);
            if (rect.Contains(point.Value))
            {
                return control;
            }
        }

        return null;
    }

    #endregion

    #region Keyboard Shortcuts

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.A when ctrl:
                SelectAll();
                e.Handled = true;
                break;

            case Key.C when ctrl:
                CopySelectedElements();
                e.Handled = true;
                break;

            case Key.X when ctrl:
                CutSelectedElements();
                e.Handled = true;
                break;

            case Key.V when ctrl:
                PasteElements();
                e.Handled = true;
                break;

            case Key.Z when ctrl && !shift:
                Undo();
                e.Handled = true;
                break;

            case Key.Z when ctrl && shift:
            case Key.Y when ctrl:
                Redo();
                e.Handled = true;
                break;

            case Key.Delete:
            case Key.Back:
                RemoveSelectedElements();
                e.Handled = true;
                break;

            case Key.Escape:
                ClearSelection();
                e.Handled = true;
                break;

            case Key.G when ctrl:
                ShowGrid = !ShowGrid;
                e.Handled = true;
                break;
        }
    }

    #endregion

    #region Clipboard Operations

    /// <summary>
    /// Copies selected elements to clipboard.
    /// </summary>
    public void CopySelectedElements()
    {
        if (_selectedElements.Count == 0) return;

        _clipboardElements = _selectedElements.Select(e => e.Clone()).ToList();
    }

    /// <summary>
    /// Cuts selected elements to clipboard.
    /// </summary>
    public void CutSelectedElements()
    {
        CopySelectedElements();
        RemoveSelectedElements();
    }

    /// <summary>
    /// Pastes elements from clipboard.
    /// </summary>
    public void PasteElements()
    {
        if (_clipboardElements == null || _clipboardElements.Count == 0 || Configuration == null) return;

        ClearSelection();

        const double offset = 20;

        foreach (var element in _clipboardElements)
        {
            var newElement = element.Clone();
            newElement.X += offset;
            newElement.Y += offset;

            Configuration.AddElement(newElement);
            AddElementControl(newElement);
            SelectElement(newElement, true);
        }

        // Update clipboard with offset for subsequent pastes
        foreach (var element in _clipboardElements)
        {
            element.X += offset;
            element.Y += offset;
        }
    }

    #endregion

    #region Undo/Redo

    /// <summary>
    /// Undoes the last action.
    /// </summary>
    public void Undo()
    {
        UndoRedoManager?.Undo();
        RefreshAllElements();
    }

    /// <summary>
    /// Redoes the last undone action.
    /// </summary>
    public void Redo()
    {
        UndoRedoManager?.Redo();
        RefreshAllElements();
    }

    #endregion

    #region Z-Order

    /// <summary>
    /// Brings selected elements to front.
    /// </summary>
    public void BringToFront()
    {
        if (Configuration == null || _selectedElements.Count == 0) return;

        var maxZOrder = Configuration.Elements.Max(e => e.ZOrder);
        foreach (var element in _selectedElements)
        {
            element.ZOrder = ++maxZOrder;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                control.ZIndex = element.ZOrder;
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Sends selected elements to back.
    /// </summary>
    public void SendToBack()
    {
        if (Configuration == null || _selectedElements.Count == 0) return;

        var minZOrder = Configuration.Elements.Min(e => e.ZOrder);
        foreach (var element in _selectedElements)
        {
            element.ZOrder = --minZOrder;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                control.ZIndex = element.ZOrder;
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    #endregion

    #region Alignment

    /// <summary>
    /// Aligns selected elements to the left.
    /// </summary>
    public void AlignLeft()
    {
        if (_selectedElements.Count < 2) return;

        var minX = _selectedElements.Min(e => e.X);
        foreach (var element in _selectedElements)
        {
            element.X = minX;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetLeft(control, element.X);
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Aligns selected elements to the right.
    /// </summary>
    public void AlignRight()
    {
        if (_selectedElements.Count < 2) return;

        var maxRight = _selectedElements.Max(e => e.X + e.Width);
        foreach (var element in _selectedElements)
        {
            element.X = maxRight - element.Width;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetLeft(control, element.X);
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Aligns selected elements to the top.
    /// </summary>
    public void AlignTop()
    {
        if (_selectedElements.Count < 2) return;

        var minY = _selectedElements.Min(e => e.Y);
        foreach (var element in _selectedElements)
        {
            element.Y = minY;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetTop(control, element.Y);
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Aligns selected elements to the bottom.
    /// </summary>
    public void AlignBottom()
    {
        if (_selectedElements.Count < 2) return;

        var maxBottom = _selectedElements.Max(e => e.Y + e.Height);
        foreach (var element in _selectedElements)
        {
            element.Y = maxBottom - element.Height;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetTop(control, element.Y);
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Centers selected elements horizontally.
    /// </summary>
    public void CenterHorizontally()
    {
        if (_selectedElements.Count < 2) return;

        var minX = _selectedElements.Min(e => e.X);
        var maxRight = _selectedElements.Max(e => e.X + e.Width);
        var centerX = (minX + maxRight) / 2;

        foreach (var element in _selectedElements)
        {
            element.X = centerX - element.Width / 2;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetLeft(control, element.X);
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Centers selected elements vertically.
    /// </summary>
    public void CenterVertically()
    {
        if (_selectedElements.Count < 2) return;

        var minY = _selectedElements.Min(e => e.Y);
        var maxBottom = _selectedElements.Max(e => e.Y + e.Height);
        var centerY = (minY + maxBottom) / 2;

        foreach (var element in _selectedElements)
        {
            element.Y = centerY - element.Height / 2;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetTop(control, element.Y);
            }
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    #endregion

    #region Distribution

    /// <summary>
    /// Distributes selected elements horizontally with equal spacing.
    /// </summary>
    public void DistributeHorizontally()
    {
        if (_selectedElements.Count < 3) return;

        var ordered = _selectedElements.OrderBy(e => e.X).ToList();
        var totalWidth = ordered.Sum(e => e.Width);
        var minX = ordered.First().X;
        var maxRight = ordered.Last().X + ordered.Last().Width;
        var availableSpace = maxRight - minX - totalWidth;
        var spacing = availableSpace / (ordered.Count - 1);

        var currentX = minX;
        foreach (var element in ordered)
        {
            element.X = currentX;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetLeft(control, element.X);
            }
            currentX += element.Width + spacing;
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    /// <summary>
    /// Distributes selected elements vertically with equal spacing.
    /// </summary>
    public void DistributeVertically()
    {
        if (_selectedElements.Count < 3) return;

        var ordered = _selectedElements.OrderBy(e => e.Y).ToList();
        var totalHeight = ordered.Sum(e => e.Height);
        var minY = ordered.First().Y;
        var maxBottom = ordered.Last().Y + ordered.Last().Height;
        var availableSpace = maxBottom - minY - totalHeight;
        var spacing = availableSpace / (ordered.Count - 1);

        var currentY = minY;
        foreach (var element in ordered)
        {
            element.Y = currentY;
            if (_elementControlMap.TryGetValue(element.Id, out var control))
            {
                Canvas.SetTop(control, element.Y);
            }
            currentY += element.Height + spacing;
        }

        ElementsModified?.Invoke(this, new ElementsModifiedEventArgs(_selectedElements.ToList()));
    }

    #endregion
}

#region Event Args

/// <summary>
/// Event args for selection change events.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public IReadOnlyList<ReportElementBase> SelectedElements { get; }

    public SelectionChangedEventArgs(IReadOnlyList<ReportElementBase> selectedElements)
    {
        SelectedElements = selectedElements;
    }
}

/// <summary>
/// Event args for element added events.
/// </summary>
public class ElementAddedEventArgs : EventArgs
{
    public ReportElementBase Element { get; }

    public ElementAddedEventArgs(ReportElementBase element)
    {
        Element = element;
    }
}

/// <summary>
/// Event args for element removed events.
/// </summary>
public class ElementRemovedEventArgs : EventArgs
{
    public ReportElementBase Element { get; }

    public ElementRemovedEventArgs(ReportElementBase element)
    {
        Element = element;
    }
}

/// <summary>
/// Event args for elements modified events.
/// </summary>
public class ElementsModifiedEventArgs : EventArgs
{
    public IReadOnlyList<ReportElementBase> ModifiedElements { get; }

    public ElementsModifiedEventArgs(IReadOnlyList<ReportElementBase> modifiedElements)
    {
        ModifiedElements = modifiedElements;
    }
}

#endregion
