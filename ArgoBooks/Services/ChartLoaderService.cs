using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
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
    private const float LegendTextSize = 14f;

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

    // Maximum length for legend labels to prevent overflow
    private const int MaxLegendLabelLength = 18;

    /// <summary>
    /// Truncates a legend label to prevent pie chart legend overflow.
    /// </summary>
    private static string TruncateLegendLabel(string? label)
    {
        if (string.IsNullOrEmpty(label))
            return "Unknown";

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
        var textColor = isDarkTheme ? SKColor.Parse("#F9FAFB") : SKColor.Parse("#1F2937");
        return new LabelVisual
        {
            Text = text,
            TextSize = 16,
            Padding = new LiveChartsCore.Drawing.Padding(15, 12),
            Paint = new SolidColorPaint(textColor) { FontFamily = "Segoe UI", SKFontStyle = new SKFontStyle(SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) }
        };
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

    // ISO code to country name mapping (reverse of CountryNameToIsoCode)
    private static readonly Dictionary<string, string> IsoCodeToCountryName = new(StringComparer.OrdinalIgnoreCase)
    {
        { "usa", "United States" },
        { "gbr", "United Kingdom" },
        { "can", "Canada" },
        { "deu", "Germany" },
        { "fra", "France" },
        { "ita", "Italy" },
        { "esp", "Spain" },
        { "aus", "Australia" },
        { "jpn", "Japan" },
        { "chn", "China" },
        { "ind", "India" },
        { "bra", "Brazil" },
        { "mex", "Mexico" },
        { "rus", "Russia" },
        { "kor", "South Korea" },
        { "nld", "Netherlands" },
        { "che", "Switzerland" },
        { "swe", "Sweden" },
        { "nor", "Norway" },
        { "dnk", "Denmark" },
        { "fin", "Finland" },
        { "pol", "Poland" },
        { "bel", "Belgium" },
        { "aut", "Austria" },
        { "irl", "Ireland" },
        { "prt", "Portugal" },
        { "grc", "Greece" },
        { "nzl", "New Zealand" },
        { "sgp", "Singapore" },
        { "hkg", "Hong Kong" },
        { "twn", "Taiwan" },
        { "zaf", "South Africa" },
        { "arg", "Argentina" },
        { "chl", "Chile" },
        { "col", "Colombia" },
        { "idn", "Indonesia" },
        { "mys", "Malaysia" },
        { "tha", "Thailand" },
        { "vnm", "Vietnam" },
        { "phl", "Philippines" },
        { "tur", "Turkey" },
        { "sau", "Saudi Arabia" },
        { "are", "UAE" },
        { "isr", "Israel" },
        { "egy", "Egypt" },
        { "nga", "Nigeria" },
        { "ken", "Kenya" },
        { "ukr", "Ukraine" },
        { "cze", "Czech Republic" },
        { "rou", "Romania" },
        { "hun", "Hungary" }
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
    /// Converts an ISO 3166-1 alpha-3 code to country name for display.
    /// </summary>
    private static string GetCountryName(string? isoCode)
    {
        if (string.IsNullOrEmpty(isoCode))
            return "Unknown";

        return IsoCodeToCountryName.TryGetValue(isoCode, out var name) ? name : isoCode.ToUpperInvariant();
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
    /// Dictionary storing export data for each chart by its title.
    /// </summary>
    private readonly Dictionary<string, ChartExportData> _chartExportDataByTitle = new();

    /// <summary>
    /// Gets or sets whether to use line charts instead of column charts.
    /// </summary>
    [Obsolete("Use ChartStyle property instead")]
    public bool UseLineChart
    {
        get => SelectedChartStyle == ChartStyle.Line;
        set => SelectedChartStyle = value ? ChartStyle.Line : ChartStyle.Column;
    }

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
                MaxBarWidth = 50
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
                MaxBarWidth = 50
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
                    MaxBarWidth = 50
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
                    MaxBarWidth = 50
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
                    return DateFormatService.Format(date);
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

    /// <summary>
    /// Converts a list of ChartDataPoint objects to a LiveChartsCore series.
    /// This enables using the shared ReportChartDataService for data fetching.
    /// </summary>
    /// <param name="dataPoints">The chart data points from the shared service.</param>
    /// <param name="seriesName">The name of the series.</param>
    /// <param name="color">The color for the series.</param>
    /// <returns>A configured ISeries for LiveChartsCore.</returns>
    private ISeries ConvertToSeries(List<ChartDataPoint> dataPoints, string seriesName, SKColor color)
    {
        if (dataPoints.Count == 0)
        {
            return new ColumnSeries<double> { Values = [], Name = seriesName };
        }

        // Check if we have date-based data
        if (dataPoints.All(p => p.Date.HasValue))
        {
            var dates = dataPoints.Select(p => p.Date!.Value).ToArray();
            var values = dataPoints.Select(p => p.Value).ToArray();
            return CreateDateTimeSeries(dates, values, seriesName, color);
        }

        // Use categorical (label-based) series
        var categoryValues = dataPoints.Select(p => p.Value).ToArray();
        return CreateTimeSeries(categoryValues, seriesName, color);
    }

    /// <summary>
    /// Converts a list of ChartDataPoint objects to a pie chart series.
    /// </summary>
    /// <param name="dataPoints">The chart data points from the shared service.</param>
    /// <returns>A collection of PieSeries for LiveChartsCore.</returns>
    public ObservableCollection<ISeries> ConvertToPieSeries(List<ChartDataPoint> dataPoints)
    {
        var series = new ObservableCollection<ISeries>();

        if (dataPoints.Count == 0)
            return series;

        var colors = GetDistributionColors();

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var point = dataPoints[i];
            var color = !string.IsNullOrEmpty(point.Color)
                ? SKColor.Parse(point.Color)
                : colors[i % colors.Length];

            series.Add(new PieSeries<double>
            {
                Values = [point.Value],
                Name = TruncateLegendLabel(point.Label),
                Fill = new SolidColorPaint(color),
                DataLabelsSize = LegendTextSize,
                DataLabelsPaint = new SolidColorPaint(_textColor),
                DataLabelsPosition = PolarLabelsPosition.Outer
            });
        }

        return series;
    }

    /// <summary>
    /// Converts multi-series chart data to LiveChartsCore series.
    /// Used for charts like Sales vs Expenses.
    /// </summary>
    /// <param name="seriesDataList">The multi-series data from the shared service.</param>
    /// <returns>A collection of ISeries for LiveChartsCore.</returns>
    public ObservableCollection<ISeries> ConvertMultiSeriesToLiveCharts(List<ChartSeriesData> seriesDataList)
    {
        var series = new ObservableCollection<ISeries>();

        foreach (var seriesData in seriesDataList)
        {
            var color = SKColor.Parse(seriesData.Color);
            series.Add(ConvertToSeries(seriesData.DataPoints, seriesData.Name, color));
        }

        return series;
    }

    /// <summary>
    /// Gets an array of colors for distribution charts.
    /// </summary>
    private static SKColor[] GetDistributionColors()
    {
        // Predefined colors for distribution charts (matching WinForms/dashboard style)
        return
        [
            SKColor.Parse("#6495ED"), // Cornflower Blue
            SKColor.Parse("#22C55E"), // Green
            SKColor.Parse("#F59E0B"), // Amber
            SKColor.Parse("#EF4444"), // Red
            SKColor.Parse("#8B5CF6"), // Violet
            SKColor.Parse("#EC4899"), // Pink
            SKColor.Parse("#06B6D4"), // Cyan
            SKColor.Parse("#84CC16"), // Lime
            SKColor.Parse("#F97316"), // Orange
            SKColor.Parse("#6366F1")  // Indigo
        ];
    }

    #endregion

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
    /// Uses ReportChartDataService for data fetching.
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

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetExpensesOverTime();

        if (dataPoints.Count == 0)
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

        labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();
        totalExpenses = dataService.GetTotalExpenses();

        series.Add(CreateDateTimeSeries(dates, values, "Expenses", RevenueColor));

        StoreExportData(new ChartExportData
        {
            ChartTitle = "Expenses Overview",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Expenses",
            TotalValue = (double)totalExpenses,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        });

        return (series, labels, dates, totalExpenses);
    }

    /// <summary>
    /// Loads revenue overview chart data as a column series.
    /// Uses ReportChartDataService for data fetching.
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

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetRevenueOverTime();

        if (dataPoints.Count == 0)
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

        labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();
        totalRevenue = dataService.GetTotalRevenue();

        series.Add(CreateDateTimeSeries(dates, values, "Revenue", ProfitColor));

        StoreExportData(new ChartExportData
        {
            ChartTitle = "Revenue Overview",
            ChartType = ChartType.Revenue,
            Labels = labels,
            Values = values,
            SeriesName = "Revenue",
            TotalValue = (double)totalRevenue,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        });

        return (series, labels, dates, totalRevenue);
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

        labels = dataPoints.Select(p => p.Label).ToArray();
        dates = dataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();
        totalProfit = (decimal)values.Sum();

        // Use profit-specific series that shows negative values in red for column charts
        foreach (var s in CreateProfitDateTimeSeries(dates, values, "Profit"))
        {
            series.Add(s);
        }

        StoreExportData(new ChartExportData
        {
            ChartTitle = "Profits Overview",
            ChartType = ChartType.Profit,
            Labels = labels,
            Values = values,
            SeriesName = "Profit",
            TotalValue = (double)totalProfit,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        });

        return (series, labels, dates, totalProfit);
    }

    /// <summary>
    /// Loads expenses vs revenue comparison chart as a multi-series column chart.
    /// Uses ReportChartDataService for data fetching with daily granularity.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadSalesVsExpensesChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var seriesData = dataService.GetSalesVsExpensesDaily();

        if (seriesData.Count < 2)
        {
            _chartExportDataByTitle["Expenses vs Revenue"] = new ChartExportData
            {
                ChartTitle = "Expenses vs Revenue",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, labels, dates);
        }

        var revenueData = seriesData.FirstOrDefault(s => s.Name == "Revenue");
        var expenseData = seriesData.FirstOrDefault(s => s.Name == "Expenses");

        if (revenueData == null || expenseData == null || revenueData.DataPoints.Count == 0)
        {
            _chartExportDataByTitle["Expenses vs Revenue"] = new ChartExportData
            {
                ChartTitle = "Expenses vs Revenue",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, labels, dates);
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
            _chartExportDataByTitle["Expenses vs Revenue"] = new ChartExportData
            {
                ChartTitle = "Expenses vs Revenue",
                ChartType = ChartType.Comparison,
                Labels = [],
                Values = [],
                SeriesName = "Expenses"
            };
            return (series, labels, dates);
        }

        labels = indicesWithData.Select(i => revenueData.DataPoints[i].Label).ToArray();
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
        _chartExportDataByTitle["Expenses vs Revenue"] = new ChartExportData
        {
            ChartTitle = "Expenses vs Revenue",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = expenseValues,
            SeriesName = "Expenses",
            AdditionalSeries = [("Revenue", revenueValues)],
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
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

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data for Google Sheets/Excel export
        var exportData = new ChartExportData
        {
            ChartTitle = "Revenue Distribution",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };
        _chartExportDataByTitle["Revenue Distribution"] = exportData;

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

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data for Google Sheets/Excel export
        PieChartExportData = new ChartExportData
        {
            ChartTitle = "Expense Distribution",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        // Also store by title for chart-specific retrieval (various UI titles)
        _chartExportDataByTitle["Expense Distribution"] = PieChartExportData;
        _chartExportDataByTitle["Purchase Distribution"] = PieChartExportData;
        _chartExportDataByTitle["Distribution of expenses"] = PieChartExportData;

        return (series, legend);
    }

    /// <summary>
    /// Loads growth rates chart data.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels) LoadGrowthRatesChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();

        var filters = CreateFiltersMonths(startDate, endDate, 12);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetGrowthRates();

        if (dataPoints.Count == 0)
            return (series, labels);

        labels = dataPoints.Select(p => p.Label).ToArray();
        var values = dataPoints.Select(p => p.Value).ToArray();

        // Only add series if there's actual data (any non-zero growth rates)
        if (values.Any(v => v != 0))
        {
            series.Add(CreateTimeSeries(values, "Growth Rate %", RevenueColor));
        }

        // Store export data
        _chartExportDataByTitle["Growth Rates"] = new ChartExportData
        {
            ChartTitle = "Growth Rates",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = values,
            SeriesName = "Growth Rate %",
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels);
    }

    /// <summary>
    /// Loads average transaction value chart with daily granularity.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadAverageTransactionValueChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.TransactionType = TransactionType.Both;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetAverageTransactionValueDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, labels, dates);

        labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var avgValues = filteredData.Select(p => p.Value).ToArray();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, avgValues, "Avg Transaction", RevenueColor));
        }

        // Store export data
        _chartExportDataByTitle["Average Transaction Value"] = new ChartExportData
        {
            ChartTitle = "Average Transaction Value",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = avgValues,
            SeriesName = "Avg Transaction",
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads total transactions count chart with daily granularity.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadTotalTransactionsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        filters.TransactionType = TransactionType.Both;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetTransactionCountDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, labels, dates);

        labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var countValues = filteredData.Select(p => p.Value).ToArray();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, countValues, "Transactions", RevenueColor));
        }

        // Store export data
        _chartExportDataByTitle["Total Transactions"] = new ChartExportData
        {
            ChartTitle = "Total Transactions",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = countValues,
            SeriesName = "Transactions",
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads average shipping costs chart with daily granularity.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadAverageShippingCostsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();
        var labels = Array.Empty<string>();
        var dates = Array.Empty<DateTime>();

        var filters = CreateFilters(startDate, endDate);
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetAverageShippingCostsDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, labels, dates);

        labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var avgShipping = filteredData.Select(p => p.Value).ToArray();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, avgShipping, "Avg Shipping", RevenueColor));
        }

        // Store export data
        _chartExportDataByTitle["Average Shipping Costs"] = new ChartExportData
        {
            ChartTitle = "Average Shipping Costs",
            ChartType = ChartType.Comparison,
            Labels = labels,
            Values = avgShipping,
            SeriesName = "Avg Shipping",
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads countries of origin (supplier countries from purchases) as a pie chart.
    /// Uses same logic as LoadWorldMapDataBySupplier for consistency.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCountriesOfOriginChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Use same logic as LoadWorldMapDataBySupplier - get supplier countries from purchases
        var mapData = LoadWorldMapDataBySupplier(companyData, startDate, endDate);

        if (mapData.Count == 0)
            return ([], []);

        // Convert country codes to names and create data points
        var dataPoints = mapData
            .Select(kvp => new ChartDataPoint { Label = GetCountryName(kvp.Key), Value = kvp.Value })
            .OrderByDescending(p => p.Value)
            .ToList();

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        var filters = CreateFilters(startDate, endDate);

        // Store export data
        _chartExportDataByTitle["Countries of Origin"] = new ChartExportData
        {
            ChartTitle = "Countries of Origin",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

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
        var dataPoints = dataService.GetSalesByCustomerCountry().ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByTitle["Countries of Destination"] = new ChartExportData
        {
            ChartTitle = "Countries of Destination",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads companies of origin (supplier companies from purchases) as a pie chart.
    /// Uses same logic as LoadWorldMapDataBySupplier for consistency.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCompaniesOfOriginChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (companyData?.Purchases == null)
            return ([], []);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        // Get supplier companies from purchases (same logic as LoadWorldMapDataBySupplier)
        var purchasesInRange = companyData.Purchases
            .Where(p => p.Date >= start && p.Date <= end)
            .ToList();

        var dataPoints = purchasesInRange
            .GroupBy(p =>
            {
                var supplierId = GetEffectiveSupplierId(p, companyData);
                if (!string.IsNullOrEmpty(supplierId))
                {
                    var supplier = companyData.GetSupplier(supplierId);
                    return supplier?.Name ?? "Unknown";
                }
                return "Unknown";
            })
            .Where(g => g.Key != "Unknown")
            .Select(g => new ChartDataPoint { Label = g.Key, Value = (double)g.Sum(p => p.Total) })
            .OrderByDescending(p => p.Value)
            .ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByTitle["Companies of Origin"] = new ChartExportData
        {
            ChartTitle = "Companies of Origin",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = start,
            EndDate = end
        };

        return (series, legend);
    }

    /// <summary>
    /// Loads companies of destination (customer companies from sales) as a pie chart.
    /// Only shows data when customers have CompanyName set.
    /// </summary>
    public (ObservableCollection<ISeries> Series, ObservableCollection<PieLegendItem> Legend) LoadCompaniesOfDestinationChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (companyData?.Sales == null)
            return ([], []);

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        // Get customer companies from sales - only include customers with CompanyName set
        var salesInRange = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end)
            .ToList();

        var dataPoints = salesInRange
            .GroupBy(s =>
            {
                var customerId = GetEffectiveCustomerId(s);
                if (!string.IsNullOrEmpty(customerId))
                {
                    var customer = companyData.GetCustomer(customerId);
                    // Only use CompanyName, not Name - this chart is for companies only
                    if (!string.IsNullOrEmpty(customer?.CompanyName))
                        return customer.CompanyName;
                }
                return null;
            })
            .Where(g => g.Key != null)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = (double)g.Sum(s => s.Total) })
            .OrderByDescending(p => p.Value)
            .ToList();

        if (dataPoints.Count == 0)
            return ([], []);

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data
        _chartExportDataByTitle["Companies of Destination"] = new ChartExportData
        {
            ChartTitle = "Companies of Destination",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = start,
            EndDate = end
        };

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

        var total = dataPoints.Sum(p => p.Value);
        var (series, legend) = CreatePieSeriesWithLegend(dataPoints);

        // Store export data (used for "Transactions by Accountant" and "Companies of Destination")
        var exportData = new ChartExportData
        {
            ChartTitle = "Transactions by Accountant",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };
        _chartExportDataByTitle["Transactions by Accountant"] = exportData;
        _chartExportDataByTitle["Workload Distribution"] = exportData;

        return (series, legend);
    }

    /// <summary>
    /// Loads customer payment status chart (Paid vs Pending vs Overdue).
    /// </summary>
    public ObservableCollection<ISeries> LoadCustomerPaymentStatusChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();

        if (companyData?.Sales == null)
            return series;

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-30);

        var salesInRange = companyData.Sales.Where(s => s.Date >= start && s.Date <= end).ToList();
        if (salesInRange.Count == 0)
            return series;

        var paid = salesInRange.Count(s => s.PaymentStatus == "Paid" || s.PaymentStatus == "Complete");
        var pending = salesInRange.Count(s => s.PaymentStatus == "Pending" || string.IsNullOrEmpty(s.PaymentStatus));
        var overdue = salesInRange.Count(s => s.PaymentStatus == "Overdue");

        var total = salesInRange.Count;

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
                Values = [count],
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

        return series;
    }

    /// <summary>
    /// Loads active vs inactive customers chart.
    /// </summary>
    public ObservableCollection<ISeries> LoadActiveInactiveCustomersChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();

        if (companyData is not { Customers: not null, Sales: not null })
            return series;

        var end = endDate ?? DateTime.Now;
        var start = startDate ?? end.AddDays(-90); // Look at 90 days for activity

        var activeCustomerIds = companyData.Sales
            .Where(s => s.Date >= start && s.Date <= end && !string.IsNullOrEmpty(s.CustomerId))
            .Select(s => s.CustomerId)
            .Distinct()
            .ToHashSet();

        var activeCount = activeCustomerIds.Count;
        var inactiveCount = companyData.Customers.Count - activeCount;
        var total = companyData.Customers.Count;

        if (total == 0)
            return series;

        if (activeCount > 0)
        {
            series.Add(new PieSeries<double>
            {
                Values = [activeCount],
                Name = "Active",
                Fill = new SolidColorPaint(SKColor.Parse("#22C55E")),
                Pushout = 0
            });
        }

        if (inactiveCount > 0)
        {
            series.Add(new PieSeries<double>
            {
                Values = [inactiveCount],
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

        return series;
    }

    /// <summary>
    /// Loads loss reasons chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public ObservableCollection<ISeries> LoadLossReasonsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossReasons().Take(8).ToList();

        if (dataPoints.Count == 0)
            return series;

        var total = (int)dataPoints.Sum(p => p.Value);

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var item = dataPoints[i];
            series.Add(new PieSeries<double>
            {
                Values = [item.Value],
                Name = TruncateLegendLabel(item.Label),
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        // Store export data
        _chartExportDataByTitle["Loss Reasons"] = new ChartExportData
        {
            ChartTitle = "Loss Reasons",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return series;
    }

    /// <summary>
    /// Loads losses by product chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public ObservableCollection<ISeries> LoadLossesByProductChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeLosses = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetLossesByProduct().Take(8).ToList();

        if (dataPoints.Count == 0)
            return series;

        var total = dataPoints.Sum(p => p.Value);

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var item = dataPoints[i];
            series.Add(new PieSeries<double>
            {
                Values = [item.Value],
                Name = TruncateLegendLabel(item.Label),
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        // Store export data
        _chartExportDataByTitle["Losses by Product"] = new ChartExportData
        {
            ChartTitle = "Losses by Product",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Amount",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return series;
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
        var totalReturns = (int)values.Sum();

        series.Add(CreateDateTimeSeries(dates, values, "Returns", ExpenseColor));

        // Store export data
        _chartExportDataByTitle["Returns Over Time"] = new ChartExportData
        {
            ChartTitle = "Returns Over Time",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Returns",
            TotalValue = totalReturns,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads return reasons as a pie chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public ObservableCollection<ISeries> LoadReturnReasonsChart(
        CompanyData? companyData,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var series = new ObservableCollection<ISeries>();

        var filters = CreateFilters(startDate, endDate);
        filters.IncludeReturns = true;
        var dataService = new ReportChartDataService(companyData, filters);

        var dataPoints = dataService.GetReturnReasons().Take(8).ToList();

        if (dataPoints.Count == 0)
            return series;

        var total = (int)dataPoints.Sum(p => p.Value);

        for (int i = 0; i < dataPoints.Count; i++)
        {
            var item = dataPoints[i];
            series.Add(new PieSeries<double>
            {
                Values = [item.Value],
                Name = TruncateLegendLabel(item.Label),
                Fill = new SolidColorPaint(GetColorForIndex(i)),
                Pushout = 0
            });
        }

        // Store export data
        var exportData = new ChartExportData
        {
            ChartTitle = "Return Reasons",
            ChartType = ChartType.Distribution,
            Labels = dataPoints.Select(p => p.Label).ToArray(),
            Values = dataPoints.Select(p => p.Value).ToArray(),
            SeriesName = "Count",
            TotalValue = total,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };
        _chartExportDataByTitle["Return Reasons"] = exportData;
        _chartExportDataByTitle["Returns by Category"] = exportData;

        return series;
    }

    /// <summary>
    /// Loads return financial impact chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadReturnFinancialImpactChart(
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

        var dataPoints = dataService.GetReturnFinancialImpactDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, labels, dates);

        labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var impactValues = filteredData.Select(p => p.Value).ToArray();
        var totalImpact = impactValues.Sum();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, impactValues, "Refunds", ExpenseColor));
        }

        // Store export data
        _chartExportDataByTitle["Financial Impact of Returns"] = new ChartExportData
        {
            ChartTitle = "Financial Impact of Returns",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = impactValues,
            SeriesName = "Refunds",
            TotalValue = totalImpact,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
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
        var totalLosses = (int)values.Sum();

        series.Add(CreateDateTimeSeries(dates, values, "Losses", ExpenseColor));

        // Store export data
        _chartExportDataByTitle["Losses Over Time"] = new ChartExportData
        {
            ChartTitle = "Losses Over Time",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = values,
            SeriesName = "Losses",
            TotalValue = totalLosses,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
    }

    /// <summary>
    /// Loads loss financial impact chart.
    /// Uses ReportChartDataService for data fetching.
    /// </summary>
    public (ObservableCollection<ISeries> Series, string[] Labels, DateTime[] Dates) LoadLossFinancialImpactChart(
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

        var dataPoints = dataService.GetLossFinancialImpactDaily();

        // Filter to only days with data (non-zero values)
        var filteredData = dataPoints.Where(p => p.Value > 0).ToList();

        if (filteredData.Count == 0)
            return (series, labels, dates);

        labels = filteredData.Select(p => p.Label).ToArray();
        dates = filteredData.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var impactValues = filteredData.Select(p => p.Value).ToArray();
        var totalImpact = impactValues.Sum();

        if (dates.Length > 0)
        {
            series.Add(CreateDateTimeSeries(dates, impactValues, "Value Lost", ExpenseColor));
        }

        // Store export data
        _chartExportDataByTitle["Financial Impact of Losses"] = new ChartExportData
        {
            ChartTitle = "Financial Impact of Losses",
            ChartType = ChartType.Expense,
            Labels = labels,
            Values = impactValues,
            SeriesName = "Value Lost",
            TotalValue = totalImpact,
            StartDate = filters.StartDate,
            EndDate = filters.EndDate
        };

        return (series, labels, dates);
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
                if (!string.IsNullOrEmpty(customer?.Address?.Country))
                    return GetCountryIsoCode(customer.Address.Country);

                // Fall back to product's supplier country
                var firstProductId = s.LineItems?.FirstOrDefault()?.ProductId;
                if (!string.IsNullOrEmpty(firstProductId))
                {
                    var product = companyData.GetProduct(firstProductId);
                    if (product != null && !string.IsNullOrEmpty(product.SupplierId))
                    {
                        var supplier = companyData.GetSupplier(product.SupplierId);
                        if (!string.IsNullOrEmpty(supplier?.Address?.Country))
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
            string[] aliases = data.ChartTitle switch
            {
                "Expenses Overview" => ["Purchase Trends", "Total Expenses"],
                "Revenue Overview" => ["Sales Trends", "Total Revenue"],
                "Profits Overview" => ["Profit Over Time", "Profits over Time"],
                _ => []
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
        if (string.IsNullOrEmpty(chartId))
            return null;

        // First try to find by exact title match in the dictionary
        if (_chartExportDataByTitle.TryGetValue(chartId, out var data))
        {
            return data;
        }

        // Handle dynamic titles with patterns (e.g., "Total expenses: $171.00")
        if (chartId.StartsWith("Total expenses:", StringComparison.OrdinalIgnoreCase))
        {
            return _chartExportDataByTitle.GetValueOrDefault("Expenses Overview") ?? CurrentExportData;
        }

        if (chartId.StartsWith("Distribution of expenses", StringComparison.OrdinalIgnoreCase))
        {
            return PieChartExportData;
        }

        // Map UI chart titles to their corresponding export data
        // Many charts share the same underlying data series
        var mappedTitle = chartId switch
        {
            // Performance tab charts that share data with other charts
            "Processing Time Trends" => "Average Transaction Value",
            "Workload Distribution" => "Total Transactions",

            // Customers tab charts that share data with other charts
            "Top Customers by Revenue" => "Companies of Origin",
            "Customer Growth" => "Growth Rates",
            "Customer Lifetime Value" => "Average Transaction Value",
            "Rentals per Customer" => "Total Transactions",

            // Returns tab charts that share data with other charts
            "Returns by Category" => "Return Reasons",
            "Returns by Product" => "Expense Distribution",
            "Purchase vs Sale Returns" => "Sales vs Expenses",

            // Losses tab charts that share data with other charts
            "Losses by Category" => "Expense Distribution",
            "Purchase vs Sale Losses" => "Sales vs Expenses",

            // Legacy Dashboard page chart identifiers
            "ExpenseDistributionChart" => "Expense Distribution",
            "ExpensesChart" => "Expenses Overview",

            _ => null
        };

        if (mappedTitle != null && _chartExportDataByTitle.TryGetValue(mappedTitle, out var mappedData))
        {
            return mappedData;
        }

        // Return PieChartExportData for distribution-related charts not found above
        if (chartId.Contains("Distribution", StringComparison.OrdinalIgnoreCase))
        {
            return PieChartExportData;
        }

        return null;
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
    /// Gets the effective customer ID for a sale.
    /// </summary>
    private static string? GetEffectiveCustomerId(Sale sale)
    {
        return sale.CustomerId;
    }

    /// <summary>
    /// Gets a color hex string for a series by index.
    /// </summary>
    /// <param name="index">The series index.</param>
    /// <returns>A hex color string for the series.</returns>
    private static string GetColorHexForIndex(int index)
    {
        string[] colors =
        [
            "#6495ED", // Cornflower Blue
            "#EF4444", // Red
            "#22C55E", // Green
            "#F59E0B", // Amber
            "#8B5CF6", // Purple
            "#EC4899", // Pink
            "#14B8A6", // Teal
            "#F97316", // Orange
            "#6366F1", // Indigo
            "#84CC16", // Lime
        ];

        return colors[index % colors.Length];
    }

    /// <summary>
    /// Gets a color for a series by index.
    /// </summary>
    /// <param name="index">The series index.</param>
    /// <returns>An SKColor for the series.</returns>
    private static SKColor GetColorForIndex(int index)
    {
        return SKColor.Parse(GetColorHexForIndex(index));
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

            series.Add(new PieSeries<double>
            {
                Values = [item.Value],
                Name = TruncateLegendLabel(item.Label),
                Fill = new SolidColorPaint(SKColor.Parse(colorHex)),
                Pushout = 0
            });

            legendItems.Add(new PieLegendItem
            {
                Label = item.Label,
                Value = item.Value,
                Percentage = percentage,
                ColorHex = colorHex
            });
        }

        // Create "Other" category if needed
        if (otherItems.Count > 0)
        {
            var otherValue = otherItems.Sum(p => p.Value);
            var otherPercentage = total > 0 ? (otherValue / total) * 100 : 0;
            var otherColorHex = "#9CA3AF"; // Gray for "Other"

            series.Add(new PieSeries<double>
            {
                Values = [otherValue],
                Name = "Other",
                Fill = new SolidColorPaint(SKColor.Parse(otherColorHex)),
                Pushout = 0
            });

            legendItems.Add(new PieLegendItem
            {
                Label = $"Other ({otherItems.Count} items)",
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
    public string ChartTitle { get; set; } = string.Empty;
    public ChartType ChartType { get; set; }
    public string[] Labels { get; set; } = [];
    public double[] Values { get; set; } = [];
    public string SeriesName { get; set; } = string.Empty;
    public double? TotalValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Additional series for multi-series charts (e.g., Sales vs Expenses).
    /// Each entry contains a series name and its values.
    /// </summary>
    public List<(string Name, double[] Values)> AdditionalSeries { get; set; } = [];

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
