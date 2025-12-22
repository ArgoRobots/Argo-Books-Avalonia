using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Base class for all report elements.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "elementType")]
[JsonDerivedType(typeof(ChartReportElement), "Chart")]
[JsonDerivedType(typeof(TableReportElement), "Table")]
[JsonDerivedType(typeof(LabelReportElement), "Label")]
[JsonDerivedType(typeof(ImageReportElement), "Image")]
[JsonDerivedType(typeof(DateRangeReportElement), "DateRange")]
[JsonDerivedType(typeof(SummaryReportElement), "Summary")]
public abstract class ReportElementBase : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private double _x;
    private double _y;
    private double _width = 200;
    private double _height = 150;
    private int _zOrder;
    private bool _isVisible = true;

    /// <summary>
    /// Fired when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Unique identifier for the element.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    /// <summary>
    /// X position on the page.
    /// </summary>
    [JsonPropertyName("x")]
    public double X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    /// <summary>
    /// Y position on the page.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    /// <summary>
    /// Element width.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width
    {
        get => _width;
        set => SetField(ref _width, value);
    }

    /// <summary>
    /// Element height.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height
    {
        get => _height;
        set => SetField(ref _height, value);
    }

    /// <summary>
    /// Z-order for layering.
    /// </summary>
    [JsonPropertyName("zOrder")]
    public int ZOrder
    {
        get => _zOrder;
        set => SetField(ref _zOrder, value);
    }

    /// <summary>
    /// Whether the element is visible.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    /// <summary>
    /// Minimum size for the element.
    /// </summary>
    [JsonIgnore]
    public virtual double MinimumSize => 40;

    /// <summary>
    /// Display name for the element type.
    /// </summary>
    [JsonIgnore]
    public abstract string DisplayName { get; }

    /// <summary>
    /// Gets the element type.
    /// </summary>
    public abstract ReportElementType GetElementType();

    /// <summary>
    /// Creates a clone of this element.
    /// </summary>
    public abstract ReportElementBase Clone();

    /// <summary>
    /// Gets the bounds as a rectangle.
    /// </summary>
    [JsonIgnore]
    public (double X, double Y, double Width, double Height) Bounds
    {
        get => (X, Y, Width, Height);
        set => (X, Y, Width, Height) = value;
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a field value and raises PropertyChanged if the value changed.
    /// </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Chart element for displaying graphs and charts.
/// </summary>
public class ChartReportElement : ReportElementBase
{
    private ChartDataType _chartType = ChartDataType.TotalRevenue;
    private bool _showLegend = true;
    private bool _showTitle = true;
    private string _fontFamily = "Segoe UI";
    private double _titleFontSize = 12;
    private double _legendFontSize = 11;
    private string _borderColor = "#808080";
    private int _borderThickness = 1;

    [JsonPropertyName("chartType")]
    public ChartDataType ChartType
    {
        get => _chartType;
        set => SetField(ref _chartType, value);
    }

    [JsonPropertyName("showLegend")]
    public bool ShowLegend
    {
        get => _showLegend;
        set => SetField(ref _showLegend, value);
    }

    [JsonPropertyName("showTitle")]
    public bool ShowTitle
    {
        get => _showTitle;
        set => SetField(ref _showTitle, value);
    }

    [JsonPropertyName("fontFamily")]
    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    [JsonPropertyName("titleFontSize")]
    public double TitleFontSize
    {
        get => _titleFontSize;
        set => SetField(ref _titleFontSize, value);
    }

    [JsonPropertyName("legendFontSize")]
    public double LegendFontSize
    {
        get => _legendFontSize;
        set => SetField(ref _legendFontSize, value);
    }

    [JsonPropertyName("borderColor")]
    public string BorderColor
    {
        get => _borderColor;
        set => SetField(ref _borderColor, value);
    }

    [JsonPropertyName("borderThickness")]
    public int BorderThickness
    {
        get => _borderThickness;
        set => SetField(ref _borderThickness, value);
    }

    public override double MinimumSize => 80;
    public override string DisplayName => "Chart";
    public override ReportElementType GetElementType() => ReportElementType.Chart;

    public override ReportElementBase Clone()
    {
        return new ChartReportElement
        {
            Id = Guid.NewGuid().ToString(),
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZOrder = ZOrder,
            IsVisible = IsVisible,
            ChartType = ChartType,
            ShowLegend = ShowLegend,
            ShowTitle = ShowTitle,
            FontFamily = FontFamily,
            TitleFontSize = TitleFontSize,
            LegendFontSize = LegendFontSize,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness
        };
    }
}

/// <summary>
/// Table element for displaying transaction data.
/// </summary>
public class TableReportElement : ReportElementBase
{
    [JsonPropertyName("transactionType")]
    public TransactionType TransactionType { get; set; } = TransactionType.Both;

    [JsonPropertyName("includeReturns")]
    public bool IncludeReturns { get; set; } = true;

    [JsonPropertyName("includeLosses")]
    public bool IncludeLosses { get; set; } = true;

    [JsonPropertyName("dataSelection")]
    public TableDataSelection DataSelection { get; set; } = TableDataSelection.All;

    [JsonPropertyName("sortOrder")]
    public TableSortOrder SortOrder { get; set; } = TableSortOrder.DateDescending;

    [JsonPropertyName("maxRows")]
    public int MaxRows { get; set; } = 10;

    [JsonPropertyName("showHeaders")]
    public bool ShowHeaders { get; set; } = true;

    [JsonPropertyName("alternateRowColors")]
    public bool AlternateRowColors { get; set; } = true;

    [JsonPropertyName("showGridLines")]
    public bool ShowGridLines { get; set; } = true;

    [JsonPropertyName("showTotalsRow")]
    public bool ShowTotalsRow { get; set; }

    [JsonPropertyName("autoSizeColumns")]
    public bool AutoSizeColumns { get; set; } = true;

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 8;

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("dataRowHeight")]
    public int DataRowHeight { get; set; } = 20;

    [JsonPropertyName("headerRowHeight")]
    public int HeaderRowHeight { get; set; } = 25;

    [JsonPropertyName("cellPadding")]
    public int CellPadding { get; set; } = 3;

    [JsonPropertyName("headerBackgroundColor")]
    public string HeaderBackgroundColor { get; set; } = "#5E94FF";

    [JsonPropertyName("headerTextColor")]
    public string HeaderTextColor { get; set; } = "#FFFFFF";

    [JsonPropertyName("dataRowTextColor")]
    public string DataRowTextColor { get; set; } = "#000000";

    [JsonPropertyName("gridLineColor")]
    public string GridLineColor { get; set; } = "#D3D3D3";

    [JsonPropertyName("baseRowColor")]
    public string BaseRowColor { get; set; } = "#FFFFFF";

    [JsonPropertyName("alternateRowColor")]
    public string AlternateRowColor { get; set; } = "#F8F8F8";

    // Column visibility
    [JsonPropertyName("showDateColumn")]
    public bool ShowDateColumn { get; set; } = true;

    [JsonPropertyName("showTransactionIdColumn")]
    public bool ShowTransactionIdColumn { get; set; } = true;

    [JsonPropertyName("showCompanyColumn")]
    public bool ShowCompanyColumn { get; set; } = true;

    [JsonPropertyName("showProductColumn")]
    public bool ShowProductColumn { get; set; } = true;

    [JsonPropertyName("showQuantityColumn")]
    public bool ShowQuantityColumn { get; set; } = true;

    [JsonPropertyName("showUnitPriceColumn")]
    public bool ShowUnitPriceColumn { get; set; }

    [JsonPropertyName("showTotalColumn")]
    public bool ShowTotalColumn { get; set; } = true;

    [JsonPropertyName("showStatusColumn")]
    public bool ShowStatusColumn { get; set; }

    [JsonPropertyName("showAccountantColumn")]
    public bool ShowAccountantColumn { get; set; }

    [JsonPropertyName("showShippingColumn")]
    public bool ShowShippingColumn { get; set; }

    public override double MinimumSize => 100;
    public override string DisplayName => "Table";
    public override ReportElementType GetElementType() => ReportElementType.Table;

    public override ReportElementBase Clone()
    {
        return new TableReportElement
        {
            Id = Guid.NewGuid().ToString(),
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZOrder = ZOrder,
            IsVisible = IsVisible,
            TransactionType = TransactionType,
            IncludeReturns = IncludeReturns,
            IncludeLosses = IncludeLosses,
            DataSelection = DataSelection,
            SortOrder = SortOrder,
            MaxRows = MaxRows,
            ShowHeaders = ShowHeaders,
            AlternateRowColors = AlternateRowColors,
            ShowGridLines = ShowGridLines,
            ShowTotalsRow = ShowTotalsRow,
            AutoSizeColumns = AutoSizeColumns,
            FontSize = FontSize,
            FontFamily = FontFamily,
            DataRowHeight = DataRowHeight,
            HeaderRowHeight = HeaderRowHeight,
            CellPadding = CellPadding,
            HeaderBackgroundColor = HeaderBackgroundColor,
            HeaderTextColor = HeaderTextColor,
            DataRowTextColor = DataRowTextColor,
            GridLineColor = GridLineColor,
            BaseRowColor = BaseRowColor,
            AlternateRowColor = AlternateRowColor,
            ShowDateColumn = ShowDateColumn,
            ShowTransactionIdColumn = ShowTransactionIdColumn,
            ShowCompanyColumn = ShowCompanyColumn,
            ShowProductColumn = ShowProductColumn,
            ShowQuantityColumn = ShowQuantityColumn,
            ShowUnitPriceColumn = ShowUnitPriceColumn,
            ShowTotalColumn = ShowTotalColumn,
            ShowStatusColumn = ShowStatusColumn,
            ShowAccountantColumn = ShowAccountantColumn,
            ShowShippingColumn = ShowShippingColumn
        };
    }
}

