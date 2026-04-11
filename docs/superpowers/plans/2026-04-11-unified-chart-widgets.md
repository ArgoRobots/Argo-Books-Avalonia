# Unified Chart Widgets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the 3 hardcoded chart widget classes with one unified chart widget that supports all ~50 `ChartDataType` values, using `ReportChartDataService` as the single data source.

**Architecture:** Add `WidgetType.Chart` and store the specific `ChartDataType` in widget config. One `UnifiedChartWidgetViewModel` loads data via `ReportChartDataService.GetChartData()` and converts the result to LiveCharts series. One unified AXAML view shows either CartesianChart or PieChart. `WidgetFactory` auto-generates catalog definitions by iterating `ChartDataType` enum values. Old widget types are mapped to the new system for backward compatibility.

**Tech Stack:** C# .NET 10, Avalonia UI, CommunityToolkit.Mvvm, LiveChartsCore, SkiaSharp

---

### File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `ArgoBooks.Core/Models/Dashboard/WidgetType.cs` | Modify | Add `Chart = 20` |
| `ArgoBooks.Core/Enums/ReportEnums.cs` | Modify | Add `IsDistribution()`, `IsMultiSeries()`, `GetChartCategory()`, `GetChartIcon()` extensions |
| `ArgoBooks/Services/WidgetFactory.cs` | Modify | Auto-generate chart definitions, backward compat mapping, add `ChartDataType?` to `WidgetDefinition` |
| `ArgoBooks/ViewModels/Dashboard/UnifiedChartWidgetViewModel.cs` | Create | Single VM for all chart widgets |
| `ArgoBooks/Controls/Dashboard/Widgets/ChartWidget.axaml` | Modify | Unified view with CartesianChart + PieChart |
| `ArgoBooks/ViewModels/Dashboard/WidgetCatalogViewModel.cs` | Modify | Change event to pass `WidgetDefinition`, update duplicate detection |
| `ArgoBooks/ViewModels/Dashboard/DashboardLayoutViewModel.cs` | Modify | Update `OnWidgetAddRequested` handler |
| `ArgoBooks/ViewModels/Dashboard/ChartWidgetViewModel.cs` | Delete | Replaced by UnifiedChartWidgetViewModel |
| `ArgoBooks/ViewModels/Dashboard/ExpenseByCategoryWidgetViewModel.cs` | Delete | Replaced by UnifiedChartWidgetViewModel |
| `ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml` | Delete | Replaced by unified ChartWidget |
| `ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml.cs` | Delete | Replaced by unified ChartWidget |

---

### Task 1: Add ChartDataType classification extensions

**Files:**
- Modify: `ArgoBooks.Core/Enums/ReportEnums.cs`

- [ ] **Step 1: Add `IsDistribution` extension method**

In `ReportEnumExtensions` class, add:

```csharp
public static bool IsDistribution(this ChartDataType chartType) => chartType switch
{
    ChartDataType.RevenueDistribution => true,
    ChartDataType.ExpensesDistribution => true,
    ChartDataType.CountriesOfOrigin => true,
    ChartDataType.CountriesOfDestination => true,
    ChartDataType.CompaniesOfOrigin => true,
    ChartDataType.CompaniesOfDestination => true,
    ChartDataType.AccountantsTransactions => true,
    ChartDataType.TopCustomersByRevenue => true,
    ChartDataType.CustomerPaymentStatus => true,
    ChartDataType.ActiveVsInactiveCustomers => true,
    ChartDataType.RentalsPerCustomer => true,
    ChartDataType.ReturnReasons => true,
    ChartDataType.ReturnsByCategory => true,
    ChartDataType.ReturnsByProduct => true,
    ChartDataType.LossReasons => true,
    ChartDataType.LossesByCategory => true,
    ChartDataType.LossesByProduct => true,
    ChartDataType.TaxByCategory => true,
    ChartDataType.TaxRateDistribution => true,
    ChartDataType.TaxByProduct => true,
    _ => false
};
```

