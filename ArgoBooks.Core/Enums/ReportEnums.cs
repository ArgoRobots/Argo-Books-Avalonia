namespace ArgoBooks.Core.Enums;

/// <summary>
/// Types of elements that can be included in a report.
/// </summary>
public enum ReportElementType
{
    Chart,
    Table,
    Label,
    Image,
    DateRange,
    Summary
}

/// <summary>
/// Supported page sizes for reports.
/// </summary>
public enum PageSize
{
    A4,
    Letter,
    Legal,
    Tabloid,
    A3,
    Custom
}

/// <summary>
/// Page orientation options.
/// </summary>
public enum PageOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Transaction type filter options.
/// </summary>
public enum TransactionType
{
    Revenue,
    Expenses,
    Both
}

/// <summary>
/// Supported export formats.
/// </summary>
public enum ExportFormat
{
    PDF,
    PNG,
    JPEG
}

/// <summary>
/// Image scaling modes for display.
/// </summary>
public enum ImageScaleMode
{
    Stretch,
    Fit,
    Fill,
    Center
}

/// <summary>
/// Table data selection modes.
/// </summary>
public enum TableDataSelection
{
    All,
    TopByAmount,
    BottomByAmount,
    ReturnsOnly,
    LossesOnly
}

/// <summary>
/// Table sorting options.
/// </summary>
public enum TableSortOrder
{
    DateDescending,
    DateAscending,
    AmountDescending,
    AmountAscending
}

/// <summary>
/// Chart data types available in reports.
/// </summary>
public enum ChartDataType
{
    // Revenue charts
    TotalRevenue,
    RevenueDistribution,

    // Expense charts
    TotalExpenses,
    ExpensesDistribution,

    // Financial charts
    TotalProfits,
    SalesVsExpenses,
    GrowthRates,

    // Transaction charts
    AverageTransactionValue,
    TotalTransactions,
    AverageShippingCosts,

    // Geographic charts
    WorldMap,
    CountriesOfOrigin,
    CountriesOfDestination,
    CompaniesOfOrigin,

    // Accountant charts
    AccountantsTransactions,

    // Returns charts
    ReturnsOverTime,
    ReturnReasons,
    ReturnFinancialImpact,
    ReturnsByCategory,
    ReturnsByProduct,
    PurchaseVsSaleReturns,

    // Losses charts
    LossesOverTime,
    LossReasons,
    LossFinancialImpact,
    LossesByCategory,
    LossesByProduct,
    PurchaseVsSaleLosses
}

/// <summary>
/// Visual chart style for rendering series in reports.
/// </summary>
public enum ReportChartStyle
{
    Bar,
    Line,
    StepLine,
    Area
}

/// <summary>
/// Text horizontal alignment options.
/// </summary>
public enum HorizontalTextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Text vertical alignment options.
/// </summary>
public enum VerticalTextAlignment
{
    Top,
    Center,
    Bottom
}
