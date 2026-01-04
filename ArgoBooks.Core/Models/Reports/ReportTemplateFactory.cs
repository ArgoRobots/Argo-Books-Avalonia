using ArgoBooks.Core.Enums;
using static ArgoBooks.Core.Models.Reports.TemplateLayoutHelper;

namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Provides predefined report templates for common business reporting scenarios.
/// </summary>
public static class ReportTemplateFactory
{
    /// <summary>
    /// Template names constants.
    /// </summary>
    public static class TemplateNames
    {
        public const string Custom = "Custom Report";
        public const string MonthlySales = "Monthly Sales Report";
        public const string FinancialOverview = "Financial Overview";
        public const string PerformanceAnalysis = "Performance Analysis";
        public const string ReturnsAnalysis = "Returns Analysis";
        public const string LossesAnalysis = "Losses Analysis";
        public const string GeographicAnalysis = "Geographic Analysis";
    }

    /// <summary>
    /// Gets all available built-in template names.
    /// </summary>
    public static string[] GetBuiltInTemplateNames()
    {
        return
        [
            TemplateNames.Custom,
            TemplateNames.MonthlySales,
            TemplateNames.FinancialOverview,
            TemplateNames.PerformanceAnalysis,
            TemplateNames.ReturnsAnalysis,
            TemplateNames.LossesAnalysis,
            TemplateNames.GeographicAnalysis
        ];
    }

    /// <summary>
    /// Checks if the given template name is a built-in template.
    /// </summary>
    public static bool IsBuiltInTemplate(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;

        return templateName == TemplateNames.Custom ||
               templateName == TemplateNames.MonthlySales ||
               templateName == TemplateNames.FinancialOverview ||
               templateName == TemplateNames.PerformanceAnalysis ||
               templateName == TemplateNames.ReturnsAnalysis ||
               templateName == TemplateNames.LossesAnalysis ||
               templateName == TemplateNames.GeographicAnalysis;
    }

    /// <summary>
    /// Creates a report configuration from a template name.
    /// </summary>
    public static ReportConfiguration CreateFromTemplate(string templateName)
    {
        return templateName switch
        {
            TemplateNames.MonthlySales => CreateMonthlySalesTemplate(),
            TemplateNames.FinancialOverview => CreateFinancialOverviewTemplate(),
            TemplateNames.PerformanceAnalysis => CreatePerformanceAnalysisTemplate(),
            TemplateNames.ReturnsAnalysis => CreateReturnsAnalysisTemplate(),
            TemplateNames.LossesAnalysis => CreateLossesAnalysisTemplate(),
            TemplateNames.GeographicAnalysis => CreateGeographicAnalysisTemplate(),
            _ => new ReportConfiguration()
        };
    }

    /// <summary>
    /// Creates a monthly sales report template.
    /// </summary>
    public static ReportConfiguration CreateMonthlySalesTemplate()
    {
        var config = new ReportConfiguration
        {
            Title = "Monthly Sales Report",
            PageSize = PageSize.A4,
            PageOrientation = PageOrientation.Landscape,
            ShowHeader = true,
            ShowFooter = true,
            ShowPageNumbers = true
        };

        config.Filters.TransactionType = TransactionType.Revenue;
        config.Filters.DatePresetName = DatePresetNames.ThisMonth;
        config.Filters.SelectedChartTypes.AddRange(
        [
            ChartDataType.TotalRevenue,
            ChartDataType.RevenueDistribution,
            ChartDataType.GrowthRates,
            ChartDataType.AverageTransactionValue
        ]);

        AddSalesReportElements(config);
        return config;
    }

    /// <summary>
    /// Creates a financial overview template.
    /// </summary>
    public static ReportConfiguration CreateFinancialOverviewTemplate()
    {
        var config = new ReportConfiguration
        {
            Title = "Financial Overview",
            PageSize = PageSize.A4,
            PageOrientation = PageOrientation.Landscape,
            ShowHeader = true,
            ShowFooter = true,
            ShowPageNumbers = true
        };

        config.Filters.TransactionType = TransactionType.Both;
        config.Filters.DatePresetName = DatePresetNames.ThisQuarter;
        config.Filters.SelectedChartTypes.AddRange(
        [
            ChartDataType.TotalRevenue,
            ChartDataType.TotalExpenses,
            ChartDataType.SalesVsExpenses,
            ChartDataType.TotalProfits
        ]);

        AddFinancialOverviewElements(config);
        return config;
    }

