using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Services;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// Property panel for editing selected report elements.
/// Displays common properties and element-specific properties based on selection.
/// </summary>
public partial class ElementPropertyPanel : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<ReportElementBase?> SelectedElementProperty =
        AvaloniaProperty.Register<ElementPropertyPanel, ReportElementBase?>(nameof(SelectedElement));

    public static readonly StyledProperty<IReadOnlyList<ReportElementBase>?> SelectedElementsProperty =
        AvaloniaProperty.Register<ElementPropertyPanel, IReadOnlyList<ReportElementBase>?>(nameof(SelectedElements));

    public static readonly StyledProperty<bool> HasSelectionProperty =
        AvaloniaProperty.Register<ElementPropertyPanel, bool>(nameof(HasSelection));

    public static readonly StyledProperty<bool> HasSingleSelectionProperty =
        AvaloniaProperty.Register<ElementPropertyPanel, bool>(nameof(HasSingleSelection));

    public static readonly StyledProperty<bool> HasMultiSelectionProperty =
        AvaloniaProperty.Register<ElementPropertyPanel, bool>(nameof(HasMultiSelection));

    #endregion

    #region Properties

    /// <summary>
    /// The currently selected element (for single selection).
    /// </summary>
    public ReportElementBase? SelectedElement
    {
        get => GetValue(SelectedElementProperty);
        set => SetValue(SelectedElementProperty, value);
    }

    /// <summary>
    /// All currently selected elements.
    /// </summary>
    public IReadOnlyList<ReportElementBase>? SelectedElements
    {
        get => GetValue(SelectedElementsProperty);
        set => SetValue(SelectedElementsProperty, value);
    }

    /// <summary>
    /// Whether any elements are selected.
    /// </summary>
    public bool HasSelection
    {
        get => GetValue(HasSelectionProperty);
        private set => SetValue(HasSelectionProperty, value);
    }

    /// <summary>
    /// Whether exactly one element is selected.
    /// </summary>
    public bool HasSingleSelection
    {
        get => GetValue(HasSingleSelectionProperty);
        private set => SetValue(HasSingleSelectionProperty, value);
    }

    /// <summary>
    /// Whether multiple elements are selected.
    /// </summary>
    public bool HasMultiSelection
    {
        get => GetValue(HasMultiSelectionProperty);
        private set => SetValue(HasMultiSelectionProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a property value changes.
    /// </summary>
    public event EventHandler<PropertyChangedEventArgs>? PropertyValueChanged;

    /// <summary>
    /// Raised when z-order change is requested.
    /// </summary>
    public event EventHandler<ZOrderChangeEventArgs>? ZOrderChangeRequested;

    #endregion

    #region Private Fields

    private NumericUpDown? _positionX;
    private NumericUpDown? _positionY;
    private NumericUpDown? _elementWidth;
    private NumericUpDown? _elementHeight;
    private CheckBox? _isLockedCheckbox;
    private CheckBox? _isVisibleCheckbox;
    private ContentControl? _elementSpecificProperties;
    private TextBlock? _elementTypeText;
    private PathIcon? _elementTypeIcon;
    private TextBlock? _selectionCountText;

    private bool _isUpdating;

    #endregion

    public ElementPropertyPanel()
    {
        InitializeComponent();
        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Helper method to translate strings.
    /// </summary>
    private static string Tr(string text) => LanguageService.Instance.Translate(text);

    /// <summary>
    /// Called when the language changes to refresh all translated content.
    /// </summary>
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Refresh the property panel with updated translations
        UpdateFromSelection();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _positionX = this.FindControl<NumericUpDown>("PositionX");
        _positionY = this.FindControl<NumericUpDown>("PositionY");
        _elementWidth = this.FindControl<NumericUpDown>("ElementWidth");
        _elementHeight = this.FindControl<NumericUpDown>("ElementHeight");
        _isLockedCheckbox = this.FindControl<CheckBox>("IsLockedCheckbox");
        _isVisibleCheckbox = this.FindControl<CheckBox>("IsVisibleCheckbox");
        _elementSpecificProperties = this.FindControl<ContentControl>("ElementSpecificProperties");
        _elementTypeText = this.FindControl<TextBlock>("ElementTypeText");
        _elementTypeIcon = this.FindControl<PathIcon>("ElementTypeIcon");
        _selectionCountText = this.FindControl<TextBlock>("SelectionCountText");

        UpdateFromSelection();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedElementProperty || change.Property == SelectedElementsProperty)
        {
            UpdateSelectionState();
            UpdateFromSelection();
        }
    }

    private void UpdateSelectionState()
    {
        var count = SelectedElements?.Count ?? 0;
        HasSelection = count > 0;
        HasSingleSelection = count == 1;
        HasMultiSelection = count > 1;

        if (_selectionCountText != null)
        {
            _selectionCountText.Text = $"{count} {Tr("elements selected")}";
        }
    }

    private void UpdateFromSelection()
    {
        if (!HasSingleSelection || SelectedElement == null)
        {
            ClearProperties();
            return;
        }

        _isUpdating = true;

        try
        {
            var element = SelectedElement;

            // Update common properties
            _positionX?.Value = (decimal)element.X;
            _positionY?.Value = (decimal)element.Y;
            _elementWidth?.Value = (decimal)element.Width;
            _elementHeight?.Value = (decimal)element.Height;
            // IsLocked is not a base property - skip for now
            _isVisibleCheckbox?.IsChecked = element.IsVisible;

            // Update type header
            UpdateElementTypeHeader(element);

            // Update element-specific properties
            UpdateElementSpecificProperties(element);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void ClearProperties()
    {
        _isUpdating = true;

        try
        {
            _positionX?.Value = 0;
            _positionY?.Value = 0;
            _elementWidth?.Value = 100;
            _elementHeight?.Value = 100;
            _isLockedCheckbox?.IsChecked = false;
            _isVisibleCheckbox?.IsChecked = true;
            _elementSpecificProperties?.Content = null;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateElementTypeHeader(ReportElementBase element)
    {
        if (_elementTypeText == null || _elementTypeIcon == null) return;

        var (typeName, iconData) = element.GetElementType() switch
        {
            ReportElementType.Chart => (Tr("Chart Element"), "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z"),
            ReportElementType.Table => (Tr("Table Element"), "M5,4H19A2,2 0 0,1 21,6V18A2,2 0 0,1 19,20H5A2,2 0 0,1 3,18V6A2,2 0 0,1 5,4M5,8V12H11V8H5M13,8V12H19V8H13M5,14V18H11V14H5M13,14V18H19V14H13Z"),
            ReportElementType.Label => (Tr("Label Element"), "M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
            ReportElementType.Image => (Tr("Image Element"), "M21,17H7V3H21M21,1H7A2,2 0 0,0 5,3V17A2,2 0 0,0 7,19H21A2,2 0 0,0 23,17V3A2,2 0 0,0 21,1M3,5H1V21A2,2 0 0,0 3,23H19V21H3M15.96,10.29L13.21,13.83L11.25,11.47L8.5,15H19.5L15.96,10.29Z"),
            ReportElementType.DateRange => (Tr("Date Range Element"), "M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1"),
            ReportElementType.Summary => (Tr("Summary Element"), "M14,17H7V15H14M17,13H7V11H17M17,9H7V7H17M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3Z"),
            _ => (Tr("Element"), "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z")
        };

        _elementTypeText.Text = typeName;
        _elementTypeIcon.Data = PathGeometry.Parse(iconData);
    }

    private void UpdateElementSpecificProperties(ReportElementBase element)
    {
        _elementSpecificProperties?.Content = element switch
        {
            ChartReportElement chart => CreateChartProperties(chart),
            TableReportElement table => CreateTableProperties(table),
            LabelReportElement label => CreateLabelProperties(label),
            ImageReportElement image => CreateImageProperties(image),
            DateRangeReportElement dateRange => CreateDateRangeProperties(dateRange),
            SummaryReportElement summary => CreateSummaryProperties(summary),
            _ => null
        };
    }

    #region Element-Specific Property Panels

    private Control CreateChartProperties(ChartReportElement chart)
    {
        var panel = new StackPanel { Spacing = 8 };

        // Chart Type
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Chart Settings"),
            Classes = { "section-header" }
        });

        panel.Children.Add(new TextBlock
        {
            Text = Tr("Chart Type"),
            Classes = { "property-label" }
        });

        var chartTypeCombo = new ComboBox
        {
            Classes = { "property-input" },
            ItemsSource = Enum.GetValues<ChartDataType>(),
            SelectedItem = chart.ChartType
        };
        chartTypeCombo.SelectionChanged += (_, _) =>
        {
            if (_isUpdating || chartTypeCombo.SelectedItem is not ChartDataType newType) return;
            var oldValue = chart.ChartType;
            chart.ChartType = newType;
            OnPropertyChanged(chart, nameof(chart.ChartType), oldValue, newType);
        };
        panel.Children.Add(chartTypeCombo);

        // Chart Style
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Chart Style"),
            Classes = { "property-label" },
            Margin = new Thickness(0, 8, 0, 0)
        });

        var chartStyleCombo = new ComboBox
        {
            Classes = { "property-input" },
            ItemsSource = Enum.GetValues<ReportChartStyle>(),
            SelectedItem = chart.ChartStyle
        };
        chartStyleCombo.SelectionChanged += (_, _) =>
        {
            if (_isUpdating || chartStyleCombo.SelectedItem is not ReportChartStyle newStyle) return;
            var oldValue = chart.ChartStyle;
            chart.ChartStyle = newStyle;
            OnPropertyChanged(chart, nameof(chart.ChartStyle), oldValue, newStyle);
        };
        panel.Children.Add(chartStyleCombo);

        // Show Title
        var showTitleCheck = new CheckBox
        {
            Content = Tr("Show Title"),
            Classes = { "property-checkbox" },
            IsChecked = chart.ShowTitle,
            Margin = new Thickness(0, 8, 0, 0)
        };
        showTitleCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = chart.ShowTitle;
            chart.ShowTitle = showTitleCheck.IsChecked ?? true;
            OnPropertyChanged(chart, nameof(chart.ShowTitle), oldValue, chart.ShowTitle);
        };
        panel.Children.Add(showTitleCheck);

        // Show Legend
        var showLegendCheck = new CheckBox
        {
            Content = Tr("Show Legend"),
            Classes = { "property-checkbox" },
            IsChecked = chart.ShowLegend
        };
        showLegendCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = chart.ShowLegend;
            chart.ShowLegend = showLegendCheck.IsChecked ?? true;
            OnPropertyChanged(chart, nameof(chart.ShowLegend), oldValue, chart.ShowLegend);
        };
        panel.Children.Add(showLegendCheck);

        return panel;
    }

    private Control CreateTableProperties(TableReportElement table)
    {
        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock
        {
            Text = Tr("Table Settings"),
            Classes = { "section-header" }
        });

        // Data Selection
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Data Selection"),
            Classes = { "property-label" }
        });

        var dataSelectionCombo = new ComboBox
        {
            Classes = { "property-input" },
            ItemsSource = Enum.GetValues<TableDataSelection>(),
            SelectedItem = table.DataSelection
        };
        dataSelectionCombo.SelectionChanged += (_, _) =>
        {
            if (_isUpdating || dataSelectionCombo.SelectedItem is not TableDataSelection newSelection) return;
            var oldValue = table.DataSelection;
            table.DataSelection = newSelection;
            OnPropertyChanged(table, nameof(table.DataSelection), oldValue, newSelection);
        };
        panel.Children.Add(dataSelectionCombo);

        // Row Count
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Max Rows"),
            Classes = { "property-label" },
            Margin = new Thickness(0, 8, 0, 0)
        });

        var rowCountInput = new NumericUpDown
        {
            Classes = { "property-input" },
            Value = table.MaxRows,
            Minimum = 1,
            Maximum = 100
        };
        rowCountInput.ValueChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = table.MaxRows;
            table.MaxRows = (int)(rowCountInput.Value ?? 10);
            OnPropertyChanged(table, nameof(table.MaxRows), oldValue, table.MaxRows);
        };
        panel.Children.Add(rowCountInput);

        // Checkboxes
        var showHeaderCheck = new CheckBox
        {
            Content = Tr("Show Header Row"),
            Classes = { "property-checkbox" },
            IsChecked = table.ShowHeaders,
            Margin = new Thickness(0, 8, 0, 0)
        };
        showHeaderCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = table.ShowHeaders;
            table.ShowHeaders = showHeaderCheck.IsChecked ?? true;
            OnPropertyChanged(table, nameof(table.ShowHeaders), oldValue, table.ShowHeaders);
        };
        panel.Children.Add(showHeaderCheck);

        var alternatingRowsCheck = new CheckBox
        {
            Content = Tr("Alternating Row Colors"),
            Classes = { "property-checkbox" },
            IsChecked = table.AlternateRowColors
        };
        alternatingRowsCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = table.AlternateRowColors;
            table.AlternateRowColors = alternatingRowsCheck.IsChecked ?? true;
            OnPropertyChanged(table, nameof(table.AlternateRowColors), oldValue, table.AlternateRowColors);
        };
        panel.Children.Add(alternatingRowsCheck);

        return panel;
    }

    private Control CreateLabelProperties(LabelReportElement label)
    {
        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock
        {
            Text = Tr("Text Settings"),
            Classes = { "section-header" }
        });

        // Text content
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Text"),
            Classes = { "property-label" }
        });

        var textInput = new TextBox
        {
            Classes = { "property-input" },
            Text = label.Text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 60
        };
        textInput.TextChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = label.Text;
            label.Text = textInput.Text ?? "";
            OnPropertyChanged(label, nameof(label.Text), oldValue, textInput.Text);
        };
        panel.Children.Add(textInput);

        // Font Size
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Font Size"),
            Classes = { "property-label" },
            Margin = new Thickness(0, 8, 0, 0)
        });

        var fontSizeInput = new NumericUpDown
        {
            Classes = { "property-input" },
            Value = (decimal)label.FontSize,
            Minimum = 8,
            Maximum = 72
        };
        fontSizeInput.ValueChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = label.FontSize;
            label.FontSize = (double)(fontSizeInput.Value ?? 14);
            OnPropertyChanged(label, nameof(label.FontSize), oldValue, label.FontSize);
        };
        panel.Children.Add(fontSizeInput);

        // Font Style
        var fontStylePanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var boldCheck = new ToggleButton
        {
            Content = "B",
            FontWeight = FontWeight.Bold,
            Width = 32,
            Height = 32,
            IsChecked = label.IsBold
        };
        boldCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = label.IsBold;
            label.IsBold = boldCheck.IsChecked ?? false;
            OnPropertyChanged(label, nameof(label.IsBold), oldValue, label.IsBold);
        };
        fontStylePanel.Children.Add(boldCheck);

        var italicCheck = new ToggleButton
        {
            Content = "I",
            FontStyle = FontStyle.Italic,
            Width = 32,
            Height = 32,
            IsChecked = label.IsItalic
        };
        italicCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = label.IsItalic;
            label.IsItalic = italicCheck.IsChecked ?? false;
            OnPropertyChanged(label, nameof(label.IsItalic), oldValue, label.IsItalic);
        };
        fontStylePanel.Children.Add(italicCheck);

        var underlineCheck = new ToggleButton
        {
            Content = "U",
            Width = 32,
            Height = 32,
            IsChecked = label.IsUnderline
        };
        underlineCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = label.IsUnderline;
            label.IsUnderline = underlineCheck.IsChecked ?? false;
            OnPropertyChanged(label, nameof(label.IsUnderline), oldValue, label.IsUnderline);
        };
        fontStylePanel.Children.Add(underlineCheck);

        panel.Children.Add(fontStylePanel);

        // Alignment
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Horizontal Alignment"),
            Classes = { "property-label" },
            Margin = new Thickness(0, 8, 0, 0)
        });

        var hAlignCombo = new ComboBox
        {
            Classes = { "property-input" },
            ItemsSource = Enum.GetValues<HorizontalTextAlignment>(),
            SelectedItem = label.HorizontalAlignment
        };
        hAlignCombo.SelectionChanged += (_, _) =>
        {
            if (_isUpdating || hAlignCombo.SelectedItem is not HorizontalTextAlignment align) return;
            var oldValue = label.HorizontalAlignment;
            label.HorizontalAlignment = align;
            OnPropertyChanged(label, nameof(label.HorizontalAlignment), oldValue, align);
        };
        panel.Children.Add(hAlignCombo);

        return panel;
    }

    private Control CreateImageProperties(ImageReportElement image)
    {
        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock
        {
            Text = Tr("Image Settings"),
            Classes = { "section-header" }
        });

        // Image Path
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Image Source"),
            Classes = { "property-label" }
        });

        var pathPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var pathInput = new TextBox
        {
            Classes = { "property-input" },
            Text = image.ImagePath,
            Watermark = Tr("Path to image..."),
            IsReadOnly = true
        };
        Grid.SetColumn(pathInput, 0);
        pathPanel.Children.Add(pathInput);

        var browseButton = new Button
        {
            Content = "...",
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(8, 6)
        };
        Grid.SetColumn(browseButton, 1);
        pathPanel.Children.Add(browseButton);

        panel.Children.Add(pathPanel);

        // Scale Mode
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Scale Mode"),
            Classes = { "property-label" },
            Margin = new Thickness(0, 8, 0, 0)
        });

        var scaleModeCombo = new ComboBox
        {
            Classes = { "property-input" },
            ItemsSource = Enum.GetValues<ImageScaleMode>(),
            SelectedItem = image.ScaleMode
        };
        scaleModeCombo.SelectionChanged += (_, _) =>
        {
            if (_isUpdating || scaleModeCombo.SelectedItem is not ImageScaleMode mode) return;
            var oldValue = image.ScaleMode;
            image.ScaleMode = mode;
            OnPropertyChanged(image, nameof(image.ScaleMode), oldValue, mode);
        };
        panel.Children.Add(scaleModeCombo);

        return panel;
    }

    private Control CreateDateRangeProperties(DateRangeReportElement dateRange)
    {
        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock
        {
            Text = Tr("Date Range Settings"),
            Classes = { "section-header" }
        });

        // Date Format
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Date Format"),
            Classes = { "property-label" }
        });

        var dateFormatInput = new TextBox
        {
            Classes = { "property-input" },
            Text = dateRange.DateFormat,
            Watermark = "MMM dd, yyyy"
        };
        dateFormatInput.TextChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = dateRange.DateFormat;
            dateRange.DateFormat = dateFormatInput.Text ?? "MMM dd, yyyy";
            OnPropertyChanged(dateRange, nameof(dateRange.DateFormat), oldValue, dateFormatInput.Text);
        };
        panel.Children.Add(dateFormatInput);

        // Font Size
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Font Size"),
            Classes = { "property-label" },
            Margin = new Thickness(0, 8, 0, 0)
        });

        var fontSizeInput = new NumericUpDown
        {
            Classes = { "property-input" },
            Value = (decimal)dateRange.FontSize,
            Minimum = 8,
            Maximum = 36
        };
        fontSizeInput.ValueChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = dateRange.FontSize;
            dateRange.FontSize = (double)(fontSizeInput.Value ?? 12);
            OnPropertyChanged(dateRange, nameof(dateRange.FontSize), oldValue, dateRange.FontSize);
        };
        panel.Children.Add(fontSizeInput);

        return panel;
    }

    private Control CreateSummaryProperties(SummaryReportElement summary)
    {
        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock
        {
            Text = Tr("Summary Settings"),
            Classes = { "section-header" }
        });

        // Items to include
        panel.Children.Add(new TextBlock
        {
            Text = Tr("Include Items"),
            Classes = { "property-label" }
        });

        var totalRevenueCheck = new CheckBox
        {
            Content = Tr("Total Sales"),
            Classes = { "property-checkbox" },
            IsChecked = summary.ShowTotalRevenue
        };
        totalRevenueCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = summary.ShowTotalRevenue;
            summary.ShowTotalRevenue = totalRevenueCheck.IsChecked ?? true;
            OnPropertyChanged(summary, nameof(summary.ShowTotalRevenue), oldValue, summary.ShowTotalRevenue);
        };
        panel.Children.Add(totalRevenueCheck);

        var transactionCountCheck = new CheckBox
        {
            Content = Tr("Total Transactions"),
            Classes = { "property-checkbox" },
            IsChecked = summary.ShowTotalTransactions
        };
        transactionCountCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = summary.ShowTotalTransactions;
            summary.ShowTotalTransactions = transactionCountCheck.IsChecked ?? true;
            OnPropertyChanged(summary, nameof(summary.ShowTotalTransactions), oldValue, summary.ShowTotalTransactions);
        };
        panel.Children.Add(transactionCountCheck);

        var avgValueCheck = new CheckBox
        {
            Content = Tr("Average Value"),
            Classes = { "property-checkbox" },
            IsChecked = summary.ShowAverageValue
        };
        avgValueCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = summary.ShowAverageValue;
            summary.ShowAverageValue = avgValueCheck.IsChecked ?? true;
            OnPropertyChanged(summary, nameof(summary.ShowAverageValue), oldValue, summary.ShowAverageValue);
        };
        panel.Children.Add(avgValueCheck);

        var growthRateCheck = new CheckBox
        {
            Content = Tr("Growth Rate"),
            Classes = { "property-checkbox" },
            IsChecked = summary.ShowGrowthRate
        };
        growthRateCheck.IsCheckedChanged += (_, _) =>
        {
            if (_isUpdating) return;
            var oldValue = summary.ShowGrowthRate;
            summary.ShowGrowthRate = growthRateCheck.IsChecked ?? true;
            OnPropertyChanged(summary, nameof(summary.ShowGrowthRate), oldValue, summary.ShowGrowthRate);
        };
        panel.Children.Add(growthRateCheck);

        return panel;
    }

    #endregion

    #region Event Handlers

    private void OnPositionXChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdating || SelectedElement == null) return;
        var oldValue = SelectedElement.X;
        SelectedElement.X = (double)(e.NewValue ?? 0);
        OnPropertyChanged(SelectedElement, "X", oldValue, SelectedElement.X);
    }

    private void OnPositionYChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdating || SelectedElement == null) return;
        var oldValue = SelectedElement.Y;
        SelectedElement.Y = (double)(e.NewValue ?? 0);
        OnPropertyChanged(SelectedElement, "Y", oldValue, SelectedElement.Y);
    }

    private void OnWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdating || SelectedElement == null) return;
        var oldValue = SelectedElement.Width;
        SelectedElement.Width = (double)(e.NewValue ?? 100);
        OnPropertyChanged(SelectedElement, "Width", oldValue, SelectedElement.Width);
    }

    private void OnHeightChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdating || SelectedElement == null) return;
        var oldValue = SelectedElement.Height;
        SelectedElement.Height = (double)(e.NewValue ?? 100);
        OnPropertyChanged(SelectedElement, "Height", oldValue, SelectedElement.Height);
    }

    private void OnIsLockedChanged(object? sender, RoutedEventArgs e)
    {
        // IsLocked is not a base property - skip for now
    }

    private void OnIsVisibleChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdating || SelectedElement == null || _isVisibleCheckbox == null) return;
        var oldValue = SelectedElement.IsVisible;
        SelectedElement.IsVisible = _isVisibleCheckbox.IsChecked ?? true;
        OnPropertyChanged(SelectedElement, "IsVisible", oldValue, SelectedElement.IsVisible);
    }

    private void OnBringToFrontClick(object? sender, RoutedEventArgs e)
    {
        ZOrderChangeRequested?.Invoke(this, new ZOrderChangeEventArgs(ZOrderChange.BringToFront));
    }

    private void OnSendToBackClick(object? sender, RoutedEventArgs e)
    {
        ZOrderChangeRequested?.Invoke(this, new ZOrderChangeEventArgs(ZOrderChange.SendToBack));
    }

    private void OnBringForwardClick(object? sender, RoutedEventArgs e)
    {
        ZOrderChangeRequested?.Invoke(this, new ZOrderChangeEventArgs(ZOrderChange.BringForward));
    }

    private void OnSendBackwardClick(object? sender, RoutedEventArgs e)
    {
        ZOrderChangeRequested?.Invoke(this, new ZOrderChangeEventArgs(ZOrderChange.SendBackward));
    }

    private void OnPropertyChanged(ReportElementBase element, string propertyName, object? oldValue, object? newValue)
    {
        PropertyValueChanged?.Invoke(this, new PropertyChangedEventArgs(element, propertyName, oldValue, newValue));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Refreshes the property panel from the current selection.
    /// </summary>
    public void Refresh()
    {
        UpdateFromSelection();
    }

    /// <summary>
    /// Sets the selection from a list of elements.
    /// </summary>
    public void SetSelection(IReadOnlyList<ReportElementBase> elements)
    {
        SelectedElements = elements;
        SelectedElement = elements.Count == 1 ? elements[0] : null;
    }

    #endregion
}

#region Event Args

/// <summary>
/// Event args for property changes.
/// </summary>
public class PropertyChangedEventArgs(
    ReportElementBase element,
    string propertyName,
    object? oldValue,
    object? newValue)
    : EventArgs
{
    public ReportElementBase Element { get; } = element;
    public string PropertyName { get; } = propertyName;
    public object? OldValue { get; } = oldValue;
    public object? NewValue { get; } = newValue;
}

/// <summary>
/// Z-order change types.
/// </summary>
public enum ZOrderChange
{
    BringToFront,
    SendToBack,
    BringForward,
    SendBackward
}

/// <summary>
/// Event args for z-order changes.
/// </summary>
public class ZOrderChangeEventArgs(ZOrderChange change) : EventArgs
{
    public ZOrderChange Change { get; } = change;
}

#endregion