- [ ] **Step 2: Add `IsMultiSeries` extension method**

```csharp
public static bool IsMultiSeries(this ChartDataType chartType) => chartType switch
{
    ChartDataType.RevenueVsExpenses => true,
    ChartDataType.AverageTransactionValue => true,
    ChartDataType.TotalTransactions => true,
    ChartDataType.ExpenseVsRevenueReturns => true,
    ChartDataType.ExpenseVsRevenueLosses => true,
    ChartDataType.TaxCollectedVsPaid => true,
    ChartDataType.ExpenseVsRevenueTax => true,
    _ => false
};
```

- [ ] **Step 3: Add `GetChartCategory` extension method**

```csharp
public static string GetChartCategory(this ChartDataType chartType) => chartType switch
{
    ChartDataType.TotalRevenue or ChartDataType.RevenueDistribution => "Revenue",
    ChartDataType.TotalExpenses or ChartDataType.ExpensesDistribution => "Expenses",
    ChartDataType.TotalProfits or ChartDataType.RevenueVsExpenses => "Financial",
    ChartDataType.AverageTransactionValue or ChartDataType.TotalTransactions
        or ChartDataType.AverageShippingCosts => "Transactions",
    ChartDataType.CountriesOfOrigin or ChartDataType.CountriesOfDestination
        or ChartDataType.CompaniesOfOrigin or ChartDataType.CompaniesOfDestination => "Geographic",
    ChartDataType.AccountantsTransactions => "Accountant",
    ChartDataType.TopCustomersByRevenue or ChartDataType.CustomerPaymentStatus
        or ChartDataType.CustomerGrowth or ChartDataType.CustomerLifetimeValue
        or ChartDataType.ActiveVsInactiveCustomers or ChartDataType.RentalsPerCustomer => "Customer",
    ChartDataType.ReturnsOverTime or ChartDataType.ReturnReasons
        or ChartDataType.ReturnFinancialImpact or ChartDataType.ReturnsByCategory
        or ChartDataType.ReturnsByProduct or ChartDataType.ExpenseVsRevenueReturns => "Returns",
    ChartDataType.LossesOverTime or ChartDataType.LossReasons
        or ChartDataType.LossFinancialImpact or ChartDataType.LossesByCategory
        or ChartDataType.LossesByProduct or ChartDataType.ExpenseVsRevenueLosses => "Losses",
    ChartDataType.TaxCollectedVsPaid or ChartDataType.TaxLiabilityTrend
        or ChartDataType.TaxByCategory or ChartDataType.TaxRateDistribution
        or ChartDataType.TaxByProduct or ChartDataType.ExpenseVsRevenueTax => "Taxes",
    _ => "Charts"
};
```

- [ ] **Step 4: Add `GetChartIcon` extension method**

```csharp
public static string GetChartIcon(this ChartDataType chartType)
{
    var category = chartType.GetChartCategory();
    return category switch
    {
        "Revenue" => "💰",
        "Expenses" => "📉",
        "Financial" => "📊",
        "Transactions" => "📝",
        "Geographic" => "🌍",
        "Accountant" => "🧾",
        "Customer" => "👥",
        "Returns" => "↩️",
        "Losses" => "⚠️",
        "Taxes" => "🏛️",
        _ => "📈"
    };
}
```

- [ ] **Step 5: Commit**

```bash
git add ArgoBooks.Core/Enums/ReportEnums.cs
git commit -m "feat: add ChartDataType classification extensions"
```

---

### Task 2: Add WidgetType.Chart and update WidgetDefinition

**Files:**
- Modify: `ArgoBooks.Core/Models/Dashboard/WidgetType.cs`
- Modify: `ArgoBooks/Services/WidgetFactory.cs`

- [ ] **Step 1: Add Chart to WidgetType enum**

In `ArgoBooks.Core/Models/Dashboard/WidgetType.cs`, add after `OverdueRentals = 19`:

```csharp
Chart = 20
```

