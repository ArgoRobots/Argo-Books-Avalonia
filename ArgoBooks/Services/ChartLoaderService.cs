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
            MaxBarWidth = 50,
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
    /// Loads revenue overview chart data as a column series.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, decimal TotalRevenue) LoadRevenueOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        decimal totalRevenue = 0;

        if (companyData?.Sales == null || companyData.Sales.Count == 0)
        {
            CurrentExportData = new ChartExportData
            {
                ChartTitle = "Revenue Overview",
                ChartType = ChartType.Revenue,
                Labels = [],
                Values = [],
                SeriesName = "Revenue"
            };
            return (series, labels, totalRevenue);
        }

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var revenueByDate = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.Total) })
            .ToList();

        if (revenueByDate.Count == 0)
        {
            CurrentExportData = new ChartExportData
            {
                ChartTitle = "Revenue Overview",
                ChartType = ChartType.Revenue,
                Labels = [],
                Values = [],
                SeriesName = "Revenue"
            };
            return (series, labels, totalRevenue);
        }

        labels = revenueByDate.Select(r => r.Date.ToString("yyyy-MM-dd")).ToArray();
        var values = revenueByDate.Select(r => (double)r.Total).ToArray();
        totalRevenue = revenueByDate.Sum(r => r.Total);

        var columnSeries = new ColumnSeries<double>
        {
            Values = values,
            Name = "Revenue",
            Fill = new SolidColorPaint(ProfitColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        };

        series.Add(columnSeries);

        CurrentExportData = new ChartExportData
        {
            ChartTitle = "Revenue Overview",
            ChartType = ChartType.Revenue,
            Labels = labels,
            Values = values,
            SeriesName = "Revenue",
            TotalValue = (double)totalRevenue,
            StartDate = start,
            EndDate = end
        };

        return (series, labels, totalRevenue);
    }

    /// <summary>
    /// Loads profits overview chart data as a column series.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, decimal TotalProfit) LoadProfitsOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        decimal totalProfit = 0;

        if (companyData == null)
        {
            CurrentExportData = new ChartExportData
            {
                ChartTitle = "Profits Overview",
                ChartType = ChartType.Profit,
                Labels = [],
                Values = [],
                SeriesName = "Profit"
            };
            return (series, labels, totalProfit);
        }

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        // Get revenue by date
        var revenueByDate = companyData.Sales?
            .Where(s => s.Date >= start && s.Date <= end)
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Total)) ?? new Dictionary<DateTime, decimal>();

        // Get expenses by date
        var expensesByDate = companyData.Purchases?
            .Where(p => p.Date >= start && p.Date <= end)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Total)) ?? new Dictionary<DateTime, decimal>();

        // Calculate profit by date
        var allDates = revenueByDate.Keys.Union(expensesByDate.Keys).OrderBy(d => d).ToList();

        if (allDates.Count == 0)
        {
            CurrentExportData = new ChartExportData
            {
                ChartTitle = "Profits Overview",
                ChartType = ChartType.Profit,
                Labels = [],
                Values = [],
                SeriesName = "Profit"
            };
            return (series, labels, totalProfit);
        }

        var profitData = allDates.Select(date => new
        {
            Date = date,
            Profit = revenueByDate.GetValueOrDefault(date, 0) - expensesByDate.GetValueOrDefault(date, 0)
        }).ToList();

        labels = profitData.Select(p => p.Date.ToString("yyyy-MM-dd")).ToArray();
        var values = profitData.Select(p => (double)p.Profit).ToArray();
        totalProfit = profitData.Sum(p => p.Profit);

        var columnSeries = new ColumnSeries<double>
        {
            Values = values,
            Name = "Profit",
            Fill = new SolidColorPaint(ProfitColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        };

        series.Add(columnSeries);

        CurrentExportData = new ChartExportData
        {
            ChartTitle = "Profits Overview",
            ChartType = ChartType.Profit,
            Labels = labels,
            Values = values,
            SeriesName = "Profit",
            TotalValue = (double)totalProfit,
            StartDate = start,
            EndDate = end
        };

        return (series, labels, totalProfit);
    }

    /// <summary>
    /// Loads sales vs expenses comparison chart as a multi-series column chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadSalesVsExpensesChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        if (companyData == null)
            return (series, labels);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-6);

        // Get months in range
        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count == 0)
            return (series, labels);

        labels = months.Select(m => m.ToString("MMM yyyy")).ToArray();

        // Calculate revenue per month
        var revenueValues = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return (double)(companyData.Sales?
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .Sum(s => s.Total) ?? 0);
        }).ToArray();

        // Calculate expenses per month
        var expenseValues = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return (double)(companyData.Purchases?
                .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                .Sum(p => p.Total) ?? 0);
        }).ToArray();

        series.Add(new ColumnSeries<double>
        {
            Values = revenueValues,
            Name = "Revenue",
            Fill = new SolidColorPaint(ProfitColor),
            Stroke = null,
            MaxBarWidth = 30,
            Padding = 2
        });

        series.Add(new ColumnSeries<double>
        {
            Values = expenseValues,
            Name = "Expenses",
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 30,
            Padding = 2
        });

        return (series, labels);
    }

    /// <summary>
    /// Loads revenue distribution by category as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadRevenueDistributionChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData?.Sales == null || companyData.Sales.Count == 0)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .GroupBy(s => s.CategoryId ?? "Unknown")
            .Select(g => new
            {
                Category = companyData.GetCategory(g.Key)?.Name ?? "Other",
                Total = g.Sum(s => s.Total)
            })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Total);

        var pieSeriesList = new List<PieSeries<double>>();
        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            pieSeriesList.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Total },
                Name = item.Category,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        foreach (var ps in pieSeriesList)
            series.Add(ps);

        return (series, total);
    }

    /// <summary>
    /// Loads expense distribution by category as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadExpenseDistributionChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData?.Purchases == null || companyData.Purchases.Count == 0)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .GroupBy(p => p.CategoryId ?? "Unknown")
            .Select(g => new
            {
                Category = companyData.GetCategory(g.Key)?.Name ?? "Other",
                Total = g.Sum(p => p.Total)
            })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Total);

        var pieSeriesList = new List<PieSeries<double>>();
        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            pieSeriesList.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Total },
                Name = item.Category,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        foreach (var ps in pieSeriesList)
            series.Add(ps);

        return (series, total);
    }

    /// <summary>
    /// Loads growth rates chart data.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadGrowthRatesChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        if (companyData?.Sales == null)
            return (series, labels);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-12);

        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count < 2)
            return (series, labels);

        var growthRates = new List<double>();
        var growthLabels = new List<string>();

        for (int i = 1; i < months.Count; i++)
        {
            var currentMonth = months[i];
            var previousMonth = months[i - 1];

            var currentMonthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
            var previousMonthStart = new DateTime(previousMonth.Year, previousMonth.Month, 1);
            var previousMonthEnd = previousMonthStart.AddMonths(1).AddDays(-1);

            var currentRevenue = companyData.Sales
                .Where(s => s.Date >= currentMonthStart && s.Date <= currentMonthEnd)
                .Sum(s => s.Total);

            var previousRevenue = companyData.Sales
                .Where(s => s.Date >= previousMonthStart && s.Date <= previousMonthEnd)
                .Sum(s => s.Total);

            double growthRate = 0;
            if (previousRevenue != 0)
                growthRate = (double)((currentRevenue - previousRevenue) / previousRevenue * 100);
            else if (currentRevenue > 0)
                growthRate = 100;

            growthRates.Add(growthRate);
            growthLabels.Add(currentMonth.ToString("MMM yyyy"));
        }

        labels = growthLabels.ToArray();

        series.Add(new ColumnSeries<double>
        {
            Values = growthRates.ToArray(),
            Name = "Growth Rate %",
            Fill = new SolidColorPaint(RevenueColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels);
    }

    /// <summary>
    /// Loads average transaction value chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadAverageTransactionValueChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        if (companyData == null)
            return (series, labels);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-6);

        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count == 0)
            return (series, labels);

        labels = months.Select(m => m.ToString("MMM yyyy")).ToArray();

        var avgValues = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var transactions = new List<decimal>();
            transactions.AddRange(companyData.Sales?
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .Select(s => s.Total) ?? []);
            transactions.AddRange(companyData.Purchases?
                .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                .Select(p => p.Total) ?? []);

            return transactions.Count > 0 ? (double)transactions.Average() : 0;
        }).ToArray();

        series.Add(new ColumnSeries<double>
        {
            Values = avgValues,
            Name = "Avg Transaction",
            Fill = new SolidColorPaint(RevenueColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels);
    }

    /// <summary>
    /// Loads total transactions count chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadTotalTransactionsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        if (companyData == null)
            return (series, labels);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-6);

        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count == 0)
            return (series, labels);

        labels = months.Select(m => m.ToString("MMM yyyy")).ToArray();

        var countValues = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var salesCount = companyData.Sales?.Count(s => s.Date >= monthStart && s.Date <= monthEnd) ?? 0;
            var purchaseCount = companyData.Purchases?.Count(p => p.Date >= monthStart && p.Date <= monthEnd) ?? 0;

            return (double)(salesCount + purchaseCount);
        }).ToArray();

        series.Add(new ColumnSeries<double>
        {
            Values = countValues,
            Name = "Transactions",
            Fill = new SolidColorPaint(RevenueColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels);
    }

    /// <summary>
    /// Loads average shipping costs chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadAverageShippingCostsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        if (companyData == null)
            return (series, labels);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-6);

        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count == 0)
            return (series, labels);

        labels = months.Select(m => m.ToString("MMM yyyy")).ToArray();

        var avgShipping = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var shippingCosts = new List<decimal>();
            shippingCosts.AddRange(companyData.Sales?
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .Select(s => s.ShippingCost) ?? []);
            shippingCosts.AddRange(companyData.Purchases?
                .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                .Select(p => p.ShippingCost) ?? []);

            return shippingCosts.Count > 0 ? (double)shippingCosts.Average() : 0;
        }).ToArray();

        series.Add(new ColumnSeries<double>
        {
            Values = avgShipping,
            Name = "Avg Shipping",
            Fill = new SolidColorPaint(RevenueColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels);
    }

    /// <summary>
    /// Loads countries of origin (sales by customer country) as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadCountriesOfOriginChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData?.Sales == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .GroupBy(s =>
            {
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address?.Country ?? "Unknown";
            })
            .Select(g => new { Country = g.Key, Total = g.Sum(s => s.Total) })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Total);

        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Total },
                Name = item.Country,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        return (series, total);
    }

    /// <summary>
    /// Loads countries of destination (purchases by supplier country) as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadCountriesOfDestinationChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData?.Purchases == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .GroupBy(p =>
            {
                var supplier = companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address?.Country ?? "Unknown";
            })
            .Select(g => new { Country = g.Key, Total = g.Sum(p => p.Total) })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Total);

        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Total },
                Name = item.Country,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        return (series, total);
    }

    /// <summary>
    /// Loads companies of origin (sales by customer) as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadCompaniesOfOriginChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData?.Sales == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .GroupBy(s => companyData.GetCustomer(s.CustomerId ?? "")?.Name ?? "Unknown")
            .Select(g => new { Company = g.Key, Total = g.Sum(s => s.Total) })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Total);

        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Total },
                Name = item.Company,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        return (series, total);
    }

    /// <summary>
    /// Loads accountants transactions as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadAccountantsTransactionsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var accountantData = new Dictionary<string, decimal>();

        // Sales by accountant
        if (companyData.Sales != null)
        {
            foreach (var sale in companyData.Sales.Where(s => s.Date >= start && s.Date <= end))
            {
                var accountantName = companyData.GetAccountant(sale.AccountantId ?? "")?.Name ?? "Unknown";
                accountantData.TryAdd(accountantName, 0);
                accountantData[accountantName] += sale.Total;
            }
        }

        // Purchases by accountant
        if (companyData.Purchases != null)
        {
            foreach (var purchase in companyData.Purchases.Where(p => p.Date >= start && p.Date <= end))
            {
                var accountantName = companyData.GetAccountant(purchase.AccountantId ?? "")?.Name ?? "Unknown";
                accountantData.TryAdd(accountantName, 0);
                accountantData[accountantName] += purchase.Total;
            }
        }

        var distribution = accountantData
            .OrderByDescending(kvp => kvp.Value)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Value);

        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Value },
                Name = item.Key,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        return (series, total);
    }

    /// <summary>
    /// Loads returns over time chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, int TotalReturns) LoadReturnsOverTimeChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        int totalReturns = 0;

        if (companyData?.Returns == null || companyData.Returns.Count == 0)
            return (series, labels, totalReturns);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var returnsByDate = companyData.Returns
            .Where(r => r.ReturnDate >= start && r.ReturnDate <= end)
            .GroupBy(r => r.ReturnDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        if (returnsByDate.Count == 0)
            return (series, labels, totalReturns);

        labels = returnsByDate.Select(r => r.Date.ToString("yyyy-MM-dd")).ToArray();
        var values = returnsByDate.Select(r => (double)r.Count).ToArray();
        totalReturns = returnsByDate.Sum(r => r.Count);

        series.Add(new ColumnSeries<double>
        {
            Values = values,
            Name = "Returns",
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels, totalReturns);
    }

    /// <summary>
    /// Loads return reasons as a pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, int Total) LoadReturnReasonsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        int total = 0;

        if (companyData?.Returns == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.Returns
            .Where(r => r.ReturnDate >= start && r.ReturnDate <= end)
            .SelectMany(r => r.Items ?? [])
            .GroupBy(item => string.IsNullOrEmpty(item.Reason) ? "Unknown" : item.Reason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
            return (series, total);

        total = distribution.Sum(d => d.Count);

        for (int i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)item.Count },
                Name = item.Reason,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        return (series, total);
    }

    /// <summary>
    /// Loads return financial impact chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, decimal TotalImpact) LoadReturnFinancialImpactChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        decimal totalImpact = 0;

        if (companyData?.Returns == null)
            return (series, labels, totalImpact);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-6);

        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count == 0)
            return (series, labels, totalImpact);

        labels = months.Select(m => m.ToString("MMM yyyy")).ToArray();

        var impactValues = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return (double)(companyData.Returns
                .Where(r => r.ReturnDate >= monthStart && r.ReturnDate <= monthEnd)
                .Sum(r => r.RefundAmount));
        }).ToArray();

        totalImpact = (decimal)impactValues.Sum();

        series.Add(new ColumnSeries<double>
        {
            Values = impactValues,
            Name = "Refunds",
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels, totalImpact);
    }

    /// <summary>
    /// Loads losses over time chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, int TotalLosses) LoadLossesOverTimeChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        int totalLosses = 0;

        if (companyData?.LostDamaged == null || companyData.LostDamaged.Count == 0)
            return (series, labels, totalLosses);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var lossesByDate = companyData.LostDamaged
            .Where(l => l.DateDiscovered >= start && l.DateDiscovered <= end)
            .GroupBy(l => l.DateDiscovered.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        if (lossesByDate.Count == 0)
            return (series, labels, totalLosses);

        labels = lossesByDate.Select(l => l.Date.ToString("yyyy-MM-dd")).ToArray();
        var values = lossesByDate.Select(l => (double)l.Count).ToArray();
        totalLosses = lossesByDate.Sum(l => l.Count);

        series.Add(new ColumnSeries<double>
        {
            Values = values,
            Name = "Losses",
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels, totalLosses);
    }

    /// <summary>
    /// Loads loss financial impact chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, decimal TotalImpact) LoadLossFinancialImpactChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        decimal totalImpact = 0;

        if (companyData?.LostDamaged == null)
            return (series, labels, totalImpact);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-6);

        var months = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            months.Add(current);
            current = current.AddMonths(1);
        }

        if (months.Count == 0)
            return (series, labels, totalImpact);

        labels = months.Select(m => m.ToString("MMM yyyy")).ToArray();

        var impactValues = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return (double)(companyData.LostDamaged
                .Where(l => l.DateDiscovered >= monthStart && l.DateDiscovered <= monthEnd)
                .Sum(l => l.ValueLost));
        }).ToArray();

        totalImpact = (decimal)impactValues.Sum();

        series.Add(new ColumnSeries<double>
        {
            Values = impactValues,
            Name = "Value Lost",
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 50,
            Padding = 2
        });

        return (series, labels, totalImpact);
    }

    /// <summary>
    /// Loads world map data for GeoMap chart.
    /// </summary>
    public Dictionary<string, double> LoadWorldMapData(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var mapData = new Dictionary<string, double>();

        if (companyData?.Sales == null)
            return mapData;

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        return companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .GroupBy(s =>
            {
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address?.Country;
            })
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => (double)g.Sum(s => s.Total));
    }

    /// <summary>
    /// Loads world map data by supplier country for GeoMap chart (destination mode).
    /// </summary>
    public Dictionary<string, double> LoadWorldMapDataBySupplier(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var mapData = new Dictionary<string, double>();

        if (companyData?.Purchases == null)
            return mapData;

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        return companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .GroupBy(p =>
            {
                var supplier = companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address?.Country;
            })
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => (double)g.Sum(p => p.Total));
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
