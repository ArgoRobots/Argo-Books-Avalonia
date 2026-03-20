# Financial Code Audit Report — Argo Books Avalonia

**Date:** 2026-03-20
**Scope:** All financial calculations, currency handling, terminology, and data integrity
**Files Reviewed:** 25+ source files across Models, Services, and ViewModels

---

## CRITICAL Issues

### 1. EffectiveSubtotalUSD Currency Mixing When TaxAmountUSD = 0

**File:** `ArgoBooks.Core/Models/Transactions/Transaction.cs`
**Code:**
```csharp
public decimal EffectiveSubtotalUSD => IsPendingConversion ? 0
    : (EffectiveTotalUSD - (TaxAmountUSD > 0 ? TaxAmountUSD : TaxAmount));
```

**Problem:** When `TaxAmountUSD` is 0 but `TaxAmount` is in a non-USD currency (e.g., 500 JPY), the fallback subtracts a local-currency amount from a USD amount. For a transaction of $100 USD total with 500 JPY tax, this would compute `EffectiveSubtotalUSD = 100 - 500 = -400` — a nonsensical result.

**Impact:** Corrupts the Income Statement (revenue and expense pre-tax amounts), Cash Flow Statement (cash from sales/expenses), Dashboard statistics, and all chart data that uses `EffectiveSubtotalUSD`.

---

### 2. Tax Summary Percentage Display Bug (Double Percentage Conversion)

**File:** `ArgoBooks.Core/Services/AccountingReportDataService.cs` (Tax Summary section)
**Context:** Revenue.TaxRate is stored as a percentage (e.g., 8 for 8%) when saved from `RevenueModalsViewModel`:
```csharp
// RevenueModalsViewModel.cs line 522
TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0
```

**Problem:** If the Tax Summary groups by `TaxRate` and formats it by multiplying by 100 again, transactions without line items (where TaxRate = 8) would display as "800%". LineItem.TaxRate is stored as a decimal (0.08), creating an inconsistency in how tax rates are interpreted across the codebase.

**Impact:** Tax reports may show wildly incorrect tax rate percentages for transactions that don't use line items.

---

### 3. Balance Sheet: Accounts Payable Not USD-Converted

**File:** `ArgoBooks.Core/Services/AccountingReportDataService.cs`
**Code (Balance Sheet section):**
```csharp
// Accounts Payable uses po.Total directly
accountsPayable = purchaseOrders.Sum(po => po.Total);
```

**Problem:** `po.Total` is in the original transaction currency. All other Balance Sheet figures (Cash, Accounts Receivable) use USD-converted amounts (`EffectiveTotalUSD`, `EffectiveBalanceUSD`, `EffectiveAmountUSD`). Mixing local-currency AP with USD-converted assets produces a balance sheet that doesn't balance.

**Impact:** Balance Sheet is mathematically incorrect for any company with non-USD purchase orders. Equity (Assets - Liabilities) will be wrong.

---

### 4. PaymentPortalService: Non-USD Payments Set AmountUSD = 0

**File:** `ArgoBooks.Core/Services/PaymentPortalService.cs`, line 347-348
**Code:**
```csharp
AmountUSD = portalPayment.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase)
    ? portalPayment.Amount : 0,
```

**Problem:** Any online payment received in a non-USD currency gets `AmountUSD = 0`. Since Balance Sheet Cash calculations use `EffectiveAmountUSD` (which falls back to `Amount` when `AmountUSD` is 0), this may cause currency mixing in cash calculations. Furthermore, any report filtering on `AmountUSD > 0` will exclude these payments entirely.

**Impact:** Non-USD online payments may be invisible in USD-denominated reports or cause currency mixing in cash calculations.

---

### 5. SaveAsDraft Doesn't Compute Totals Before Setting USD Fields

**File:** `ArgoBooks/ViewModels/InvoiceModalsViewModel.cs`
**Context:** When saving as draft, `TotalUSD` and `BalanceUSD` are set, but since the computed properties `Subtotal`, `TaxAmount`, and `Total` may not have been triggered/calculated at that point, the USD values could be set to 0 or stale values.