- [ ] **Step 2: Add ChartDataType field to WidgetDefinition**

In `ArgoBooks/Services/WidgetFactory.cs`, change the `WidgetDefinition` record (line 8-16) from:

```csharp
public record WidgetDefinition(
    WidgetType Type,
    string Name,
    string Description,
    string Category,
    string Icon,
    WidgetSize DefaultSize,
    WidgetSize[] AvailableSizes
);
```

to:

```csharp
public record WidgetDefinition(
    WidgetType Type,
    string Name,
    string Description,
    string Category,
    string Icon,
    WidgetSize DefaultSize,
    WidgetSize[] AvailableSizes,
    ChartDataType? ChartDataType = null
);
```

Add `using ArgoBooks.Core.Enums;` to the file's usings.

- [ ] **Step 3: Commit**

```bash
git add ArgoBooks.Core/Models/Dashboard/WidgetType.cs ArgoBooks/Services/WidgetFactory.cs
git commit -m "feat: add WidgetType.Chart and ChartDataType to WidgetDefinition"
```

---

### Task 3: Create UnifiedChartWidgetViewModel

**Files:**
- Create: `ArgoBooks/ViewModels/Dashboard/UnifiedChartWidgetViewModel.cs`

- [ ] **Step 1: Create the unified VM**

```csharp
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

    // Config for distribution charts (pie vs donut)
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
        ChartLoaderService.SelectedChartStyle = chartSettings.SelectedChartType switch
        {
            "Line" => ChartStyle.Line,
            "Column" => ChartStyle.Column,
            "Step Line" => ChartStyle.StepLine,
            "Area" => ChartStyle.Area,
            "Scatter" => ChartStyle.Scatter,
            _ => ChartStyle.Line
        };

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
                Values = [(double)Math.Round(point.Value, 2)],
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
            .SelectMany(s => s.Points.Where(p => p.Date.HasValue).Select(p => p.Date!.Value))
            .Distinct().OrderBy(d => d).ToArray();

        var series = new ObservableCollection<ISeries>();
        for (int i = 0; i < seriesData.Count; i++)
        {
            var sd = seriesData[i];
            var values = allDates.Select(date =>
                (double)(sd.Points.FirstOrDefault(p => p.Date == date)?.Value ?? 0m)).ToArray();

            var colorHex = sd.Color ?? AppColors.Palette[i % AppColors.Palette.Length];
            series.Add(ChartLoaderService.CreateSeries(values, sd.Name, colorHex));
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
        var values = points.Select(p => (double)p.Value).ToArray();

        var series = new ObservableCollection<ISeries>();
        series.Add(ChartLoaderService.CreateSeries(values, ChartDataType.GetDisplayName(),
            AppColors.Palette[0]));

        XAxes = ChartLoaderService.CreateDateXAxes(dates);
        YAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        Series = series;
        HasData = dates.Length > 0;
    }

    private static string TruncateLabel(string? label)
    {
        if (string.IsNullOrEmpty(label)) return "Unknown";
        return label.Length > 18 ? label[..17] + "\u2026" : label;
    }
}
```

**Important:** This VM uses `ChartLoaderService.CreateSeries()` for building LiveCharts series with the right style (Line/Column/Area/etc.). Check that this method exists — if not, the implementer should look at how `ChartLoaderService.LoadProfitsOverviewChart` creates series internally and extract/reuse that pattern. The existing `ChartLoaderService` methods like `CreateDateXAxes()`, `CreateCurrencyYAxes()`, and `CreateChartTitle()` are public utilities that remain useful.

- [ ] **Step 2: Commit**

```bash
git add ArgoBooks/ViewModels/Dashboard/UnifiedChartWidgetViewModel.cs
git commit -m "feat: create UnifiedChartWidgetViewModel"
```

---

### Task 4: Rewrite ChartWidget.axaml as unified view

**Files:**
- Modify: `ArgoBooks/Controls/Dashboard/Widgets/ChartWidget.axaml`
- Modify: `ArgoBooks/Controls/Dashboard/Widgets/ChartWidget.axaml.cs`