/// <summary>
/// Label element for displaying text content.
/// </summary>
public class LabelReportElement : ReportElementBase
{
    private string _text = "Sample Text";
    private string _fontFamily = "Segoe UI";
    private double _fontSize = 12;
    private bool _isBold;
    private bool _isItalic;
    private bool _isUnderline;
    private string _textColor = "#000000";
    private HorizontalTextAlignment _horizontalAlignment = HorizontalTextAlignment.Center;
    private VerticalTextAlignment _verticalAlignment = VerticalTextAlignment.Center;

    [JsonPropertyName("text")]
    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    [JsonPropertyName("fontFamily")]
    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    [JsonPropertyName("fontSize")]
    public double FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    [JsonPropertyName("isBold")]
    public bool IsBold
    {
        get => _isBold;
        set => SetField(ref _isBold, value);
    }

    [JsonPropertyName("isItalic")]
    public bool IsItalic
    {
        get => _isItalic;
        set => SetField(ref _isItalic, value);
    }

    [JsonPropertyName("isUnderline")]
    public bool IsUnderline
    {
        get => _isUnderline;
        set => SetField(ref _isUnderline, value);
    }

    [JsonPropertyName("textColor")]
    public string TextColor
    {
        get => _textColor;
        set => SetField(ref _textColor, value);
    }

