# Calculation Standards

This document is the single source of truth for how money is counted across the app: revenue, expenses, profit, tax, refunds, invoices, payments. Every dashboard stat card, chart, report, and analytics figure should follow these rules so that numbers agree wherever the user looks.

If you find a calculation that disagrees with this document, the calculation is wrong — fix the code, not the doc. If a new requirement genuinely needs a different rule, update this doc first, then update the code.

---

## 1. Vocabulary

| Term | Meaning |
|---|---|
| **Invoice** | A bill the business issued to a customer. Has a status (Draft, Sent, Paid, etc.) and a balance owed. |
| **Revenue** | A row representing money the business earned. Linked to an invoice when one exists. Has a `PaymentStatus`. |
| **Expense** | A row representing money the business spent. Linked to a supplier. |
| **Payment** | A row representing a single money movement, linked to either an invoice or a revenue. Positive = money in. Negative + `IsRefund=true` = money out. |
| **Refund** | A `Payment` row with `IsRefund=true` and a negative amount, tied to the invoice (and usually the original payment) it offsets. |
| **Subtotal** | Pre-tax amount. What stayed in the business if we set tax aside. |
| **Tax amount** | The sales tax portion. Money collected on behalf of the government, not the business. |
| **Total** | Subtotal + tax + fees + shipping − discount. The grand total the customer was charged or the business paid. |

---

## 2. The two foundational rules

These are the two rules that resolve almost every disagreement between surfaces of the app.

### Rule 1 — Revenue display is **gross** (includes tax); profit is **pre-tax**.

Tax we collect from customers is money we owe the government, not money we keep. So:

- **"Total Revenue" stat card / charts / customer billings** → use `EffectiveTotalUSD` (gross, includes tax). This is what the user typed on the invoice and matches the Revenue page.
- **"Net Profit" / "Profit Margin" / "Profit Over Time"** → use `EffectiveSubtotalUSD` (pre-tax) on the revenue side. This excludes the tax we owe out.
- **Expenses** always use `EffectiveTotalUSD` (gross). Tax we paid suppliers is real cash out the door, not a separate liability we can ignore.

```
Total Revenue (display)  = Σ Revenue.EffectiveTotalUSD
Net Profit               = Σ Revenue.EffectiveSubtotalUSD − Σ Expense.EffectiveTotalUSD − Refunds(pre-tax)
Profit Margin            = Net Profit / Σ Revenue.EffectiveSubtotalUSD
Tax Owed to Government   = Σ Revenue.EffectiveTaxAmountUSD     (collected) − tax paid on expenses (if user is tracking it)
```

### Rule 2 — Dashboard figures are **cash-basis** (paid-only).

The dashboard and analytics charts answer "how is the business actually doing" — not "how much have we invoiced." Unpaid invoices are promises, not money.

- Every revenue aggregation on the dashboard or in `ReportChartDataService` must filter by `RevenueAggregator.IsCollected` first. That keeps only rows whose `PaymentStatus` is `Paid` or `Complete`.
- Partial / Pending / Unpaid / Overdue revenues are **excluded** from dashboard sums.
- Outstanding-balance widgets (Outstanding Invoices, Overdue Invoices) are the exception — their whole purpose is to show what hasn't been paid.

Exceptions to Rule 2 — surfaces where unpaid revenue legitimately counts:
- The Revenue page list view (it lists all revenues regardless of status).
- "Customer Growth" and "Active vs Inactive Customers" — these count customer engagement, not money.
- Formal accounting reports (Income Statement, Balance Sheet) — see §10.

### Rule 3 — Aggregate in **USD**, display in the **user's currency**.

Every aggregation in `ArgoBooks.Core` is USD-normalized so multi-currency companies roll up correctly. But every number the user sees — stat cards, chart bars, chart titles, chart axes, tooltips, exports — must be in the **display currency** they chose in settings.

Conversion happens at the **presentation boundary**, not at the data layer:

- **Stat cards** call `CurrencyService.FormatFromUSD(usdAmount, date)` as the final step. Aggregations stay USD; only the display string converts.
- **Cartesian charts (LiveCharts)** — `ChartLoaderService.CreateDateTimeSeries` and `CreateProfitDateTimeSeries` auto-convert USD → display currency before handing values to LiveCharts (controlled by the `convertFromUSD` parameter, default `true`). Count-based callers pass `false`.
- **Pie charts** — `ChartLoaderService.CreatePieSeriesWithLegend` follows the same pattern; tooltips and legend `Value` are in display currency.
- **Chart titles** sum the USD values and call `FormatFromUSD` on the sum.
- **Y-axis labels and tooltips** receive display-currency values, so the default `{currencySymbol}{value:N2}` labeler is correct as-is.
- **Chart exports** (`ChartExportData.Values`) are stored in display currency so spreadsheet / PDF exports match what the chart shows.