- [ ] **Step 1: Rewrite ChartWidget.axaml**

Replace the entire file with:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ArgoBooks.ViewModels.Dashboard"
             xmlns:lvc="using:LiveChartsCore.SkiaSharpView.Avalonia"
             xmlns:controls="using:ArgoBooks.Controls"
             x:Class="ArgoBooks.Controls.Dashboard.Widgets.ChartWidget"
             x:DataType="vm:UnifiedChartWidgetViewModel">

    <Border Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource BorderBrush}"
            BorderThickness="1"
            CornerRadius="12"
            ClipToBounds="True"
            MinHeight="320">
        <Grid>
            <!-- Cartesian chart (time-series, bar, etc.) -->
            <lvc:CartesianChart
                Title="{Binding ChartTitleVisual}"
                Series="{Binding Series}"
                XAxes="{Binding XAxes}"
                YAxes="{Binding YAxes}"
                TooltipPosition="Top"
                ZoomMode="Both"
                IsVisible="{Binding HasData}"
                IsVisible.1="{Binding !IsDistribution}" />

            <!-- Pie/donut chart (distribution) -->
            <Grid RowDefinitions="Auto,*"
                  IsVisible="{Binding IsDistribution}">
                <Border Grid.Row="0"
                        Padding="20,16"
                        BorderBrush="{DynamicResource BorderBrush}"
                        BorderThickness="0,0,0,1">
                    <TextBlock Text="{Binding ChartTitle}"
                               FontSize="16"
                               FontWeight="SemiBold"
                               Foreground="{DynamicResource TextPrimaryBrush}" />
                </Border>
                <lvc:PieChart Grid.Row="1"
                              Series="{Binding Series}"
                              LegendPosition="Right"
                              TooltipPosition="Top"
                              IsVisible="{Binding HasData}"
                              MinHeight="250" />
            </Grid>

            <!-- Empty state -->
            <controls:ChartEmptyState
                IsVisible="{Binding HasNoData}"
                DefaultMessage="{Binding EmptyStateMessage}" />
        </Grid>
    </Border>
</UserControl>
```

**Note:** The `IsVisible.1` pseudo-syntax doesn't exist in Avalonia. The implementer needs to combine the two visibility conditions. The simplest approach: wrap the CartesianChart in a Panel that checks `!IsDistribution`, and let the `HasData` binding stay on the CartesianChart itself. Alternatively, add a computed `ShowCartesian` property to the VM (`HasData && !IsDistribution`). Use whatever approach is cleanest.

- [ ] **Step 2: Update ChartWidget.axaml.cs DataType**

The code-behind file only has `InitializeComponent()` — no changes needed to the C# unless the namespace changes. Verify the `x:Class` matches.

- [ ] **Step 3: Commit**

```bash
git add ArgoBooks/Controls/Dashboard/Widgets/ChartWidget.axaml ArgoBooks/Controls/Dashboard/Widgets/ChartWidget.axaml.cs
git commit -m "feat: rewrite ChartWidget as unified cartesian+pie view"
```

---

### Task 5: Update WidgetFactory — auto-generate chart definitions and backward compat

**Files:**
- Modify: `ArgoBooks/Services/WidgetFactory.cs`

- [ ] **Step 1: Remove old chart entries from Definitions dictionary**

Remove these three entries from the `Definitions` dictionary:

```csharp
[WidgetType.ProfitsChart] = new(...),
[WidgetType.RevenueVsExpensesChart] = new(...),
[WidgetType.ExpenseByCategory] = new(...),
```

- [ ] **Step 2: Add chart definition auto-generation**

Add a static method and modify `GetAllDefinitions()`:

```csharp
private static readonly Dictionary<ChartDataType, WidgetDefinition> ChartDefinitions = BuildChartDefinitions();

