#pragma warning disable CS0618 // LabelVisual is obsolete
using System.Collections.ObjectModel;
using ArgoBooks.Core;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class UnifiedChartWidgetViewModel : WidgetViewModelBase
{
    public ChartDataType ChartDataType { get; private set; }

    public override WidgetType WidgetType => WidgetType.Chart;

    public bool IsDistribution => ChartDataType.IsDistribution();

    public ChartLoaderService ChartLoaderService { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CartesianSeries))]
    [NotifyPropertyChangedFor(nameof(PieSeries))]
    private ObservableCollection<ISeries> _series = [];

    // Separate bindings so PieChart never sees LineSeries and vice versa
    public ObservableCollection<ISeries>? CartesianSeries => IsDistribution ? null : Series;
    public ObservableCollection<ISeries>? PieSeries => IsDistribution ? Series : null;

    [ObservableProperty]
    private Axis[] _xAxes = [new Axis()];

    [ObservableProperty]
    private Axis[] _yAxes = [new Axis()];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private bool _hasData;

    public bool HasNoData => !HasData;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChartTitleVisual))]
    private string _chartTitle = "";

    public LabelVisual ChartTitleVisual => ChartLoaderService.CreateChartTitle(ChartTitle);

    [ObservableProperty]
    private string _emptyStateMessage = "No data available";

    [ObservableProperty]
    private string _chartStyle = "pie";

    public string[] ChartStyleOptions { get; } = ["pie", "donut"];

    public override bool HasConfig => IsDistribution;

    partial void OnChartStyleChanged(string value) => LoadData();

    public UnifiedChartWidgetViewModel(ChartDataType chartDataType)
    {
        ChartDataType = chartDataType;
        ChartTitle = chartDataType.GetDisplayName();
        EmptyStateMessage = $"No {chartDataType.GetChartCategory().ToLowerInvariant()} data available";
    }

    public override void Initialize(Dictionary<string, string> config)
    {
        ApplyConfig(config);
    }

    public override void ApplyConfig(Dictionary<string, string> config)
    {
        if (config.TryGetValue("ChartDataType", out var typeStr)
            && Enum.TryParse<ChartDataType>(typeStr, out var parsed))
        {
            ChartDataType = parsed;
            OnPropertyChanged(nameof(IsDistribution));
            OnPropertyChanged(nameof(HasConfig));
        }

        if (config.TryGetValue("ChartStyle", out var style))
            ChartStyle = style;
    }

    public override Dictionary<string, string> GetConfig()
    {
        var config = new Dictionary<string, string>
        {
            ["ChartDataType"] = ChartDataType.ToString()
        };
        if (IsDistribution)
            config["ChartStyle"] = ChartStyle;
        return config;
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        if (data == null) return;

        var chartSettings = ChartSettingsService.Instance;

        ChartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);
        ChartLoaderService.SelectedChartStyle = chartSettings.SelectedChartType switch
        {
            "Line" => Services.ChartStyle.Line,
            "Column" => Services.ChartStyle.Column,
            "Step Line" => Services.ChartStyle.StepLine,
            "Area" => Services.ChartStyle.Area,
            "Scatter" => Services.ChartStyle.Scatter,
            _ => Services.ChartStyle.Line
        };

        var filters = new ReportFilters
        {
            StartDate = chartSettings.StartDate,
            EndDate = chartSettings.EndDate
        };

        ChartTitle = ChartDataType.GetDisplayName();

        // Total Profits uses the analytics-page loader so the dashboard widget
        // gets the same positive=green / negative=red bar split and computed title.
        if (ChartDataType == ChartDataType.TotalProfits)
        {
            LoadTotalProfitsChart(data, chartSettings.StartDate, chartSettings.EndDate);
            return;
        }

        var service = new ReportChartDataService(data, filters);
        var result = service.GetChartData(ChartDataType);

        if (IsDistribution)
            LoadDistributionChart(result);
        else if (ChartDataType.IsMultiSeries())
            LoadMultiSeriesChart(result);
        else
            LoadSingleSeriesChart(result);
    }

    private void LoadTotalProfitsChart(CompanyData data, DateTime startDate, DateTime endDate)
    {
        var (series, _, dates, totalProfit) = ChartLoaderService.LoadProfitsOverviewChart(data, startDate, endDate);

        var localizedName = ChartDataType.TotalProfits.GetDisplayName().Translate();
        var formattedSum = CurrencyService.FormatFromUSD(totalProfit, DateTime.Now);
        ChartTitle = $"{localizedName}: {formattedSum}";

        XAxes = ChartLoaderService.CreateDateXAxes(dates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        Series = series;
        HasData = dates.Length > 0;
    }

    private void LoadDistributionChart(object result)
    {
        if (result is not List<ChartDataPoint> points || points.Count == 0)
        {
            Series = [];
            HasData = false;
            return;
        }

        var isDonut = ChartStyle == "donut";
        var series = new ObservableCollection<ISeries>();

        // Convert USD aggregates to display currency once at this boundary
        // so pie slices / tooltips / export all agree with the stat cards.
        // See docs/Calculations.md §3.
        var top = points.OrderByDescending(p => p.Value).Take(8).ToList();
        var displayValues = ChartLoaderService.ConvertUSDValuesToDisplay(
            top.Select(p => p.Value).ToArray());
        for (int i = 0; i < top.Count; i++)
        {
            var point = top[i];
            var colorHex = AppColors.Palette[i % AppColors.Palette.Length];
            series.Add(new PieSeries<double>
            {
                Values = [Math.Round(displayValues[i], 2)],
                Name = TruncateLabel(point.Label),
                Fill = new SolidColorPaint(SKColor.Parse(colorHex)),
                InnerRadius = isDonut ? 50 : 0,
                Pushout = 0,
                // Values are already in display currency, so just format.
                ToolTipLabelFormatter = p =>
                    CurrencyService.Format((decimal)p.Coordinate.PrimaryValue)
            });
        }

        Series = series;
        HasData = true;

        ChartLoaderService.StoreExportData(ChartDataType, new ChartExportData
        {
            ChartTitle = ChartTitle,
            ChartType = ChartType.Distribution,
            Labels = top.Select(p => p.Label).ToArray(),
            Values = displayValues.Select(v => Math.Round(v, 2)).ToArray(),
            SeriesName = ChartDataType.GetDisplayName()
        });
    }

    private void LoadMultiSeriesChart(object result)
    {
        if (result is not List<ChartSeriesData> seriesData || seriesData.Count == 0)
        {
            Series = [];
            HasData = false;
            return;
        }

        var allDates = seriesData
            .SelectMany(s => s.DataPoints.Where(p => p.Date.HasValue).Select(p => p.Date!.Value))
            .Distinct().OrderBy(d => d).ToArray();

        // CreateDateTimeSeries converts USD aggregates to display currency
        // internally — pass USD values straight through.
        var series = new ObservableCollection<ISeries>();
        var seriesUsdValues = new List<double[]>();
        for (int i = 0; i < seriesData.Count; i++)
        {
            var sd = seriesData[i];
            var usdValues = allDates.Select(date =>
                sd.DataPoints.FirstOrDefault(p => p.Date == date)?.Value ?? 0.0).ToArray();
            seriesUsdValues.Add(usdValues);

            var colorHex = sd.Color ?? AppColors.Palette[i % AppColors.Palette.Length];
            series.Add(ChartLoaderService.CreateDateTimeSeries(
                allDates, usdValues, sd.Name, SKColor.Parse(colorHex)));
        }

        XAxes = ChartLoaderService.CreateDateXAxes(allDates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        Series = series;
        HasData = allDates.Length > 0;

        if (seriesData.Count > 0)
        {
            // Export values in display currency too, so spreadsheet/PDF
            // exports match what the chart shows.
            var primaryDisplay = ChartLoaderService.ConvertUSDValuesToDisplay(seriesUsdValues[0]);
            ChartLoaderService.StoreExportData(ChartDataType, new ChartExportData
            {
                ChartTitle = ChartTitle,
                ChartType = ChartDataType.GetChartExportType(),
                Labels = allDates.Select(d => d.ToString("yyyy-MM-dd")).ToArray(),
                Values = primaryDisplay,
                SeriesName = seriesData[0].Name,
                AdditionalSeries = seriesData
                    .Skip(1)
                    .Select((sd, idx) => (sd.Name, ChartLoaderService.ConvertUSDValuesToDisplay(seriesUsdValues[idx + 1])))
                    .ToList()
            });
        }
    }

    private void LoadSingleSeriesChart(object result)
    {
        if (result is not List<ChartDataPoint> points || points.Count == 0)
        {
            Series = [];
            HasData = false;
            return;
        }

        var dated = points.Where(p => p.Date.HasValue).ToList();

        // Bucket daily data into weeks/months when the date range is wide so
        // column bars are a readable width instead of hairline-thin slivers.
        if (dated.Count >= 2)
        {
            var bucket = ReportChartDataService.GetTimeBucket(
                dated[0].Date!.Value, dated[^1].Date!.Value);
            if (bucket != ReportChartDataService.TimeBucket.Day)
                dated = ReportChartDataService.RebucketSum(dated, bucket);
        }

        var dates = dated.Select(p => p.Date!.Value).ToArray();
        var usdValues = dated.Select(p => p.Value).ToArray();

        // CreateDateTimeSeries converts USD → display currency internally.
        var series = new ObservableCollection<ISeries>();
        series.Add(ChartLoaderService.CreateDateTimeSeries(
            dates, usdValues, ChartDataType.GetDisplayName(), SKColor.Parse(AppColors.Palette[0])));

        XAxes = ChartLoaderService.CreateDateXAxes(dates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        Series = series;
        HasData = dates.Length > 0;

        // Export values in display currency too, so spreadsheet/PDF
        // exports match what the chart shows.
        ChartLoaderService.StoreExportData(ChartDataType, new ChartExportData
        {
            ChartTitle = ChartTitle,
            ChartType = ChartDataType.GetChartExportType(),
            Labels = dated.Select(p => p.Label).ToArray(),
            Values = ChartLoaderService.ConvertUSDValuesToDisplay(usdValues),
            SeriesName = ChartDataType.GetDisplayName()
        });
    }

    private static string TruncateLabel(string? label)
    {
        if (string.IsNullOrEmpty(label)) return "Unknown";
        return label.Length > 18 ? label[..17] + "\u2026" : label;
    }
}

internal static class ChartDataTypeExportExtensions
{
    internal static Services.ChartType GetChartExportType(this ChartDataType type) => type switch
    {
        ChartDataType.TotalExpenses => Services.ChartType.Expense,
        ChartDataType.TotalRevenue => Services.ChartType.Revenue,
        ChartDataType.TotalProfits => Services.ChartType.Profit,
        _ when type.IsDistribution() => Services.ChartType.Distribution,
        _ when type.IsMultiSeries() => Services.ChartType.Comparison,
        _ => Services.ChartType.Revenue
    };
}