    /// <summary>
    /// Creates a performance analysis template.
    /// </summary>
    public static ReportConfiguration CreatePerformanceAnalysisTemplate()
    {
        var config = new ReportConfiguration
        {
            Title = "Performance Analysis",
            PageSize = PageSize.A4,
            PageOrientation = PageOrientation.Portrait,
            ShowHeader = true,
            ShowFooter = true,
            ShowPageNumbers = true
        };

        config.Filters.TransactionType = TransactionType.Revenue;
        config.Filters.DatePresetName = DatePresetNames.Last30Days;
        config.Filters.SelectedChartTypes.AddRange(
        [
            ChartDataType.GrowthRates,
            ChartDataType.AverageTransactionValue,
            ChartDataType.TotalTransactions,
            ChartDataType.ReturnsOverTime
        ]);

        AddPerformanceAnalysisElements(config);
        return config;
    }

    /// <summary>
    /// Creates a returns analysis template.
    /// </summary>
    public static ReportConfiguration CreateReturnsAnalysisTemplate()
    {
        var config = new ReportConfiguration
        {
            Title = "Returns Analysis",
            PageSize = PageSize.A4,
            PageOrientation = PageOrientation.Portrait,
            ShowHeader = true,
            ShowFooter = true,
            ShowPageNumbers = true
        };

        config.Filters.TransactionType = TransactionType.Revenue;
        config.Filters.IncludeReturns = true;
        config.Filters.IncludeLosses = false;
        config.Filters.DatePresetName = DatePresetNames.YearToDate;
        config.Filters.SelectedChartTypes.AddRange(
        [
            ChartDataType.ReturnsOverTime,
            ChartDataType.ReturnReasons,
            ChartDataType.ReturnFinancialImpact,
            ChartDataType.ReturnsByCategory,
            ChartDataType.ReturnsByProduct
        ]);

        AddReturnsAnalysisElements(config);
        return config;
    }

    /// <summary>
    /// Creates a losses analysis template.
    /// </summary>
    public static ReportConfiguration CreateLossesAnalysisTemplate()
    {
        var config = new ReportConfiguration
        {
            Title = "Losses Analysis",
            PageSize = PageSize.A4,
            PageOrientation = PageOrientation.Portrait,
            ShowHeader = true,
            ShowFooter = true,
            ShowPageNumbers = true
        };

        config.Filters.TransactionType = TransactionType.Revenue;
        config.Filters.IncludeReturns = false;
        config.Filters.IncludeLosses = true;
        config.Filters.DatePresetName = DatePresetNames.YearToDate;
        config.Filters.SelectedChartTypes.AddRange(
        [
            ChartDataType.LossesOverTime,
            ChartDataType.LossReasons,
            ChartDataType.LossFinancialImpact,
            ChartDataType.LossesByCategory,
            ChartDataType.LossesByProduct
        ]);

        AddLossesAnalysisElements(config);
        return config;
    }

    /// <summary>
    /// Creates a geographic analysis template.
    /// </summary>
    public static ReportConfiguration CreateGeographicAnalysisTemplate()
    {
        var config = new ReportConfiguration
        {
            Title = "Geographic Analysis",
            PageSize = PageSize.A4,
            PageOrientation = PageOrientation.Landscape,
            ShowHeader = true,
            ShowFooter = true,
            ShowPageNumbers = true
        };

        config.Filters.TransactionType = TransactionType.Revenue;
        config.Filters.DatePresetName = DatePresetNames.Last30Days;
        config.Filters.SelectedChartTypes.AddRange(
        [
            ChartDataType.WorldMap,
            ChartDataType.CountriesOfOrigin,
            ChartDataType.CountriesOfDestination,
            ChartDataType.CompaniesOfOrigin
        ]);

        AddGeographicAnalysisElements(config);
        return config;
    }

    #region Element Addition Methods

