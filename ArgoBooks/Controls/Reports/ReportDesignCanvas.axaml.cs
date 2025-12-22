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
        set => SetValue(ZoomLevelProperty, Math.Clamp(value, 0.25, 4.0));
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

        _elementsCanvas = this.FindControl<Canvas>("ElementsCanvas");
        _gridLinesCanvas = this.FindControl<Canvas>("GridLinesCanvas");
        _marginGuide = this.FindControl<Rectangle>("MarginGuide");
        _headerArea = this.FindControl<Border>("HeaderArea");
        _footerArea = this.FindControl<Border>("FooterArea");
        _selectionRectangle = this.FindControl<Rectangle>("SelectionRectangle");
        _pageBackground = this.FindControl<Border>("PageBackground");
        _dropIndicator = this.FindControl<Border>("DropIndicator");

        UpdateLayout();

        // If Configuration was set before template was applied, process it now
        if (Configuration != null && _elementsCanvas != null && _elementControls.Count == 0)
        {
            OnConfigurationChanged();
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
        UpdateLayout();
        DrawGrid();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
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
        if (_pageBackground?.Parent is Border zoomContainer)
        {
            zoomContainer.RenderTransform = new ScaleTransform(ZoomLevel, ZoomLevel);
            zoomContainer.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }
    }

    /// <summary>
    /// Zooms in by 25%.
    /// </summary>
    public void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + 0.25, 4.0);
    }

    /// <summary>
    /// Zooms out by 25%.
    /// </summary>
    public void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);
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
        if (Parent is not Control parent) return;

        var scaleX = (parent.Bounds.Width - 80) / _pageWidth;
        var scaleY = (parent.Bounds.Height - 80) / _pageHeight;
        ZoomLevel = Math.Min(scaleX, scaleY);
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

    private CanvasElementControl AddElementControl(ReportElementBase element)
    {
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

        // Set content based on element type
        control.ElementContent = CreateElementContent(element);

        _elementsCanvas?.Children.Add(control);
        _elementControls.Add(control);
        _elementControlMap[element.Id] = control;

        return control;
    }

    private void RemoveElementControl(CanvasElementControl control)
    {
        control.ElementSelected -= OnElementSelected;
        control.PositionChanged -= OnElementPositionChanged;
        control.SizeChanged -= OnElementSizeChanged;
        control.DragStarted -= OnElementDragStarted;
        control.DragEnded -= OnElementDragEnded;
        control.DeleteRequested -= OnElementDeleteRequested;

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

    private static Control CreateElementContent(ReportElementBase element)
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
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E3F2FD")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new PathIcon
                    {
                        Width = 32,
                        Height = 32,
                        Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
                        Data = PathGeometry.Parse("M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z")
                    },
                    new TextBlock
                    {
                        Text = element?.ChartType.ToString() ?? "Chart",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#1565C0")),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
            }
        };
    }

    private static Control CreateTablePreview(TableReportElement? element)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E8F5E9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#4CAF50")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new PathIcon
                    {
                        Width = 32,
                        Height = 32,
                        Foreground = new SolidColorBrush(Color.Parse("#4CAF50")),
                        Data = PathGeometry.Parse("M5,4H19A2,2 0 0,1 21,6V18A2,2 0 0,1 19,20H5A2,2 0 0,1 3,18V6A2,2 0 0,1 5,4M5,8V12H11V8H5M13,8V12H19V8H13M5,14V18H11V14H5M13,14V18H19V14H13Z")
                    },
                    new TextBlock
                    {
                        Text = "Data Table",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#2E7D32")),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
            }
        };
    }

    private static Control CreateLabelPreview(LabelReportElement? element)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FFF3E0")),
            BorderBrush = new SolidColorBrush(Color.Parse("#FF9800")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Child = new TextBlock
            {
                Text = element?.Text ?? "Label",
                FontSize = element?.FontSize ?? 14,
                FontWeight = element?.IsBold == true ? FontWeight.Bold : FontWeight.Normal,
                FontStyle = element?.IsItalic == true ? FontStyle.Italic : FontStyle.Normal,
                TextWrapping = TextWrapping.Wrap,
                Foreground = element?.TextColor != null
                    ? new SolidColorBrush(Color.Parse(element.TextColor))
                    : Brushes.Black
            }
        };
    }

    private static Control CreateImagePreview(ImageReportElement? element)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F3E5F5")),
            BorderBrush = new SolidColorBrush(Color.Parse("#9C27B0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new PathIcon
                    {
                        Width = 32,
                        Height = 32,
                        Foreground = new SolidColorBrush(Color.Parse("#9C27B0")),
                        Data = PathGeometry.Parse("M21,17H7V3H21M21,1H7A2,2 0 0,0 5,3V17A2,2 0 0,0 7,19H21A2,2 0 0,0 23,17V3A2,2 0 0,0 21,1M3,5H1V21A2,2 0 0,0 3,23H19V21H3M15.96,10.29L13.21,13.83L11.25,11.47L8.5,15H19.5L15.96,10.29Z")
                    },
                    new TextBlock
                    {
                        Text = string.IsNullOrEmpty(element?.ImagePath) ? "No Image" : "Image",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#7B1FA2")),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
            }
        };
    }

    private static Control CreateDateRangePreview(DateRangeReportElement? element)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#E0F7FA")),
            BorderBrush = new SolidColorBrush(Color.Parse("#00BCD4")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Child = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new PathIcon
                    {
                        Width = 20,
                        Height = 20,
                        Foreground = new SolidColorBrush(Color.Parse("#00BCD4")),
                        Data = PathGeometry.Parse("M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1")
                    },
                    new TextBlock
                    {
                        Text = element?.DateFormat ?? "Date Range",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#00838F")),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    }
                }
            }
        };
    }

    private static Control CreateSummaryPreview(SummaryReportElement? element)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#ECEFF1")),
            BorderBrush = new SolidColorBrush(Color.Parse("#607D8B")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Child = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new PathIcon
                    {
                        Width = 32,
                        Height = 32,
                        Foreground = new SolidColorBrush(Color.Parse("#607D8B")),
                        Data = PathGeometry.Parse("M14,17H7V15H14M17,13H7V11H17M17,9H7V7H17M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3Z")
                    },
                    new TextBlock
                    {
                        Text = "Summary",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#455A64")),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
            }
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

        if (point.Properties.IsLeftButtonPressed)
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

        if (_isMultiSelecting && _selectionRectangle != null)
        {
            var currentPoint = e.GetPosition(_elementsCanvas);
            UpdateSelectionRectangle(_selectionStartPoint, currentPoint);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isMultiSelecting)
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