private static Dictionary<ChartDataType, WidgetDefinition> BuildChartDefinitions()
{
    var defs = new Dictionary<ChartDataType, WidgetDefinition>();
    foreach (var type in Enum.GetValues<ChartDataType>())
    {
        if (type == ChartDataType.WorldMap) continue;
        defs[type] = new WidgetDefinition(
            WidgetType.Chart,
            type.GetDisplayName(),
            $"{type.GetChartCategory()} chart",
            type.IsDistribution() ? "Charts" : "Charts",
            type.GetChartIcon(),
            WidgetSize.Medium,
            [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large],
            type);
    }
    return defs;
}
```

Update `GetAllDefinitions()` to include chart definitions:

```csharp
public static IReadOnlyList<WidgetDefinition> GetAllDefinitions()
    => Definitions.Values.Concat(ChartDefinitions.Values).ToList();
```

- [ ] **Step 3: Add backward compat mapping for old widget types**

Add a helper that maps old WidgetType entries to ChartDataType:

```csharp
private static ChartDataType? MapLegacyChartType(WidgetType type) => type switch
{
    WidgetType.ProfitsChart => ChartDataType.TotalProfits,
    WidgetType.RevenueVsExpensesChart => ChartDataType.RevenueVsExpenses,
    WidgetType.ExpenseByCategory => ChartDataType.ExpensesDistribution,
    _ => null
};
```

- [ ] **Step 4: Update CreateWidgetHost to handle Chart type and legacy mapping**

Modify `CreateWidgetHost`:

```csharp
public static WidgetHostViewModel CreateWidgetHost(DashboardWidgetEntry entry)
{
    // Handle legacy chart types
    var legacyChart = MapLegacyChartType(entry.WidgetType);
    if (legacyChart.HasValue)
    {
        entry.Config.TryAdd("ChartDataType", legacyChart.Value.ToString());
    }

    if (entry.WidgetType == WidgetType.Chart || legacyChart.HasValue)
    {
        var chartDataType = ChartDataType.TotalProfits; // default
        if (entry.Config.TryGetValue("ChartDataType", out var typeStr)
            && Enum.TryParse<ChartDataType>(typeStr, out var parsed))
            chartDataType = parsed;

        var def = ChartDefinitions.TryGetValue(chartDataType, out var chartDef)
            ? chartDef
            : ChartDefinitions.Values.First();

        var viewModel = new UnifiedChartWidgetViewModel(chartDataType);
        return new WidgetHostViewModel(entry, viewModel, def.AvailableSizes);
    }

    var definition = GetDefinition(entry.WidgetType);
    var vm = CreateViewModel(entry.WidgetType);
    return new WidgetHostViewModel(entry, vm, definition.AvailableSizes);
}
```

- [ ] **Step 5: Update CreateViewModel — remove old chart cases**

Remove these cases from `CreateViewModel`:

```csharp
WidgetType.ProfitsChart => new ChartWidgetViewModel(ChartWidgetKind.Profits),
WidgetType.RevenueVsExpensesChart => new ChartWidgetViewModel(ChartWidgetKind.RevenueVsExpenses),
WidgetType.ExpenseByCategory => new ExpenseByCategoryWidgetViewModel(),
```

Replace with a single fallback or remove entirely (since `CreateWidgetHost` handles chart types before calling `CreateViewModel`). The implementer should make sure the remaining cases still compile.

- [ ] **Step 6: Update CreateView — remove old chart cases**

Remove these cases from `CreateView`:

```csharp
WidgetType.ProfitsChart or WidgetType.RevenueVsExpensesChart => new ChartWidget(),
WidgetType.ExpenseByCategory => new ExpenseByCategoryWidget(),
```

Add a case for `WidgetType.Chart`:

```csharp
WidgetType.Chart => new ChartWidget(),
```

Keep the legacy types mapping to `new ChartWidget()` too for safety:

```csharp
WidgetType.ProfitsChart or WidgetType.RevenueVsExpensesChart
    or WidgetType.ExpenseByCategory or WidgetType.Chart => new ChartWidget(),
