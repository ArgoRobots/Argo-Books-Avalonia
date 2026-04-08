#pragma warning disable CS0618 // LabelVisual is obsolete
using System.Collections.ObjectModel;
using ArgoBooks.Core;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class ExpenseByCategoryWidgetViewModel : WidgetViewModelBase
{
    public override WidgetType WidgetType => WidgetType.ExpenseByCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoData))]
    private ObservableCollection<ISeries> _series = [];

    [ObservableProperty]
    private bool _hasData;

    public bool HasNoData => !HasData;

    public override bool HasConfig => true;

    [ObservableProperty]
    private string _chartStyle = "donut";

    public string[] ChartStyleOptions { get; } = ["pie", "donut"];

    public override void Initialize(Dictionary<string, string> config)
    {
        ApplyConfig(config);
    }

    public override void ApplyConfig(Dictionary<string, string> config)
    {
        if (config.TryGetValue("ChartStyle", out var style))
            ChartStyle = style;
    }

    public override Dictionary<string, string> GetConfig()
    {
        return new Dictionary<string, string>
        {
            ["ChartStyle"] = ChartStyle
        };
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        if (data == null) return;

        LoadExpenseByCategory(data);
    }

    private void LoadExpenseByCategory(CompanyData data)
    {
        var chartSettings = ChartSettingsService.Instance;
        var startDate = chartSettings.StartDate;
        var endDate = chartSettings.EndDate;

        var categoryTotals = new Dictionary<string, decimal>();

        foreach (var expense in data.Expenses.Where(e => e.Date >= startDate && e.Date <= endDate))
        {
            if (expense.LineItems.Count > 0)
            {
                var lineItemsTotal = expense.LineItems.Sum(li => li.Subtotal);
                var subtotalUSD = expense.EffectiveSubtotalUSD;

                if (lineItemsTotal != 0)
                {
                    foreach (var li in expense.LineItems)
                    {
                        var product = li.ProductId != null ? data.GetProduct(li.ProductId) : null;
                        var categoryName = product?.CategoryId != null
                            ? data.GetCategory(product.CategoryId)?.Name ?? "Other"
                            : "Other";
                        categoryTotals.TryAdd(categoryName, 0);
                        categoryTotals[categoryName] += li.Subtotal / lineItemsTotal * subtotalUSD;
                    }
                }
                else
                {
                    categoryTotals.TryAdd("Other", 0);
                    categoryTotals["Other"] += subtotalUSD;
                }
            }
            else
            {
                categoryTotals.TryAdd("Other", 0);
                categoryTotals["Other"] += expense.EffectiveSubtotalUSD;
            }
        }

        if (categoryTotals.Count == 0)
        {
            Series = [];
            HasData = false;
            return;
        }

        var sorted = categoryTotals
            .OrderByDescending(kvp => kvp.Value)
            .Take(8)
            .ToList();

        var isDonut = ChartStyle == "donut";
        var series = new ObservableCollection<ISeries>();

        for (int i = 0; i < sorted.Count; i++)
        {
            var kvp = sorted[i];
            var colorHex = AppColors.Palette[i % AppColors.Palette.Length];
            var roundedValue = (double)Math.Round(kvp.Value, 2);

            series.Add(new PieSeries<double>
            {
                Values = [roundedValue],
                Name = TruncateLabel(kvp.Key),
                Fill = new SolidColorPaint(SKColor.Parse(colorHex)),
                InnerRadius = isDonut ? 50 : 0,
                Pushout = 0,
                ToolTipLabelFormatter = point =>
                    CurrencyService.FormatFromUSD((decimal)point.Coordinate.PrimaryValue, DateTime.Now)
            });
        }

        Series = series;
        HasData = true;
    }

    private static string TruncateLabel(string? label)
    {
        if (string.IsNullOrEmpty(label)) return "Unknown";
        return label.Length > 18 ? label[..17] + "\u2026" : label;
    }
}
