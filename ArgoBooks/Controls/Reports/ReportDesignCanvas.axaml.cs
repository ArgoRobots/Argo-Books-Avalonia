using System.ComponentModel;
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

    private ScrollViewer? _scrollViewer;
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

        // Subscribe to element property changes for real-time rendering
        element.PropertyChanged += OnElementPropertyChanged;

        // Set content based on element type
        control.ElementContent = CreateElementContent(element);

        _elementsCanvas?.Children.Add(control);
        _elementControls.Add(control);
        _elementControlMap[element.Id] = control;

        return control;
    }

    private void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ReportElementBase element)
        {
            // Refresh the element's visual content when properties change
            RefreshElementContent(element);
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

    private static Control CreateTablePreview(TableReportElement? element)
    {
        var showHeaders = element?.ShowHeaders ?? true;
        var headerBgColor = element?.HeaderBackgroundColor ?? "#E0E0E0";
        var headerTextColor = element?.HeaderTextColor ?? "#000000";
        var fontSize = element?.FontSize ?? 12;
        var showGridLines = element?.ShowGridLines ?? true;
        var gridLineColor = element?.GridLineColor ?? "#CCCCCC";

        var grid = new Grid();

        // Get visible columns
        var columns = GetVisibleTableColumns(element);
        var columnCount = Math.Max(columns.Count, 3);

        // Create column definitions
        for (int i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }

        // Create row definitions
        var rowCount = showHeaders ? 4 : 3; // Header + 3 data rows
        for (int i = 0; i < rowCount; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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
                    Padding = new Thickness(4, 2),
                    Child = new TextBlock
                    {
                        Text = col < columns.Count ? columns[col] : $"Col {col + 1}",
                        FontSize = fontSize,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse(headerTextColor)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    }
                };
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, col);
                grid.Children.Add(headerCell);
            }
        }

        // Add sample data rows
        var startRow = showHeaders ? 1 : 0;
        for (int row = startRow; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                var dataCell = new Border
                {
                    BorderBrush = showGridLines ? new SolidColorBrush(Color.Parse(gridLineColor)) : null,
                    BorderThickness = showGridLines ? new Thickness(0, 0, 1, 1) : new Thickness(0),
                    Padding = new Thickness(4, 2),
                    Child = new TextBlock
                    {
                        Text = "...",
                        FontSize = fontSize,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    }
                };
                Grid.SetRow(dataCell, row);
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
                    };
                }
                else
                {
                    content = CreateImagePlaceholder("Image not found");
                }
            }
            catch
            {
                content = CreateImagePlaceholder("Error loading image");
            }
        }
        else
        {
            content = CreateImagePlaceholder("No image selected");
        }

        return new Border
        {
            Background = bgColor != "#00FFFFFF" ? new SolidColorBrush(Color.Parse(bgColor)) : Brushes.Transparent,
            BorderBrush = borderThickness > 0 && borderColor != "#00FFFFFF"
                ? new SolidColorBrush(Color.Parse(borderColor))
                : null,
            BorderThickness = new Thickness(borderThickness),
            Child = content
        };
    }

    private static Control CreateImagePlaceholder(string message)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F0F0F0")),
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
        var fontSize = element?.FontSize ?? 12;
        var textColor = element?.TextColor ?? "#000000";
        var isItalic = element?.IsItalic ?? false;
        var hAlign = element?.HorizontalAlignment ?? HorizontalTextAlignment.Center;
        var vAlign = element?.VerticalAlignment ?? VerticalTextAlignment.Center;

        // Create sample date range text
        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        var text = $"Period: {startDate.ToString(dateFormat)} to {endDate.ToString(dateFormat)}";

        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal,
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

    private static Control CreateSummaryPreview(SummaryReportElement? element)
    {
        var bgColor = element?.BackgroundColor ?? "#F5F5F5";
        var borderColor = element?.BorderColor ?? "#CCCCCC";
        var borderThickness = element?.BorderThickness ?? 1;
        var fontSize = element?.FontSize ?? 12;
        var hAlign = element?.HorizontalAlignment ?? HorizontalTextAlignment.Left;
        var vAlign = element?.VerticalAlignment ?? VerticalTextAlignment.Top;
        var transactionType = element?.TransactionType ?? TransactionType.Revenue;

        // Create sample summary lines
        var lines = new List<string>();
        if (element?.ShowTotalSales ?? true)
        {
            var label = transactionType == TransactionType.Expenses ? "Total Expenses" : "Total Revenue";
            lines.Add($"{label}: $12,345.67");
        }
        if (element?.ShowTotalTransactions ?? true)
        {
            lines.Add("Transactions: 156");
        }
        if (element?.ShowAverageValue ?? true)
        {
            lines.Add("Average Value: $79.14");
        }
        if (element?.ShowGrowthRate ?? true)
        {
            lines.Add("Growth Rate: +8.5%");
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
                Margin = new Thickness(0, 2)
            });
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse(bgColor)),
            BorderBrush = borderThickness > 0 ? new SolidColorBrush(Color.Parse(borderColor)) : null,
            BorderThickness = new Thickness(borderThickness),
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
            _scrollViewer.Offset = new Vector(
                _panStartOffset.X + delta.X,
                _panStartOffset.Y + delta.Y);
            e.Handled = true;
        }
        else if (_isMultiSelecting && _selectionRectangle != null)
        {
            var currentPoint = e.GetPosition(_elementsCanvas);
            UpdateSelectionRectangle(_selectionStartPoint, currentPoint);
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