```

- [ ] **Step 7: Update IsKnownType to accept Chart**

Check that `IsKnownType` handles `WidgetType.Chart`. Currently it checks `Definitions.ContainsKey(type)`. Since chart definitions are in a separate dictionary, update:

```csharp
public static bool IsKnownType(WidgetType type)
    => Definitions.ContainsKey(type) || type == WidgetType.Chart;
```

- [ ] **Step 8: Update GetDefinition to handle Chart type**

`GetDefinition` is called in some places. Add a safe fallback:

```csharp
public static WidgetDefinition GetDefinition(WidgetType type)
    => Definitions.TryGetValue(type, out var def) ? def : ChartDefinitions.Values.First();
```

- [ ] **Step 9: Commit**

```bash
git add ArgoBooks/Services/WidgetFactory.cs
git commit -m "feat: auto-generate chart definitions in WidgetFactory"
```

---

### Task 6: Update catalog and layout VM for WidgetDefinition-based events

**Files:**
- Modify: `ArgoBooks/ViewModels/Dashboard/WidgetCatalogViewModel.cs`
- Modify: `ArgoBooks/ViewModels/Dashboard/DashboardLayoutViewModel.cs`

- [ ] **Step 1: Change WidgetAddRequested event type**

In `WidgetCatalogViewModel`, change:

```csharp
public event EventHandler<WidgetType>? WidgetAddRequested;
```

to:

```csharp
public event EventHandler<WidgetDefinition>? WidgetAddRequested;
```

And update `AddWidget`:

```csharp
[RelayCommand]
private void AddWidget(CatalogItem item)
{
    if (item.IsAlreadyAdded || item.CannotFitInRow) return;
    WidgetAddRequested?.Invoke(this, item.Definition);
    IsOpen = false;
}
```

- [ ] **Step 2: Update duplicate detection in Refresh**

Change the `IsAlreadyAdded` logic to handle chart widgets by comparing `ChartDataType` from config:

```csharp
var placedTypes = currentWidgets.Select(w => w.WidgetType).ToHashSet();
var placedChartTypes = currentWidgets
    .Where(w => w.WidgetType == WidgetType.Chart ||
                w.WidgetType == WidgetType.ProfitsChart ||
                w.WidgetType == WidgetType.RevenueVsExpensesChart ||
                w.WidgetType == WidgetType.ExpenseByCategory)
    .Select(w => w.WidgetViewModel is UnifiedChartWidgetViewModel ucvm ? ucvm.ChartDataType : (ChartDataType?)null)
    .Where(t => t.HasValue)
    .Select(t => t!.Value)
    .ToHashSet();