    [JsonPropertyName("horizontalAlignment")]
    public HorizontalTextAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set => SetField(ref _horizontalAlignment, value);
    }

    [JsonPropertyName("verticalAlignment")]
    public VerticalTextAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set => SetField(ref _verticalAlignment, value);
    }

    public override string DisplayName => "Label";
    public override ReportElementType GetElementType() => ReportElementType.Label;

    public override ReportElementBase Clone()
    {
        return new LabelReportElement
        {
            Id = Guid.NewGuid().ToString(),
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZOrder = ZOrder,
            IsVisible = IsVisible,
            Text = Text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderline = IsUnderline,
            TextColor = TextColor,
            HorizontalAlignment = HorizontalAlignment,
            VerticalAlignment = VerticalAlignment
        };
    }
}

/// <summary>
/// Image element for displaying images.
/// </summary>
public class ImageReportElement : ReportElementBase
{
    private string _imagePath = string.Empty;
    private ImageScaleMode _scaleMode = ImageScaleMode.Fit;
    private string _backgroundColor = "#00FFFFFF";
    private string _borderColor = "#00FFFFFF";
    private int _borderThickness = 1;
    private int _cornerRadiusPercent;
    private byte _opacity = 255;

    [JsonPropertyName("imagePath")]
    public string ImagePath
    {
        get => _imagePath;
        set => SetField(ref _imagePath, value);
    }