    private static void AddSalesReportElements(ReportConfiguration config)
    {
        var context = new LayoutContext(config);

        // Create a 2x2 grid for charts below the date range
        var grid = CreateGrid(context, 2, 2);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.TotalRevenue,
            X = grid[0, 0].X,
            Y = grid[0, 0].Y,
            Width = grid[0, 0].Width,
            Height = grid[0, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.RevenueDistribution,
            X = grid[0, 1].X,
            Y = grid[0, 1].Y,
            Width = grid[0, 1].Width,
            Height = grid[0, 1].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.GrowthRates,
            X = grid[1, 0].X,
            Y = grid[1, 0].Y,
            Width = grid[1, 0].Width,
            Height = grid[1, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.AverageTransactionValue,
            X = grid[1, 1].X,
            Y = grid[1, 1].Y,
            Width = grid[1, 1].Width,
            Height = grid[1, 1].Height
        });

        // Date range element - added last so it renders on top (highest ZOrder)
        var dateRangeBounds = GetDateRangeBounds(context);
        config.AddElement(new DateRangeReportElement
        {
            X = context.Margin + (context.ContentWidth - 200) / 2,
            Y = dateRangeBounds.Y,
            Height = dateRangeBounds.Height
        });
    }

    private static void AddFinancialOverviewElements(ReportConfiguration config)
    {
        var context = new LayoutContext(config);

        // Create 2x2 grid for charts below the date range
        var grid = CreateGrid(context, 2, 2);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.SalesVsExpenses,
            X = grid[0, 0].X,
            Y = grid[0, 0].Y,
            Width = grid[0, 0].Width,
            Height = grid[0, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.TotalProfits,
            X = grid[0, 1].X,
            Y = grid[0, 1].Y,
            Width = grid[0, 1].Width,
            Height = grid[0, 1].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.TotalRevenue,
            X = grid[1, 0].X,
            Y = grid[1, 0].Y,
            Width = grid[1, 0].Width,
            Height = grid[1, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.TotalExpenses,
            X = grid[1, 1].X,
            Y = grid[1, 1].Y,
            Width = grid[1, 1].Width,
            Height = grid[1, 1].Height
        });

        // Date range element - added last so it renders on top (highest ZOrder)
        var dateRangeBounds = GetDateRangeBounds(context);
        config.AddElement(new DateRangeReportElement
        {
            X = context.Margin + (context.ContentWidth - 200) / 2,
            Y = dateRangeBounds.Y,
            Height = dateRangeBounds.Height
        });
    }

    private static void AddPerformanceAnalysisElements(ReportConfiguration config)
    {
        var context = new LayoutContext(config);

        // Create vertical stack: summary (15%), then 3 equal charts below the date range
        var stack = CreateVerticalStack(context, 0.15, 0.28, 0.28, 0.29);

        config.AddElement(new SummaryReportElement
        {
            X = stack[0].X,
            Y = stack[0].Y,
            Width = stack[0].Width,
            Height = stack[0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.GrowthRates,
            X = stack[1].X,
            Y = stack[1].Y,
            Width = stack[1].Width,
            Height = stack[1].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.AverageTransactionValue,
            X = stack[2].X,
            Y = stack[2].Y,
            Width = stack[2].Width,
            Height = stack[2].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.ReturnsOverTime,
            X = stack[3].X,
            Y = stack[3].Y,
            Width = stack[3].Width,
            Height = stack[3].Height
        });

        // Date range element - added last so it renders on top (highest ZOrder)
        var dateRangeBounds = GetDateRangeBounds(context);
        config.AddElement(new DateRangeReportElement
        {
            X = context.Margin + (context.ContentWidth - 200) / 2,
            Y = dateRangeBounds.Y,
            Height = dateRangeBounds.Height
        });
    }

    private static void AddReturnsAnalysisElements(ReportConfiguration config)
    {
        var context = new LayoutContext(config);

        // Mixed layout: full-width chart at top, then 2x2 grid below the date range
        var topStack = CreateVerticalStack(context, 0.33, 0.67);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.ReturnsOverTime,
            X = topStack[0].X,
            Y = topStack[0].Y,
            Width = topStack[0].Width,
            Height = topStack[0].Height
        });

