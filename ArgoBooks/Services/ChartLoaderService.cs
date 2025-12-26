using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ArgoBooks.Services;

/// <summary>
/// Service for loading and configuring charts with consistent styling.
/// Designed to work with Google Sheets export, Microsoft Excel export, and report generator systems.
/// </summary>
public class ChartLoaderService
{
    // Chart text size matching WinForms version
    private const float AxisTextSize = 14f;
    private const float LegendTextSize = 14f;
    private const float TooltipTextSize = 14f;

    // Chart colors
    private static readonly SKColor RevenueColor = SKColor.Parse("#6495ED"); // Cornflower Blue (matches WinForms)
    private static readonly SKColor ExpenseColor = SKColor.Parse("#EF4444"); // Red
    private static readonly SKColor ProfitColor = SKColor.Parse("#22C55E"); // Green

    // Theme colors (will be updated based on current theme)
    private SKColor _textColor = SKColor.Parse("#F9FAFB"); // Light text for dark theme
    private SKColor _gridColor = SKColor.Parse("#374151"); // Grid lines
    private SKColor _backgroundColor = SKColor.Parse("#1F2937"); // Chart background

    /// <summary>
    /// Gets or sets the current data for export functionality.
    /// This is used by Google Sheets and Excel exporters.
    /// </summary>
    public ChartExportData? CurrentExportData { get; private set; }