    [JsonPropertyName("scaleMode")]
    public ImageScaleMode ScaleMode
    {
        get => _scaleMode;
        set => SetField(ref _scaleMode, value);
    }

    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetField(ref _backgroundColor, value);
    }

    [JsonPropertyName("borderColor")]
    public string BorderColor
    {
        get => _borderColor;
        set => SetField(ref _borderColor, value);
    }

    [JsonPropertyName("borderThickness")]
    public int BorderThickness
    {
        get => _borderThickness;
        set => SetField(ref _borderThickness, value);
    }

    [JsonPropertyName("cornerRadiusPercent")]
    public int CornerRadiusPercent
    {
        get => _cornerRadiusPercent;
        set => SetField(ref _cornerRadiusPercent, value);
    }

    [JsonPropertyName("opacity")]
    public byte Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, value);
    }

    public override string DisplayName => "Image";
    public override ReportElementType GetElementType() => ReportElementType.Image;

    public override ReportElementBase Clone()
    {
        return new ImageReportElement
        {
            Id = Guid.NewGuid().ToString(),
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZOrder = ZOrder,
            IsVisible = IsVisible,
            ImagePath = ImagePath,
            ScaleMode = ScaleMode,
            BackgroundColor = BackgroundColor,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            CornerRadiusPercent = CornerRadiusPercent,
            Opacity = Opacity
        };
    }
}

/// <summary>
/// Date range element for displaying report date filters.
/// </summary>
public class DateRangeReportElement : ReportElementBase
{
    [JsonPropertyName("dateFormat")]
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = "#808080";

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 10;

    [JsonPropertyName("isItalic")]
    public bool IsItalic { get; set; } = true;

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("horizontalAlignment")]
    public HorizontalTextAlignment HorizontalAlignment { get; set; } = HorizontalTextAlignment.Center;

    [JsonPropertyName("verticalAlignment")]
    public VerticalTextAlignment VerticalAlignment { get; set; } = VerticalTextAlignment.Center;

    public override string DisplayName => "Date Range";
    public override ReportElementType GetElementType() => ReportElementType.DateRange;

    public override ReportElementBase Clone()
    {
        return new DateRangeReportElement
        {
            Id = Guid.NewGuid().ToString(),
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZOrder = ZOrder,
            IsVisible = IsVisible,
            DateFormat = DateFormat,
            TextColor = TextColor,
            FontSize = FontSize,
            IsItalic = IsItalic,
            FontFamily = FontFamily,
            HorizontalAlignment = HorizontalAlignment,
            VerticalAlignment = VerticalAlignment
        };
    }
}

/// <summary>
/// Summary element for displaying statistics and key metrics.
/// </summary>
public class SummaryReportElement : ReportElementBase
{
    [JsonPropertyName("transactionType")]
    public TransactionType TransactionType { get; set; } = TransactionType.Both;

    [JsonPropertyName("includeReturns")]
    public bool IncludeReturns { get; set; } = true;

    [JsonPropertyName("includeLosses")]
    public bool IncludeLosses { get; set; } = true;

    [JsonPropertyName("showTotalSales")]
    public bool ShowTotalSales { get; set; } = true;

    [JsonPropertyName("showTotalTransactions")]
    public bool ShowTotalTransactions { get; set; } = true;

    [JsonPropertyName("showAverageValue")]
    public bool ShowAverageValue { get; set; } = true;

    [JsonPropertyName("showGrowthRate")]
    public bool ShowGrowthRate { get; set; } = true;

    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; } = "#F5F5F5";

    [JsonPropertyName("borderThickness")]
    public int BorderThickness { get; set; } = 1;

    [JsonPropertyName("borderColor")]
    public string BorderColor { get; set; } = "#D3D3D3";

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 10;

    [JsonPropertyName("horizontalAlignment")]
    public HorizontalTextAlignment HorizontalAlignment { get; set; } = HorizontalTextAlignment.Left;

    [JsonPropertyName("verticalAlignment")]
    public VerticalTextAlignment VerticalAlignment { get; set; } = VerticalTextAlignment.Top;

    public override string DisplayName => "Summary";
    public override ReportElementType GetElementType() => ReportElementType.Summary;

    public override ReportElementBase Clone()
    {
        return new SummaryReportElement
        {
            Id = Guid.NewGuid().ToString(),
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZOrder = ZOrder,
            IsVisible = IsVisible,
            TransactionType = TransactionType,
            IncludeReturns = IncludeReturns,
            IncludeLosses = IncludeLosses,
            ShowTotalSales = ShowTotalSales,
            ShowTotalTransactions = ShowTotalTransactions,
            ShowAverageValue = ShowAverageValue,
            ShowGrowthRate = ShowGrowthRate,
            BackgroundColor = BackgroundColor,
            BorderThickness = BorderThickness,
            BorderColor = BorderColor,
            FontFamily = FontFamily,
            FontSize = FontSize,
            HorizontalAlignment = HorizontalAlignment,
            VerticalAlignment = VerticalAlignment
        };
    }
}
