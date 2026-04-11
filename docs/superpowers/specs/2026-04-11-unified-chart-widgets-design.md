# Unified Chart Widgets Design

## Goal

Replace the 3 hardcoded chart widget classes with one unified chart widget system that supports all ~50 `ChartDataType` values (except `WorldMap`). Data loading uses `ReportChartDataService.GetChartData()` — no duplication between dashboard and report generator.

## Architecture

```
ChartDataType enum (already exists, ~50 values)
        │
        ▼
ReportChartDataService.GetChartData(type)  ← single source of truth
        │
        ▼
  List<ChartDataPoint> or List<ChartSeriesData>
        │
        ▼
UnifiedChartWidgetViewModel  ← one VM for ALL chart widgets
        │
  converts to LiveCharts ISeries
        │
        ▼
UnifiedChartWidget.axaml  ← one view: CartesianChart OR PieChart
```

## Components

### 1. WidgetType.Chart enum value

Add `Chart = 20` to `WidgetType`. The specific chart type is stored in `DashboardWidgetEntry.Config["ChartDataType"]`.

Old entries (`ProfitsChart`, `RevenueVsExpensesChart`, `ExpenseByCategory`) remain for JSON backward compat. `WidgetFactory` maps them to the new system internally.

### 2. ChartDataType classification extensions

Add to `ReportEnumExtensions`:

- `IsDistribution(this ChartDataType)` — returns `true` for pie/donut chart types (e.g., `RevenueDistribution`, `ExpensesDistribution`, `ReturnReasons`, `ReturnsByCategory`, `LossesByCategory`, `TaxByCategory`, `TaxRateDistribution`, `CustomerPaymentStatus`, `ActiveVsInactiveCustomers`, `TopCustomersByRevenue`, `RentalsPerCustomer`, etc.)
- `IsMultiSeries(this ChartDataType)` — returns `true` for multi-series types (e.g., `RevenueVsExpenses`, `AverageTransactionValue`, `TotalTransactions`, `ExpenseVsRevenueReturns`, `ExpenseVsRevenueLosses`, `TaxCollectedVsPaid`, `ExpenseVsRevenueTax`)
- `GetChartCategory(this ChartDataType)` — returns catalog category string: "Revenue", "Expenses", "Financial", "Transactions", "Geographic", "Customer", "Returns", "Losses", "Taxes"
- `GetChartIcon(this ChartDataType)` — returns emoji icon per category

### 3. UnifiedChartWidgetViewModel

Single VM class replacing `ChartWidgetViewModel` and `ExpenseByCategoryWidgetViewModel`.

**Constructor:** Takes `ChartDataType`.

**Properties:**
- `ChartDataType ChartDataType` — which chart to render
- `bool IsDistribution` — drives view switching (CartesianChart vs PieChart)
- `ObservableCollection<ISeries> Series` — LiveCharts series
- `Axis[] XAxes, YAxes` — for cartesian charts
- `bool HasData` — controls empty state
- `string ChartTitle` — from `ChartDataType.GetDisplayName()`

**LoadData():**
1. Get `CompanyData` from `CompanyManager`
2. Get date range from `ChartSettingsService.Instance`
3. Create `ReportChartDataService(companyData, filters)` with a `ReportFilters` built from the chart settings date range
4. Call `GetChartData(ChartDataType)` — returns `object` (either `List<ChartDataPoint>` or `List<ChartSeriesData>`)
5. Convert to LiveCharts ISeries:
   - `List<ChartDataPoint>` + distribution → `PieSeries<double>` per point (with `AppColors.Palette`)
   - `List<ChartDataPoint>` + time-series → single `LineSeries<double>` (or Column/Area/etc. based on `ChartSettingsService.SelectedChartType`)
   - `List<ChartSeriesData>` → one `LineSeries<double>` per series
6. Set axes via `ChartLoaderService.CreateDateXAxes()` / `CreateCurrencyYAxes()` (reuse existing axis helpers)

**Config:** `HasConfig => true` for distribution charts (pie/donut toggle). Uses existing `GetConfig()`/`ApplyConfig()` pattern with `Config["ChartStyle"]`.

### 4. Unified chart widget view

One AXAML file with both chart controls, toggled by `IsDistribution`:

```xml
<CartesianChart IsVisible="{Binding !IsDistribution}" Series="{Binding Series}" ... />
<PieChart IsVisible="{Binding IsDistribution}" Series="{Binding Series}" ... />
```

Reuses existing styles from `ChartWidget.axaml` and `ExpenseByCategoryWidget.axaml`.

### 5. WidgetFactory changes

**Auto-generated definitions:** Iterate all `ChartDataType` values (skip `WorldMap`). For each, produce a `WidgetDefinition` with:
- `Type = WidgetType.Chart`
- `Name = chartDataType.GetDisplayName()`
- `Description` = short description derived from category
- `Category = chartDataType.GetChartCategory()`
- `Icon = chartDataType.GetChartIcon()`
- `DefaultSize = WidgetSize.Medium`
- `AvailableSizes = [Small, Medium, Large]`
- `ChartDataType = chartDataType` (new field on WidgetDefinition)

**Backward compat mapping:** `CreateWidgetHost` maps old types:
- `WidgetType.ProfitsChart` → `ChartDataType.TotalProfits`
- `WidgetType.RevenueVsExpensesChart` → `ChartDataType.RevenueVsExpenses`
- `WidgetType.ExpenseByCategory` → `ChartDataType.ExpensesDistribution`

**WidgetDefinition record:** Add optional `ChartDataType? ChartDataType` field.

### 6. Catalog changes

**WidgetAddRequested event** — Change from `EventHandler<WidgetType>` to `EventHandler<WidgetDefinition>`. The handler reads `definition.ChartDataType` to populate `Config["ChartDataType"]`.

**Duplicate detection** — `IsAlreadyAdded` checks: for chart widgets, match on `ChartDataType` (from config); for non-chart widgets, match on `WidgetType` as before.

**Category tabs** — The catalog currently has 3 tabs: "Stat Cards", "Charts", "Tables". Chart definitions get sorted into the "Charts" tab. With ~50 chart types this tab will be long, which is fine — the catalog already scrolls.

### 7. Files deleted

- `ArgoBooks/ViewModels/Dashboard/ChartWidgetViewModel.cs` (and `ChartWidgetKind` enum)
- `ArgoBooks/ViewModels/Dashboard/ExpenseByCategoryWidgetViewModel.cs`
- `ArgoBooks/Controls/Dashboard/Widgets/ExpenseByCategoryWidget.axaml` + `.axaml.cs`

### 8. Files modified

- `ArgoBooks.Core/Models/Dashboard/WidgetType.cs` — add `Chart = 20`
- `ArgoBooks.Core/Enums/ReportEnums.cs` — add `IsDistribution()`, `IsMultiSeries()`, `GetChartCategory()`, `GetChartIcon()` extensions
- `ArgoBooks/Services/WidgetFactory.cs` — auto-generate chart definitions, backward compat mapping, update `WidgetDefinition` record
- `ArgoBooks/ViewModels/Dashboard/WidgetCatalogViewModel.cs` — change event type, update duplicate detection
- `ArgoBooks/ViewModels/Dashboard/DashboardLayoutViewModel.cs` — update `OnWidgetAddRequested` to handle `WidgetDefinition`
- `ArgoBooks/Controls/Dashboard/Widgets/ChartWidget.axaml` + `.axaml.cs` — rewrite as unified view

### 9. Files created

- `ArgoBooks/ViewModels/Dashboard/UnifiedChartWidgetViewModel.cs`