Symptoms of breaking this rule: stat card shows `$91`, chart bar shows `$66.46`, chart title shows `$81`. All three are derived from the same underlying revenue row — they only disagree when display conversion is applied unevenly.

The helper for any new chart surface: `ChartLoaderService.ConvertUSDValuesToDisplay(double[])`.

---

## 3. Effective USD properties

Every aggregation across multi-currency data uses the `Effective*USD` properties on `Transaction`, `Invoice`, and `Payment`. Do not sum native fields (`Total`, `Amount`, `TaxAmount`) directly — they're in the original currency and silently mix dollars and euros.

| Property | Meaning |
|---|---|
| `EffectiveTotalUSD` | Gross-of-tax USD. Use for revenue display & expense sums. |
| `EffectiveSubtotalUSD` | Pre-tax USD. Use for profit revenue side. (= Total − Tax) |
| `EffectiveTaxAmountUSD` | Tax portion USD. Use for sales-tax-owed reporting. |
| `EffectiveShippingCostUSD` | Shipping USD. Already inside Total. |
| `EffectiveAmountUSD` (`Payment`) | Single payment/refund USD. Negative for refunds. |

`IsPendingConversion = true` means the row was saved offline and never got a USD conversion. All `Effective*USD` properties return `0` in that state to prevent cross-currency contamination — those rows are invisible to aggregations until the user re-enters them with a rate.

---

## 4. Invoice math (per-invoice)

How an invoice's grand total is built from its parts. The Invoice model stores the final numbers; this is how they're derived.

```
1. lineItemsSubtotal  = Σ over LineItem of (Quantity × UnitPrice − Discount)
2. invoiceDiscount    = DiscountIsPercent ? lineItemsSubtotal × DiscountAmount/100 : DiscountAmount
3. invoiceCustomFee   = CustomFeeIsPercent ? lineItemsSubtotal × CustomFeeAmount/100 : CustomFeeAmount
4. Subtotal           = lineItemsSubtotal − invoiceDiscount + invoiceCustomFee
5. TaxAmount          = Subtotal × TaxRate
6. Total              = Subtotal + TaxAmount + SecurityDeposit
```