**Impact:** Draft invoices may have `TotalUSD = 0` and `BalanceUSD = 0`, causing them to be excluded from or miscounted in financial reports that filter or aggregate by USD amounts.

---

### 6. ReportTableDataService: Summary Statistics Use Local Currency

**File:** `ArgoBooks.Core/Services/ReportTableDataService.cs`
**Code:**
```csharp
// Revenue statistics use s.Subtotal (local currency property)
totalRevenue = revenues.Sum(s => s.Subtotal);
// Expense statistics use p.Total - p.TaxAmount (local currency)
totalExpenses = expenses.Sum(p => p.Total - p.TaxAmount);
```

**Problem:** These summary statistics use local-currency amounts instead of `EffectiveSubtotalUSD`. In a multi-currency environment, this sums USD + EUR + JPY amounts as if they were the same currency.

**Impact:** Revenue/expense summary statistics, growth rate calculations, top products/customers/suppliers analysis — all produce meaningless numbers for multi-currency companies.

---

## HIGH Issues

### 7. Growth Rate Uses Local Currency for Period Comparison

**File:** `ArgoBooks.Core/Services/ReportTableDataService.cs`
**Problem:** Revenue growth rate comparing current vs. prior period uses `s.Subtotal` (local currency). If currency mix changes between periods (e.g., more EUR sales this quarter), the growth rate reflects currency composition changes, not actual business growth.

**Impact:** Misleading growth metrics that conflate currency fluctuations with business performance.

---

### 8. Invoice EffectiveTotalUSD Missing IsPendingConversion Check

**File:** `ArgoBooks.Core/Models/Transactions/Invoice.cs`
**Code:**
```csharp
public decimal EffectiveTotalUSD => TotalUSD > 0 ? TotalUSD : Total;
public decimal EffectiveBalanceUSD => BalanceUSD > 0 ? BalanceUSD : Balance;
```

**Problem:** Unlike `Transaction.EffectiveTotalUSD` which returns 0 when `IsPendingConversion` is true, Invoice has no such check. Invoices pending conversion will return local-currency `Total`/`Balance` as if they were USD.

**Impact:** Invoices awaiting currency conversion will have their local-currency amounts treated as USD in all reports.

---

### 9. Cash Flow vs. Balance Sheet Inconsistency (Pre-tax vs. Post-tax)

**File:** `ArgoBooks.Core/Services/AccountingReportDataService.cs`
**Problem:**
- Cash Flow Statement uses `EffectiveSubtotalUSD` (pre-tax) for cash from sales and cash expenses
- Balance Sheet Cash uses `EffectiveTotalUSD` (post-tax) for revenue and expenses

Both represent the same underlying cash, but compute different amounts. If a company has $100 in sales with $8 tax, Cash Flow shows $92 inflow but Balance Sheet shows $100 - $8 tax liability treatment differently.

**Impact:** Cash Flow ending balance won't reconcile with Balance Sheet cash position.

---

### 10. CreateRevenueFromInvoice Doesn't Set TaxAmountUSD

