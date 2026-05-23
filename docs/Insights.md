# Insights & Forecasting

The Insights tab surfaces predictive analytics for your business: revenue and expense trends, anomalies, forecasts with confidence intervals, and rule-based recommendations. All analysis runs on-device. No cloud calls are made.

## What you'll see

**Trends**: changes in revenue, expenses, transaction volume, day-of-week patterns, and seasonal cycles. Surfaced when a metric crosses a meaningful threshold (typically 15–30%).

**Anomalies**: statistical outliers such as unusual spending spikes, revenue drops, return rate jumps, or unusually large individual transactions.

**Forecasts**: forward predictions of revenue, expenses, profit, and new customers for the upcoming period, each with a confidence interval and an overall confidence score.

**Recommendations**: rule-based suggestions.

- Top performing product (highest profit margin)
- Customer retention (active customers who've gone quiet for 60+ days)
- Payment collection (overdue invoices with an outstanding balance)
- Supplier concentration (one supplier accounts for >60% of spending)
- Customer concentration (one customer accounts for >40% of revenue)

## Forecasting

Forecasts use two on-device algorithms: Holt-Winters triple exponential smoothing for shorter histories, and ML.NET's SSA (Singular Spectrum Analysis) for richer histories. With enough data, both run and their results are blended.

The blend is **adaptive**: each method is weighted by how accurate it has historically been on your specific data. The longer you use Argo Books, the better-tuned your forecasts become.

### Data requirements

| Feature | Minimum data |
|---|---|
| Any insights at all | 5 transactions |
| Basic forecast (weighted-average fallback) | 2 months |
| Reliable forecast (Holt-Winters) | 12 months |
| Best-quality forecast (SSA) | 24 months |
| Seasonal pattern detection | 12 months |

Forecast horizons of Next Month, Next Quarter, Next Year, and rolling 30/90/365 day windows are all available, but confidence drops the further out you look.

### Gap-filling

Months with no transactions are interpolated linearly, never zero-filled. Zero-filling would create phantom seasonal patterns and conflate "no recorded activity" with "no business."

## Forecast accuracy

After each forecast period ends, Argo Books retroactively checks how accurate the forecast was and stores the result in your company file. This drives:

- A **trend indicator** in the Insights tab: Improving, Stable, or Declining.
- **Adaptive method weighting**: methods that have been more accurate for your data get more weight in future forecasts.
- The **confidence score** shown next to each forecast.

The confidence score combines four signals: data quantity, data stability, seasonal pattern strength, and historical accuracy. Historical accuracy carries the heaviest weight.

## Notable limitations

- **Monthly granularity only.** No daily or weekly forecasts.
- **Profit forecast is an approximation** of forecasted revenue minus forecasted expenses. The strict pre-tax-revenue formula from [Calculations](Calculations.md) is not applied to forecasts.
- **No caching.** Opening the Insights tab recomputes everything from current data, so it always reflects the latest entries.
