using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Transactions;
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
    /// Gets the legend text paint based on the current theme.
    /// </summary>
    public static SolidColorPaint GetLegendTextPaint()
    {
        var isDarkTheme = ThemeService.Instance.IsDarkTheme;
        var textColor = isDarkTheme ? SKColor.Parse("#F9FAFB") : SKColor.Parse("#1F2937");
        return new SolidColorPaint(textColor) { FontFamily = "Segoe UI" };
    }

    // Country name to ISO 3166-1 alpha-3 code mapping for GeoMap
    private static readonly Dictionary<string, string> CountryNameToIsoCode = new(StringComparer.OrdinalIgnoreCase)
    {
        { "United States", "usa" }, { "USA", "usa" }, { "US", "usa" }, { "America", "usa" },
        { "United Kingdom", "gbr" }, { "UK", "gbr" }, { "Great Britain", "gbr" }, { "England", "gbr" },
        { "Canada", "can" }, { "CA", "can" },
        { "Germany", "deu" }, { "DE", "deu" },
        { "France", "fra" }, { "FR", "fra" },
        { "Italy", "ita" }, { "IT", "ita" },
        { "Spain", "esp" }, { "ES", "esp" },
        { "Australia", "aus" }, { "AU", "aus" },
        { "Japan", "jpn" }, { "JP", "jpn" },
        { "China", "chn" }, { "CN", "chn" },
        { "India", "ind" }, { "IN", "ind" },
        { "Brazil", "bra" }, { "BR", "bra" },
        { "Mexico", "mex" }, { "MX", "mex" },
        { "Russia", "rus" }, { "RU", "rus" },
        { "South Korea", "kor" }, { "Korea", "kor" }, { "KR", "kor" },
        { "Netherlands", "nld" }, { "NL", "nld" },
        { "Switzerland", "che" }, { "CH", "che" },
        { "Sweden", "swe" }, { "SE", "swe" },
        { "Norway", "nor" }, { "NO", "nor" },
        { "Denmark", "dnk" }, { "DK", "dnk" },
        { "Finland", "fin" }, { "FI", "fin" },
        { "Poland", "pol" }, { "PL", "pol" },
        { "Belgium", "bel" }, { "BE", "bel" },
        { "Austria", "aut" }, { "AT", "aut" },
        { "Ireland", "irl" }, { "IE", "irl" },
        { "Portugal", "prt" }, { "PT", "prt" },
        { "Greece", "grc" }, { "GR", "grc" },
        { "New Zealand", "nzl" }, { "NZ", "nzl" },
        { "Singapore", "sgp" }, { "SG", "sgp" },
        { "Hong Kong", "hkg" }, { "HK", "hkg" },
        { "Taiwan", "twn" }, { "TW", "twn" },
        { "South Africa", "zaf" }, { "ZA", "zaf" },
        { "Argentina", "arg" }, { "AR", "arg" },
        { "Chile", "chl" }, { "CL", "chl" },
        { "Colombia", "col" }, { "CO", "col" },
        { "Indonesia", "idn" }, { "ID", "idn" },
        { "Malaysia", "mys" }, { "MY", "mys" },
        { "Thailand", "tha" }, { "TH", "tha" },
        { "Vietnam", "vnm" }, { "VN", "vnm" },
        { "Philippines", "phl" }, { "PH", "phl" },
        { "Turkey", "tur" }, { "TR", "tur" },
        { "Saudi Arabia", "sau" }, { "SA", "sau" },
        { "UAE", "are" }, { "United Arab Emirates", "are" }, { "AE", "are" },
        { "Israel", "isr" }, { "IL", "isr" },
        { "Egypt", "egy" }, { "EG", "egy" },
        { "Nigeria", "nga" }, { "NG", "nga" },
        { "Kenya", "ken" }, { "KE", "ken" },
        { "Ukraine", "ukr" }, { "UA", "ukr" },
        { "Czech Republic", "cze" }, { "Czechia", "cze" }, { "CZ", "cze" },
        { "Romania", "rou" }, { "RO", "rou" },
        { "Hungary", "hun" }, { "HU", "hun" }
    };

    /// <summary>
    /// Converts a country name to ISO 3166-1 alpha-3 code for GeoMap.
    /// </summary>
    private static string GetCountryIsoCode(string? countryName)
    {
        if (string.IsNullOrEmpty(countryName))
            return string.Empty;

        return CountryNameToIsoCode.TryGetValue(countryName, out var code) ? code : countryName.ToLowerInvariant();
    }

    /// <summary>
    /// Gets or sets the current data for export functionality.
    /// This is used by Google Sheets and Excel exporters.
    /// </summary>
    public ChartExportData? CurrentExportData { get; private set; }

    /// <summary>
    /// Gets or sets the pie chart data for export functionality.
    /// </summary>
    public ChartExportData? PieChartExportData { get; private set; }

    /// <summary>
    /// Dictionary storing export data for each chart by its title.
    /// </summary>
    private readonly Dictionary<string, ChartExportData> _chartExportDataByTitle = new();

    /// <summary>
    /// Gets or sets whether to use line charts instead of column charts.
    /// </summary>
    public bool UseLineChart { get; set; }

    /// <summary>
    /// Creates a series for time-based data, either as line or column based on UseLineChart.
    /// Uses categorical (index-based) positioning - dates will be evenly spaced.
    /// </summary>
    private ISeries CreateTimeSeries(double[] values, string name, SKColor color)
    {
        if (UseLineChart)
        {
            return new LineSeries<double>
            {
                Values = values,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            };
        }
        return new ColumnSeries<double>
        {
            Values = values,
            Name = name,
            Fill = new SolidColorPaint(color),
            Stroke = null,
            MaxBarWidth = 50
        };
    }

    /// <summary>
    /// Creates a series for date-based data with proportional spacing.
    /// Points are positioned based on actual date values, not evenly spaced.
    /// </summary>
    private ISeries CreateDateTimeSeries(DateTime[] dates, double[] values, string name, SKColor color)
    {
        // Convert to DateTimePoint for proper date-based positioning
        var points = dates.Zip(values, (d, v) => new DateTimePoint(d, v)).ToArray();

        if (UseLineChart)
        {
            return new LineSeries<DateTimePoint>
            {
                Values = points,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6,
                Mapping = (point, index) => new LiveChartsCore.Kernel.Coordinate(point.DateTime.ToOADate(), point.Value ?? 0)
            };
        }
        return new ColumnSeries<DateTimePoint>
        {
            Values = points,
            Name = name,
            Fill = new SolidColorPaint(color),
            Stroke = null,
            MaxBarWidth = 50,
            Mapping = (point, index) => new LiveChartsCore.Kernel.Coordinate(point.DateTime.ToOADate(), point.Value ?? 0)
        };
    }

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
    /// Creates X-axis configuration for a cartesian chart with categorical (evenly-spaced) labels.
    /// Use this for non-date categories like product names, months, etc.
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
    /// Creates X-axis configuration for a cartesian chart with proportional date spacing.
    /// Use this for time-series charts where date spacing should be proportional to actual time differences.
    /// </summary>
    /// <param name="dates">The actual DateTime values for proportional positioning.</param>
    /// <returns>Configured X-axis array.</returns>
    public Axis[] CreateDateXAxes(DateTime[]? dates = null)
    {
        if (dates == null || dates.Length == 0)
        {
            return CreateXAxes();
        }

        // Calculate min and max OADate values with padding
        var minDate = dates.Min().ToOADate();
        var maxDate = dates.Max().ToOADate();
        var padding = Math.Max(0.5, (maxDate - minDate) * 0.05); // 5% padding or at least 0.5 days

        var axis = new Axis
        {
            TextSize = AxisTextSize,
            LabelsPaint = new SolidColorPaint(_textColor),
            LabelsRotation = 0,
            MinLimit = minDate - padding,
            MaxLimit = maxDate + padding,
            Labeler = value =>
            {
                // Validate OADate range to prevent exceptions
                // Valid OADate range is approximately -657434 (year 100) to 2958466 (year 9999)
                if (value < -657434 || value > 2958466)
                {
                    return string.Empty;
                }
                try
                {
                    var date = DateTime.FromOADate(value);
                    return date.ToString("yyyy-MM-dd");
                }
                catch
                {
                    return string.Empty;
                }
            },
            MinStep = 1 // Minimum step of 1 day
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
    /// <returns>A tuple containing the series collection, X-axis labels, dates for proportional spacing, and total expenses.</returns>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates, decimal TotalExpenses) LoadExpensesOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();
        decimal totalExpenses = 0;

        if (companyData?.Purchases == null || companyData.Purchases.Count == 0)
        {
            // Store empty export data
            StoreExportData(new ChartExportData
            {
                ChartTitle = "Expenses Overview",
                ChartType = ChartType.Expense,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            });
            return (series, labels, dates, totalExpenses);
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
            StoreExportData(new ChartExportData
            {
                ChartTitle = "Expenses Overview",
                ChartType = ChartType.Expense,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            });
            return (series, labels, dates, totalExpenses);
        }

        // Create labels, dates, and values
        labels = expensesByDate.Select(e => e.Date.ToString("yyyy-MM-dd")).ToArray();
        dates = expensesByDate.Select(e => e.Date).ToArray();
        var values = expensesByDate.Select(e => (double)e.Total).ToArray();
        totalExpenses = expensesByDate.Sum(e => e.Total);

        // Create series with proportional date spacing
        series.Add(CreateDateTimeSeries(dates, values, "Expenses", RevenueColor));

        // Store export data for Google Sheets/Excel export
        StoreExportData(new ChartExportData
        {
            ChartTitle = "Expenses Overview",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Expenses",
            TotalValue = (double)totalExpenses,
            StartDate = start,
            EndDate = end
        });

        return (series, labels, dates, totalExpenses);
    }

    /// <summary>
    /// Loads revenue overview chart data as a column series.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates, decimal TotalRevenue) LoadRevenueOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();
        decimal totalRevenue = 0;

        if (companyData?.Sales == null || companyData.Sales.Count == 0)
        {
            StoreExportData(new ChartExportData
            {
                ChartTitle = "Revenue Overview",
                ChartType = ChartType.Revenue,
                Labels = [],
                Values = [],
                SeriesName = "Revenue"
            });
            return (series, labels, dates, totalRevenue);
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
            StoreExportData(new ChartExportData
            {
                ChartTitle = "Revenue Overview",
                ChartType = ChartType.Revenue,
                Labels = [],
                Values = [],
                SeriesName = "Revenue"
            });
            return (series, labels, dates, totalRevenue);
        }

        labels = revenueByDate.Select(r => r.Date.ToString("yyyy-MM-dd")).ToArray();
        dates = revenueByDate.Select(r => r.Date).ToArray();
        var values = revenueByDate.Select(r => (double)r.Total).ToArray();
        totalRevenue = revenueByDate.Sum(r => r.Total);

        series.Add(CreateDateTimeSeries(dates, values, "Revenue", ProfitColor));

        StoreExportData(new ChartExportData
        {
            ChartTitle = "Revenue Overview",
            ChartType = ChartType.Revenue,
            Labels = labels,
            Values = values,
            SeriesName = "Revenue",
            TotalValue = (double)totalRevenue,
            StartDate = start,
            EndDate = end
        });

        return (series, labels, dates, totalRevenue);
    }

    /// <summary>
    /// Loads profits overview chart data as a column series.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates, decimal TotalProfit) LoadProfitsOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();
        decimal totalProfit = 0;

        if (companyData == null)
        {
            StoreExportData(new ChartExportData
            {
                ChartTitle = "Profits Overview",
                ChartType = ChartType.Profit,
                Labels = [],
                Values = [],
                SeriesName = "Profit"
            });
            return (series, labels, dates, totalProfit);
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
            StoreExportData(new ChartExportData
            {
                ChartTitle = "Profits Overview",
                ChartType = ChartType.Profit,
                Labels = [],
                Values = [],
                SeriesName = "Profit"
            });
            return (series, labels, dates, totalProfit);
        }

        var profitData = allDates.Select(date => new
        {
            Date = date,
            Profit = revenueByDate.GetValueOrDefault(date, 0) - expensesByDate.GetValueOrDefault(date, 0)
        }).ToList();

        labels = profitData.Select(p => p.Date.ToString("yyyy-MM-dd")).ToArray();
        dates = profitData.Select(p => p.Date).ToArray();
        var values = profitData.Select(p => (double)p.Profit).ToArray();
        totalProfit = profitData.Sum(p => p.Profit);

        series.Add(CreateDateTimeSeries(dates, values, "Profit", ProfitColor));

        StoreExportData(new ChartExportData
        {
            ChartTitle = "Profits Overview",
            ChartType = ChartType.Profit,
            Labels = labels,
            Values = values,
            SeriesName = "Profit",
            TotalValue = (double)totalProfit,
            StartDate = start,
            EndDate = end
        });

        return (series, labels, dates, totalProfit);
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

        // Only add series if there's actual data
        if (revenueValues.Any(v => v > 0) || expenseValues.Any(v => v > 0))
        {
            series.Add(CreateTimeSeries(revenueValues, "Revenue", ProfitColor));
            series.Add(CreateTimeSeries(expenseValues, "Expenses", ExpenseColor));
        }

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

        var salesInRange = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .ToList();

        var distribution = salesInRange
            .GroupBy(s => GetEffectiveCategoryId(s, companyData) ?? "Unknown")
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

        // Store export data for Google Sheets/Excel export
        var exportData = new ChartExportData
        {
            ChartTitle = "Sales Distribution",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Category).ToArray(),
            Values = distribution.Select(d => (double)d.Total).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };
        _chartExportDataByTitle["Sales Distribution"] = exportData;

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
        {
            PieChartExportData = null;
            return (series, total);
        }

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var purchasesInRange = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .ToList();

        var distribution = purchasesInRange
            .GroupBy(p => GetEffectiveCategoryId(p, companyData) ?? "Unknown")
            .Select(g => new
            {
                Category = companyData.GetCategory(g.Key)?.Name ?? "Other",
                Total = g.Sum(p => p.Total)
            })
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        if (distribution.Count == 0)
        {
            PieChartExportData = null;
            return (series, total);
        }

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

        // Store export data for Google Sheets/Excel export
        PieChartExportData = new ChartExportData
        {
            ChartTitle = "Expense Distribution",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Category).ToArray(),
            Values = distribution.Select(d => (double)d.Total).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };

        // Also store by title for chart-specific retrieval (various UI titles)
        _chartExportDataByTitle["Expense Distribution"] = PieChartExportData;
        _chartExportDataByTitle["Purchase Distribution"] = PieChartExportData;
        _chartExportDataByTitle["Distribution of expenses"] = PieChartExportData;

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

        // Only add series if there's actual data (any non-zero growth rates)
        if (growthRates.Any(v => v != 0))
        {
            series.Add(new ColumnSeries<double>
            {
                Values = growthRates.ToArray(),
                Name = "Growth Rate %",
                Fill = new SolidColorPaint(RevenueColor),
                Stroke = null,
                MaxBarWidth = 50,
                Padding = 2
            });
        }

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

        // Only add series if there's actual data
        if (avgValues.Any(v => v > 0))
        {
            series.Add(new ColumnSeries<double>
            {
                Values = avgValues,
                Name = "Avg Transaction",
                Fill = new SolidColorPaint(RevenueColor),
                Stroke = null,
                MaxBarWidth = 50,
                Padding = 2
            });
        }

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

        // Only add series if there's actual data
        if (countValues.Any(v => v > 0))
        {
            series.Add(new ColumnSeries<double>
            {
                Values = countValues,
                Name = "Transactions",
                Fill = new SolidColorPaint(RevenueColor),
                Stroke = null,
                MaxBarWidth = 50,
                Padding = 2
            });
        }

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

        // Only add series if there's actual data
        if (avgShipping.Any(v => v > 0))
        {
            series.Add(new ColumnSeries<double>
            {
                Values = avgShipping,
                Name = "Avg Shipping",
                Fill = new SolidColorPaint(RevenueColor),
                Stroke = null,
                MaxBarWidth = 50,
                Padding = 2
            });
        }

        return (series, labels);
    }

    /// <summary>
    /// Loads countries of origin (sales by customer country, or product supplier country as fallback) as a pie chart.
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

        var salesInRange = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .ToList();

        var distribution = salesInRange
            .GroupBy(s =>
            {
                // First try customer country
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                if (customer?.Address?.Country != null && !string.IsNullOrEmpty(customer.Address.Country))
                    return customer.Address.Country;

                // Fall back to product's supplier country (origin of the product)
                var firstProductId = s.LineItems?.FirstOrDefault()?.ProductId;
                if (!string.IsNullOrEmpty(firstProductId))
                {
                    var product = companyData.GetProduct(firstProductId);
                    if (product != null && !string.IsNullOrEmpty(product.SupplierId))
                    {
                        var supplier = companyData.GetSupplier(product.SupplierId);
                        if (supplier?.Address?.Country != null && !string.IsNullOrEmpty(supplier.Address.Country))
                            return supplier.Address.Country;
                    }
                }

                return "Unknown";
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

        // Store export data
        _chartExportDataByTitle["Countries of Origin"] = new ChartExportData
        {
            ChartTitle = "Countries of Origin",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Country).ToArray(),
            Values = distribution.Select(d => (double)d.Total).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };

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

        var purchasesInRange = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .ToList();

        var distribution = purchasesInRange
            .GroupBy(p =>
            {
                // First try supplier from purchase
                var supplierId = GetEffectiveSupplierId(p, companyData);
                if (!string.IsNullOrEmpty(supplierId))
                {
                    var supplier = companyData.GetSupplier(supplierId);
                    if (supplier?.Address?.Country != null && !string.IsNullOrEmpty(supplier.Address.Country))
                        return supplier.Address.Country;
                }
                return "Unknown";
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

        // Store export data
        _chartExportDataByTitle["Countries of Destination"] = new ChartExportData
        {
            ChartTitle = "Countries of Destination",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Country).ToArray(),
            Values = distribution.Select(d => (double)d.Total).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };

        return (series, total);
    }

    /// <summary>
    /// Loads companies of origin (sales by customer, or product supplier as fallback) as a pie chart.
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

        var salesInRange = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .ToList();

        var distribution = salesInRange
            .GroupBy(s =>
            {
                // First try customer name
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                if (customer != null && !string.IsNullOrEmpty(customer.Name))
                    return customer.Name;

                // Fall back to product's supplier name (origin of the product)
                var firstProductId = s.LineItems?.FirstOrDefault()?.ProductId;
                if (!string.IsNullOrEmpty(firstProductId))
                {
                    var product = companyData.GetProduct(firstProductId);
                    if (product != null && !string.IsNullOrEmpty(product.SupplierId))
                    {
                        var supplier = companyData.GetSupplier(product.SupplierId);
                        if (supplier != null && !string.IsNullOrEmpty(supplier.Name))
                            return supplier.Name;
                    }
                }

                return "Unknown";
            })
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

        // Store export data
        _chartExportDataByTitle["Companies of Origin"] = new ChartExportData
        {
            ChartTitle = "Companies of Origin",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Company).ToArray(),
            Values = distribution.Select(d => (double)d.Total).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };

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

        // Store export data (used for "Transactions by Accountant" and "Companies of Destination")
        var exportData = new ChartExportData
        {
            ChartTitle = "Transactions by Accountant",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Key).ToArray(),
            Values = distribution.Select(d => (double)d.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };
        _chartExportDataByTitle["Transactions by Accountant"] = exportData;
        _chartExportDataByTitle["Companies of Destination"] = exportData;
        _chartExportDataByTitle["Workload Distribution"] = exportData;

        return (series, total);
    }

    /// <summary>
    /// Loads customer payment status chart (Paid vs Pending vs Overdue).
    /// </summary>
    public (ObservableCollection<ISeries> Series, int Total) LoadCustomerPaymentStatusChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        int total = 0;

        if (companyData?.Sales == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var salesInRange = companyData.Sales.Where(s => s.Date >= start && s.Date <= end).ToList();
        if (salesInRange.Count == 0)
            return (series, total);

        var paid = salesInRange.Count(s => s.PaymentStatus == "Paid" || s.PaymentStatus == "Complete");
        var pending = salesInRange.Count(s => s.PaymentStatus == "Pending" || string.IsNullOrEmpty(s.PaymentStatus));
        var overdue = salesInRange.Count(s => s.PaymentStatus == "Overdue");

        total = salesInRange.Count;

        var statusData = new[]
        {
            ("Paid", paid, SKColor.Parse("#22C55E")),
            ("Pending", pending, SKColor.Parse("#F59E0B")),
            ("Overdue", overdue, SKColor.Parse("#EF4444"))
        }.Where(x => x.Item2 > 0).ToList();

        foreach (var (name, count, color) in statusData)
        {
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)count },
                Name = name,
                Fill = new SolidColorPaint(color),
                Pushout = 0
            });
        }

        // Store export data
        _chartExportDataByTitle["Customer Payment Status"] = new ChartExportData
        {
            ChartTitle = "Customer Payment Status",
            ChartType = ChartType.Distribution,
            Labels = statusData.Select(d => d.Item1).ToArray(),
            Values = statusData.Select(d => (double)d.Item2).ToArray(),
            SeriesName = "Count",
            TotalValue = total,
            StartDate = start,
            EndDate = end
        };

        return (series, total);
    }

    /// <summary>
    /// Loads active vs inactive customers chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, int Total) LoadActiveInactiveCustomersChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        int total = 0;

        if (companyData?.Customers == null || companyData.Sales == null)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-90); // Look at 90 days for activity

        var activeCustomerIds = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end && !string.IsNullOrEmpty(s.CustomerId))
            .Select(s => s.CustomerId)
            .Distinct()
            .ToHashSet();

        var activeCount = activeCustomerIds.Count;
        var inactiveCount = companyData.Customers.Count - activeCount;
        total = companyData.Customers.Count;

        if (total == 0)
            return (series, total);

        if (activeCount > 0)
        {
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)activeCount },
                Name = "Active",
                Fill = new SolidColorPaint(SKColor.Parse("#22C55E")),
                Pushout = 0
            });
        }

        if (inactiveCount > 0)
        {
            series.Add(new PieSeries<double>
            {
                Values = new[] { (double)inactiveCount },
                Name = "Inactive",
                Fill = new SolidColorPaint(SKColor.Parse("#6B7280")),
                Pushout = 0
            });
        }

        // Store export data
        var statusList = new List<(string Label, int Value)>();
        if (activeCount > 0) statusList.Add(("Active", activeCount));
        if (inactiveCount > 0) statusList.Add(("Inactive", inactiveCount));

        _chartExportDataByTitle["Active vs Inactive Customers"] = new ChartExportData
        {
            ChartTitle = "Active vs Inactive Customers",
            ChartType = ChartType.Distribution,
            Labels = statusList.Select(d => d.Label).ToArray(),
            Values = statusList.Select(d => (double)d.Value).ToArray(),
            SeriesName = "Count",
            TotalValue = total,
            StartDate = start,
            EndDate = end
        };

        return (series, total);
    }

    /// <summary>
    /// Loads loss reasons chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, int Total) LoadLossReasonsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        int total = 0;

        if (companyData?.LostDamaged == null || companyData.LostDamaged.Count == 0)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.LostDamaged
            .Where(l => l.DateDiscovered >= start && l.DateDiscovered <= end)
            .GroupBy(l => l.Reason.ToString())
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

        // Store export data
        _chartExportDataByTitle["Loss Reasons"] = new ChartExportData
        {
            ChartTitle = "Loss Reasons",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Reason).ToArray(),
            Values = distribution.Select(d => (double)d.Count).ToArray(),
            SeriesName = "Count",
            TotalValue = total,
            StartDate = start,
            EndDate = end
        };

        return (series, total);
    }

    /// <summary>
    /// Loads losses by product chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, decimal Total) LoadLossesByProductChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        decimal total = 0;

        if (companyData?.LostDamaged == null || companyData.LostDamaged.Count == 0)
            return (series, total);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var distribution = companyData.LostDamaged
            .Where(l => l.DateDiscovered >= start && l.DateDiscovered <= end)
            .GroupBy(l => companyData.GetProduct(l.ProductId ?? "")?.Name ?? "Unknown")
            .Select(g => new { Product = g.Key, Total = g.Sum(l => l.ValueLost) })
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
                Name = item.Product,
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        // Store export data
        _chartExportDataByTitle["Losses by Product"] = new ChartExportData
        {
            ChartTitle = "Losses by Product",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Product).ToArray(),
            Values = distribution.Select(d => (double)d.Total).ToArray(),
            SeriesName = "Amount",
            TotalValue = (double)total,
            StartDate = start,
            EndDate = end
        };

        return (series, total);
    }

    /// <summary>
    /// Loads returns over time chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates, int TotalReturns) LoadReturnsOverTimeChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();
        int totalReturns = 0;

        if (companyData?.Returns == null || companyData.Returns.Count == 0)
            return (series, labels, dates, totalReturns);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var returnsByDate = companyData.Returns
            .Where(r => r.ReturnDate >= start && r.ReturnDate <= end)
            .GroupBy(r => r.ReturnDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        if (returnsByDate.Count == 0)
            return (series, labels, dates, totalReturns);

        labels = returnsByDate.Select(r => r.Date.ToString("yyyy-MM-dd")).ToArray();
        dates = returnsByDate.Select(r => r.Date).ToArray();
        var values = returnsByDate.Select(r => (double)r.Count).ToArray();
        totalReturns = returnsByDate.Sum(r => r.Count);

        series.Add(CreateDateTimeSeries(dates, values, "Returns", ExpenseColor));

        return (series, labels, dates, totalReturns);
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

        // Store export data
        var exportData = new ChartExportData
        {
            ChartTitle = "Return Reasons",
            ChartType = ChartType.Distribution,
            Labels = distribution.Select(d => d.Reason).ToArray(),
            Values = distribution.Select(d => (double)d.Count).ToArray(),
            SeriesName = "Count",
            TotalValue = total,
            StartDate = start,
            EndDate = end
        };
        _chartExportDataByTitle["Return Reasons"] = exportData;
        _chartExportDataByTitle["Returns by Category"] = exportData;

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

        // Only add series if there's actual data
        if (impactValues.Any(v => v > 0))
        {
            series.Add(new ColumnSeries<double>
            {
                Values = impactValues,
                Name = "Refunds",
                Fill = new SolidColorPaint(ExpenseColor),
                Stroke = null,
                MaxBarWidth = 50,
                Padding = 2
            });
        }

        return (series, labels, totalImpact);
    }

    /// <summary>
    /// Loads losses over time chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates, int TotalLosses) LoadLossesOverTimeChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();
        int totalLosses = 0;

        if (companyData?.LostDamaged == null || companyData.LostDamaged.Count == 0)
            return (series, labels, dates, totalLosses);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var lossesByDate = companyData.LostDamaged
            .Where(l => l.DateDiscovered >= start && l.DateDiscovered <= end)
            .GroupBy(l => l.DateDiscovered.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToList();

        if (lossesByDate.Count == 0)
            return (series, labels, dates, totalLosses);

        labels = lossesByDate.Select(l => l.Date.ToString("yyyy-MM-dd")).ToArray();
        dates = lossesByDate.Select(l => l.Date).ToArray();
        var values = lossesByDate.Select(l => (double)l.Count).ToArray();
        totalLosses = lossesByDate.Sum(l => l.Count);

        series.Add(CreateDateTimeSeries(dates, values, "Losses", ExpenseColor));

        return (series, labels, dates, totalLosses);
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

        // Only add series if there's actual data
        if (impactValues.Any(v => v > 0))
        {
            series.Add(new ColumnSeries<double>
            {
                Values = impactValues,
                Name = "Value Lost",
                Fill = new SolidColorPaint(ExpenseColor),
                Stroke = null,
                MaxBarWidth = 50,
                Padding = 2
            });
        }

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

        var salesInRange = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .ToList();

        return salesInRange
            .GroupBy(s =>
            {
                // First try customer country
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                if (customer?.Address?.Country != null && !string.IsNullOrEmpty(customer.Address.Country))
                    return GetCountryIsoCode(customer.Address.Country);

                // Fall back to product's supplier country
                var firstProductId = s.LineItems?.FirstOrDefault()?.ProductId;
                if (!string.IsNullOrEmpty(firstProductId))
                {
                    var product = companyData.GetProduct(firstProductId);
                    if (product != null && !string.IsNullOrEmpty(product.SupplierId))
                    {
                        var supplier = companyData.GetSupplier(product.SupplierId);
                        if (supplier?.Address?.Country != null && !string.IsNullOrEmpty(supplier.Address.Country))
                            return GetCountryIsoCode(supplier.Address.Country);
                    }
                }

                return string.Empty;
            })
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => (double)g.Sum(s => s.Total));
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

        var purchasesInRange = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .ToList();

        return purchasesInRange
            .GroupBy(p =>
            {
                var supplierId = GetEffectiveSupplierId(p, companyData);
                if (!string.IsNullOrEmpty(supplierId))
                {
                    var supplier = companyData.GetSupplier(supplierId);
                    if (supplier?.Address?.Country != null && !string.IsNullOrEmpty(supplier.Address.Country))
                        return GetCountryIsoCode(supplier.Address.Country);
                }
                return string.Empty;
            })
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => (double)g.Sum(p => p.Total));
    }

    /// <summary>
    /// Stores export data for a chart, making it available for later retrieval by title.
    /// Also stores aliases for titles that differ between ChartLoaderService and UI.
    /// </summary>
    private void StoreExportData(ChartExportData data)
    {
        CurrentExportData = data;
        if (!string.IsNullOrEmpty(data.ChartTitle))
        {
            _chartExportDataByTitle[data.ChartTitle] = data;

            // Store aliases for UI titles that differ from internal titles
            var aliases = data.ChartTitle switch
            {
                "Expenses Overview" => new[] { "Purchase Trends", "Total Expenses" },
                "Revenue Overview" => new[] { "Sales Trends", "Total Revenue" },
                "Profits Overview" => new[] { "Profit Over Time", "Profits over Time" },
                _ => Array.Empty<string>()
            };

            foreach (var alias in aliases)
            {
                _chartExportDataByTitle[alias] = data;
            }
        }
    }

    /// <summary>
    /// Gets the export data for a specific chart by its identifier or title.
    /// </summary>
    /// <param name="chartId">The identifier or title of the chart.</param>
    /// <returns>The chart export data, or null if not found.</returns>
    public ChartExportData? GetExportDataForChart(string chartId)
    {
        // First try to find by exact title match in the dictionary
        if (!string.IsNullOrEmpty(chartId) && _chartExportDataByTitle.TryGetValue(chartId, out var data))
        {
            return data;
        }

        // Fall back to legacy handling for Dashboard page charts
        return chartId switch
        {
            "ExpenseDistributionChart" => PieChartExportData,
            "ExpensesChart" => CurrentExportData,
            _ => CurrentExportData
        };
    }

    /// <summary>
    /// Gets the data for exporting to Google Sheets.
    /// Does not include total row as it would appear as a category in the chart.
    /// </summary>
    /// <param name="chartId">The identifier of the chart to export. If empty, defaults to CurrentExportData.</param>
    /// <returns>Export data formatted for Google Sheets.</returns>
    public List<List<object>> GetGoogleSheetsExportData(string chartId = "")
    {
        var exportData = GetExportDataForChart(chartId);

        if (exportData == null)
            return [];

        // Use "Category" for pie charts, "Date" for time-based charts
        var labelHeader = exportData.ChartType == ChartType.Distribution ? "Category" : "Date";

        var data = new List<List<object>>
        {
            // Header row
            new() { labelHeader, exportData.SeriesName }
        };

        // Data rows (no total - it would show up as a category in the chart)
        for (int i = 0; i < exportData.Labels.Length; i++)
        {
            data.Add(new List<object>
            {
                exportData.Labels[i],
                exportData.Values[i]
            });
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
    /// Gets the effective category ID for a sale, checking LineItems if not set directly.
    /// </summary>
    private static string? GetEffectiveCategoryId(Sale sale, CompanyData companyData)
    {
        // First check if CategoryId is set directly on the sale
        if (!string.IsNullOrEmpty(sale.CategoryId))
            return sale.CategoryId;

        // Otherwise, look at the first LineItem's product
        var firstProductId = sale.LineItems?.FirstOrDefault()?.ProductId;
        if (!string.IsNullOrEmpty(firstProductId))
        {
            var product = companyData.GetProduct(firstProductId);
            if (product != null && !string.IsNullOrEmpty(product.CategoryId))
                return product.CategoryId;
        }

        return null;
    }

    /// <summary>
    /// Gets the effective category ID for a purchase, checking LineItems if not set directly.
    /// </summary>
    private static string? GetEffectiveCategoryId(Purchase purchase, CompanyData companyData)
    {
        // First check if CategoryId is set directly on the purchase
        if (!string.IsNullOrEmpty(purchase.CategoryId))
            return purchase.CategoryId;

        // Otherwise, look at the first LineItem's product
        var firstProductId = purchase.LineItems?.FirstOrDefault()?.ProductId;
        if (!string.IsNullOrEmpty(firstProductId))
        {
            var product = companyData.GetProduct(firstProductId);
            if (product != null && !string.IsNullOrEmpty(product.CategoryId))
                return product.CategoryId;
        }

        return null;
    }

    /// <summary>
    /// Gets the effective supplier ID for a purchase, checking LineItems if not set directly.
    /// </summary>
    private static string? GetEffectiveSupplierId(Purchase purchase, CompanyData companyData)
    {
        // First check if SupplierId is set directly on the purchase
        if (!string.IsNullOrEmpty(purchase.SupplierId))
            return purchase.SupplierId;

        // Otherwise, look at the first LineItem's product
        var firstProductId = purchase.LineItems?.FirstOrDefault()?.ProductId;
        if (!string.IsNullOrEmpty(firstProductId))
        {
            var product = companyData.GetProduct(firstProductId);
            if (product != null && !string.IsNullOrEmpty(product.SupplierId))
                return product.SupplierId;
        }

        return null;
    }

    /// <summary>
    /// Gets the effective customer ID for a sale, checking LineItems if not set directly.
    /// For sales, we may look at the product's supplier as a fallback for some charts.
    /// </summary>
    private static string? GetEffectiveCustomerId(Sale sale, CompanyData companyData)
    {
        // First check if CustomerId is set directly on the sale
        if (!string.IsNullOrEmpty(sale.CustomerId))
            return sale.CustomerId;

        return null;
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
