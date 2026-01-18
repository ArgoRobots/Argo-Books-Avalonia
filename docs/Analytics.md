# Analytics & Reporting

The Analytics module provides data visualization, AI-powered insights, forecasting, and custom report generation.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Analytics Architecture (Analytics.svg)                         │
│                                                                 │
│                    ┌─────────────┐                              │
│                    │ CompanyData │                              │
│                    └──────┬──────┘                              │
│                           │                                     │
│       ┌───────────────────┼───────────────────┐                │
│       ▼                   ▼                   ▼                 │
│  ┌─────────┐      ┌────────────┐      ┌─────────────┐          │
│  │Dashboard│      │ Analytics  │      │   Reports   │          │
│  │  (KPIs) │      │  (Charts)  │      │  (Custom)   │          │
│  └─────────┘      └────────────┘      └─────────────┘          │
│       │                   │                   │                 │
│       │           ┌───────┴───────┐           │                 │
│       │           ▼               ▼           │                 │
│       │    ┌──────────┐   ┌────────────┐      │                 │
│       │    │ Insights │   │ Forecasting│      │                 │
│       │    │   (AI)   │   │    (ML)    │      │                 │
│       │    └──────────┘   └────────────┘      │                 │
│       │                                       │                 │
│       └───────────────────────────────────────┘                │
│                           │                                     │
│                           ▼                                     │
│                    ┌─────────────┐                              │
│                    │   Export    │                              │
│                    │ (PDF/Excel) │                              │
│                    └─────────────┘                              │
│                                                                 │
│  Show: Data flow from source to visualization                   │
│  Include: AI and ML integration points                          │
└─────────────────────────────────────────────────────────────────┘
```

## Dashboard

Real-time business metrics and KPI visualization.

| Component | Location |
|-----------|----------|
| ViewModel | `ArgoBooks/ViewModels/DashboardPageViewModel.cs` |
| View | `ArgoBooks/Views/DashboardPage.axaml` |
| Control | `ArgoBooks/Controls/StatCard.axaml` |

### Features
- Key metric stat cards
- Real-time data updates
- Visual dashboard overview
- Quick navigation to details

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Dashboard Layout (DashboardLayout.svg)                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  [Revenue]  [Expenses]  [Profit]  [Outstanding]         │   │
│  │   StatCard   StatCard   StatCard    StatCard            │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────┐  ┌───────────────────────────────┐   │
│  │                      │  │                               │   │
│  │    Revenue Chart     │  │      Expense Breakdown        │   │
│  │      (Line)          │  │         (Pie)                 │   │
│  │                      │  │                               │   │
│  └──────────────────────┘  └───────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Recent Transactions Table                   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Show: Dashboard component arrangement                          │
│  Include: StatCard grid, chart areas, data tables               │
└─────────────────────────────────────────────────────────────────┘
```

## Analytics

Interactive charts and data visualization.

| Component | Location |
|-----------|----------|
| ViewModel | `ArgoBooks/ViewModels/AnalyticsPageViewModel.cs` |
| View | `ArgoBooks/Views/AnalyticsPage.axaml` |
| Service | `ArgoBooks/Services/ChartLoaderService.cs` |

### Features
- Multiple chart types (Line, Bar, Pie, etc.)
- Interactive filtering
- Date range selection
- Trend analysis
- Comparison views

### Chart Types

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Chart Types (ChartTypes.svg)                                   │
│                                                                 │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐               │
│  │    Line    │  │    Bar     │  │    Pie     │               │
│  │   Chart    │  │   Chart    │  │   Chart    │               │
│  │  (Trends)  │  │(Comparison)│  │(Breakdown) │               │
│  └────────────┘  └────────────┘  └────────────┘               │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐               │
│  │   Stacked  │  │   Gauge    │  │  Cartesian │               │
│  │    Bar     │  │   Chart    │  │   (Combo)  │               │
│  │(Cumulative)│  │   (KPI)    │  │            │               │
│  └────────────┘  └────────────┘  └────────────┘               │
│                                                                 │
│  Show: Available chart types with use cases                     │
│  Include: LiveCharts2 chart components                          │
└─────────────────────────────────────────────────────────────────┘
```

## AI Insights

Intelligent business recommendations powered by AI.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/InsightsData.cs` |
| Service | `ArgoBooks.Core/Services/InsightsService.cs` |
| OpenAI | `ArgoBooks.Core/Services/OpenAiService.cs` |

### Features
- AI-generated business insights
- Anomaly detection
- Opportunity identification
- Risk flagging
- Actionable recommendations

### Insights Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Insights Pipeline (InsightsPipeline.svg)                       │
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │ Transaction │───▶│   Pattern   │───▶│   OpenAI    │         │
│  │    Data     │    │  Analysis   │    │   Service   │         │
│  └─────────────┘    └─────────────┘    └─────────────┘         │
│                                               │                 │
│                                               ▼                 │
│                                        ┌─────────────┐         │
│                                        │ InsightsData│         │
│                                        └─────────────┘         │
│                                               │                 │
│       ┌───────────────────────────────────────┼───────┐        │
│       ▼                   ▼                   ▼       ▼        │
│  ┌─────────┐       ┌─────────┐         ┌─────────┐ ┌─────┐    │
│  │Anomalies│       │ Trends  │         │ Advice  │ │Risks│    │
│  └─────────┘       └─────────┘         └─────────┘ └─────┘    │
│                                                                 │
│  Show: Data flow from raw data to actionable insights           │
│  Include: AI processing step                                    │
└─────────────────────────────────────────────────────────────────┘
```

## Forecasting

Predictive analytics for business planning.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/ForecastAccuracyData.cs` |
| Service | `ArgoBooks.Core/Services/Forecasting/HoltWintersForecasting.cs` |
| Service | `ArgoBooks.Core/Services/Forecasting/LocalMLForecastingService.cs` |
| Service | `ArgoBooks.Core/Services/Forecasting/ForecastAccuracyService.cs` |