```

Then in the item creation loop:

```csharp
var item = new CatalogItem(d)
{
    IsAlreadyAdded = d.ChartDataType.HasValue
        ? placedChartTypes.Contains(d.ChartDataType.Value)
        : placedTypes.Contains(d.Type),
    CannotFitInRow = d.DefaultSize.ToFraction() > remainingFraction + 0.001
};
```

- [ ] **Step 3: Update OnWidgetAddRequested in DashboardLayoutViewModel**

Change the handler signature from `WidgetType type` to `WidgetDefinition def`:

```csharp
private void OnWidgetAddRequested(object? sender, WidgetDefinition def)
{
    var targetRow = _targetRowForAdd ?? Rows.LastOrDefault();
    if (targetRow == null)
    {
        targetRow = new DashboardRowViewModel { IsEditMode = true };
        Rows.Add(targetRow);
    }

    var entry = new DashboardWidgetEntry(def.Type, def.DefaultSize);
    if (def.ChartDataType.HasValue)
        entry.Config["ChartDataType"] = def.ChartDataType.Value.ToString();

    if (!targetRow.CanFit(entry.Size))
    {
        _targetRowForAdd = null;
        return;
    }

    var host = WidgetFactory.CreateWidgetHost(entry);
    host.SetCompanyManager(_companyManager);
    WireUpWidgetEvents(host);
    host.IsEditMode = true;
    host.LoadData();
    targetRow.Widgets.Add(host);
    Catalog.Refresh(Rows.SelectMany(r => r.Widgets));
    _targetRowForAdd = null;
    OnPropertyChanged(nameof(HasWidgets));
}
```

- [ ] **Step 4: Update the event subscription type**

In `Initialize`, the subscription `Catalog.WidgetAddRequested += OnWidgetAddRequested;` should still work since we changed both the event and handler signatures. Verify it compiles.

- [ ] **Step 5: Commit**

```bash
git add ArgoBooks/ViewModels/Dashboard/WidgetCatalogViewModel.cs ArgoBooks/ViewModels/Dashboard/DashboardLayoutViewModel.cs
git commit -m "feat: update catalog to pass WidgetDefinition and detect chart duplicates"
```

---

### Task 7: Delete old chart widget files and update default layout

**Files:**
- Delete: `ArgoBooks/ViewModels/Dashboard/ChartWidgetViewModel.cs`
- Delete: `ArgoBooks/ViewModels/Dashboard/ExpenseByCategoryWidgetViewModel.cs`
- Delete: `ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml`
- Delete: `ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml.cs`
- Modify: `ArgoBooks.Core/Models/Dashboard/DashboardLayout.cs`

- [ ] **Step 1: Delete old VM and view files**

```bash
rm ArgoBooks/ViewModels/Dashboard/ChartWidgetViewModel.cs
rm ArgoBooks/ViewModels/Dashboard/ExpenseByCategoryWidgetViewModel.cs
rm ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml
rm ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml.cs
```

- [ ] **Step 2: Update CreateDefault in DashboardLayout.cs**

Change the default layout to use `WidgetType.Chart` with config for the chart rows. The current default has:

```csharp
new DashboardRow(
    new DashboardWidgetEntry(WidgetType.ProfitsChart, WidgetSize.Medium),
    new DashboardWidgetEntry(WidgetType.RevenueVsExpensesChart, WidgetSize.Medium)),
```

Replace with:

```csharp
new DashboardRow(
    new DashboardWidgetEntry(WidgetType.Chart, WidgetSize.Medium)
        { Config = new() { ["ChartDataType"] = "TotalProfits" } },
    new DashboardWidgetEntry(WidgetType.Chart, WidgetSize.Medium)
        { Config = new() { ["ChartDataType"] = "RevenueVsExpenses" } }),
```

Also update the `ExpenseByCategory` entry in the default if it exists (check the current default — it may not be there).

- [ ] **Step 3: Fix any remaining compile errors**

Search for any remaining references to:
- `ChartWidgetViewModel`
- `ChartWidgetKind`
- `ExpenseByCategoryWidgetViewModel`
- `ExpenseByCategoryWidget`

Fix or remove them. Common places: `WidgetFactory.CreateViewModel`, `WidgetFactory.CreateView`, any `using` statements.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: delete old chart widgets, update default layout to use WidgetType.Chart"
```

---

### Task 8: Verify and fix compilation

**Files:**
- Various — fix any compilation issues from the refactor

- [ ] **Step 1: Build the solution**

```bash
dotnet build ArgoBooks.sln
```

Fix any errors. Common issues:
- Missing `using` statements for `ChartDataType`, `ReportFilters`, `ReportChartDataService`
- `ChartLoaderService.CreateSeries()` may not exist — check and adapt. If it doesn't exist, look at how `LoadProfitsOverviewChart` creates series internally and either make a public `CreateSeries` method or inline the series creation in the unified VM.
- The `WidgetCatalogViewModel` needs `using ArgoBooks.Core.Enums;` and `using ArgoBooks.Core.Models.Dashboard;`
- `DashboardLayoutViewModel` may need `using ArgoBooks.Services;` for `WidgetDefinition`

- [ ] **Step 2: Fix any remaining issues and commit**

```bash
git add -A
git commit -m "fix: resolve compilation errors from chart widget unification"
```