    /// <summary>
    /// Updates theme colors based on the current application theme.
    /// </summary>
    /// <param name="isDarkTheme">Whether the dark theme is active.</param>
    public void UpdateThemeColors(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            _textColor = SKColor.Parse("#F9FAFB");
            _gridColor = SKColor.Parse("#374151");
            _backgroundColor = SKColor.Parse("#1F2937");
        }
        else
        {
            _textColor = SKColor.Parse("#111827");
            _gridColor = SKColor.Parse("#E5E7EB");
            _backgroundColor = SKColor.Parse("#FFFFFF");
        }
    }

    /// <summary>
    /// Creates X-axis configuration for a cartesian chart.
    /// </summary>
    /// <param name="labels">The labels for the X-axis.</param>
    /// <returns>Configured X-axis array.</returns>
    public Axis[] CreateXAxes(IEnumerable<string>? labels = null)
    {
        var axis = new Axis
        {
            TextSize = AxisTextSize,
            LabelsPaint = new SolidColorPaint(_textColor),
            Labels = labels?.ToArray(),
            LabelsRotation = 0
        };

        return [axis];
    }

    /// <summary>
    /// Creates Y-axis configuration for a cartesian chart with currency formatting.
    /// </summary>
    /// <param name="currencySymbol">The currency symbol to use (default: $).</param>
    /// <returns>Configured Y-axis array.</returns>
    public Axis[] CreateCurrencyYAxes(string currencySymbol = "$")
    {
        var axis = new Axis
        {
            TextSize = AxisTextSize,
            LabelsPaint = new SolidColorPaint(_textColor),
            SeparatorsPaint = new SolidColorPaint(_gridColor) { StrokeThickness = 1 },
            Labeler = value => $"{currencySymbol}{value:N2}"
        };

        return [axis];
    }

    /// <summary>
    /// Creates Y-axis configuration for a cartesian chart with number formatting.
    /// </summary>
    /// <returns>Configured Y-axis array.</returns>
    public Axis[] CreateNumberYAxes()
    {
        var axis = new Axis
        {
            TextSize = AxisTextSize,
            LabelsPaint = new SolidColorPaint(_textColor),
            SeparatorsPaint = new SolidColorPaint(_gridColor) { StrokeThickness = 1 },
            Labeler = value => value.ToString("N0")
        };

        return [axis];
    }

    /// <summary>
    /// Creates the tooltip paint for charts.
    /// </summary>
    /// <returns>Tooltip paint configuration.</returns>
    public SolidColorPaint CreateTooltipTextPaint()
    {
        return new SolidColorPaint(_textColor);
    }

    /// <summary>
    /// Creates the tooltip background paint for charts.
    /// </summary>
    /// <returns>Tooltip background paint configuration.</returns>
    public SolidColorPaint CreateTooltipBackgroundPaint()
    {
        return new SolidColorPaint(_backgroundColor);
    }

    /// <summary>
    /// Loads expenses overview chart data as a column series.
    /// </summary>
    /// <param name="companyData">The company data to load from.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>A tuple containing the series collection and X-axis labels.</returns>
    public (ObservableCollection<ISeries> Series, string[] Labels, decimal TotalExpenses) LoadExpensesOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        decimal totalExpenses = 0;

        if (companyData?.Purchases == null || companyData.Purchases.Count == 0)
        {
            // Store empty export data
            CurrentExportData = new ChartExportData
            {
                ChartTitle = "Expenses Overview",
                ChartType = ChartType.Expense,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, labels, totalExpenses);
        }

        // Default date range: last 30 days
        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        // Group purchases/expenses by date
        var expensesByDate = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .GroupBy(p => p.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Sum(p => p.Total)
            })
            .ToList();

        if (expensesByDate.Count == 0)
        {
            CurrentExportData = new ChartExportData
            {
                ChartTitle = "Expenses Overview",
                ChartType = ChartType.Expense,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, labels, totalExpenses);
        }

        // Create labels and values
        labels = expensesByDate.Select(e => e.Date.ToString("yyyy-MM-dd")).ToArray();
        var values = expensesByDate.Select(e => (double)e.Total).ToArray();
        totalExpenses = expensesByDate.Sum(e => e.Total);

        // Create column series with WinForms-style appearance
        var columnSeries = new ColumnSeries<double>
        {
            Values = values,
            Name = "Expenses",
            Fill = new SolidColorPaint(RevenueColor), // Using the blue color like WinForms
            Stroke = null,
            MaxBarWidth = double.MaxValue,
            Padding = 2
        };

        series.Add(columnSeries);

        // Store export data for Google Sheets/Excel export
        CurrentExportData = new ChartExportData
        {
            ChartTitle = "Expenses Overview",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Expenses",
            TotalValue = (double)totalExpenses,
            StartDate = start,
            EndDate = end
        };

        return (series, labels, totalExpenses);
    }

    /// <summary>
    /// Gets the data for exporting to Google Sheets.
    /// </summary>
    /// <returns>Export data formatted for Google Sheets.</returns>
    public List<List<object>> GetGoogleSheetsExportData()
    {
        if (CurrentExportData == null)
            return [];

        var data = new List<List<object>>
        {
            // Header row
            new() { "Date", CurrentExportData.SeriesName }
        };

        // Data rows
        for (int i = 0; i < CurrentExportData.Labels.Length; i++)
        {
            data.Add(new List<object>
            {
                CurrentExportData.Labels[i],
                CurrentExportData.Values[i]
            });
        }

        // Total row
        if (CurrentExportData.TotalValue.HasValue)
        {
            data.Add(new List<object> { "Total", CurrentExportData.TotalValue.Value });
        }

        return data;
    }

    /// <summary>
    /// Gets the data for exporting to Microsoft Excel.
    /// </summary>
    /// <returns>Export data formatted for Excel.</returns>
    public ExcelExportData GetExcelExportData()
    {
        if (CurrentExportData == null)
            return new ExcelExportData();

        return new ExcelExportData
        {
            SheetName = CurrentExportData.ChartTitle,
            Headers = ["Date", CurrentExportData.SeriesName],
            Rows = CurrentExportData.Labels
                .Zip(CurrentExportData.Values, (label, value) => new object[] { label, value })
                .ToList(),
            TotalRow = CurrentExportData.TotalValue.HasValue
                ? new object[] { "Total", CurrentExportData.TotalValue.Value }
                : null
        };
    }

    /// <summary>
    /// Gets raw chart data for report generation.
    /// </summary>
    /// <returns>The current export data.</returns>
    public ChartExportData? GetReportData()
    {
        return CurrentExportData;
    }

    /// <summary>
    /// Resets the zoom on a cartesian chart by clearing axis limits.
    /// </summary>
    /// <param name="xAxes">The X-axes to reset.</param>
    /// <param name="yAxes">The Y-axes to reset.</param>
    public static void ResetZoom(IEnumerable<Axis>? xAxes, IEnumerable<Axis>? yAxes)
    {
        if (xAxes != null)
        {
            foreach (var axis in xAxes)
            {
                axis.MinLimit = null;
                axis.MaxLimit = null;
            }
        }

        if (yAxes != null)
        {
            foreach (var axis in yAxes)
            {
                axis.MinLimit = null;
                axis.MaxLimit = null;
            }
        }
    }

    /// <summary>
    /// Gets a color for a series by index.
    /// </summary>
    /// <param name="index">The series index.</param>
    /// <returns>An SKColor for the series.</returns>
    public static SKColor GetColorForIndex(int index)
    {
        var colors = new[]
        {
            SKColor.Parse("#6495ED"), // Cornflower Blue
            SKColor.Parse("#EF4444"), // Red
            SKColor.Parse("#22C55E"), // Green
            SKColor.Parse("#F59E0B"), // Amber
            SKColor.Parse("#8B5CF6"), // Purple
            SKColor.Parse("#EC4899"), // Pink
            SKColor.Parse("#14B8A6"), // Teal
            SKColor.Parse("#F97316"), // Orange
        };

        return colors[index % colors.Length];
    }
}

/// <summary>
/// Data structure for chart export functionality.
/// </summary>
public class ChartExportData
{
    public string ChartTitle { get; set; } = string.Empty;
    public ChartType ChartType { get; set; }
    public string[] Labels { get; set; } = [];
    public double[] Values { get; set; } = [];
    public string SeriesName { get; set; } = string.Empty;
    public double? TotalValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Chart type enumeration for export handling.
/// </summary>
public enum ChartType
{
    Revenue,
    Expense,
    Profit,
    Distribution,
    Comparison
}

/// <summary>
/// Data structure for Excel export.
/// </summary>
public class ExcelExportData
{
    public string SheetName { get; set; } = "Chart Data";
    public string[] Headers { get; set; } = [];
    public List<object[]> Rows { get; set; } = [];
    public object[]? TotalRow { get; set; }
}
