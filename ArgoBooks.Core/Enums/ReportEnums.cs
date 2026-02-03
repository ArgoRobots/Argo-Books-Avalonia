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
/// Element alignment options for the report designer.
/// </summary>
public enum ElementAlignment
{
    Left,
    Right,
    Top,
    Bottom,
    CenterH,
    CenterV
}

/// <summary>
/// Distribution direction for spacing elements evenly.
/// </summary>
public enum DistributeDirection
{
    Horizontal,
    Vertical
}

/// <summary>
/// Size matching mode for making elements the same size.
/// </summary>
public enum MatchSizeMode
{
    Width,
    Height,
    Both
}

/// <summary>
/// Transaction type filter options.
/// </summary>
public enum TransactionType
{
    Revenue,
    Expenses
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
    RevenueVsExpenses,
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
    CompaniesOfDestination,

    // Accountant charts
    AccountantsTransactions,

    // Customer charts
    TopCustomersByRevenue,
    CustomerPaymentStatus,
    CustomerGrowth,
    CustomerLifetimeValue,
    ActiveVsInactiveCustomers,
    RentalsPerCustomer,

    // Returns charts
    ReturnsOverTime,
    ReturnReasons,
    ReturnFinancialImpact,
    ReturnsByCategory,
    ReturnsByProduct,
    ExpenseVsRevenueReturns,

    // Losses charts
    LossesOverTime,
    LossReasons,
    LossFinancialImpact,
    LossesByCategory,
    LossesByProduct,
    ExpenseVsRevenueLosses
}

/// <summary>
/// Visual chart style for rendering series in reports.
/// </summary>
public enum ReportChartStyle
{
    Bar,
    Line,
    StepLine,
    Area,
    Scatter
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

/// <summary>
/// Extension methods for report enums.
/// </summary>
public static class ReportEnumExtensions
{
    /// <summary>
    /// Gets a user-friendly display name for a table sort order.
    /// </summary>
    public static string GetDisplayName(this TableSortOrder sortOrder)
    {
        return sortOrder switch
        {
            TableSortOrder.DateDescending => "Date descending",
            TableSortOrder.DateAscending => "Date ascending",
            TableSortOrder.AmountDescending => "Amount descending",
            TableSortOrder.AmountAscending => "Amount ascending",
            _ => sortOrder.ToString()
        };
    }

    /// <summary>
    /// Gets a user-friendly display name for a chart data type.
    /// </summary>
    public static string GetDisplayName(this ChartDataType chartType)
    {
        return chartType switch
        {
            // Revenue charts
            ChartDataType.TotalRevenue => "Revenue Trends",
            ChartDataType.RevenueDistribution => "Revenue Distribution",

            // Expense charts
            ChartDataType.TotalExpenses => "Expense Trends",
            ChartDataType.ExpensesDistribution => "Expense Distribution",

            // Financial charts
            ChartDataType.TotalProfits => "Profit Over Time",
            ChartDataType.RevenueVsExpenses => "Expenses vs Revenue",
            ChartDataType.GrowthRates => "Growth Rates",

            // Transaction charts
            ChartDataType.AverageTransactionValue => "Average Transaction Value",
            ChartDataType.TotalTransactions => "Total Transactions",
            ChartDataType.AverageShippingCosts => "Average Shipping Costs",

            // Geographic charts
            ChartDataType.WorldMap => "World Map Overview",
            ChartDataType.CountriesOfOrigin => "Countries of Origin",
            ChartDataType.CountriesOfDestination => "Countries of Destination",
            ChartDataType.CompaniesOfOrigin => "Companies of Origin",
            ChartDataType.CompaniesOfDestination => "Companies of Destination",

            // Accountant charts
            ChartDataType.AccountantsTransactions => "Transactions by Accountant",

            // Customer charts
            ChartDataType.TopCustomersByRevenue => "Top Customers by Revenue",
            ChartDataType.CustomerPaymentStatus => "Customer Payment Status",
            ChartDataType.CustomerGrowth => "Customer Growth",
            ChartDataType.CustomerLifetimeValue => "Customer Lifetime Value",
            ChartDataType.ActiveVsInactiveCustomers => "Active vs Inactive Customers",
            ChartDataType.RentalsPerCustomer => "Rentals per Customer",

            // Returns charts
            ChartDataType.ReturnsOverTime => "Returns Over Time",
            ChartDataType.ReturnReasons => "Return Reasons",
            ChartDataType.ReturnFinancialImpact => "Financial Impact of Returns",
            ChartDataType.ReturnsByCategory => "Returns by Category",
            ChartDataType.ReturnsByProduct => "Returns by Product",
            ChartDataType.ExpenseVsRevenueReturns => "Expense vs Revenue Returns",

            // Losses charts
            ChartDataType.LossesOverTime => "Losses Over Time",
            ChartDataType.LossReasons => "Loss Reasons",
            ChartDataType.LossFinancialImpact => "Financial Impact of Losses",
            ChartDataType.LossesByCategory => "Losses by Category",
            ChartDataType.LossesByProduct => "Losses by Product",
            ChartDataType.ExpenseVsRevenueLosses => "Expense vs Revenue Losses",

            _ => chartType.ToString()
        };
    }
}