**File:** `ArgoBooks/ViewModels/InvoiceModalsViewModel.cs`
**Problem:** When creating a Revenue record from an invoice, `TaxAmountUSD` is not explicitly set. This means it defaults to 0, which triggers the currency-mixing fallback in `EffectiveSubtotalUSD` (Issue #1).

**Impact:** All revenue records created from invoices will have the EffectiveSubtotalUSD currency-mixing bug.

---

### 11. PaymentPortalService: Balance Reconciliation Currency Mixing

**File:** `ArgoBooks.Core/Services/PaymentPortalService.cs`, lines 358-363
**Code:**
```csharp
var totalPaid = companyData.Payments
    .Where(p => p.InvoiceId == invoice.Id && p.Amount > 0)
    .Sum(p => p.Amount);
invoice.Balance = invoice.Total - totalPaid;
```

**Problem:** `p.Amount` may be in different currencies than `invoice.Total`. Summing payments in mixed currencies and subtracting from the invoice total produces incorrect balances.

**Impact:** Invoice balances may be wrong when payments are received in currencies different from the invoice currency. BalanceUSD is also not updated after payment.

---

### 12. InsightsService: Product Profit Margin Uses Local Currency

**File:** `ArgoBooks.Core/Services/InsightsService.cs`, lines 896-909
**Code:**
```csharp
Revenue = g.Sum(li => li.Subtotal),  // local currency from line items
Cost = g.Sum(li => li.Quantity * (companyData.GetProduct(li.ProductId ?? "")?.CostPrice ?? 0)),
// ...
Margin = (p.Revenue - p.Cost) / p.Revenue * 100
```

**Problem:** `li.Subtotal` is in the transaction's original currency while `CostPrice` is stored in whatever currency the product was defined in. If a product is sold in EUR but its cost is recorded in USD, the margin calculation is meaningless.

**Impact:** Product profitability insights are unreliable in multi-currency scenarios. Division by zero also possible if Revenue = 0.

---

## MEDIUM Issues

### 13. Dashboard: Outstanding Invoices Include Drafts, Balance Sheet AR Excludes Drafts

**File:** `ArgoBooks/ViewModels/DashboardPageViewModel.cs` vs. `AccountingReportDataService.cs`
**Problem:** Dashboard outstanding invoices include Draft status invoices, but the Balance Sheet Accounts Receivable calculation excludes Drafts. Users see different "amounts owed" depending on which screen they look at.

**Impact:** Confusing and inconsistent financial picture across different views.

---

### 14. Dashboard: Net Profit Uses Math.Abs() Hiding Loss Sign

**File:** `ArgoBooks/ViewModels/DashboardPageViewModel.cs`
**Code:**
```csharp
NetProfitFormatted = CurrencyService.Format(Math.Abs(netProfitUSD));
```

**Problem:** `Math.Abs()` removes the negative sign from a net loss. The dashboard may show "Net Profit: $5,000" when the company actually has a $5,000 loss.

**Impact:** Users cannot tell if the company is profitable or losing money from the dashboard display.

---

### 15. Revenue/Expense Distribution Charts: Only First Line Item Categorized

**File:** `ArgoBooks.Core/Services/ReportChartDataService.cs`
**Code:**
```csharp
ProductId = s.LineItems.FirstOrDefault()?.ProductId
```

**Problem:** Multi-line-item transactions are categorized entirely by the first line item's product. A $10,000 transaction with 3 different products is attributed 100% to the first product.

**Impact:** Revenue/expense distribution by product is inaccurate for multi-item transactions.

---

### 16. Shipping Cost Charts Use Local Currency

**File:** `ArgoBooks.Core/Services/ReportChartDataService.cs`
**Code:** Uses `s.ShippingCost` instead of `ShippingCostUSD` or a USD-converted equivalent.

**Impact:** Shipping cost analysis mixes currencies, producing meaningless totals in multi-currency companies.

---

### 17. Return/Loss Financial Impact Charts Use Local Currency

**File:** `ArgoBooks.Core/Services/ReportChartDataService.cs`
**Problem:** Financial impact charts for returns and losses use local-currency amounts without USD conversion.

**Impact:** Financial impact analysis is inaccurate for multi-currency companies.

---

### 18. LostDamaged ValueLost Overstates Actual Loss

**File:** `ArgoBooks/ViewModels/RevenueModalsViewModel.cs`
**Code:**
```csharp
ValueLost = revenue.Total  // includes tax, shipping, fees
```

**Problem:** `revenue.Total` includes tax, shipping costs, and fees. The actual value lost should be the product cost or the pre-tax subtotal, not the full invoice amount including collected taxes and service fees.

**Impact:** Loss reports overstate the financial impact of damaged goods.

---

### 19. Return RefundAmount Overstates Actual Refund

**File:** `ArgoBooks/ViewModels/RevenueModalsViewModel.cs`
**Code:**
```csharp
RefundAmount = revenue.Total  // includes everything
```

**Problem:** Similar to #18, the default refund amount includes tax, shipping, and fees. While a full refund might include these, using the post-tax total with all fees as the default is likely to overstate typical refunds.

**Impact:** Return/refund reports may overstate refund obligations.

---

### 20. Top Products/Customers/Suppliers Analysis Uses Local Currency

**File:** `ArgoBooks.Core/Services/ReportTableDataService.cs`
**Problem:** Top-N analysis for products, customers, and suppliers ranks by local-currency amounts. A customer with 1,000,000 JPY in purchases (~$7,000) would rank above a customer with $50,000 in purchases.

**Impact:** Rankings are dominated by high-denomination currencies rather than actual business value.

---

### 21. Spreadsheet Import: No Validation That Balance = Total - AmountPaid

**File:** `ArgoBooks.Core/Services/SpreadsheetImportService.cs`
**Problem:** Imported invoices accept Balance, Total, and AmountPaid independently without validating consistency. Inconsistent imported data will propagate through all reports.

**Impact:** Imported data may have internal inconsistencies that corrupt financial reports.

---

## LOW Issues

### 22. ModalTaxRate Naming Confusion

**File:** `ArgoBooks/ViewModels/TransactionModalsViewModelBase.cs`, line 319
**Code:**
```csharp
public decimal TaxAmount => ModalTaxRate;  // ModalTaxRate actually stores dollar amount
```

**Problem:** `ModalTaxRate` stores a tax dollar amount, not a rate. It's populated from `transaction.TaxAmount`. While functionally correct, this naming confusion increases maintenance risk — a future developer may "fix" this by treating it as an actual rate.

**Impact:** Maintenance hazard; no current calculation bug.

---

### 23. InsightsService: Inconsistent PercentageChange Semantics

**File:** `ArgoBooks.Core/Services/InsightsService.cs`
**Problem:** The `PercentageChange` field in insight items sometimes means "percentage change" (calculated via `CalculatePercentChange`), sometimes means "absolute difference in percentage points" (return rate: `currentReturnRate - historicalReturnRate`), and sometimes means "a raw percentage metric" (seasonal strength * 100).

**Impact:** Consumers of insight data may misinterpret the meaning of PercentageChange values.

---

### 24. TaxRate Format Inconsistency Between Models

**Problem:**
- `Transaction.TaxRate`: Stored as percentage (8 for 8%)
- `LineItem.TaxRate`: Stored as decimal (0.08 for 8%)
- `Product.TaxRate`: Documented as decimal (0.08 for 8%)
- `Invoice.TaxRate`: Treated as percentage in ViewModel (`TaxRate / 100m`)

**Impact:** Any code that processes tax rates must know which model it came from to interpret the value correctly. This inconsistency invites bugs in any new feature that works with tax rates.

---

## Summary

| Severity | Count | Key Theme |
|----------|-------|-----------|
| CRITICAL | 6 | Currency mixing, incorrect USD conversions, tax display |
| HIGH | 6 | Inconsistent USD usage, missing conversions, wrong reconciliation |
| MEDIUM | 9 | Inconsistent views, local currency in charts, overstated amounts |
| LOW | 3 | Naming confusion, format inconsistencies |
| **Total** | **24** | |

### Most Impactful Patterns

1. **Currency mixing is the #1 systemic risk.** The `> 0 ? USD : local` fallback pattern appears in Transaction, Invoice, and Payment models. It silently produces wrong numbers whenever a USD field is legitimately 0.

2. **No single source of truth for "USD amount."** Some code paths use `EffectiveTotalUSD`, others use `EffectiveSubtotalUSD`, others use raw `Total` or `Subtotal`. The inconsistency means different reports show different numbers for the same transactions.

3. **Pre-tax vs. post-tax inconsistency.** Income Statement and Cash Flow use pre-tax amounts; Balance Sheet uses post-tax amounts. These should be reconcilable but currently are not.

4. **ReportTableDataService is entirely local-currency.** While chart and dashboard services use USD-converted amounts, the table/statistics service uses local currency throughout, making it unreliable for multi-currency companies.