Notes:
- **Discount on a line item** reduces only that line's subtotal.
- **Invoice-level discount** reduces the whole subtotal before tax.
- **Custom fee** is added to subtotal before tax (so it's taxable).
- **Security deposit** is added to the total but is *not* taxed and is *not* considered revenue earned — it's a refundable hold against damages. If the deposit is forfeited, it should be moved into revenue separately.
- **Tax** is applied to the post-discount, post-fee subtotal. Argo Books treats tax as a single flat rate on the invoice subtotal. We do not support different tax rates per line item at the invoice-roll-up level (line items each carry a `TaxRate` but the invoice header rate is what's stored as the final tax).

---

## 5. Payment math (per-invoice running totals)

These four fields on `Invoice` are kept in sync from the `Payment` rows attached to that invoice. `InvoiceTotalsService` owns this calculation — anything that mutates an invoice's payment list must call `InvoiceTotalsService.Recalculate(invoice, allPayments)` after the change.

A payment can instead be linked to a revenue (`Payment.RevenueId`) rather than an invoice — used for direct cash sales that have no invoice. Revenue-linked payments do not feed invoice totals (there is no invoice) and are not summed into revenue or profit (income is counted from the Revenue rows themselves, per §2). They are informational records of the money movement.

| Field | Formula | Notes |
|---|---|---|
| `AmountPaid` | Σ over positive (non-refund) Payments of `Amount` (in invoice's currency) | Gross payments received. Does not decrease on refund. |
| `AmountRefunded` | Σ over refund Payments of `|Amount|` (in invoice's currency) | Always ≥ 0. |
| `Balance` | `max(0, Total − AmountPaid)` | What the customer still owes. Refunds do not raise the balance — the customer paid; they don't owe again because we returned money. |
| `BalanceUSD` | `max(0, TotalUSD − sumOfPaymentsUSD)` | USD-normalized balance for cross-currency aggregation. |
| `NetPaid` *(computed)* | `AmountPaid − AmountRefunded` | Cash kept from this invoice. Used in revenue/profit aggregation. |

`InvoiceTotalsService.RecalculateStatus` separately recomputes the stored `Status` from `AmountPaid` / `AmountRefunded`, flipping between Paid / Partial / PartiallyRefunded / Refunded as appropriate. Lifecycle states (Draft / Pending / Sent / Viewed / Cancelled / Overdue) are owned by the surfaces that drive them and are not overwritten by this service.

A one-time recalc pass runs on `CompanyData` load (`CompanyManager.OpenCompanyAsync`) to heal any historic drift, scoped to invoices that actually have Payment rows — invoices imported from spreadsheets without payments keep the stored `AmountPaid` value the import gave them.

### `Payment.Amount` is the gross money movement and never changes after creation

A `Payment` row records a single money movement: a positive `Amount` when money came in, a negative `Amount` (with `IsRefund=true`) when money went out. Once written, neither field is mutated — a refund is a **separate row**, not a downward edit of the original payment.

This matters for any surface that shows individual payments:

- The **Payments page list** displays `Payment.Amount` (the original gross, including any processing fee the customer absorbed). It does **not** show a net "remaining" figure. Refund context is conveyed by the **Status** column flipping to "Refunded" / "Partially Refunded". Refund rows themselves are hidden from the list so each transaction appears once.
- Net-of-refund figures are still available where they're needed — `Invoice.NetPaid`, `RefundAggregator.GetRefundedForPayment(...)`, and the dashboard stats subtract refunds from gross at aggregation time. But the per-row Amount column always shows the gross.

If you find code that subtracts refunds from a `Payment.Amount` before displaying it, that's a bug — netting belongs in aggregations, not in row data.

---

## 6. Invoice status — what each one means and when it applies

| Status | Meaning | How it's set |
|---|---|---|
| `Draft` | Being prepared; never sent. | User saves without sending. |
| `Pending` | Ready but not sent yet. | User explicitly chose to hold off. |
| `Sent` | Sent to customer, awaiting payment. | After send action. |
| `Viewed` | Recipient opened it. | Portal tracking signal. |
| `Partial` | Customer paid some, owes more. | `0 < AmountPaid < Total`. |
| `Paid` | `AmountPaid >= Total`. | First positive payment that closes the balance. |
| `Overdue` | Today is past `DueDate` and not fully paid / cancelled. | Derived (`IsOverdue` getter), not a stored transition. |
| `Cancelled` | Invoice voided. | Explicit user action. |
| `PartiallyRefunded` | Was paid, then some (but not all) was refunded — OR was fully refunded and then paid again. | See refund rule below. |
| `Refunded` | Was paid, then fully refunded with no subsequent payment. | See refund rule below. |

**Refund status rule.** `AmountRefunded vs Total` alone isn't enough, because two scenarios both produce `AmountPaid > Total`:

1. *Single payment + processing fee, fully refunded.* AmountPaid = \$103 (\$100 invoice + \$3 fee customer absorbed), AmountRefunded = \$100 (refunds don't include the fee). Status should be `Refunded`.
2. *Pay → refund → pay again.* AmountPaid = \$200, AmountRefunded = \$100. Status should be `PartiallyRefunded` so the refund history stays visible alongside the new payment.

The discriminator is **net paid** (`AmountPaid − AmountRefunded`). If it is less than one full invoice value (`Total`), the gap is fee residue — status is `Refunded`. If it is at least one full invoice value, the customer paid the invoice over again on top of the refund — status is `PartiallyRefunded`.

Owned by `InvoiceTotalsService.RecalculateStatus`.

Heuristics:
- `Overdue` is a *display state*, not a write. Always compute it from `DueDate` and `Status != Paid/Cancelled` rather than storing it.
- Refund statuses (`PartiallyRefunded` / `Refunded`) supersede `Paid` as soon as a refund row exists for the invoice. The display layer self-heals using `AmountRefunded` vs `Total` rather than relying on the stored status — if the stored status disagrees, the computed one wins.

---

## 7. Revenue `PaymentStatus` — the cash-basis filter

`Revenue.PaymentStatus` is the `RevenuePaymentStatus` enum (`ArgoBooks.Core.Enums.RevenuePaymentStatus`). A permissive `JsonConverter` handles legacy `.argo` files — typos, alternate spellings ("settled", "received"), and empty strings all deserialize to `Paid`.

| Enum value | Treated as collected? |
|---|---|
| `Paid` | ✅ Yes (default for new rows) |
| `Complete` | ✅ Yes (legacy alias from older imports) |
| `Partial` | ❌ No |
| `Pending` | ❌ No |
| `Unpaid` | ❌ No |
| `Overdue` | ❌ No |

Use `RevenueAggregator.IsCollected(revenue)` everywhere — never inline the enum comparison. If a new status should count as collected, add it to that helper in one place. The spreadsheet importer's `NormalizePaymentStatus` maps free-form text to the enum and is the only place that should parse strings.

`Payment.Source` is similarly the `PaymentSource` enum (`Manual` / `Online`), also with a permissive JSON converter for legacy data.

---

## 8. Refunds

A refund is a `Payment` row with `IsRefund = true` and a negative `Amount`. It is dated on the day the refund was issued, not the day of the original payment — this matters for cash-basis charts.

### Effect on revenue (gross, display)

Subtract the **full refund amount** from gross revenue:

```
Revenue in period = Σ Revenue.EffectiveTotalUSD (paid-only, in date range)
                  − Σ |Payment.EffectiveAmountUSD| for refunds in date range
```

Helper: `RefundAggregator.GetRefundedInDateRangeUSD(...)`.

### Effect on profit (pre-tax)

Subtract the **pre-tax portion** of the refund. Because a refund both reverses revenue *and* reverses the tax we owed on that revenue, the net profit impact is only the subtotal portion:

```
Per refund:   profit reduction = |Payment.EffectiveAmountUSD| × (Invoice.Subtotal / Invoice.Total)
Fallback:     if invoice link missing → full refund amount
```

Helper: `RefundAggregator.GetRefundedPreTaxInDateRangeUSD(payments, invoicesById, start, end)`. `ProfitCalculator` already calls this; new profit surfaces should reuse `ProfitCalculator.CalculateNetProfitUSD` or `CalculateNetProfitByDayUSD` rather than re-deriving the formula.

Worked example:
- Invoice: \$86.91 subtotal + \$32.09 tax = \$119 total.
- Customer pays \$119; profit picks up \$86.91; we owe gov \$32.09.
- Full refund of \$119 issued.
- Profit reduces by \$86.91 (not \$119) — the \$32.09 tax was never ours.
- Tax owed simultaneously drops by \$32.09 (we owe nothing on a refunded transaction). Net profit impact on this transaction: \$0. Correct.

### Effect on invoice status

See §6. The status flips to `PartiallyRefunded` or `Refunded` based on `AmountRefunded` vs `Total`.

---

## 9. Expenses

Expenses are simpler than revenue because there's no "paid vs unpaid" distinction — an expense row means money already went out.

- All expense aggregations use `EffectiveTotalUSD`. The full amount paid to the supplier (including any tax paid) is the expense.
- The "Expenses" stat card, "Expenses Over Time" chart, "Revenue vs Expenses" chart — all use the same Total figure.
- Sales tax paid on expenses *can* in principle be deducted from sales tax collected, but Argo Books does **not** net these automatically. The user reports total tax collected; tax deductions are a manual / advisor conversation, in keeping with the "simple bookkeeping, not accounting" philosophy.

---

## 10. Where these rules don't apply: formal accounting reports

`AccountingReportDataService` and `ReportTableDataService` power the Income Statement, Balance Sheet, General Ledger, and similar formal reports. They intentionally diverge from the dashboard rules:

- They use `EffectiveSubtotalUSD` for revenue **and** expense, because formal reports break tax out as a separate liability line.
- They include all invoiced revenue, not just collected — formal reports typically follow accrual basis. (If the user toggles cash-basis on a report, the report layer applies the paid-only filter itself.)
- `GetCashFlowData` is the exception inside this service: it correctly uses paid-only revenue plus all Payment cash events plus all Expense rows (which are themselves cash-out events — there is no `PaymentStatus` on expenses).

If you find yourself touching these services, do not "fix" them to match the dashboard — they are correct for their context.

### Insights tab (`InsightsService`)

The Insights tab — trends, anomalies, forecasts, recommendations — follows the **dashboard** rules: `IsCollected` filter + `EffectiveTotalUSD` for revenue figures shown in descriptions, `EffectiveTotalUSD` for expenses. Forecast inputs use the same cash-basis signal so projections match the Revenue stat card. Forecast profit is computed as `forecasted gross revenue − forecasted gross expenses`, an approximation of net profit; the exact net profit formula (§2 Rule 1) is not currently applied to forecasts because doing so would require two parallel revenue series (gross for display, pre-tax for profit).

### Returns and Losses

Returns (customer-returned items) and Losses (lost / damaged inventory) have their own charts but do **not** participate in the revenue / profit / expense pipeline directly. They surface in dedicated "Return Financial Impact" / "Loss Financial Impact" charts that sum `Return.RefundAmount` and `LostDamaged.ValueLost` respectively. The recorded *refund* (a Payment row) is what flows through the revenue / profit subtraction defined in §8.

### Bank matching

Bank Matching (`BankMatchingService`) is a non-financial reference layer: it imports bank statement lines and links them to existing Revenue and Expense rows to help the user verify their books. It only sets a "matched" flag (`BankMatched`) on records and never feeds revenue / expense / profit / tax aggregation, so none of the rules above are affected. It matches against Revenue and Expenses only — invoices and payments are excluded because invoiced sales are already represented by their Revenue row (§5). Matching compares the gross transacted amount (the actual cash movement), not a USD-normalized aggregate.

---

## 11. Quick-reference: where each number comes from

| Surface | Revenue field | Expense field | Paid-only filter? | Subtract refunds? |
|---|---|---|---|---|
| Total Revenue stat card | `EffectiveTotalUSD` | — | Yes | Full amount |
| Total Expenses stat card | — | `EffectiveTotalUSD` | n/a | n/a |
| Net Profit stat card | `EffectiveSubtotalUSD` | `EffectiveTotalUSD` | Yes | Pre-tax portion |
| Profit Margin (Analytics) | `EffectiveSubtotalUSD` | `EffectiveTotalUSD` | Yes | Pre-tax portion |
| Revenue Over Time | `EffectiveTotalUSD` | — | Yes | Full amount |
| Expenses Over Time | — | `EffectiveTotalUSD` | n/a | n/a |
| Profit Over Time | `EffectiveSubtotalUSD` | `EffectiveTotalUSD` | Yes | Pre-tax portion |
| Revenue vs Expenses | `EffectiveTotalUSD` | `EffectiveTotalUSD` | Yes (revenue side) | Full amount (revenue side) |
| Top Customers by Revenue | `EffectiveTotalUSD` | — | Yes | Full amount |
| Customer Lifetime Value | `EffectiveTotalUSD` | — | Yes | Full amount |
| Geographic / Country charts | `EffectiveTotalUSD` | `EffectiveTotalUSD` | Yes (revenue side) | (not applied — TODO) |
| Outstanding Invoices stat | `EffectiveBalanceUSD` | — | **No** (by design) | n/a |
| Overdue Invoices stat | `EffectiveBalanceUSD` | — | **No** (by design) | n/a |
| Revenue page list | `EffectiveTotalUSD` | — | **No** (shows all) | n/a |
| Income Statement / GL | `EffectiveSubtotalUSD` | `EffectiveSubtotalUSD` | Per-report setting | Per-report setting |

---

## 12. Implementation pointers

When writing or reviewing aggregation code:

**Filters and aggregators (`ArgoBooks.Core/Services/`):**
- `RevenueAggregator.IsCollected(revenue)` — paid-only filter; switch on the `RevenuePaymentStatus` enum, never on strings.
- `RevenueAggregator.SumCollectedRevenueUSD(revenues, start, end)` — gross-of-tax revenue, paid-only, USD.
- `RevenueAggregator.SumCollectedRevenuePreTaxUSD(revenues, start, end)` — pre-tax revenue, paid-only, USD (use for profit math, not display).
- `ExpenseAggregator.SumExpensesUSD(expenses, start, end)` — gross expenses, USD.
- `RefundAggregator.GetRefundedInDateRangeUSD(payments, start, end)` — gross refund sum.
- `RefundAggregator.GetRefundedPreTaxInDateRangeUSD(payments, invoicesById, start, end)` — pre-tax refund portion for profit math.
- `RefundAggregator.GroupRefundsByDayUSD(payments, start, end)` — per-day refund map for time-series charts.
- `ProfitCalculator.CalculateNetProfitUSD(data, start, end)` — the cross-cutting net profit formula. Use this; don't re-derive.
- `ProfitCalculator.CalculateNetProfitByDayUSD(data, start, end)` — per-day profit for charts.
- `InvoiceTotalsService.Recalculate(invoice, allPayments)` — call after any mutation to an invoice's payment list.

**Display helpers (`ArgoBooks/Services/`):**
- `CurrencyService.FormatFromUSD(amountUSD, date)` — USD → display currency string. Use as the last step before binding to a UI text.
- `CurrencyService.GetDisplayAmount(amountUSD, date)` — USD → display currency decimal (when you need the number, not the string).
- `ChartLoaderService.ConvertUSDValuesToDisplay(double[])` — bulk variant for chart values.

**Rules of thumb:**
- For profit, sum `EffectiveSubtotalUSD` on revenue. For everything else revenue-side, sum `EffectiveTotalUSD`.
- All aggregations stay in USD. Convert to display currency only at the presentation boundary (stat card binding, chart series construction).
- For any new chart or stat card, look at the table in §11 to decide which column you fall under, and copy the pattern from a neighbor that is already correct.
- If you can't find an answer here, the rule does not exist yet — propose one before writing the code.
