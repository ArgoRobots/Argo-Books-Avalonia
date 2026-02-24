# Report Generator - Problems, Limitations & Improvement Opportunities

## Business Context

**Argo Books** is a cross-platform desktop accounting application for small-to-medium businesses, offering AI receipt scanning, expense/revenue tracking, predictive analytics, inventory management, rental management, customer management, and invoicing. Data stays local (AES-256 encrypted `.argo` files), and the app runs on Windows, macOS, and Linux via Avalonia UI.

The report generator is a core feature that lets users create visual business reports with charts, tables, and summaries, exported as PDF, PNG, or JPEG.

---

## Architecture Summary

The report generator uses a **3-step wizard**:
1. **Template & Settings** - Select template, date range, transaction type, charts
2. **Layout Designer** - SkiaSharp WYSIWYG drag-and-drop canvas with undo/redo
3. **Preview & Export** - Bitmap preview, export to PDF/PNG/JPEG

Key components:
- `ReportRenderer` - SkiaSharp rendering engine for charts, tables, labels, images, summaries
- `ReportChartDataService` - Generates chart data points from `CompanyData`
- `ReportTableDataService` - Generates table rows from transactions
- `ReportTemplateFactory` - 8 built-in templates + blank template
- `ReportTemplateStorage` - JSON-based custom template persistence

---

## Critical Problems

### 1. Single-Page Only - No Pagination

**Location:** `ReportRenderer.cs:119-131`, `ExportToPdfAsync` at line 232-238

The entire report renders to a single page. The PDF export creates exactly one page:

```csharp
using var canvas = document.BeginPage(scaledWidth, scaledHeight);
RenderToCanvas(canvas, scaledWidth, scaledHeight);
document.EndPage();
```

Tables are clipped to fit within element bounds (`ReportRenderer.cs:1464-1466`). If a business has hundreds of transactions, data is silently truncated. For an accounting application, this is the most critical gap — users cannot produce complete transaction listings.

**Impact:** Users with non-trivial transaction volumes lose data in reports.

### 2. No Standard Accounting Reports

The report templates focus on visual/analytical charts. Standard accounting reports that businesses and their accountants need are **entirely missing**:
- Balance Sheet / Statement of Financial Position
- Income Statement / Profit & Loss Statement
- Cash Flow Statement
- Trial Balance
- General Ledger
- Accounts Receivable/Payable Aging
- Tax Summary Report

The data model has raw transaction data but no service to produce these standard financial statements.

**Impact:** Users cannot generate reports that accountants, auditors, and tax authorities expect.

---

## High-Severity Problems

### 3. Hardcoded Currency Symbol (`$`)

**Location:** `ReportRenderer.cs:64-67`

```csharp
return ShouldShowCurrency(chartType) ? $"${value:N0}" : $"{value:N0}";
```

The renderer always shows `$` despite the app supporting multi-currency transactions (the data model has `EffectiveTotalUSD`). Businesses in EUR, GBP, CAD, or other currencies see the wrong symbol.

**Improvement:** Read currency symbol from company settings or use the transaction's currency.

### 4. Silent Error Swallowing

Throughout the report pipeline, errors are caught and silently discarded:
- `ExportToImageAsync` (`ReportRenderer.cs:208-211`): `catch { return false; }`
- `ExportToPdfAsync` (`ReportRenderer.cs:241-245`): `catch { return false; }`
- `GeneratePreview` (`ReportsPageViewModel.cs:1328-1331`): `catch { PreviewImage = null; }`
- Several methods in `ReportTemplateStorage` (`GetSavedTemplateNames`, `GetAllTemplatesAsync`, `DeleteTemplate`, `RenameTemplateAsync`, `CopyImageToStorage`): `catch { /* return empty/false */ }`. Note: `SaveTemplateAsync` and `LoadTemplateAsync` do log errors to `_errorLogger`.

Users get generic "Export failed" messages with no diagnostic information.

**Improvement:** Log specific exceptions to the error logger and surface actionable messages.

---

## Medium-Severity Problems

### 5. Hardcoded Font Family (`Segoe UI`)

**Location:** `ReportRenderer.cs:82-83`

```csharp
_defaultTypeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
```

Segoe UI is a **Windows-only** font. On macOS and Linux, this falls back to `SKTypeface.Default`, producing inconsistent or poor typography.

**Improvement:** Use a cross-platform default (e.g., system font detection) or bundle a font.

### 6. Date Range Inconsistency Between Services

`ReportTableDataService.GetDateRange()` and `ReportChartDataService.GetDateRange()` have **different logic** for end-date normalization. The chart service adds `.AddDays(1).AddSeconds(-1)` for custom ranges (`ReportChartDataService.cs:23-24`), while the table service does not (`ReportTableDataService.cs:28-30`).

Tables and charts in the same report can show slightly different data for the same date range.

**Improvement:** Extract shared date range logic into a common utility.

### 7. Chart Data Recalculated on Every Render

**Location:** `ReportRenderer.cs:77-80`

`ReportChartDataService` is instantiated fresh per render. Data queries iterate the full in-memory list repeatedly. With 30+ chart types and no caching, rendering a report with many charts is slow on large datasets.

**Improvement:** Cache chart data per configuration, invalidate on filter change.

---

## Low-Severity Problems

### 8. Pie Chart Legend Truncation at 12 Characters

**Location:** `ReportRenderer.cs:971`

```csharp
if (legendText.Length > 12) legendText = legendText[..12] + "...";
```

Country names, product names, and company names > 12 chars are cut off with no user control. "United States" becomes "United State...".

### 9. No Year-over-Year Comparison Reports

No mechanism to compare two time periods side-by-side (e.g., "Q1 2025 vs Q1 2024"). `CalculateGrowthRate` computes a single percentage but doesn't expose comparative data visually.

### 10. Image Elements May Use Absolute Paths

`ImageReportElement.ImagePath` stores file paths that can be absolute. `ReportTemplateStorage` provides `CopyImageToStorage()` and `ResolveImagePath()` to partially mitigate this by copying images to a shared directory and resolving relative paths. However, images added directly (not through template storage) still use absolute paths, and templates can break when opened on different machines or after OS reinstalls.

### 11. Chart Distribution Capped at Top 10

Most distribution chart methods use `.Take(10)` (13 instances across `ReportChartDataService`, including `GetRevenueDistribution`, `GetExpenseDistribution`, `GetRevenueByCustomerCountry`, `GetExpensesByCountryOfDestination`, `GetExpensesBySupplierCompany`, `GetRevenueByCompanyOfOrigin`, `GetRevenueByCompanyOfDestination`, `GetTopCustomersByRevenue`, `GetRentalsPerCustomer`, `GetReturnsByCategory`, `GetReturnsByProduct`, `GetLossesByCategory`, and `GetLossesByProduct`). Users with more than 10 categories/countries/products see incomplete data with no "Other" aggregation bucket.
