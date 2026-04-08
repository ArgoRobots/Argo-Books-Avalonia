#pragma warning disable CS0618 // LabelVisual is obsolete — DrawnLabelVisual is not API-compatible
using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ArgoBooks.ViewModels.Dashboard;

public enum ChartWidgetKind
{
    Profits,
    RevenueVsExpenses
}

public partial class ChartWidgetViewModel : WidgetViewModelBase
{
    public ChartWidgetKind Kind { get; }

    public override WidgetType WidgetType => Kind switch
    {
        ChartWidgetKind.Profits => WidgetType.ProfitsChart,
        ChartWidgetKind.RevenueVsExpenses => WidgetType.RevenueVsExpensesChart,
        _ => WidgetType.ProfitsChart
    };

    public ChartLoaderService ChartLoaderService { get; } = new();

    [ObservableProperty]
    private ObservableCollection<ISeries> _series = [];

    [ObservableProperty]
    private Axis[] _xAxes = [new Axis()];

    [ObservableProperty]
    private Axis[] _yAxes = [new Axis()];

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChartTitleVisual))]
    private string _chartTitle = "";

    public LabelVisual ChartTitleVisual => ChartLoaderService.CreateChartTitle(ChartTitle);

    [ObservableProperty]
    private string _emptyStateMessage = "";

    public ChartWidgetViewModel(ChartWidgetKind kind)
    {
        Kind = kind;
        EmptyStateMessage = kind == ChartWidgetKind.Profits
            ? "No profit data"
            : "No expense/revenue data available";
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        if (data == null) return;

        var chartSettings = ChartSettingsService.Instance;

        // Update theme colors and chart style
        ChartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);
        ChartLoaderService.SelectedChartStyle = chartSettings.SelectedChartType switch
        {
            "Line" => ChartStyle.Line,
            "Column" => ChartStyle.Column,
            "Step Line" => ChartStyle.StepLine,
            "Area" => ChartStyle.Area,
            "Scatter" => ChartStyle.Scatter,
            _ => ChartStyle.Line
        };

        var startDate = chartSettings.StartDate;
        var endDate = chartSettings.EndDate;

        switch (Kind)
        {
            case ChartWidgetKind.Profits:
                LoadProfitsChart(data, startDate, endDate);
                break;
            case ChartWidgetKind.RevenueVsExpenses:
                LoadRevenueVsExpensesChart(data, startDate, endDate);
                break;
        }
    }

    private void LoadProfitsChart(Core.Data.CompanyData data, DateTime startDate, DateTime endDate)
    {
        var (series, labels, dates, totalProfit) = ChartLoaderService.LoadProfitsOverviewChart(data, startDate, endDate);
        System.Diagnostics.Debug.WriteLine($"[Chart:Profits] series={series.Count}, labels={labels.Length}, dates={dates.Length}, totalProfit={totalProfit}, startDate={startDate}, endDate={endDate}");

        Series = series;
        XAxes = ChartLoaderService.CreateDateXAxes(dates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        ChartTitle = $"Total profits: {CurrencyService.FormatFromUSD(totalProfit, DateTime.Now)}";
        HasData = series.Count > 0 && labels.Length > 0;
    }

    private void LoadRevenueVsExpensesChart(Core.Data.CompanyData data, DateTime startDate, DateTime endDate)
    {
        var (series, dates) = ChartLoaderService.LoadRevenueVsExpensesChart(data, startDate, endDate);

        Series = series;
        XAxes = ChartLoaderService.CreateDateXAxes(dates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        ChartTitle = ChartDataType.RevenueVsExpenses.GetDisplayName();
        HasData = series.Count > 0 && dates.Length > 0;
    }
}