### Features
- Sales trend forecasting
- Seasonal pattern detection
- Holt-Winters algorithm
- On-device ML predictions
- Forecast accuracy metrics

### Forecasting Methods

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Forecasting Methods (ForecastingMethods.svg)                   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                  Historical Data                         │   │
│  └────────────────────────┬────────────────────────────────┘   │
│                           │                                     │
│              ┌────────────┼────────────┐                       │
│              ▼            ▼            ▼                        │
│       ┌────────────┐ ┌─────────┐ ┌──────────────┐              │
│       │Holt-Winters│ │Local ML │ │   Seasonal   │              │
│       │ Algorithm  │ │ Service │ │Decomposition │              │
│       └────────────┘ └─────────┘ └──────────────┘              │
│              │            │            │                        │
│              └────────────┼────────────┘                       │
│                           ▼                                     │
│                    ┌─────────────┐                              │
│                    │  Forecast   │                              │
│                    │   Result    │                              │
│                    └─────────────┘                              │
│                           │                                     │
│                           ▼                                     │
│                    ┌─────────────┐                              │
│                    │  Accuracy   │                              │
│                    │  Tracking   │                              │
│                    └─────────────┘                              │
│                                                                 │
│  Show: Multiple forecasting approaches                          │
│  Include: Accuracy validation loop                              │
└─────────────────────────────────────────────────────────────────┘
```

## Reports

Custom report builder with templates.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Reports/ReportConfiguration.cs` |
| Model | `ArgoBooks.Core/Models/Reports/ReportTemplate.cs` |
| Model | `ArgoBooks.Core/Models/Reports/ReportElement.cs` |
| ViewModel | `ArgoBooks/ViewModels/ReportsPageViewModel.cs` |
| Service | `ArgoBooks.Core/Services/Reports/ReportRenderer.cs` |
| Service | `ArgoBooks.Core/Services/Reports/ReportTemplateStorage.cs` |
| Service | `ArgoBooks.Core/Services/Reports/ReportChartDataService.cs` |
| Service | `ArgoBooks.Core/Services/Reports/ReportTableDataService.cs` |

### Features
- Custom report builder
- Pre-built report templates
- Drag-and-drop elements
- Data visualization in reports
- PDF export
- Undo/redo for report editing

### Report Builder Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Report Builder Architecture (ReportBuilder.svg)                │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                 ReportConfiguration                      │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │              ReportTemplate                      │    │   │
│  │  │  ┌───────────────────────────────────────────┐  │    │   │
│  │  │  │           ReportElement[]                 │  │    │   │
│  │  │  │  ┌─────────┐ ┌─────────┐ ┌─────────┐     │  │    │   │
│  │  │  │  │  Text   │ │  Chart  │ │  Table  │     │  │    │   │
│  │  │  │  │ Element │ │ Element │ │ Element │     │  │    │   │
│  │  │  │  └─────────┘ └─────────┘ └─────────┘     │  │    │   │
│  │  │  └───────────────────────────────────────────┘  │    │   │
│  │  └─────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           │                                     │
│                           ▼                                     │
│                    ┌─────────────┐                              │
│                    │  Renderer   │                              │
│                    └─────────────┘                              │
│                           │                                     │
│              ┌────────────┼────────────┐                       │
│              ▼            ▼            ▼                        │
│         ┌────────┐   ┌────────┐   ┌────────┐                   │
│         │ Screen │   │  PDF   │   │ Excel  │                   │
│         └────────┘   └────────┘   └────────┘                   │
│                                                                 │
│  Show: Report model hierarchy and rendering                     │
│  Include: Element types and export formats                      │
└─────────────────────────────────────────────────────────────────┘
```

## Export Services

| Service | Purpose |
|---------|---------|
| `SpreadsheetExportService` | Excel/CSV export |
| `ChartExcelExportService` | Chart data to Excel |
| `ReportRenderer` | PDF generation |

### Export Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Export Flow (ExportFlow.svg)                                   │
│                                                                 │
│  ┌─────────────┐                                               │
│  │  Data View  │                                               │
│  └──────┬──────┘                                               │
│         │                                                       │
│         ▼                                                       │
│  ┌─────────────┐    ┌─────────────┐                            │
│  │Export Button│───▶│Format Select│                            │
│  └─────────────┘    └──────┬──────┘                            │
│                            │                                    │
│         ┌──────────────────┼──────────────────┐                │
│         ▼                  ▼                  ▼                 │
│    ┌─────────┐       ┌─────────┐        ┌─────────┐            │
│    │   CSV   │       │  Excel  │        │   PDF   │            │
│    └─────────┘       └─────────┘        └─────────┘            │
│                                                                 │
│  Show: User-initiated export workflow                           │
│  Include: Format options                                        │
└─────────────────────────────────────────────────────────────────┘
```
