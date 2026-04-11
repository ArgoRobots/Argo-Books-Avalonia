#pragma warning disable CS0618 // LabelVisual is obsolete
using System.Collections.ObjectModel;
using ArgoBooks.Core;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
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
    private ObservableCollection<ISeries> _series = [];

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
    private string _chartStyle = "donut";

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

        var filters = new ReportFilters
        {
            StartDate = chartSettings.StartDate,
            EndDate = chartSettings.EndDate
        };

        var service = new ReportChartDataService(data, filters);
        var result = service.GetChartData(ChartDataType);

        ChartTitle = ChartDataType.GetDisplayName();

        if (IsDistribution)
            LoadDistributionChart(result);
        else if (ChartDataType.IsMultiSeries())
            LoadMultiSeriesChart(result);
        else
            LoadSingleSeriesChart(result);
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

        var top = points.OrderByDescending(p => p.Value).Take(8).ToList();
        for (int i = 0; i < top.Count; i++)
        {
            var point = top[i];
            var colorHex = AppColors.Palette[i % AppColors.Palette.Length];
            series.Add(new PieSeries<double>
            {
                Values = [Math.Round(point.Value, 2)],
                Name = TruncateLabel(point.Label),
                Fill = new SolidColorPaint(SKColor.Parse(colorHex)),
                InnerRadius = isDonut ? 50 : 0,
                Pushout = 0,
                ToolTipLabelFormatter = p =>
                    CurrencyService.FormatFromUSD((decimal)p.Coordinate.PrimaryValue, DateTime.Now)
            });
        }

        Series = series;
        HasData = true;
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

        var series = new ObservableCollection<ISeries>();
        for (int i = 0; i < seriesData.Count; i++)
        {
            var sd = seriesData[i];
            var values = allDates.Select(date =>
                sd.DataPoints.FirstOrDefault(p => p.Date == date)?.Value ?? 0.0).ToArray();

            var colorHex = sd.Color ?? AppColors.Palette[i % AppColors.Palette.Length];
            series.Add(CreateLineSeries(values, sd.Name, colorHex));
        }

        XAxes = ChartLoaderService.CreateDateXAxes(allDates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        Series = series;
        HasData = allDates.Length > 0;
    }

    private void LoadSingleSeriesChart(object result)
    {
        if (result is not List<ChartDataPoint> points || points.Count == 0)
        {
            Series = [];
            HasData = false;
            return;
        }

        var dates = points.Where(p => p.Date.HasValue).Select(p => p.Date!.Value).ToArray();
        var values = points.Select(p => p.Value).ToArray();

        var series = new ObservableCollection<ISeries>();
        series.Add(CreateLineSeries(values, ChartDataType.GetDisplayName(), AppColors.Palette[0]));

        XAxes = ChartLoaderService.CreateDateXAxes(dates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        Series = series;
        HasData = dates.Length > 0;
    }

    private static LineSeries<double> CreateLineSeries(double[] values, string name, string colorHex)
    {
        var color = SKColor.Parse(colorHex);
        return new LineSeries<double>
        {
            Values = values,
            Name = name,
            Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
            GeometryStroke = new SolidColorPaint(color) { StrokeThickness = 2 },
            GeometrySize = 0,
            Fill = null
        };
    }

    private static string TruncateLabel(string? label)
    {
        if (string.IsNullOrEmpty(label)) return "Unknown";
        return label.Length > 18 ? label[..17] + "\u2026" : label;
    }
}
