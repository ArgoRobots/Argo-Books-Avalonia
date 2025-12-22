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
    private TransactionType _transactionType = TransactionType.Both;
    private bool _includeReturns = true;
    private bool _includeLosses = true;
    private TableDataSelection _dataSelection = TableDataSelection.All;
    private TableSortOrder _sortOrder = TableSortOrder.DateDescending;
    private int _maxRows = 10;
    private bool _showHeaders = true;
    private bool _alternateRowColors = true;
    private bool _showGridLines = true;
    private bool _showTotalsRow;
    private bool _autoSizeColumns = true;
    private double _fontSize = 8;
    private string _fontFamily = "Segoe UI";
    private int _dataRowHeight = 20;
    private int _headerRowHeight = 25;
    private int _cellPadding = 3;
    private string _headerBackgroundColor = "#5E94FF";
    private string _headerTextColor = "#FFFFFF";
    private string _dataRowTextColor = "#000000";
    private string _gridLineColor = "#D3D3D3";
    private string _baseRowColor = "#FFFFFF";
    private string _alternateRowColor = "#F8F8F8";
    private bool _showDateColumn = true;
    private bool _showTransactionIdColumn = true;
    private bool _showCompanyColumn = true;
    private bool _showProductColumn = true;
    private bool _showQuantityColumn = true;
    private bool _showUnitPriceColumn;
    private bool _showTotalColumn = true;
    private bool _showStatusColumn;
    private bool _showAccountantColumn;
    private bool _showShippingColumn;

    [JsonPropertyName("transactionType")]
    public TransactionType TransactionType
    {
        get => _transactionType;
        set => SetField(ref _transactionType, value);
    }

    [JsonPropertyName("includeReturns")]
    public bool IncludeReturns
    {
        get => _includeReturns;
        set => SetField(ref _includeReturns, value);
    }

    [JsonPropertyName("includeLosses")]
    public bool IncludeLosses
    {
        get => _includeLosses;
        set => SetField(ref _includeLosses, value);
    }

    [JsonPropertyName("dataSelection")]
    public TableDataSelection DataSelection
    {
        get => _dataSelection;
        set => SetField(ref _dataSelection, value);
    }

    [JsonPropertyName("sortOrder")]
    public TableSortOrder SortOrder
    {
        get => _sortOrder;
        set => SetField(ref _sortOrder, value);
    }

    [JsonPropertyName("maxRows")]
    public int MaxRows
    {
        get => _maxRows;
        set => SetField(ref _maxRows, value);
    }

    [JsonPropertyName("showHeaders")]
    public bool ShowHeaders
    {
        get => _showHeaders;
        set => SetField(ref _showHeaders, value);
    }

    [JsonPropertyName("alternateRowColors")]
    public bool AlternateRowColors
    {
        get => _alternateRowColors;
        set => SetField(ref _alternateRowColors, value);
    }

    [JsonPropertyName("showGridLines")]
    public bool ShowGridLines
    {
        get => _showGridLines;
        set => SetField(ref _showGridLines, value);
    }

    [JsonPropertyName("showTotalsRow")]
    public bool ShowTotalsRow
    {
        get => _showTotalsRow;
        set => SetField(ref _showTotalsRow, value);
    }

    [JsonPropertyName("autoSizeColumns")]
    public bool AutoSizeColumns
    {
        get => _autoSizeColumns;
        set => SetField(ref _autoSizeColumns, value);
    }

    [JsonPropertyName("fontSize")]
    public double FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    [JsonPropertyName("fontFamily")]
    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    [JsonPropertyName("dataRowHeight")]
    public int DataRowHeight
    {
        get => _dataRowHeight;
        set => SetField(ref _dataRowHeight, value);
    }

    [JsonPropertyName("headerRowHeight")]
    public int HeaderRowHeight
    {
        get => _headerRowHeight;
        set => SetField(ref _headerRowHeight, value);
    }

    [JsonPropertyName("cellPadding")]
    public int CellPadding
    {
        get => _cellPadding;
        set => SetField(ref _cellPadding, value);
    }

    [JsonPropertyName("headerBackgroundColor")]
    public string HeaderBackgroundColor
    {
        get => _headerBackgroundColor;
        set => SetField(ref _headerBackgroundColor, value);
    }

    [JsonPropertyName("headerTextColor")]
    public string HeaderTextColor
    {
        get => _headerTextColor;
        set => SetField(ref _headerTextColor, value);
    }

    [JsonPropertyName("dataRowTextColor")]
    public string DataRowTextColor
    {
        get => _dataRowTextColor;
        set => SetField(ref _dataRowTextColor, value);
    }

    [JsonPropertyName("gridLineColor")]
    public string GridLineColor
    {
        get => _gridLineColor;
        set => SetField(ref _gridLineColor, value);
    }

    [JsonPropertyName("baseRowColor")]
    public string BaseRowColor
    {
        get => _baseRowColor;
        set => SetField(ref _baseRowColor, value);
    }

    [JsonPropertyName("alternateRowColor")]
    public string AlternateRowColor
    {
        get => _alternateRowColor;
        set => SetField(ref _alternateRowColor, value);
    }

    // Column visibility
    [JsonPropertyName("showDateColumn")]
    public bool ShowDateColumn
    {
        get => _showDateColumn;
        set => SetField(ref _showDateColumn, value);
    }

    [JsonPropertyName("showTransactionIdColumn")]
    public bool ShowTransactionIdColumn
    {
        get => _showTransactionIdColumn;
        set => SetField(ref _showTransactionIdColumn, value);
    }

    [JsonPropertyName("showCompanyColumn")]
    public bool ShowCompanyColumn
    {
        get => _showCompanyColumn;
        set => SetField(ref _showCompanyColumn, value);
    }

    [JsonPropertyName("showProductColumn")]
    public bool ShowProductColumn
    {
        get => _showProductColumn;
        set => SetField(ref _showProductColumn, value);
    }

    [JsonPropertyName("showQuantityColumn")]
    public bool ShowQuantityColumn
    {
        get => _showQuantityColumn;
        set => SetField(ref _showQuantityColumn, value);
    }

    [JsonPropertyName("showUnitPriceColumn")]
    public bool ShowUnitPriceColumn
    {
        get => _showUnitPriceColumn;
        set => SetField(ref _showUnitPriceColumn, value);
    }

    [JsonPropertyName("showTotalColumn")]
    public bool ShowTotalColumn
    {
        get => _showTotalColumn;
        set => SetField(ref _showTotalColumn, value);
    }

    [JsonPropertyName("showStatusColumn")]
    public bool ShowStatusColumn
    {
        get => _showStatusColumn;
        set => SetField(ref _showStatusColumn, value);
    }

    [JsonPropertyName("showAccountantColumn")]
    public bool ShowAccountantColumn
    {
        get => _showAccountantColumn;
        set => SetField(ref _showAccountantColumn, value);
    }

    [JsonPropertyName("showShippingColumn")]
    public bool ShowShippingColumn
    {
        get => _showShippingColumn;
        set => SetField(ref _showShippingColumn, value);
    }

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
    private string _dateFormat = "yyyy-MM-dd";
    private string _textColor = "#808080";
    private double _fontSize = 10;
    private bool _isBold;
    private bool _isItalic = true;
    private bool _isUnderline;
    private string _fontFamily = "Segoe UI";
    private HorizontalTextAlignment _horizontalAlignment = HorizontalTextAlignment.Center;
    private VerticalTextAlignment _verticalAlignment = VerticalTextAlignment.Center;

    [JsonPropertyName("dateFormat")]
    public string DateFormat
    {
        get => _dateFormat;
        set => SetField(ref _dateFormat, value);
    }

    [JsonPropertyName("textColor")]
    public string TextColor
    {
        get => _textColor;
        set => SetField(ref _textColor, value);
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

    [JsonPropertyName("fontFamily")]
    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
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
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderline = IsUnderline,
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
    private TransactionType _transactionType = TransactionType.Both;
    private bool _includeReturns = true;
    private bool _includeLosses = true;
    private bool _showTotalSales = true;
    private bool _showTotalTransactions = true;
    private bool _showAverageValue = true;
    private bool _showGrowthRate = true;
    private string _backgroundColor = "#F5F5F5";
    private int _borderThickness = 1;
    private string _borderColor = "#D3D3D3";
    private string _fontFamily = "Segoe UI";
    private double _fontSize = 10;
    private HorizontalTextAlignment _horizontalAlignment = HorizontalTextAlignment.Left;
    private VerticalTextAlignment _verticalAlignment = VerticalTextAlignment.Top;

    [JsonPropertyName("transactionType")]
    public TransactionType TransactionType
    {
        get => _transactionType;
        set => SetField(ref _transactionType, value);
    }

    [JsonPropertyName("includeReturns")]
    public bool IncludeReturns
    {
        get => _includeReturns;
        set => SetField(ref _includeReturns, value);
    }

    [JsonPropertyName("includeLosses")]
    public bool IncludeLosses
    {
        get => _includeLosses;
        set => SetField(ref _includeLosses, value);
    }

    [JsonPropertyName("showTotalSales")]
    public bool ShowTotalSales
    {
        get => _showTotalSales;
        set => SetField(ref _showTotalSales, value);
    }

    [JsonPropertyName("showTotalTransactions")]
    public bool ShowTotalTransactions
    {
        get => _showTotalTransactions;
        set => SetField(ref _showTotalTransactions, value);
    }

    [JsonPropertyName("showAverageValue")]
    public bool ShowAverageValue
    {
        get => _showAverageValue;
        set => SetField(ref _showAverageValue, value);
    }

    [JsonPropertyName("showGrowthRate")]
    public bool ShowGrowthRate
    {
        get => _showGrowthRate;
        set => SetField(ref _showGrowthRate, value);
    }

    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetField(ref _backgroundColor, value);
    }

    [JsonPropertyName("borderThickness")]
    public int BorderThickness
    {
        get => _borderThickness;
        set => SetField(ref _borderThickness, value);
    }

    [JsonPropertyName("borderColor")]
    public string BorderColor
    {
        get => _borderColor;
        set => SetField(ref _borderColor, value);
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
