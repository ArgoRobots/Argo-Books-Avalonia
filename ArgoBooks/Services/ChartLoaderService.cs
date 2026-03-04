using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
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


    // Chart colors (from AppColors)
    private static readonly SKColor RevenueColor = SKColor.Parse(AppColors.Primary);
    private static readonly SKColor ExpenseColor = SKColor.Parse(AppColors.ExpenseRed);
    private static readonly SKColor ProfitColor = SKColor.Parse(AppColors.Success);
    private static readonly SKColor CustomerColor = SKColor.Parse(AppColors.Primary);

    // Theme colors (will be updated based on current theme)
    private SKColor _textColor = SKColor.Parse(AppColors.TextDark);
    private SKColor _gridColor = SKColor.Parse(AppColors.ChartAxis);

    /// <summary>
    /// Gets the legend text paint based on the current theme.
    /// </summary>
    public static SolidColorPaint GetLegendTextPaint()
    {
        var isDarkTheme = ThemeService.Instance.IsDarkTheme;
        var textColor = isDarkTheme ? SKColor.Parse(AppColors.TextDark) : SKColor.Parse(AppColors.TextLight);
        return new SolidColorPaint(textColor) { FontFamily = "Segoe UI" };
    }

    // Maximum length for legend labels to prevent overflow
    private const int MaxLegendLabelLength = 18;

    /// <summary>
    /// Truncates a legend label to prevent pie chart legend overflow.
    /// </summary>
    private static string TruncateLegendLabel(string? label)
    {
        if (string.IsNullOrEmpty(label))
            return LanguageService.Instance.Translate("Unknown");

        return label.Length > MaxLegendLabelLength
            ? label[..(MaxLegendLabelLength - 1)] + "…"
            : label;
    }

    /// <summary>
    /// Creates a chart title visual element with consistent styling.
    /// </summary>
    /// <param name="text">The title text.</param>
    /// <returns>A configured LabelVisual for use as a chart title.</returns>
    public static LabelVisual CreateChartTitle(string text)
    {
        var isDarkTheme = ThemeService.Instance.IsDarkTheme;
        var textColor = isDarkTheme ? SKColor.Parse(AppColors.TextDark) : SKColor.Parse(AppColors.TextLight);

        // Handle titles that may already be translated or contain dynamic values
        // If the text contains a colon followed by a value (e.g., "Total profits: $7,246.51"),
        // only translate the label part before the colon
        var translatedText = TranslateChartTitle(text);

        return new LabelVisual
        {
            Text = translatedText,
            TextSize = 16,
            Padding = new Padding(15, 12),
            Paint = new SolidColorPaint(textColor) { FontFamily = "Segoe UI", SKFontStyle = new SKFontStyle(SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) }
        };
    }

    /// <summary>
    /// Translates a chart title, handling titles with dynamic values.
    /// </summary>
    private static string TranslateChartTitle(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Check if the title contains a colon followed by a value (e.g., "Total profits: $7,246.51")
        var colonIndex = text.IndexOf(':');
        if (colonIndex > 0 && colonIndex < text.Length - 1)
        {
            var label = text[..colonIndex].Trim();
            var value = text[(colonIndex + 1)..].Trim();

            // Only translate the label part if the value looks like a number/currency
            if (value.Length > 0 && (char.IsDigit(value[0]) || value[0] == '$' || value[0] == '-' || value[0] == '+'))
            {
                var translatedLabel = LanguageService.Instance.Translate(label);
                return $"{translatedLabel}: {value}";
            }
        }

        // Standard translation for simple titles
        return LanguageService.Instance.Translate(text);
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
    private ChartExportData? CurrentExportData { get; set; }

    /// <summary>
    /// Gets or sets the pie chart data for export functionality.
    /// </summary>
    private ChartExportData? PieChartExportData { get; set; }

    /// <summary>
    /// Dictionary storing export data for each chart by its ChartDataType enum.
    /// </summary>
    private readonly Dictionary<ChartDataType, ChartExportData> _chartExportDataByType = new();

    /// <summary>
    /// Gets or sets the chart style for rendering series.
    /// </summary>
    public ChartStyle SelectedChartStyle { get; set; } = ChartStyle.Line;

    /// <summary>
    /// Creates a series for time-based data based on SelectedChartStyle.
    /// Uses categorical (index-based) positioning - dates will be evenly spaced.
    /// </summary>
    private ISeries CreateTimeSeries(double[] values, string name, SKColor color)
    {
        return SelectedChartStyle switch
        {
            ChartStyle.Line => new LineSeries<double>
            {
                Values = values,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            },
            ChartStyle.StepLine => new StepLineSeries<double>
            {
                Values = values,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            },
            ChartStyle.Area => new LineSeries<double>
            {
                Values = values,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = new SolidColorPaint(color.WithAlpha(80)),
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            },
            ChartStyle.Scatter => new ScatterSeries<double>
            {
                Values = values,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = new SolidColorPaint(color),
                GeometrySize = 10
            },
            _ => new ColumnSeries<double>
            {
                Values = values,
                Name = name,
                Fill = new SolidColorPaint(color),
                Stroke = null,
                MaxBarWidth = 100
            }
        };
    }

    /// <summary>
    /// Creates a series for date-based data with proportional spacing.
    /// Points are positioned based on actual date values (converted to OADate), not evenly spaced.
    /// </summary>
    private ISeries CreateDateTimeSeries(DateTime[] dates, double[] values, string name, SKColor color)
    {
        // Convert dates to OADate (days since Dec 30, 1899) for X coordinate
        // Use ObservablePoint which directly stores X,Y coordinates
        var points = dates.Zip(values, (d, v) => new ObservablePoint(d.ToOADate(), v)).ToArray();

        return SelectedChartStyle switch
        {
            ChartStyle.Line => new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            },
            ChartStyle.StepLine => new StepLineSeries<ObservablePoint>
            {
                Values = points,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = null,
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            },
            ChartStyle.Area => new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = new SolidColorPaint(color.WithAlpha(80)),
                GeometryStroke = new SolidColorPaint(color, 2),
                GeometryFill = new SolidColorPaint(color),
                GeometrySize = 6
            },
            ChartStyle.Scatter => new ScatterSeries<ObservablePoint>
            {
                Values = points,
                Name = name,
                Stroke = new SolidColorPaint(color, 2),
                Fill = new SolidColorPaint(color),
                GeometrySize = 10
            },
            _ => new ColumnSeries<ObservablePoint>
            {
                Values = points,
                Name = name,
                Fill = new SolidColorPaint(color),
                Stroke = null,
                MaxBarWidth = 100
            }
        };
    }

    /// <summary>
    /// Creates series for profit data with negative values shown in red (for Column mode).
    /// </summary>
    private IEnumerable<ISeries> CreateProfitDateTimeSeries(DateTime[] dates, double[] values, string name)
    {
        // For column charts, split into positive (green) and negative (red) series
        // Column is the default when not Line, StepLine, Area, or Scatter
        var isColumnStyle = SelectedChartStyle != ChartStyle.Line &&
                           SelectedChartStyle != ChartStyle.StepLine &&
                           SelectedChartStyle != ChartStyle.Area &&
                           SelectedChartStyle != ChartStyle.Scatter;

        if (isColumnStyle)
        {
            // Create separate lists for positive and negative values
            var positivePoints = new List<ObservablePoint>();
            var negativePoints = new List<ObservablePoint>();

            for (int i = 0; i < dates.Length; i++)
            {
                var x = dates[i].ToOADate();
                var y = values[i];

                if (y >= 0)
                    positivePoints.Add(new ObservablePoint(x, y));
                else
                    negativePoints.Add(new ObservablePoint(x, y));
            }

            if (positivePoints.Count > 0)
            {
                yield return new ColumnSeries<ObservablePoint>
                {
                    Values = positivePoints,
                    Name = name,
                    Fill = new SolidColorPaint(ProfitColor),
                    Stroke = null,
                    MaxBarWidth = 100,
                    IgnoresBarPosition = true
                };
            }

            if (negativePoints.Count > 0)
            {
                yield return new ColumnSeries<ObservablePoint>
                {
                    Values = negativePoints,
                    Name = $"{name} (Loss)",
                    Fill = new SolidColorPaint(ExpenseColor),
                    Stroke = null,
                    MaxBarWidth = 100,
                    IgnoresBarPosition = true
                };
            }

            yield break;
        }

        // For line-based charts, use single series with green color
        yield return CreateDateTimeSeries(dates, values, name, ProfitColor);
    }

    /// <summary>
    /// Updates theme colors based on the current application theme.
    /// </summary>
    /// <param name="isDarkTheme">Whether the dark theme is active.</param>
    public void UpdateThemeColors(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            _textColor = SKColor.Parse(AppColors.TextDark);
            _gridColor = SKColor.Parse(AppColors.ChartAxis);
        }
        else
        {
            _textColor = SKColor.Parse(AppColors.TextLightAlt);
            _gridColor = SKColor.Parse(AppColors.ChartGrid);
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
    /// Uses LiveCharts' native label stepping for smooth resize behavior.
    /// </summary>
    public Axis[] CreateDateXAxes(DateTime[]? dates = null)
    {
        if (dates == null || dates.Length == 0)
        {
            return CreateXAxes();
        }

        var sortedOADates = dates.Select(d => d.ToOADate()).OrderBy(d => d).ToArray();
        var minDate = sortedOADates[0];
        var maxDate = sortedOADates[^1];
        var padding = Math.Max(0.5, (maxDate - minDate) * 0.05);

        // Calculate UnitWidth based on minimum gap between consecutive dates.
        // This tells LiveCharts how wide each data point "slot" is, so column bars
        // fill the space between points instead of defaulting to 1 day.
        double unitWidth = 1;
        if (sortedOADates.Length >= 2)
        {
            var minGap = double.MaxValue;
            for (var i = 1; i < sortedOADates.Length; i++)
            {
                var gap = sortedOADates[i] - sortedOADates[i - 1];
                if (gap > 0 && gap < minGap) minGap = gap;
            }
            if (minGap < double.MaxValue) unitWidth = minGap;
        }

        var axis = new Axis
        {
            TextSize = AxisTextSize,
            LabelsPaint = new SolidColorPaint(_textColor),
            LabelsRotation = 0,
            MinLimit = minDate - padding,
            MaxLimit = maxDate + padding,
            UnitWidth = unitWidth,
            MinStep = unitWidth,
            Labeler = value =>
            {
                if (value < -657434 || value > 2958466)
                {
                    return string.Empty;
                }
                try
                {
                    var date = DateTime.FromOADate(value);
                    return DateFormatService.Format(date);
                }
                catch
                {
                    return string.Empty;
                }
            }
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

    #region Shared Data Model Conversion

    /// <summary>
    /// Creates a ReportFilters instance from date parameters.
    /// This enables using ReportChartDataService for data fetching.
    /// </summary>
    /// <param name="startDate">Optional start date.</param>
    /// <param name="endDate">Optional end date.</param>
    /// <param name="defaultDaysBack">Default number of days to look back if no start date specified.</param>
    /// <returns>A configured ReportFilters instance.</returns>
    private static ReportFilters CreateFilters(DateTime? startDate, DateTime? endDate, int defaultDaysBack = 30)
    {
        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-defaultDaysBack);
        return new ReportFilters
        {
            StartDate = start,
            EndDate = end,
            IncludeReturns = true,
            IncludeLosses = true
        };
    }

    /// <summary>
    /// Creates a ReportFilters instance with month-based default range.
    /// </summary>
    private static ReportFilters CreateFiltersMonths(DateTime? startDate, DateTime? endDate, int defaultMonthsBack = 6)
    {
        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddMonths(-defaultMonthsBack);
        return new ReportFilters
        {
            StartDate = start,
            EndDate = end,
            IncludeReturns = true,
            IncludeLosses = true
        };
    }

    #endregion

    /// <summary>
    /// Loads expenses overview chart data as a column series.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    /// <param name="companyData">The company data to load from.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>A tuple containing the series collection and dates for proportional spacing.</returns>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadExpensesOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetExpensesOverTime();

        if (dataPoints.Count == 0)
        {
            StoreExportData(ChartDataType.TotalExpenses, new ChartExportData
            {
                ChartTitle = "Expenses Overview",
                ChartType = ChartType.Expense,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            });
            return (series, dates);
        }

        var labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        series.Add(CreateDateTimeSeries(dates, values, "Expenses", RevenueColor));

        StoreExportData(ChartDataType.TotalExpenses, new ChartExportData
        {
            ChartTitle = "Expenses Overview",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Expenses",
        });

        return (series, dates);
    }

    /// <summary>
    /// Loads revenue overview chart data as a column series.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadRevenueOverviewChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetRevenueOverTime();

        if (dataPoints.Count == 0)
        {
            StoreExportData(ChartDataType.TotalRevenue, new ChartExportData
            {
                ChartTitle = "Revenue Overview",
                ChartType = ChartType.Revenue,
                Labels = [],
                Values = [],
                SeriesName = "Revenue"
            });
            return (series, dates);
        }

        var labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        series.Add(CreateDateTimeSeries(dates, values, "Revenue", ProfitColor));

        StoreExportData(ChartDataType.TotalRevenue, new ChartExportData
        {
            ChartTitle = "Revenue Overview",
            ChartType = ChartType.Revenue,
            Labels = labels,
            Values = values,
            SeriesName = "Revenue",
        });

        return (series, dates);
    }

    /// <summary>
    /// Loads profits overview chart data as a column series.
    /// Uses ReportChartDataService for data fetching.
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

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetProfitOverTime();

        if (dataPoints.Count == 0)
        {
            StoreExportData(ChartDataType.TotalProfits, new ChartExportData
            {
                ChartTitle = "Profits Overview",
                ChartType = ChartType.Profit,
                Labels = [],
                Values = [],
                SeriesName = "Profit"
            });
            return (series, labels, dates, totalProfit);
        }

        labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();
        totalProfit = (decimal)values.Sum();

        // Use profit-specific series that shows negative values in red for column charts
        foreach (var s in CreateProfitDateTimeSeries(dates, values, "Profit"))
        {
            series.Add(s);
        }

        StoreExportData(ChartDataType.TotalProfits, new ChartExportData
        {
            ChartTitle = "Profits Overview",
            ChartType = ChartType.Profit,
            Labels = labels,
            Values = values,
            SeriesName = "Profit",
        });

        return (series, labels, dates, totalProfit);
    }

    /// <summary>
    /// Loads expenses vs revenue comparison chart as a multi-series column chart.
    /// Uses ReportChartDataService for data fetching with daily granularity.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadRevenueVsExpensesChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetRevenueVsExpensesDaily();

        if (seriesData.Count < 2)
        {
            _chartExportDataByType[ChartDataType.RevenueVsExpenses] = new ChartExportData
            {
                ChartTitle = "Expenses vs Revenue",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, dates);
        }

        var revenueData = seriesData.FirstOrDefault(s => s.Name == "Revenue");
        var expenseData = seriesData.FirstOrDefault(s => s.Name == "Expenses");

        if (revenueData == null || expenseData == null || revenueData.DataPoints.Count == 0)
        {
            _chartExportDataByType[ChartDataType.RevenueVsExpenses] = new ChartExportData
            {
                ChartTitle = "Expenses vs Revenue",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, dates);
        }

        // Filter to only include days with data (at least one non-zero value)
        var indicesWithData = new List<int>();
        for (int i = 0; i < revenueData.DataPoints.Count; i++)
        {
            if (revenueData.DataPoints[i].Value > 0 || expenseData.DataPoints[i].Value > 0)
            {
                indicesWithData.Add(i);
            }
        }

        if (indicesWithData.Count == 0)
        {
            _chartExportDataByType[ChartDataType.RevenueVsExpenses] = new ChartExportData
            {
                ChartTitle = "Expenses vs Revenue",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, dates);
        }

        var labels = indicesWithData.Select(i => revenueData.DataPoints[i].Label).ToArray();
        dates = indicesWithData.Where(i => revenueData.DataPoints[i].Date.HasValue).Select(i => revenueData.DataPoints[i].Date!.Value).ToArray();
        var revenueValues = indicesWithData.Select(i => revenueData.DataPoints[i].Value).ToArray();
        var expenseValues = indicesWithData.Select(i => expenseData.DataPoints[i].Value).ToArray();

        // Add series (expenses first, then revenue for consistency)
        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, expenseValues, "Expenses", ExpenseColor));
            series.Add(CreateDateTimeSeries(dates, revenueValues, "Revenue", ProfitColor));
        }

        // Store export data for multi-series chart
        _chartExportDataByType[ChartDataType.RevenueVsExpenses] = new ChartExportData
        {
            ChartTitle = "Expenses vs Revenue",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = expenseValues,
            SeriesName = "Expenses",
            AdditionalSeries = [("Revenue", revenueValues)]        };

        return (series, dates);
    }

    /// <summary>
    /// Loads revenue distribution by category as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadRevenueDistributionChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetRevenueDistribution().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data for Google Sheets/Excel export
        var exportData = new ChartExportData
        {
            ChartTitle = "Revenue Distribution",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };
        _chartExportDataByType[ChartDataType.RevenueDistribution] = exportData;

        return (series, legend);
    }

    /// <summary>
    /// Loads expense distribution by category as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadExpenseDistributionChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetExpenseDistribution().ToList();

        if (dataPoints.Count == 0)
        {
            PieChartExportData = null;
            return ([], []);
        }

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data for Google Sheets/Excel export
        PieChartExportData = new ChartExportData
        {
            ChartTitle = "Expense Distribution",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };

        // Store by type for chart-specific retrieval
        _chartExportDataByType[ChartDataType.ExpensesDistribution] = PieChartExportData;

        return (series, legend);
    }

    /// <summary>
    /// Loads growth rates chart data with dynamic granularity based on date range.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    /// <param name="companyData">The company data containing customer information.</param>
    /// <param name="startDate">Optional start date for filtering.</param>
    /// <param name="endDate">Optional end date for filtering.</param>
    /// <param name="datePresetName">Optional date preset name (e.g., "This Month", "Last Quarter").</param>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadCustomerGrowthChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? datePresetName = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        var filters = CreateFiltersMonths(startDate, endDate, 12);
        filters.DatePresetName = datePresetName;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetCustomerGrowth();

        if (dataPoints.Count == 0)
            return (series, labels);

        labels = dataPoints.Select(p => p.Label).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        // Only add series if there's actual data
        if (values.Any(v => v != 0))
        {
            series.Add(CreateTimeSeries(values, "New Customers", CustomerColor));
        }

        // Store export data
        _chartExportDataByType[ChartDataType.CustomerGrowth] = new ChartExportData
        {
            ChartTitle = "Customer Growth",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = values,
            SeriesName = "New Customers"
        };

        return (series, labels);
    }

    /// <summary>
    /// Loads average transaction value chart with daily granularity.
    /// Shows separate series for revenue and expense transactions.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadAverageTransactionValueChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetAverageTransactionValueDailyBySeries();

        if (seriesData.Count == 0)
            return (series, dates);

        // Get dates from the first series (both series have the same dates)
        var revenueSeriesData = seriesData.FirstOrDefault(s => s.Name == "Revenue");
        var expenseSeriesData = seriesData.FirstOrDefault(s => s.Name == "Expenses");

        if (revenueSeriesData?.DataPoints == null || revenueSeriesData.DataPoints.Count == 0)
            return (series, dates);

        dates = revenueSeriesData.DataPoints
            .Where(p => p.Date.HasValue)
            .Select(p => p.Date!.Value)
            .ToArray();

        if (dates.Length > 0)
        {
            // Revenue series (green)
            var revenueValues = revenueSeriesData.DataPoints.Select(p => p.Value).ToArray();
            series.Add(CreateDateTimeSeries(dates, revenueValues, "Revenue", ProfitColor));

            // Expense series (red)
            if (expenseSeriesData?.DataPoints != null)
            {
                var expenseValues = expenseSeriesData.DataPoints.Select(p => p.Value).ToArray();
                series.Add(CreateDateTimeSeries(dates, expenseValues, "Expenses", ExpenseColor));
            }
        }

        // Store export data
        var labels = revenueSeriesData.DataPoints.Select(p => p.Label).ToArray();
        _chartExportDataByType[ChartDataType.AverageTransactionValue] = new ChartExportData
        {
            ChartTitle = "Average Transaction Value",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = revenueSeriesData.DataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Revenue"
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads total transactions count chart with daily granularity.
    /// Shows separate series for revenue and expense transactions.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadTotalTransactionsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetTransactionCountDailyBySeries();

        if (seriesData.Count == 0)
            return (series, dates);

        // Get dates from the first series (both series have the same dates)
        var revenueSeriesData = seriesData.FirstOrDefault(s => s.Name == "Revenue");
        var expenseSeriesData = seriesData.FirstOrDefault(s => s.Name == "Expenses");

        if (revenueSeriesData?.DataPoints == null || revenueSeriesData.DataPoints.Count == 0)
            return (series, dates);

        dates = revenueSeriesData.DataPoints
            .Where(p => p.Date.HasValue)
            .Select(p => p.Date!.Value)
            .ToArray();

        if (dates.Length > 0)
        {
            var revenueValues = revenueSeriesData.DataPoints.Select(p => p.Value).ToArray();
            series.Add(CreateDateTimeSeries(dates, revenueValues, "Revenue", ProfitColor));

            if (expenseSeriesData?.DataPoints != null)
            {
                var expenseValues = expenseSeriesData.DataPoints.Select(p => p.Value).ToArray();
                series.Add(CreateDateTimeSeries(dates, expenseValues, "Expenses", ExpenseColor));
            }
        }

        // Store export data (combined for backwards compatibility)
        var labels = revenueSeriesData.DataPoints.Select(p => p.Label).ToArray();
        var totalValues = revenueSeriesData.DataPoints
            .Zip(expenseSeriesData?.DataPoints ?? [], (r, e) => r.Value + e.Value)
            .ToArray();

        _chartExportDataByType[ChartDataType.TotalTransactions] = new ChartExportData
        {
            ChartTitle = "Total Transactions",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = totalValues,
            SeriesName = "Transactions"
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads average shipping costs chart with daily granularity.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadAverageShippingCostsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetAverageShippingCostsDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, dates);

        var labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var avgShipping = filteredData.Select(p => p.Value).ToArray();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, avgShipping, "Avg Shipping", RevenueColor));
        }

        // Store export data
        _chartExportDataByType[ChartDataType.AverageShippingCosts] = new ChartExportData
        {
            ChartTitle = "Average Shipping Costs",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = avgShipping,
            SeriesName = "Avg Shipping"        };

        return (series, dates);
    }

    /// <summary>
    /// Loads countries of origin (supplier countries from purchases) as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCountriesOfOriginChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetExpensesByCountryOfDestination().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.CountriesOfOrigin] = new ChartExportData
        {
            ChartTitle = "Countries of Origin",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads countries of destination (sales by customer country) as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCountriesOfDestinationChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        // Use sales with customer country lookup - destination is where products are shipped to (customer location)
        var dataPoints = dataService.GetRevenueByCustomerCountry().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.CountriesOfDestination] = new ChartExportData
        {
            ChartTitle = "Countries of Destination",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads companies of origin (supplier companies from purchases) as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCompaniesOfOriginChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetExpensesBySupplierCompany().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.CompaniesOfOrigin] = new ChartExportData
        {
            ChartTitle = "Companies of Origin",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads companies of destination (customer companies from sales) as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCompaniesOfDestinationChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetRevenueByCompanyOfDestination().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.CompaniesOfDestination] = new ChartExportData
        {
            ChartTitle = "Companies of Destination",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads accountants transactions as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadAccountantsTransactionsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetTransactionsByAccountant().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data (used for "Transactions by Accountant" and "Companies of Destination")
        var exportData = new ChartExportData
        {
            ChartTitle = "Transactions by Accountant",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"        };
        _chartExportDataByType[ChartDataType.AccountantsTransactions] = exportData;

        return (series, legend);
    }

    /// <summary>
    /// Loads customer payment status chart (Paid vs Pending vs Overdue).
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCustomerPaymentStatusChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetCustomerPaymentStatus().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.CustomerPaymentStatus] = new ChartExportData
        {
            ChartTitle = "Customer Payment Status",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads active vs inactive customers chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadActiveInactiveCustomersChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate, defaultDaysBack: 90); // 90 days for activity
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetActiveVsInactiveCustomers().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.ActiveVsInactiveCustomers] = new ChartExportData
        {
            ChartTitle = "Active vs Inactive Customers",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads loss reasons chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadLossReasonsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossReasons().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.LossReasons] = new ChartExportData
        {
            ChartTitle = "Loss Reasons",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads losses by product chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadLossesByProductChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossesByProduct().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.LossesByProduct] = new ChartExportData
        {
            ChartTitle = "Losses by Product",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"        };

        return (series, legend);
    }

    /// <summary>
    /// Loads returns over time chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadReturnsOverTimeChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetReturnsOverTime();

        if (dataPoints.Count == 0)
            return (series, labels, dates);

        labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        series.Add(CreateDateTimeSeries(dates, values, "Returns", ExpenseColor));

        // Store export data
        _chartExportDataByType[ChartDataType.ReturnsOverTime] = new ChartExportData
        {
            ChartTitle = "Returns Over Time",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Returns"        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads return reasons as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadReturnReasonsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetReturnReasons().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        var exportData = new ChartExportData
        {
            ChartTitle = "Return Reasons",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"        };
        _chartExportDataByType[ChartDataType.ReturnReasons] = exportData;

        return (series, legend);
    }

    /// <summary>
    /// Loads returns by category as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadReturnsByCategoryChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetReturnsByCategory().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.ReturnsByCategory] = new ChartExportData
        {
            ChartTitle = "Returns by Category",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads return financial impact chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadReturnFinancialImpactChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetReturnFinancialImpactDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, dates);

        var labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var impactValues = filteredData.Select(p => p.Value).ToArray();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, impactValues, "Refunds", ExpenseColor));
        }

        // Store export data
        _chartExportDataByType[ChartDataType.ReturnFinancialImpact] = new ChartExportData
        {
            ChartTitle = "Financial Impact of Returns",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = impactValues,
            SeriesName = "Refunds"        };

        return (series, dates);
    }

    /// <summary>
    /// Loads losses over time chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadLossesOverTimeChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossesOverTime();

        if (dataPoints.Count == 0)
            return (series, labels, dates);

        labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        series.Add(CreateDateTimeSeries(dates, values, "Losses", ExpenseColor));

        // Store export data
        _chartExportDataByType[ChartDataType.LossesOverTime] = new ChartExportData
        {
            ChartTitle = "Losses Over Time",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Losses"        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads loss financial impact chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadLossFinancialImpactChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossFinancialImpactDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, dates);

        var labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var impactValues = filteredData.Select(p => p.Value).ToArray();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, impactValues, "Value Lost", ExpenseColor));
        }

        // Store export data
        _chartExportDataByType[ChartDataType.LossFinancialImpact] = new ChartExportData
        {
            ChartTitle = "Financial Impact of Losses",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = impactValues,
            SeriesName = "Value Lost"        };

        return (series, dates);
    }

    /// <summary>
    /// Loads returns by product chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadReturnsByProductChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetReturnsByProduct().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.ReturnsByProduct] = new ChartExportData
        {
            ChartTitle = "Returns by Product",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads losses by category chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadLossesByCategoryChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossesByCategory().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByType[ChartDataType.LossesByCategory] = new ChartExportData
        {
            ChartTitle = "Losses by Category",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count"
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads expense vs revenue returns chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadExpenseVsRevenueReturnsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetExpenseVsRevenueReturns();

        if (seriesData.Count == 0)
        {
            _chartExportDataByType[ChartDataType.ExpenseVsRevenueReturns] = new ChartExportData
            {
                ChartTitle = "Expense vs Revenue Returns",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Revenue Returns"
            };
            return (series, dates);
        }

        var revenueReturns = seriesData.FirstOrDefault(s => s.Name == "Revenue Returns");
        var expenseReturns = seriesData.FirstOrDefault(s => s.Name == "Expense Returns");

        if (revenueReturns == null || revenueReturns.DataPoints.Count == 0)
        {
            _chartExportDataByType[ChartDataType.ExpenseVsRevenueReturns] = new ChartExportData
            {
                ChartTitle = "Expense vs Revenue Returns",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Revenue Returns"
            };
            return (series, dates);
        }

        var labels = revenueReturns.DataPoints.Select(p => p.Label).ToArray();
        dates = revenueReturns.DataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var revenueReturnValues = revenueReturns.DataPoints.Select(p => p.Value).ToArray();
        var expenseReturnValues = expenseReturns?.DataPoints.Select(p => p.Value).ToArray() ?? [];

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, revenueReturnValues, "Revenue Returns", ExpenseColor));
            if (expenseReturnValues.Length > 0)
            {
                series.Add(CreateDateTimeSeries(dates, expenseReturnValues, "Expense Returns", SKColor.Parse(AppColors.PurpleDark)));
            }
        }

        // Store export data
        _chartExportDataByType[ChartDataType.ExpenseVsRevenueReturns] = new ChartExportData
        {
            ChartTitle = "Expense vs Revenue Returns",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = revenueReturnValues,
            SeriesName = "Revenue Returns",
            AdditionalSeries = expenseReturnValues.Length > 0 ? [("Expense Returns", expenseReturnValues)] : []
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads expense vs revenue losses chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadExpenseVsRevenueLossesChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetExpenseVsRevenueLosses();

        if (seriesData.Count == 0)
        {
            _chartExportDataByType[ChartDataType.ExpenseVsRevenueLosses] = new ChartExportData
            {
                ChartTitle = "Expense vs Revenue Losses",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expense Losses"
            };
            return (series, dates);
        }

        var expenseLosses = seriesData.FirstOrDefault(s => s.Name == "Expense Losses");
        var revenueLosses = seriesData.FirstOrDefault(s => s.Name == "Revenue Losses");

        if (expenseLosses == null || expenseLosses.DataPoints.Count == 0)
        {
            _chartExportDataByType[ChartDataType.ExpenseVsRevenueLosses] = new ChartExportData
            {
                ChartTitle = "Expense vs Revenue Losses",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expense Losses"
            };
            return (series, dates);
        }

        var labels = expenseLosses.DataPoints.Select(p => p.Label).ToArray();
        dates = expenseLosses.DataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var expenseLossValues = expenseLosses.DataPoints.Select(p => p.Value).ToArray();
        var revenueLossValues = revenueLosses?.DataPoints.Select(p => p.Value).ToArray() ?? [];

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, expenseLossValues, "Expense Losses", ExpenseColor));
            if (revenueLossValues.Length > 0)
            {
                series.Add(CreateDateTimeSeries(dates, revenueLossValues, "Revenue Losses", SKColor.Parse(AppColors.PurpleDark)));
            }
        }

        // Store export data
        _chartExportDataByType[ChartDataType.ExpenseVsRevenueLosses] = new ChartExportData
        {
            ChartTitle = "Expense vs Revenue Losses",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = expenseLossValues,
            SeriesName = "Expense Losses",
            AdditionalSeries = revenueLossValues.Length > 0 ? [("Revenue Losses", revenueLossValues)] : []
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads tax collected vs tax paid chart with two series over time.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadTaxCollectedVsPaidChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetTaxCollectedVsPaid();

        if (seriesData.Count == 0)
        {
            _chartExportDataByType[ChartDataType.TaxCollectedVsPaid] = new ChartExportData
            {
                ChartTitle = "Tax Collected vs Paid",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Tax Collected"
            };
            return (series, dates);
        }

        var taxCollected = seriesData.FirstOrDefault(s => s.Name == "Tax Collected");
        var taxPaid = seriesData.FirstOrDefault(s => s.Name == "Tax Paid");

        if (taxCollected == null || taxCollected.DataPoints.Count == 0)
        {
            _chartExportDataByType[ChartDataType.TaxCollectedVsPaid] = new ChartExportData
            {
                ChartTitle = "Tax Collected vs Paid",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Tax Collected"
            };
            return (series, dates);
        }

        var labels = taxCollected.DataPoints.Select(p => p.Label).ToArray();
        dates = taxCollected.DataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var collectedValues = taxCollected.DataPoints.Select(p => p.Value).ToArray();
        var paidValues = taxPaid?.DataPoints.Select(p => p.Value).ToArray() ?? [];

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, collectedValues, "Tax Collected", ProfitColor));
            if (paidValues.Length > 0)
            {
                series.Add(CreateDateTimeSeries(dates, paidValues, "Tax Paid", ExpenseColor));
            }
        }

        _chartExportDataByType[ChartDataType.TaxCollectedVsPaid] = new ChartExportData
        {
            ChartTitle = "Tax Collected vs Paid",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = collectedValues,
            SeriesName = "Tax Collected",
            AdditionalSeries = paidValues.Length > 0 ? [("Tax Paid", paidValues)] : []
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads net tax liability trend chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadTaxLiabilityTrendChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetTaxLiabilityOverTime();

        if (dataPoints.Count == 0)
            return (series, dates);

        var labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        series.Add(CreateDateTimeSeries(dates, values, "Net Tax Liability", RevenueColor));

        _chartExportDataByType[ChartDataType.TaxLiabilityTrend] = new ChartExportData
        {
            ChartTitle = "Net Tax Liability",
            ChartType = ChartType.Revenue,
            Labels = labels,
            Values = values,
            SeriesName = "Net Tax Liability"
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads tax by category pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadTaxByCategoryChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetTaxByCategory().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        _chartExportDataByType[ChartDataType.TaxByCategory] = new ChartExportData
        {
            ChartTitle = "Tax by Category",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads tax rate distribution as a stacked bar chart with Revenue vs Expense series.
    /// </summary>
    public (ObservableCollection<ISeries> Series, Axis[] XAxes, Axis[] YAxes) LoadTaxRateDistributionChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var (revenueRates, expenseRates, labels) = dataService.GetTaxRateDistribution();

        if (labels.Length == 0)
        {
            _chartExportDataByType[ChartDataType.TaxRateDistribution] = new ChartExportData
            {
                ChartTitle = "Tax Rate Distribution",
                ChartType = ChartType.Distribution,
                Labels = [],
                Values = [],
                SeriesName = "Revenue"
            };
            return (series, [], []);
        }

        var revenueValues = revenueRates.Select(p => p.Value).ToArray();
        var expenseValues = expenseRates.Select(p => p.Value).ToArray();

        series.Add(new StackedColumnSeries<double>
        {
            Values = revenueValues,
            Name = "Revenue",
            Fill = new SolidColorPaint(ProfitColor),
            Stroke = null,
            MaxBarWidth = 40
        });

        series.Add(new StackedColumnSeries<double>
        {
            Values = expenseValues,
            Name = "Expense",
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 40
        });

        var xAxes = new Axis[]
        {
            new()
            {
                Labels = labels,
                TextSize = AxisTextSize,
                LabelsPaint = new SolidColorPaint(_textColor),
                SeparatorsPaint = null
            }
        };

        var yAxes = CreateNumberYAxes();

        _chartExportDataByType[ChartDataType.TaxRateDistribution] = new ChartExportData
        {
            ChartTitle = "Tax Rate Distribution",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = revenueValues,
            SeriesName = "Revenue",
            AdditionalSeries = [("Expense", expenseValues)]
        };

        return (series, xAxes, yAxes);
    }

    /// <summary>
    /// Loads tax by product pie chart.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadTaxByProductChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetTaxByProduct().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        _chartExportDataByType[ChartDataType.TaxByProduct] = new ChartExportData
        {
            ChartTitle = "Tax by Product",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount"
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads expense vs revenue tax chart with two series over time.
    /// </summary>
    public (ObservableCollection<ISeries> Series, DateTime[] Dates) LoadExpenseVsRevenueTaxChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetExpenseVsRevenueTax();

        if (seriesData.Count == 0)
        {
            _chartExportDataByType[ChartDataType.ExpenseVsRevenueTax] = new ChartExportData
            {
                ChartTitle = "Expense vs Revenue Tax",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Revenue Tax"
            };
            return (series, dates);
        }

        var revenueTax = seriesData.FirstOrDefault(s => s.Name == "Revenue Tax");
        var expenseTax = seriesData.FirstOrDefault(s => s.Name == "Expense Tax");

        if (revenueTax == null || revenueTax.DataPoints.Count == 0)
        {
            _chartExportDataByType[ChartDataType.ExpenseVsRevenueTax] = new ChartExportData
            {
                ChartTitle = "Expense vs Revenue Tax",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Revenue Tax"
            };
            return (series, dates);
        }

        var labels = revenueTax.DataPoints.Select(p => p.Label).ToArray();
        dates = revenueTax.DataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var revenueValues = revenueTax.DataPoints.Select(p => p.Value).ToArray();
        var expenseValues = expenseTax?.DataPoints.Select(p => p.Value).ToArray() ?? [];

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, revenueValues, "Revenue Tax", ProfitColor));
            if (expenseValues.Length > 0)
            {
                series.Add(CreateDateTimeSeries(dates, expenseValues, "Expense Tax", ExpenseColor));
            }
        }

        _chartExportDataByType[ChartDataType.ExpenseVsRevenueTax] = new ChartExportData
        {
            ChartTitle = "Expense vs Revenue Tax",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = revenueValues,
            SeriesName = "Revenue Tax",
            AdditionalSeries = expenseValues.Length > 0 ? [("Expense Tax", expenseValues)] : []
        };

        return (series, dates);
    }

    /// <summary>
    /// Loads world map data for GeoMap chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public Dictionary<string, double> LoadWorldMapData(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var countryData = dataService.GetWorldMapData();

        // Convert country names to ISO codes for GeoMap
        return countryData
            .Select(kvp => (GetCountryIsoCode(kvp.Key), kvp.Value))
            .Where(x => !string.IsNullOrEmpty(x.Item1))
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Item2));
    }

    /// <summary>
    /// Loads world map data by supplier country for GeoMap chart (destination mode).
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public Dictionary<string, double> LoadWorldMapDataBySupplier(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var countryData = dataService.GetWorldMapDataBySupplier();

        // Convert country names to ISO codes for GeoMap
        return countryData
            .Select(kvp => (GetCountryIsoCode(kvp.Key), kvp.Value))
            .Where(x => !string.IsNullOrEmpty(x.Item1))
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Item2));
    }

    /// <summary>
    /// Stores export data for a chart, making it available for later retrieval by ChartDataType.
    /// </summary>
    private void StoreExportData(ChartDataType chartDataType, ChartExportData data)
    {
        CurrentExportData = data;
        _chartExportDataByType[chartDataType] = data;
    }

    /// <summary>
    /// Gets the export data for a specific chart by its ChartDataType.
    /// </summary>
    /// <param name="chartDataType">The ChartDataType of the chart.</param>
    /// <returns>The chart export data, or null if not found.</returns>
    public ChartExportData? GetExportDataForChart(ChartDataType? chartDataType)
    {
        if (chartDataType is not { } type)
            return CurrentExportData;

        if (_chartExportDataByType.TryGetValue(type, out var data))
            return data;

        return CurrentExportData;
    }

    /// <summary>
    /// Gets the data for exporting to Google Sheets.
    /// Does not include total row as it would appear as a category in the chart.
    /// </summary>
    /// <param name="chartDataType">The ChartDataType of the chart to export.</param>
    /// <returns>Export data formatted for Google Sheets.</returns>
    public List<List<object>> GetGoogleSheetsExportData(ChartDataType? chartDataType = null)
    {
        var exportData = GetExportDataForChart(chartDataType);

        if (exportData == null)
            return [];

        // Use "Category" for pie charts, "Date" or "Month" for time-based charts
        var labelHeader = exportData.ChartType switch
        {
            ChartType.Distribution => "Category",
            ChartType.Comparison => "Month",
            _ => "Date"
        };

        // Build header row with all series names
        var headerRow = new List<object> { labelHeader, exportData.SeriesName };
        foreach (var (name, _) in exportData.AdditionalSeries)
        {
            headerRow.Add(name);
        }

        var data = new List<List<object>> { headerRow };

        // Data rows (no total - it would show up as a category in the chart)
        for (int i = 0; i < exportData.Labels.Length; i++)
        {
            var row = new List<object>
            {
                exportData.Labels[i],
                exportData.Values[i]
            };

            // Add values from additional series
            foreach (var (_, values) in exportData.AdditionalSeries)
            {
                row.Add(i < values.Length ? values[i] : 0.0);
            }

            data.Add(row);
        }

        return data;
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
    /// Gets a color hex string for a series by index.
    /// </summary>
    /// <param name="index">The series index.</param>
    /// <returns>A hex color string for the series.</returns>
    private static string GetColorHexForIndex(int index)
    {
        return AppColors.Palette[index % AppColors.Palette.Length];
    }

    /// <summary>
    /// Creates pie chart series and legend items with "Other" grouping based on MaxPieSlices setting.
    /// </summary>
    /// <param name="dataPoints">The source data points.</param>
    /// <returns>A tuple containing the series collection and legend items.</returns>
    private static (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> LegendItems) CreatePieSeriesWithLegend(
        List<ChartDataPoint> dataPoints)
    {
        var maxSlices = ChartSettingsService.GetMaxPieSlices();
        return CreatePieSeriesWithLegend(dataPoints, maxSlices);
    }

    /// <summary>
    /// Creates pie chart series and legend items with "Other" grouping.
    /// </summary>
    /// <param name="dataPoints">The source data points.</param>
    /// <param name="maxSlices">Maximum number of slices before grouping into "Other".</param>
    /// <returns>A tuple containing the series collection and legend items.</returns>
    private static (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> LegendItems) CreatePieSeriesWithLegend(
        List<ChartDataPoint> dataPoints,
        int maxSlices)
    {
        var series = new ObservableCollection<ISeries>();
        var legendItems = new ObservableCollection<PieLegendItem>();

        if (dataPoints.Count == 0)
            return (series, legendItems);

        // Sort by value descending
        var sortedPoints = dataPoints.OrderByDescending(p => p.Value).ToList();
        var total = sortedPoints.Sum(p => p.Value);

        // Take top items and group the rest into "Other"
        var topItems = sortedPoints.Take(maxSlices - 1).ToList();
        var otherItems = sortedPoints.Skip(maxSlices - 1).ToList();

        // If there's only one "other" item, just show it directly instead of grouping
        if (otherItems.Count == 1)
        {
            topItems.Add(otherItems[0]);
            otherItems.Clear();
        }

        // Create series and legend items for top items
        for (int i = 0; i < topItems.Count; i++)
        {
            var item = topItems[i];
            var colorHex = GetColorHexForIndex(i);
            var percentage = total > 0 ? (item.Value / total) * 100 : 0;
            var roundedValue = Math.Round(item.Value, 2);

            series.Add(new PieSeries<double>
            {
                Values = [roundedValue],
                Name = TruncateLegendLabel(item.Label),
                Fill = new SolidColorPaint(SKColor.Parse(colorHex)),
                Pushout = 0,
                ToolTipLabelFormatter = point => $"${point.Coordinate.PrimaryValue:N2}"
            });

            legendItems.Add(new PieLegendItem
            {
                Label = item.Label,
                Value = roundedValue,
                Percentage = percentage,
                ColorHex = colorHex
            });
        }

        // Create "Other" category if needed
        if (otherItems.Count > 0)
        {
            var otherValue = Math.Round(otherItems.Sum(p => p.Value), 2);
            var otherPercentage = total > 0 ? (otherValue / total) * 100 : 0;
            var otherColorHex = AppColors.Gray;

            series.Add(new PieSeries<double>
            {
                Values = [otherValue],
                Name = LanguageService.Instance.Translate("Other"),
                Fill = new SolidColorPaint(SKColor.Parse(otherColorHex)),
                Pushout = 0,
                ToolTipLabelFormatter = point => $"${point.Coordinate.PrimaryValue:N2}"
            });

            var itemsText = LanguageService.Instance.Translate("items");
            legendItems.Add(new PieLegendItem
            {
                Label = $"{LanguageService.Instance.Translate("Other")} ({otherItems.Count} {itemsText})",
                Value = otherValue,
                Percentage = otherPercentage,
                ColorHex = otherColorHex
            });
        }

        return (series, legendItems);
    }

}

/// <summary>
/// Data structure for chart export functionality.
/// </summary>
public class ChartExportData
{
    public string ChartTitle { get; init; } = string.Empty;
    public ChartType ChartType { get; init; }
    public string[] Labels { get; init; } = [];
    public double[] Values { get; init; } = [];
    public string SeriesName { get; init; } = string.Empty;

    /// <summary>
    /// Additional series for multi-series charts (e.g., Revenue vs Expenses).
    /// Each entry contains a series name and its values.
    /// </summary>
    public List<(string Name, double[] Values)> AdditionalSeries { get; init; } = [];

    /// <summary>
    /// Returns true if this chart has multiple series.
    /// </summary>
    public bool IsMultiSeries => AdditionalSeries.Count > 0;
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
/// Visual chart style for rendering series.
/// </summary>
public enum ChartStyle
{
    Line,
    Column,
    StepLine,
    Area,
    Scatter
}

