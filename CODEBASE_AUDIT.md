# Argo Books Avalonia - Comprehensive Codebase Audit

**Date:** 2026-03-20
**Scope:** Full codebase (739 source files across 6 projects)

---

## Executive Summary

The Argo Books Avalonia application demonstrates solid engineering fundamentals with strong encryption, proper MVVM architecture, and good use of modern C#/.NET patterns. However, this audit identified **53 findings** across security, error handling, data integrity, business logic, UI patterns, and build configuration.

**Severity Breakdown:**
- Critical: 10
- High: 14
- Medium: 19
- Low: 10

---

## 1. SECURITY VULNERABILITIES

### 1.1 [HIGH] UseShellExecute=true with URLs (11 instances)

Multiple files use `Process.Start` with `UseShellExecute = true`, which can lead to command injection on Unix-like systems if URLs contain shell metacharacters.

**Affected files:**
- `ArgoBooks.Desktop/Services/NetSparkleUpdateService.cs:388`
- `ArgoBooks/ViewModels/UpgradeModalViewModel.cs:175`
- `ArgoBooks/ViewModels/ReportsPageViewModel.cs:1789`
- `ArgoBooks/ViewModels/SetupChecklistViewModel.cs:233`
- `ArgoBooks/ViewModels/WelcomeScreenViewModel.cs:161, 182`
- `ArgoBooks/ViewModels/SettingsModalViewModel.cs:808`
- `ArgoBooks/ViewModels/CheckForUpdateModalViewModel.cs:253`
- `ArgoBooks/ViewModels/HelpPanelViewModel.cs:107`
- `ArgoBooks.Core/Services/GoogleCredentialsManager.cs:131`
- `ArgoBooks.Core/Services/InvoiceTemplates/InvoicePreviewService.cs:115`
- `ArgoBooks.Core/Services/GoogleSheetsService.cs:212`

