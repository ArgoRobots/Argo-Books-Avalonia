#pragma warning disable CS0618 // LabelVisual is obsolete
using System.Collections.ObjectModel;
using System.Globalization;
using ArgoBooks.Core;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class CashFlowWidgetViewModel : WidgetViewModelBase
{
    public override WidgetType WidgetType => WidgetType.CashFlowSummary;

    public ChartLoaderService ChartLoaderService { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<ISeries> _series = [];

    [ObservableProperty]
    private Axis[] _xAxes = [new Axis()];

    [ObservableProperty]
    private Axis[] _yAxes = [new Axis()];

    [ObservableProperty]
    private bool _hasData;

    public bool HasNoData => !HasData;

    [ObservableProperty]
    private string _netCashFlow = "";

    public override bool HasConfig => true;

    [ObservableProperty]
    private string _period = "monthly";

    public string[] PeriodOptions { get; } = ["weekly", "monthly"];

    partial void OnPeriodChanged(string value) => LoadData();

    public override void Initialize(Dictionary<string, string> config)
    {
        ApplyConfig(config);
    }

    public override void ApplyConfig(Dictionary<string, string> config)
    {
        if (config.TryGetValue("Period", out var period))
            Period = period;
    }

    public override Dictionary<string, string> GetConfig()
    {
        return new Dictionary<string, string>
        {
            ["Period"] = Period
        };
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        if (data == null) return;

        ChartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);
        LoadCashFlow(data);
    }

    private void LoadCashFlow(CompanyData data)
    {
        var chartSettings = ChartSettingsService.Instance;
        var startDate = chartSettings.StartDate;
        var endDate = chartSettings.EndDate;

        var periods = BuildPeriods(startDate, endDate);
        if (periods.Count == 0)
        {
            Series = [];
            HasData = false;
            NetCashFlow = "";
            return;
        }

        var positivePoints = new List<ObservablePoint>();
        var negativePoints = new List<ObservablePoint>();
        var labels = new List<string>();
        decimal totalNet = 0;

        for (int i = 0; i < periods.Count; i++)
        {
            var (periodStart, periodEnd, label) = periods[i];

            var revenue = data.Revenues
                .Where(r => r.Date >= periodStart && r.Date <= periodEnd)
                .Sum(r => r.EffectiveSubtotalUSD);

            var expenses = data.Expenses
                .Where(e => e.Date >= periodStart && e.Date <= periodEnd)
                .Sum(e => e.EffectiveSubtotalUSD);

            var net = revenue - expenses;
            totalNet += net;
            labels.Add(label);

            if (net >= 0)
            {
                positivePoints.Add(new ObservablePoint(i, (double)net));
                negativePoints.Add(new ObservablePoint(i, 0));
            }
            else
            {
                positivePoints.Add(new ObservablePoint(i, 0));
                negativePoints.Add(new ObservablePoint(i, (double)net));
            }
        }

        var series = new ObservableCollection<ISeries>();

        if (positivePoints.Any(p => p.Y > 0))
        {
            series.Add(new ColumnSeries<ObservablePoint>
            {
                Values = positivePoints,
                Name = "Positive",
                Fill = new SolidColorPaint(SKColor.Parse(AppColors.Success)),
                Stroke = null,
                MaxBarWidth = 40
            });
        }

        if (negativePoints.Any(p => p.Y < 0))
        {
            series.Add(new ColumnSeries<ObservablePoint>
            {
                Values = negativePoints,
                Name = "Negative",
                Fill = new SolidColorPaint(SKColor.Parse(AppColors.ExpenseRed)),
                Stroke = null,
                MaxBarWidth = 40
            });
        }

        Series = series;
        XAxes = ChartLoaderService.CreateXAxes(labels.ToArray());
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        NetCashFlow = CurrencyService.FormatFromUSD(totalNet, DateTime.Now);
        HasData = series.Count > 0;
    }

    private List<(DateTime Start, DateTime End, string Label)> BuildPeriods(DateTime startDate, DateTime endDate)
    {
        var periods = new List<(DateTime Start, DateTime End, string Label)>();

        if (Period == "weekly")
        {
            // Align to start of week (Monday)
            var current = startDate.Date;
            while (current.DayOfWeek != DayOfWeek.Monday && current < endDate)
                current = current.AddDays(1);

            while (current < endDate)
            {
                var weekEnd = current.AddDays(6);
                if (weekEnd > endDate) weekEnd = endDate;
                var label = current.ToString("MMM d", CultureInfo.InvariantCulture);
                periods.Add((current, weekEnd, label));
                current = current.AddDays(7);
            }
        }
        else // monthly
        {
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            while (current <= endDate)
            {
                var monthEnd = current.AddMonths(1).AddDays(-1);
                if (monthEnd > endDate) monthEnd = endDate;
                var periodStart = current < startDate ? startDate : current;
                var label = current.ToString("MMM yy", CultureInfo.InvariantCulture);
                periods.Add((periodStart, monthEnd, label));
                current = current.AddMonths(1);
            }
        }

        return periods;
    }
}
