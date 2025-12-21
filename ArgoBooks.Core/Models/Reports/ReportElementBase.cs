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
public abstract class ReportElementBase
{
    /// <summary>
    /// Unique identifier for the element.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// X position on the page.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Y position on the page.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Element width.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; set; } = 200;

    /// <summary>
    /// Element height.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; set; } = 150;

    /// <summary>
    /// Z-order for layering.
    /// </summary>
    [JsonPropertyName("zOrder")]
    public int ZOrder { get; set; }

    /// <summary>
    /// Whether the element is visible.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; } = true;

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
}

/// <summary>
/// Chart element for displaying graphs and charts.
/// </summary>
public class ChartReportElement : ReportElementBase
{
    [JsonPropertyName("chartType")]
    public ChartDataType ChartType { get; set; } = ChartDataType.TotalRevenue;

    [JsonPropertyName("showLegend")]
    public bool ShowLegend { get; set; } = true;

    [JsonPropertyName("showTitle")]
    public bool ShowTitle { get; set; } = true;

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("titleFontSize")]
    public double TitleFontSize { get; set; } = 12;

    [JsonPropertyName("legendFontSize")]
    public double LegendFontSize { get; set; } = 11;

    [JsonPropertyName("borderColor")]
    public string BorderColor { get; set; } = "#808080";

    [JsonPropertyName("borderThickness")]
    public int BorderThickness { get; set; } = 1;

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
    [JsonPropertyName("text")]
    public string Text { get; set; } = "Sample Text";

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Segoe UI";

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 12;

    [JsonPropertyName("isBold")]
    public bool IsBold { get; set; }

    [JsonPropertyName("isItalic")]
    public bool IsItalic { get; set; }

    [JsonPropertyName("isUnderline")]
    public bool IsUnderline { get; set; }

    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = "#000000";

    [JsonPropertyName("horizontalAlignment")]
    public HorizontalTextAlignment HorizontalAlignment { get; set; } = HorizontalTextAlignment.Center;

    [JsonPropertyName("verticalAlignment")]
    public VerticalTextAlignment VerticalAlignment { get; set; } = VerticalTextAlignment.Center;

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
    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("scaleMode")]
    public ImageScaleMode ScaleMode { get; set; } = ImageScaleMode.Fit;

    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; } = "#00FFFFFF";

    [JsonPropertyName("borderColor")]
    public string BorderColor { get; set; } = "#00FFFFFF";

    [JsonPropertyName("borderThickness")]
    public int BorderThickness { get; set; } = 1;

    [JsonPropertyName("cornerRadiusPercent")]
    public int CornerRadiusPercent { get; set; }

    [JsonPropertyName("opacity")]
    public byte Opacity { get; set; } = 255;

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