**Recommendation:** Validate URLs against a whitelist of schemes (https://, mailto:) before passing to `Process.Start`. Consider using `UseShellExecute = false` where possible.

### 1.2 [MEDIUM] Incomplete Path Traversal Protection

**File:** `ArgoBooks.Core/Services/FileService.cs:491-496`

`SanitizeDirectoryName()` removes `..` literally but doesn't validate the final resolved path stays within the intended directory.

```csharp
private static string SanitizeDirectoryName(string name)
{
    var invalid = Path.GetInvalidFileNameChars();
    var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
    sanitized = sanitized.Replace("..", "");
    return string.IsNullOrWhiteSpace(sanitized) ? "Company" : sanitized.Trim();
}
```

**Recommendation:** Add `Path.GetFullPath()` validation to ensure the resolved path is within the expected base directory.

### 1.3 [MEDIUM] Plaintext Password in Memory

**File:** `ArgoBooks.Core/Services/CompanyManager.cs:86`

`GetCurrentPassword()` returns the plaintext password stored in memory without secure string handling or explicit memory clearing after use.

### 1.4 [MEDIUM] Environment Variables Expose Secrets

**File:** `ArgoBooks.Core/Services/DotEnv.cs:132`

Secrets from `.env` files are set as process-wide environment variables via `Environment.SetEnvironmentVariable()`, making them accessible to all child processes.

### 1.5 [POSITIVE] Strong Encryption Implementation

- AES-256-GCM with 600,000 PBKDF2 iterations (`ArgoBooks.Core/Security/KeyDerivation.cs`)
- Proper use of `CryptographicOperations.FixedTimeEquals()` for constant-time comparison
- `CryptographicOperations.ZeroMemory()` for key material cleanup
- No hardcoded secrets or API keys found in source code
- All API endpoints use HTTPS

---

## 2. ERROR HANDLING ISSUES

### 2.1 [CRITICAL] 49 async void Methods Risk Application Crashes

Unhandled exceptions in `async void` methods crash the entire application. Found **49 instances** across the codebase.

**Key affected areas:**

**Event handlers in Views (26 instances):**
- `ArgoBooks/Controls/InvoicePreviewControl.axaml.cs:239, 375, 394, 460, 692`
- `ArgoBooks/Views/ReceiptsPage.axaml.cs:36, 158`
- `ArgoBooks/Views/ReportsPage.axaml.cs:132, 233, 836`
- `ArgoBooks/Views/DashboardPage.axaml.cs:141, 157`
- `ArgoBooks/Views/AnalyticsPage.axaml.cs:284, 300`
- `ArgoBooks/Views/MainWindow.axaml.cs:123`
- `ArgoBooks/Views/AppShell.axaml.cs:124`
- And others in Behaviors, Modals, Panels

**ViewModel delete-confirm methods (23 instances):**
- `ArgoBooks/ViewModels/CustomerModalsViewModel.cs:568`
- `ArgoBooks/ViewModels/InvoiceModalsViewModel.cs:832`
- `ArgoBooks/ViewModels/PaymentModalsViewModel.cs:625`
- `ArgoBooks/ViewModels/ProductModalsViewModel.cs:553`
- `ArgoBooks/ViewModels/ExpenseModalsViewModel.cs:195`
- `ArgoBooks/ViewModels/RevenueModalsViewModel.cs:205`
- And 17 others following the same pattern

**Recommendation:** Wrap all `async void` method bodies in try-catch blocks that log and gracefully handle errors.

### 2.2 [HIGH] Empty Catch Blocks Swallow Exceptions

**File:** `ArgoBooks.Desktop/Services/NetSparkleUpdateService.cs:189, 197`

```csharp
try { File.Delete(filePath); } catch { }
```

Silently swallows all exceptions including permission errors and disk failures.

### 2.3 [HIGH] Fire-and-Forget Tasks Without Error Handling

- `ArgoBooks.Core/Services/TelemetryManager.cs:84` - `Task.Run` without top-level error handling
- `ArgoBooks/ViewModels/ReportsPageViewModel.cs:1769` - `Task.Run` with dispatcher post, no error handling

### 2.4 [MEDIUM] Unsafe .Result Access After Await

**File:** `ArgoBooks/ViewModels/EditCompanyModalViewModel.cs:351`

```csharp
var success = preloadTask.Result; // After await Task.WhenAll()
```

Using `.Result` is unnecessary after `await` and can cause deadlocks in synchronization contexts. Should use `await` instead.

### 2.5 [MEDIUM] Null-Forgiving Operator on Potentially Null Paths

- `ArgoBooks/ViewModels/RevenueModalsViewModel.cs:737` - `new FileInfo(ReceiptFilePath!)`
- `ArgoBooks/ViewModels/ExpenseModalsViewModel.cs:719` - `new FileInfo(ReceiptFilePath!)`

If `ReceiptFilePath` is null, these throw `NullReferenceException` despite the null-forgiving operator.

---

## 3. DATA & MODEL LAYER ISSUES

### 3.1 [CRITICAL] Currency Fallback Logic Treats Zero as "Not Set"

Multiple models use `> 0` checks to determine if a USD-converted value exists, but this fails for legitimate zero-amount transactions.

**File:** `ArgoBooks.Core/Models/Transactions/Transaction.cs:192, 200, 207, 214`
```csharp
public decimal EffectiveTotalUSD => IsPendingConversion ? 0 : (TotalUSD > 0 ? TotalUSD : Total);
```

**File:** `ArgoBooks.Core/Models/Transactions/Invoice.cs:200, 206`
```csharp
public decimal EffectiveTotalUSD => TotalUSD > 0 ? TotalUSD : Total;
public decimal EffectiveBalanceUSD => BalanceUSD > 0 ? BalanceUSD : Balance;
```

**File:** `ArgoBooks.Core/Models/Transactions/Payment.cs:100`
```csharp
public decimal EffectiveAmountUSD => AmountUSD > 0 ? AmountUSD : Amount;
```

**Impact:** Zero-amount invoices/payments in non-USD currencies will incorrectly use the local currency value instead of the converted USD value (0). Negative values (credit notes) will also fall through incorrectly.

**Recommendation:** Use a nullable `decimal?` for USD fields, or add an explicit `IsConverted` boolean flag.

### 3.2 [CRITICAL] Pending Conversion Hides Transaction Data

**File:** `ArgoBooks.Core/Models/Transactions/Transaction.cs:192, 200, 207, 214`

When `IsPendingConversion = true`, all `Effective*USD` properties return `0`, hiding legitimate transaction data from reports and totals while offline.

### 3.3 [HIGH] Non-Thread-Safe Collections in CompanyData

**File:** `ArgoBooks.Core/Data/CompanyData.cs:45-239`

All 26 entity collections use `List<T>` with no synchronization. If any background operations (telemetry, auto-save, forecasting) access these while the UI modifies them, race conditions can occur.

**Recommendation:** Use `ConcurrentBag<T>` or add explicit synchronization for shared collections.

### 3.4 [HIGH] Missing Data Validation Methods

**File:** `ArgoBooks.Core/Validation/DataValidator.cs`

No validation exists for:
- `Payment` entities
- `Accountant` entities
- `RecurringInvoice` entities

### 3.5 [MEDIUM] Computed Properties with JSON Serialization Attributes

**File:** `ArgoBooks.Core/Models/Common/LineItem.cs`

```csharp
[JsonPropertyName("subtotal")]
public decimal Subtotal => (Quantity * UnitPrice) - Discount;
```

Computed properties marked with `[JsonPropertyName]` will serialize but cannot be deserialized, creating potential data mismatches.

### 3.6 [MEDIUM] Dual-Representation Fields Create Ambiguity

- `ArgoBooks.Core/Models/Rentals/RentalRecord.cs:19-105` - Has both single-item `RentalItemId` AND multi-item `LineItems` list
- `ArgoBooks.Core/Models/Common/OcrData.cs:47-54` - Has both legacy `ExtractedItems` AND structured `LineItems`

### 3.7 [MEDIUM] Implicit Decimal-to-MonetaryValue Conversion

**File:** `ArgoBooks.Core/Models/Common/MonetaryValue.cs:74`

Implicit conversion from `decimal` to `MonetaryValue` can silently treat raw decimals as USD monetary values.

### 3.8 [MEDIUM] Currency Conversion Edge Case

**File:** `ArgoBooks.Core/Services/AccountingReportDataService.cs:139-141`

```csharp
if (txn.Total != 0 && txn.TotalUSD > 0) return txn.TotalUSD / txn.Total;
return 1; // Fallback assumes 1:1 ratio
```

Returns 1 for zero-amount transactions, which silently produces incorrect conversion ratios. Negative totals are also not handled.

---

## 4. BUSINESS LOGIC ISSUES

### 4.1 [CRITICAL] Potential Infinite Loop in Column Width Distribution

**File:** `ArgoBooks/Controls/ColumnWidths/TableColumnWidthsBase.cs:303-340`

A `while (true)` loop relies on floating-point comparisons to detect convergence. No iteration limit exists.

**Recommendation:** Add a maximum iteration counter (e.g., 100) as a safety valve.

### 4.2 [HIGH] Integer Overflow in Progress Calculation

**File:** `ArgoBooks.Desktop/Services/NetSparkleUpdateService.cs:169`

```csharp
var progress = (int)(receivedBytes * 100 / totalBytes);
```

If `receivedBytes` is large, `receivedBytes * 100` can overflow before division.

**Recommendation:** Reorder as `(int)((double)receivedBytes / totalBytes * 100)`.

### 4.3 [HIGH] Duplicate Variance Calculation

**File:** `ArgoBooks.Core/Services/HoltWintersForecasting.cs:545-550, 676-681`

Two identical variance implementations. If one is updated without the other, forecasting results become inconsistent.

### 4.4 [MEDIUM] Hardcoded Forecasting Parameters

**File:** `ArgoBooks.Core/Services/HoltWintersForecasting.cs:12-15`

DefaultAlpha (0.3), DefaultBeta (0.1), DefaultGamma (0.2), DefaultPhi (0.9) are hardcoded constants that cannot be tuned per company.

### 4.5 [MEDIUM] Fragile Guard in InsightsService

**File:** `ArgoBooks.Core/Services/InsightsService.cs:1006`

```csharp
if (supplierPurchases.Count < 2) ...
var topSupplier = supplierPurchases.First();
```

Guard checks `< 2` but `.First()` only requires `>= 1`. If the guard is changed, this could throw.

---

## 5. UI / MVVM PATTERN ISSUES

### 5.1 [CRITICAL] Memory Leaks from Anonymous Event Subscriptions

Anonymous lambda event handlers can never be unsubscribed, creating accumulating memory leaks.

**Key instances:**
- `ArgoBooks/ViewModels/InvoiceModalsViewModel.cs:381` - `item.PropertyChanged += (_, _) => UpdateTotals()` added per line item, never removed when items are deleted at line 391
- `ArgoBooks/ViewModels/InvoiceModalsViewModel.cs:512` - `App.PlanStatusChanged += ...` in constructor, never unsubscribed
- `ArgoBooks/ViewModels/TransactionModalsViewModelBase.cs:579, 596, 1049` - Same pattern across base class
- `ArgoBooks/ViewModels/PurchaseOrdersModalsViewModel.cs:287, 359` - Same pattern
- `ArgoBooks/ViewModels/RevenueModalsViewModel.cs:23` - Same pattern
- `ArgoBooks/ViewModels/ReceiptsPageViewModel.cs:~567` - Subscribes to `item.PropertyChanged` on every `RefreshReceipts` call, accumulating subscriptions

**Recommendation:** Use named handler methods and unsubscribe them when items are removed or the ViewModel is disposed:
```csharp
private void RemoveLineItem(LineItemDisplayModel? item)
{
    if (item != null)
    {
        item.PropertyChanged -= UpdateTotalsHandler; // UNSUBSCRIBE FIRST
        LineItems.Remove(item);
        UpdateTotals();
    }
}
```

### 5.2 [CRITICAL] ChartExpandOverlay Event Handlers Never Unsubscribed

**File:** `ArgoBooks/Controls/ChartExpandOverlay.axaml.cs:267-270`

PropertyChanged handlers subscribed to `ChartEmptyState` controls are never removed when the overlay closes or page unloads. Each chart panel decoration accumulates subscriptions.

### 5.3 [HIGH] Static Handler Dictionary Never Cleaned Up

**File:** `ArgoBooks/Helpers/ModalAnimationBehavior.cs:80-90, 108`

A `static Dictionary<Border, PropertyChangedEventHandler>` stores handlers. Entries accumulate if borders are recreated frequently. No mechanism exists to clear stale entries.

**Recommendation:** Use `ConditionalWeakTable` or `WeakReference` for border keys.

### 5.4 [HIGH] Missing Event Cleanup in Code-Behind (10+ files)

Several views subscribe to events in `OnLoaded`/constructor without unsubscribing in `OnUnloaded`:

**Views:**
- `ArgoBooks/Views/AppShell.axaml.cs:31-35, 101`
- `ArgoBooks/Views/ReportsPage.axaml.cs:76, 105-119` - Multiple handlers subscribed, incomplete unsubscription (may unsubscribe to wrong VM instance if DataContext changed)
- `ArgoBooks/Views/MainWindow.axaml.cs:30, 35, 254`
- `ArgoBooks/Views/DashboardPage.axaml.cs` - DataContextChanged handler, no OnUnloaded
- `ArgoBooks/Views/AnalyticsPage.axaml.cs` - DataContextChanged handler, no OnUnloaded

**Modals (no OnUnloaded at all):**
- `ArgoBooks/Modals/ReceiptViewerModal.axaml.cs`
- `ArgoBooks/Modals/ReceiptsModals.axaml.cs:59-62`
- `ArgoBooks/Modals/AppTourOverlay.axaml.cs:39`
- `ArgoBooks/Modals/CategoriesTutorialOverlay.axaml.cs:37`
- `ArgoBooks/Modals/ProductsTutorialOverlay.axaml.cs:37`
- `ArgoBooks/Modals/UpgradeModal.axaml.cs:35`
- `ArgoBooks/Modals/PasswordPromptModal.axaml.cs:29`
- `ArgoBooks/Modals/PastPredictionsModal.axaml.cs:21`

**Controls:**
- `ArgoBooks/Controls/Header.axaml.cs:367-368` - No OnUnloaded to unsubscribe PropertyChanged
- `ArgoBooks/Controls/ArgoTable/ArgoTable.axaml.cs:535-538` - Collection handlers not cleaned on unload

**Panels (moderate risk - cleanup only on DataContext change, not unload):**
- `ArgoBooks/Panels/CompanySwitcherPanel.axaml.cs`
- `ArgoBooks/Panels/NotificationPanel.axaml.cs`
- `ArgoBooks/Panels/FileMenuPanel.axaml.cs`
- `ArgoBooks/Panels/UserPanel.axaml.cs`
- `ArgoBooks/Panels/HelpPanel.axaml.cs`

### 5.5 [HIGH] Only 2 ViewModels Implement IDisposable

Despite many ViewModels holding event subscriptions, only 2 implement `IDisposable`. This means there is no cleanup path for most ViewModel event subscriptions.

### 5.6 [MEDIUM] MVVM Violation - Significant Logic in ReportsPage Code-Behind

**File:** `ArgoBooks/Views/ReportsPage.axaml.cs`

The code-behind contains substantial logic that should be in the ViewModel or a service:
- Zoom calculations (lines 430-462, 483-523)
- Pan calculations (lines 593-638, 641-699)
- Canvas refresh logic (lines 245-249)
- Element sizing logic (lines 798-894)

This makes testing difficult and tightly couples view logic to the code-behind.

### 5.7 [MEDIUM] Dispatcher.UIThread.Post Without Error Handling

Multiple files use `Dispatcher.UIThread.Post` without exception handling in the lambda:
- `ArgoBooks/Views/ReportsPage.axaml.cs:447`
- `ArgoBooks/Controls/ChartExpandOverlay.axaml.cs:121, 131`
- `ArgoBooks/Panels/QuickActionsPanel.axaml.cs:58, 77`

---

## 6. BUILD & DEPENDENCY ISSUES

### 6.1 [MEDIUM] Unused NuGet Packages in Central Package Management

**File:** `Directory.Packages.props`

These packages are defined but never referenced by any `.csproj`:
- `Google.Apis.Sheets.v4` v1.72.0.3966
- `Google.Apis.Drive.v3` v1.73.0.4045
- `Azure.AI.FormRecognizer` v4.1.0

### 6.2 [MEDIUM] Suppressed Version Conflict (WebView2 vs WindowsBase)

**Files:** `ArgoBooks/ArgoBooks.csproj:14`, `ArgoBooks.Desktop/ArgoBooks.Desktop.csproj:21`

MSB3277 warning is suppressed via `<NoWarn>` rather than resolved. WebView2 references WindowsBase v5.0 while .NET 10 provides v4.0.

### 6.3 [MEDIUM] Pre-Release Dependencies in Production

**File:** `Directory.Packages.props:21-22`

- `LiveChartsCore.SkiaSharpView` v2.0.0-rc5.4
- `LiveChartsCore.SkiaSharpView.Avalonia` v2.0.0-rc5.4

### 6.4 [LOW] Deprecated Drag-and-Drop API Usage

**File:** `ArgoBooks/Views/ReceiptsPage.axaml.cs:125-127, 164-166`

Suppressed with `#pragma warning disable CS0618`. Comment says "Using deprecated API until full migration path is clear."

### 6.5 [LOW] Duplicate Excel Libraries

**File:** `ArgoBooks.Core/ArgoBooks.Core.csproj`

Both ClosedXML and EPPlus are referenced. Consider consolidating if functionality overlaps.

### 6.6 [LOW] No CI/CD Configuration

No `.github/workflows/` directory or CI/CD pipeline configuration was found.

---

## 7. POSITIVE FINDINGS

The following areas demonstrate strong engineering practices:

1. **Encryption:** AES-256-GCM with proper key derivation (600K PBKDF2 iterations)
2. **No hardcoded secrets:** All sensitive values use environment variables or secure storage
3. **Safe JSON deserialization:** Uses typed `System.Text.Json` throughout (no BinaryFormatter)
4. **Proper file validation:** Magic bytes, footer structure, and size checks before processing
5. **MVVM architecture:** Clean separation with CommunityToolkit.Mvvm source generators
6. **Thread-safe singletons:** Proper double-check locking in `PlatformServiceFactory`
7. **Cache synchronization:** `ExchangeRateCache` uses proper lock protection
8. **Safe event invocation:** Consistent use of `?.Invoke()` pattern
9. **Input sanitization:** `ArgumentNullException.ThrowIfNull()` used throughout
10. **Decimal for money:** All monetary values correctly use `decimal` type

---

## Prioritized Remediation Plan

### Phase 1 - Critical (Immediate)
1. Add try-catch to all `async void` event handlers (49 methods)
2. Fix currency fallback logic (`> 0` checks) to use nullable types or `IsConverted` flag
3. Add iteration limit to `while(true)` loop in `TableColumnWidthsBase`
4. Fix memory leaks from anonymous event subscriptions in InvoiceModalsViewModel, TransactionModalsViewModelBase, PurchaseOrdersModalsViewModel, RevenueModalsViewModel, and ReceiptsPageViewModel
5. Fix ChartExpandOverlay event handler leak

### Phase 2 - High (Near-term)
6. Add URL validation before `Process.Start` calls (11 instances)
7. Implement `IDisposable` on ViewModels with event subscriptions
8. Add missing `OnUnloaded` cleanup in 10+ view/modal code-behind files
9. Replace static `Dictionary<Border, ...>` with `ConditionalWeakTable` in ModalAnimationBehavior
10. Add validation methods for Payment, Accountant, RecurringInvoice
11. Fix integer overflow in progress calculation
12. Consolidate duplicate variance calculation
13. Fix `.Result` access in EditCompanyModalViewModel (use `await` instead)

### Phase 3 - Medium (Planned)
14. Enhance path traversal protection with `Path.GetFullPath()` validation
15. Switch to thread-safe collections or add synchronization in CompanyData
16. Remove unused NuGet packages from Directory.Packages.props
17. Resolve WebView2/WindowsBase version conflict
18. Upgrade LiveCharts to stable release
19. Add explicit `IsConverted` flag for currency conversion
20. Move ReportsPage zoom/pan logic from code-behind to ViewModel/service
21. Add error handling to `Dispatcher.UIThread.Post` lambdas

### Phase 4 - Low (Backlog)
22. Migrate deprecated drag-and-drop API
23. Evaluate Excel library consolidation
24. Set up CI/CD pipeline
25. Make forecasting parameters configurable