        // Create a 2x2 grid in the bottom portion
        var bottomGrid = SplitIntoGrid(topStack[1], 2, 2, context.ElementSpacing);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.ReturnReasons,
            X = bottomGrid[0, 0].X,
            Y = bottomGrid[0, 0].Y,
            Width = bottomGrid[0, 0].Width,
            Height = bottomGrid[0, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.ReturnFinancialImpact,
            X = bottomGrid[0, 1].X,
            Y = bottomGrid[0, 1].Y,
            Width = bottomGrid[0, 1].Width,
            Height = bottomGrid[0, 1].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.ReturnsByCategory,
            X = bottomGrid[1, 0].X,
            Y = bottomGrid[1, 0].Y,
            Width = bottomGrid[1, 0].Width,
            Height = bottomGrid[1, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.ReturnsByProduct,
            X = bottomGrid[1, 1].X,
            Y = bottomGrid[1, 1].Y,
            Width = bottomGrid[1, 1].Width,
            Height = bottomGrid[1, 1].Height
        });

        // Date range element - added last so it renders on top (highest ZOrder)
        var dateRangeBounds = GetDateRangeBounds(context);
        config.AddElement(new DateRangeReportElement
        {
            X = context.Margin + (context.ContentWidth - 200) / 2,
            Y = dateRangeBounds.Y,
            Height = dateRangeBounds.Height
        });
    }

    private static void AddLossesAnalysisElements(ReportConfiguration config)
    {
        var context = new LayoutContext(config);

        // Mixed layout: full-width chart at top, then 2x2 grid below the date range
        var topStack = CreateVerticalStack(context, 0.33, 0.67);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.LossesOverTime,
            X = topStack[0].X,
            Y = topStack[0].Y,
            Width = topStack[0].Width,
            Height = topStack[0].Height
        });

        // Create a 2x2 grid in the bottom portion
        var bottomGrid = SplitIntoGrid(topStack[1], 2, 2, context.ElementSpacing);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.LossReasons,
            X = bottomGrid[0, 0].X,
            Y = bottomGrid[0, 0].Y,
            Width = bottomGrid[0, 0].Width,
            Height = bottomGrid[0, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.LossFinancialImpact,
            X = bottomGrid[0, 1].X,
            Y = bottomGrid[0, 1].Y,
            Width = bottomGrid[0, 1].Width,
            Height = bottomGrid[0, 1].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.LossesByCategory,
            X = bottomGrid[1, 0].X,
            Y = bottomGrid[1, 0].Y,
            Width = bottomGrid[1, 0].Width,
            Height = bottomGrid[1, 0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.LossesByProduct,
            X = bottomGrid[1, 1].X,
            Y = bottomGrid[1, 1].Y,
            Width = bottomGrid[1, 1].Width,
            Height = bottomGrid[1, 1].Height
        });

        // Date range element - added last so it renders on top (highest ZOrder)
        var dateRangeBounds = GetDateRangeBounds(context);
        config.AddElement(new DateRangeReportElement
        {
            X = context.Margin + (context.ContentWidth - 200) / 2,
            Y = dateRangeBounds.Y,
            Height = dateRangeBounds.Height
        });
    }

    private static void AddGeographicAnalysisElements(ReportConfiguration config)
    {
        var context = new LayoutContext(config);

        // Create vertical split: map (50%), then bottom grid (50%) below the date range
        var stack = CreateVerticalStack(context, 0.5, 0.5);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.WorldMap,
            X = stack[0].X,
            Y = stack[0].Y,
            Width = stack[0].Width,
            Height = stack[0].Height
        });

        // Split bottom area into 2 columns
        var columns = SplitHorizontally(stack[1], [0.5, 0.5], context.ElementSpacing);

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.CountriesOfOrigin,
            X = columns[0].X,
            Y = columns[0].Y,
            Width = columns[0].Width,
            Height = columns[0].Height
        });

        config.AddElement(new ChartReportElement
        {
            ChartType = ChartDataType.CountriesOfDestination,
            X = columns[1].X,
            Y = columns[1].Y,
            Width = columns[1].Width,
            Height = columns[1].Height
        });

        // Date range element - added last so it renders on top (highest ZOrder)
        var dateRangeBounds = GetDateRangeBounds(context);
        config.AddElement(new DateRangeReportElement
        {
            X = context.Margin + (context.ContentWidth - 200) / 2,
            Y = dateRangeBounds.Y,
            Height = dateRangeBounds.Height
        });
    }

    #endregion
}
