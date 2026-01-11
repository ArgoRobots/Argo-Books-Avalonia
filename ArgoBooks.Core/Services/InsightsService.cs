using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Insights;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for generating business insights using local statistical analysis.
/// Uses deterministic algorithms (no external AI) for trend detection, anomaly detection,
/// forecasting, and recommendations.
/// </summary>
public class InsightsService : IInsightsService
{
    // Minimum data requirements
    private const int MinimumTransactionsForInsights = 5;
    private const int MinimumDaysForTrends = 14;
    private const int MinimumMonthsForForecasting = 2;

    // Anomaly detection thresholds
    private const double ZScoreThreshold = 2.0; // Standard deviations for anomaly
    private const decimal SignificantChangePercent = 15.0m; // % change to flag as significant

    /// <inheritdoc />
    public async Task<InsightsData> GenerateInsightsAsync(CompanyData companyData, AnalysisDateRange dateRange)
    {
        return await Task.Run(() =>
        {
            var insights = new InsightsData
            {
                GeneratedAt = DateTime.Now
            };

            // Check for sufficient data
            var dataCheck = CheckDataSufficiency(companyData, dateRange);
            if (!dataCheck.HasSufficientData)
            {
                insights.HasSufficientData = false;
                insights.InsufficientDataMessage = dataCheck.Message;
                return insights;
            }

            insights.HasSufficientData = true;

            // Generate all insight types
            insights.RevenueTrends = AnalyzeTrends(companyData, dateRange);
            insights.Anomalies = DetectAnomalies(companyData, dateRange);
            insights.Forecasts = GenerateForecastInsights(companyData, dateRange);
            insights.Recommendations = GenerateRecommendations(companyData, dateRange);
            insights.Forecast = GenerateForecast(companyData, dateRange);

            // Calculate summary
            insights.Summary = new InsightsSummary
            {
                TotalInsights = insights.RevenueTrends.Count + insights.Anomalies.Count +
                               insights.Forecasts.Count + insights.Recommendations.Count,
                TrendsDetected = insights.RevenueTrends.Count,
                AnomaliesDetected = insights.Anomalies.Count,
                Opportunities = insights.Recommendations.Count,
                MonthsOfData = dataCheck.MonthsOfData
            };

            return insights;
        });
    }

    /// <inheritdoc />
    public async Task<ForecastData> GenerateForecastAsync(CompanyData companyData, AnalysisDateRange dateRange)
    {
        return await Task.Run(() => GenerateForecast(companyData, dateRange));
    }

    /// <inheritdoc />
    public async Task<List<InsightItem>> DetectAnomaliesAsync(CompanyData companyData, AnalysisDateRange dateRange)
    {
        return await Task.Run(() => DetectAnomalies(companyData, dateRange));
    }

    /// <inheritdoc />
    public async Task<List<InsightItem>> AnalyzeTrendsAsync(CompanyData companyData, AnalysisDateRange dateRange)
    {
        return await Task.Run(() => AnalyzeTrends(companyData, dateRange));
    }

    /// <inheritdoc />
    public async Task<List<InsightItem>> GenerateRecommendationsAsync(CompanyData companyData, AnalysisDateRange dateRange)
    {
        return await Task.Run(() => GenerateRecommendations(companyData, dateRange));
    }

    #region Data Sufficiency Check

    private (bool HasSufficientData, string? Message, int MonthsOfData) CheckDataSufficiency(
        CompanyData companyData, AnalysisDateRange dateRange)
    {
        var sales = companyData.Sales.Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate).ToList();
        var purchases = companyData.Purchases.Where(p => p.Date >= dateRange.StartDate && p.Date <= dateRange.EndDate).ToList();

        var totalTransactions = sales.Count + purchases.Count;

        if (totalTransactions < MinimumTransactionsForInsights)
        {
            return (false, $"Need at least {MinimumTransactionsForInsights} transactions for meaningful insights. Currently have {totalTransactions}.", 0);
        }

        // Calculate months of data
        var allDates = sales.Select(s => s.Date)
            .Concat(purchases.Select(p => p.Date))
            .ToList();

        if (!allDates.Any())
        {
            return (false, "No transaction data available for the selected period.", 0);
        }

        var minDate = allDates.Min();
        var maxDate = allDates.Max();
        var monthsOfData = ((maxDate.Year - minDate.Year) * 12) + maxDate.Month - minDate.Month + 1;

        return (true, null, monthsOfData);
    }

    #endregion

    #region Trend Analysis

    private List<InsightItem> AnalyzeTrends(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var insights = new List<InsightItem>();

        // Get current and previous period data
        var previousPeriod = dateRange.GetPreviousPeriod();

        var currentSales = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .ToList();

        var previousSales = companyData.Sales
            .Where(s => s.Date >= previousPeriod.StartDate && s.Date <= previousPeriod.EndDate)
            .ToList();

        var currentPurchases = companyData.Purchases
            .Where(p => p.Date >= dateRange.StartDate && p.Date <= dateRange.EndDate)
            .ToList();

        var previousPurchases = companyData.Purchases
            .Where(p => p.Date >= previousPeriod.StartDate && p.Date <= previousPeriod.EndDate)
            .ToList();

        // Revenue trend
        var currentRevenue = currentSales.Sum(s => s.EffectiveTotalUSD);
        var previousRevenue = previousSales.Sum(s => s.EffectiveTotalUSD);

        if (previousRevenue > 0)
        {
            var revenueChange = CalculatePercentChange(previousRevenue, currentRevenue);

            if (Math.Abs(revenueChange) >= SignificantChangePercent)
            {
                var isGrowth = revenueChange > 0;
                insights.Add(new InsightItem
                {
                    Title = isGrowth ? "Revenue Growth Detected" : "Revenue Decline Detected",
                    Description = $"Your revenue has {(isGrowth ? "increased" : "decreased")} by {Math.Abs(revenueChange):F1}% compared to the previous period ({FormatCurrency(previousRevenue)} → {FormatCurrency(currentRevenue)}).",
                    Recommendation = isGrowth
                        ? "Consider analyzing which products or services drove this growth to replicate success."
                        : "Review recent changes that may have impacted revenue and consider promotional strategies.",
                    Severity = isGrowth ? InsightSeverity.Success : InsightSeverity.Warning,
                    Category = InsightCategory.RevenueTrend,
                    MetricValue = currentRevenue,
                    PercentageChange = revenueChange
                });
            }
        }

        // Expense trend
        var currentExpenses = currentPurchases.Sum(p => p.EffectiveTotalUSD);
        var previousExpenses = previousPurchases.Sum(p => p.EffectiveTotalUSD);

        if (previousExpenses > 0)
        {
            var expenseChange = CalculatePercentChange(previousExpenses, currentExpenses);

            if (Math.Abs(expenseChange) >= SignificantChangePercent)
            {
                var isIncrease = expenseChange > 0;
                insights.Add(new InsightItem
                {
                    Title = isIncrease ? "Expense Increase Detected" : "Expense Reduction Achieved",
                    Description = $"Your expenses have {(isIncrease ? "increased" : "decreased")} by {Math.Abs(expenseChange):F1}% compared to the previous period ({FormatCurrency(previousExpenses)} → {FormatCurrency(currentExpenses)}).",
                    Recommendation = isIncrease
                        ? "Review expense categories to identify areas where costs can be optimized."
                        : "Good job on cost management! Document what strategies worked for future reference.",
                    Severity = isIncrease ? InsightSeverity.Warning : InsightSeverity.Success,
                    Category = InsightCategory.ExpenseTrend,
                    MetricValue = currentExpenses,
                    PercentageChange = expenseChange
                });
            }
        }

        // Day-of-week pattern analysis
        var dayOfWeekInsight = AnalyzeDayOfWeekPattern(currentSales);
        if (dayOfWeekInsight != null)
        {
            insights.Add(dayOfWeekInsight);
        }

        // Seasonal pattern (if we have enough months)
        var seasonalInsight = AnalyzeSeasonalPattern(companyData.Sales, dateRange);
        if (seasonalInsight != null)
        {
            insights.Add(seasonalInsight);
        }

        // Transaction volume trend
        var volumeTrendInsight = AnalyzeTransactionVolumeTrend(currentSales.Count, previousSales.Count);
        if (volumeTrendInsight != null)
        {
            insights.Add(volumeTrendInsight);
        }

        return insights;
    }

    private InsightItem? AnalyzeDayOfWeekPattern(List<Sale> sales)
    {
        if (sales.Count < 14) return null;

        var salesByDay = sales
            .GroupBy(s => s.Date.DayOfWeek)
            .Select(g => new { Day = g.Key, Total = g.Sum(s => s.EffectiveTotalUSD), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        if (!salesByDay.Any()) return null;

        var bestDay = salesByDay.First();
        var worstDay = salesByDay.Last();
        var averageDaily = salesByDay.Average(x => x.Total);

        if (bestDay.Total > averageDaily * 1.3m) // 30% above average
        {
            var percentAbove = ((bestDay.Total / averageDaily) - 1) * 100;
            return new InsightItem
            {
                Title = $"{bestDay.Day} Sales Performance",
                Description = $"{bestDay.Day}s generate {percentAbove:F0}% more revenue than average daily sales ({FormatCurrency(bestDay.Total)} vs {FormatCurrency(averageDaily)} average).",
                Recommendation = $"Consider running promotions or increasing staffing on {bestDay.Day}s to maximize this opportunity.",
                Severity = InsightSeverity.Info,
                Category = InsightCategory.RevenueTrend,
                PercentageChange = percentAbove
            };
        }

        return null;
    }

    private InsightItem? AnalyzeSeasonalPattern(List<Sale> allSales, AnalysisDateRange dateRange)
    {
        // Need at least 12 months of data for seasonal analysis
        var monthlyData = allSales
            .Where(s => s.Date >= DateTime.Now.AddMonths(-12))
            .GroupBy(s => s.Date.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(s => s.EffectiveTotalUSD) })
            .OrderByDescending(x => x.Total)
            .ToList();

        if (monthlyData.Count < 6) return null;

        var bestMonth = monthlyData.First();
        var averageMonthly = monthlyData.Average(x => x.Total);

        if (bestMonth.Total > averageMonthly * 1.25m) // 25% above average
        {
            var monthName = new DateTime(2024, bestMonth.Month, 1).ToString("MMMM");
            var percentAbove = ((bestMonth.Total / averageMonthly) - 1) * 100;

            return new InsightItem
            {
                Title = "Seasonal Pattern Identified",
                Description = $"Historical data shows {monthName} generates {percentAbove:F0}% more revenue than average months.",
                Recommendation = $"Plan inventory and marketing campaigns ahead of {monthName} to capitalize on this seasonal trend.",
                Severity = InsightSeverity.Info,
                Category = InsightCategory.RevenueTrend,
                PercentageChange = percentAbove
            };
        }

        return null;
    }

    private InsightItem? AnalyzeTransactionVolumeTrend(int currentCount, int previousCount)
    {
        if (previousCount == 0) return null;

        var volumeChange = CalculatePercentChange(previousCount, currentCount);

        if (Math.Abs(volumeChange) >= 20) // 20% change in volume
        {
            var isIncrease = volumeChange > 0;
            return new InsightItem
            {
                Title = isIncrease ? "Transaction Volume Increasing" : "Transaction Volume Declining",
                Description = $"Number of transactions has {(isIncrease ? "increased" : "decreased")} by {Math.Abs(volumeChange):F0}% ({previousCount} → {currentCount} transactions).",
                Recommendation = isIncrease
                    ? "Ensure operational capacity can handle increased demand."
                    : "Consider outreach campaigns to re-engage customers.",
                Severity = isIncrease ? InsightSeverity.Success : InsightSeverity.Warning,
                Category = InsightCategory.RevenueTrend,
                PercentageChange = volumeChange
            };
        }

        return null;
    }

    #endregion

    #region Anomaly Detection

    private List<InsightItem> DetectAnomalies(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var anomalies = new List<InsightItem>();

        // Expense spike detection
        var expenseAnomaly = DetectExpenseAnomalies(companyData, dateRange);
        if (expenseAnomaly != null)
        {
            anomalies.Add(expenseAnomaly);
        }

        // Return rate anomaly
        var returnAnomaly = DetectReturnRateAnomaly(companyData, dateRange);
        if (returnAnomaly != null)
        {
            anomalies.Add(returnAnomaly);
        }

        // Revenue anomaly (unusual drop or spike)
        var revenueAnomalies = DetectRevenueAnomalies(companyData, dateRange);
        anomalies.AddRange(revenueAnomalies);

        // Large single transaction anomaly
        var largeTransactionAnomaly = DetectLargeTransactionAnomaly(companyData, dateRange);
        if (largeTransactionAnomaly != null)
        {
            anomalies.Add(largeTransactionAnomaly);
        }

        return anomalies;
    }

    private InsightItem? DetectExpenseAnomalies(CompanyData companyData, AnalysisDateRange dateRange)
    {
        // Get weekly expense data for the past 12 weeks
        var twelveWeeksAgo = dateRange.EndDate.AddDays(-84);
        var weeklyExpenses = companyData.Purchases
            .Where(p => p.Date >= twelveWeeksAgo && p.Date <= dateRange.EndDate)
            .GroupBy(p => GetWeekNumber(p.Date))
            .Select(g => g.Sum(p => p.EffectiveTotalUSD))
            .ToList();

        if (weeklyExpenses.Count < 4) return null;

        var currentWeekExpenses = companyData.Purchases
            .Where(p => p.Date >= dateRange.EndDate.AddDays(-7) && p.Date <= dateRange.EndDate)
            .Sum(p => p.EffectiveTotalUSD);

        var stats = CalculateStatistics(weeklyExpenses.Select(x => (double)x).ToList());

        if (stats.StandardDeviation > 0)
        {
            var zScore = ((double)currentWeekExpenses - stats.Mean) / stats.StandardDeviation;

            if (zScore > ZScoreThreshold)
            {
                var percentAbove = ((currentWeekExpenses / (decimal)stats.Mean) - 1) * 100;
                return new InsightItem
                {
                    Title = "Unusual Expense Spike Detected",
                    Description = $"This week's expenses ({FormatCurrency(currentWeekExpenses)}) are {percentAbove:F0}% above your typical weekly average ({FormatCurrency((decimal)stats.Mean)}).",
                    Recommendation = "Review recent expense entries for any errors, unexpected costs, or one-time purchases.",
                    Severity = InsightSeverity.Warning,
                    Category = InsightCategory.Anomaly,
                    MetricValue = currentWeekExpenses,
                    PercentageChange = percentAbove
                };
            }
        }

        return null;
    }

    private InsightItem? DetectReturnRateAnomaly(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var currentReturns = companyData.Returns
            .Where(r => r.ReturnDate >= dateRange.StartDate && r.ReturnDate <= dateRange.EndDate)
            .ToList();

        var currentSales = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .ToList();

        if (currentSales.Count < 10) return null;

        var currentReturnRate = currentSales.Count > 0
            ? (decimal)currentReturns.Count / currentSales.Count * 100
            : 0;

        // Get historical return rate (past 6 months)
        var sixMonthsAgo = dateRange.StartDate.AddMonths(-6);
        var historicalReturns = companyData.Returns
            .Where(r => r.ReturnDate >= sixMonthsAgo && r.ReturnDate < dateRange.StartDate)
            .Count();

        var historicalSales = companyData.Sales
            .Where(s => s.Date >= sixMonthsAgo && s.Date < dateRange.StartDate)
            .Count();

        if (historicalSales < 10) return null;

        var historicalReturnRate = (decimal)historicalReturns / historicalSales * 100;

        if (currentReturnRate > historicalReturnRate + 3) // 3 percentage points higher
        {
            // Find most common return category
            var returnsByProduct = currentReturns
                .SelectMany(r => r.Items)
                .GroupBy(i => i.ProductId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var productInfo = returnsByProduct != null
                ? companyData.GetProduct(returnsByProduct.Key)
                : null;

            var categoryNote = productInfo != null
                ? $" Most returns are for: {productInfo.Name}."
                : "";

            return new InsightItem
            {
                Title = "Return Rate Above Normal",
                Description = $"Current return rate is {currentReturnRate:F1}% compared to historical average of {historicalReturnRate:F1}%.{categoryNote}",
                Recommendation = "Investigate product quality, description accuracy, or shipping issues for affected items.",
                Severity = InsightSeverity.Warning,
                Category = InsightCategory.Anomaly,
                MetricValue = currentReturnRate,
                PercentageChange = currentReturnRate - historicalReturnRate
            };
        }

        return null;
    }

    private List<InsightItem> DetectRevenueAnomalies(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var anomalies = new List<InsightItem>();

        // Daily revenue analysis for short periods, weekly for longer
        var periodDays = dateRange.DayCount;
        var groupByWeek = periodDays > 30;

        var historicalStart = dateRange.StartDate.AddDays(-periodDays * 3); // 3x the period for baseline

        var historicalData = companyData.Sales
            .Where(s => s.Date >= historicalStart && s.Date < dateRange.StartDate)
            .GroupBy(s => groupByWeek ? GetWeekNumber(s.Date) : s.Date.DayOfYear)
            .Select(g => g.Sum(s => s.EffectiveTotalUSD))
            .ToList();

        if (historicalData.Count < 5) return anomalies;

        var stats = CalculateStatistics(historicalData.Select(x => (double)x).ToList());

        // Check current period data points
        var currentData = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .GroupBy(s => groupByWeek ? GetWeekNumber(s.Date) : s.Date.DayOfYear)
            .Select(g => new { Period = g.Key, Total = g.Sum(s => s.EffectiveTotalUSD) })
            .ToList();

        foreach (var period in currentData)
        {
            if (stats.StandardDeviation > 0)
            {
                var zScore = ((double)period.Total - stats.Mean) / stats.StandardDeviation;

                if (zScore < -ZScoreThreshold) // Unusually low
                {
                    var percentBelow = (1 - (period.Total / (decimal)stats.Mean)) * 100;
                    anomalies.Add(new InsightItem
                    {
                        Title = "Unusual Revenue Drop",
                        Description = $"Revenue for a recent period ({FormatCurrency(period.Total)}) was {percentBelow:F0}% below typical levels.",
                        Recommendation = "Check for any operational issues, competitor activity, or external factors that may have affected sales.",
                        Severity = InsightSeverity.Critical,
                        Category = InsightCategory.Anomaly,
                        MetricValue = period.Total,
                        PercentageChange = -percentBelow
                    });
                    break; // Only report one
                }
            }
        }

        return anomalies;
    }

    private InsightItem? DetectLargeTransactionAnomaly(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var currentSales = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .ToList();

        if (currentSales.Count < 5) return null;

        var stats = CalculateStatistics(currentSales.Select(s => (double)s.EffectiveTotalUSD).ToList());

        var largestSale = currentSales.OrderByDescending(s => s.EffectiveTotalUSD).First();

        if (stats.StandardDeviation > 0)
        {
            var zScore = ((double)largestSale.EffectiveTotalUSD - stats.Mean) / stats.StandardDeviation;

            if (zScore > 3.0) // Very unusual
            {
                var customer = companyData.GetCustomer(largestSale.CustomerId ?? "");
                var customerName = customer?.Name ?? "a customer";

                return new InsightItem
                {
                    Title = "Unusually Large Transaction",
                    Description = $"A sale of {FormatCurrency(largestSale.EffectiveTotalUSD)} to {customerName} on {largestSale.Date:MMM d} is significantly larger than your typical transaction size ({FormatCurrency((decimal)stats.Mean)}).",
                    Recommendation = "Verify this transaction is correct and consider nurturing this high-value customer relationship.",
                    Severity = InsightSeverity.Info,
                    Category = InsightCategory.Anomaly,
                    MetricValue = largestSale.EffectiveTotalUSD
                };
            }
        }

        return null;
    }

    #endregion

    #region Forecasting

    private ForecastData GenerateForecast(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var forecast = new ForecastData();

        // Get monthly data for forecasting
        var monthlyRevenue = GetMonthlyTotals(companyData.Sales, s => s.EffectiveTotalUSD);
        var monthlyExpenses = GetMonthlyTotals(companyData.Purchases, p => p.EffectiveTotalUSD);

        forecast.DataMonthsUsed = Math.Max(monthlyRevenue.Count, monthlyExpenses.Count);

        if (monthlyRevenue.Count >= MinimumMonthsForForecasting)
        {
            // Revenue forecast using linear regression with exponential smoothing fallback
            var revenueForecast = ForecastNextPeriod(monthlyRevenue);
            forecast.ForecastedRevenue = Math.Max(0, revenueForecast.Value);

            // Calculate growth
            var lastMonthRevenue = monthlyRevenue.LastOrDefault();
            if (lastMonthRevenue > 0)
            {
                forecast.RevenueGrowthPercent = CalculatePercentChange(lastMonthRevenue, forecast.ForecastedRevenue);
            }
        }

        if (monthlyExpenses.Count >= MinimumMonthsForForecasting)
        {
            var expenseForecast = ForecastNextPeriod(monthlyExpenses);
            forecast.ForecastedExpenses = Math.Max(0, expenseForecast.Value);

            var lastMonthExpenses = monthlyExpenses.LastOrDefault();
            if (lastMonthExpenses > 0)
            {
                forecast.ExpenseGrowthPercent = CalculatePercentChange(lastMonthExpenses, forecast.ForecastedExpenses);
            }
        }

        // Calculate profit forecast
        forecast.ForecastedProfit = forecast.ForecastedRevenue - forecast.ForecastedExpenses;

        // Calculate profit growth
        var currentProfit = monthlyRevenue.LastOrDefault() - monthlyExpenses.LastOrDefault();
        if (currentProfit != 0)
        {
            forecast.ProfitGrowthPercent = CalculatePercentChange(currentProfit, forecast.ForecastedProfit);
        }

        // Customer growth forecast
        var monthlyNewCustomers = GetMonthlyNewCustomers(companyData);
        if (monthlyNewCustomers.Count >= MinimumMonthsForForecasting)
        {
            var customerForecast = ForecastNextPeriod(monthlyNewCustomers.Select(x => (decimal)x).ToList());
            forecast.ExpectedNewCustomers = Math.Max(0, (int)Math.Round(customerForecast.Value));

            var lastMonthCustomers = monthlyNewCustomers.LastOrDefault();
            if (lastMonthCustomers > 0)
            {
                forecast.CustomerGrowthPercent = CalculatePercentChange(lastMonthCustomers, forecast.ExpectedNewCustomers);
            }
        }

        // Calculate confidence score
        forecast.ConfidenceScore = CalculateConfidenceScore(
            monthlyRevenue.Count,
            CalculateDataVariance(monthlyRevenue),
            monthlyExpenses.Count
        );

        forecast.ConfidenceLevel = forecast.ConfidenceScore switch
        {
            >= 80 => "High",
            >= 50 => "Medium",
            _ => "Low"
        };

        return forecast;
    }

    private List<InsightItem> GenerateForecastInsights(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var insights = new List<InsightItem>();
        var forecast = GenerateForecast(companyData, dateRange);

        // Revenue forecast insight
        if (forecast.ForecastedRevenue > 0)
        {
            var confidenceRange = forecast.ConfidenceScore >= 70 ? "±10%" : "±20%";
            var lowEstimate = forecast.ForecastedRevenue * (forecast.ConfidenceScore >= 70 ? 0.9m : 0.8m);
            var highEstimate = forecast.ForecastedRevenue * (forecast.ConfidenceScore >= 70 ? 1.1m : 1.2m);

            insights.Add(new InsightItem
            {
                Title = "Next Month Revenue Forecast",
                Description = $"Based on {forecast.DataMonthsUsed} months of historical data, expected revenue for next month is {FormatCurrency(lowEstimate)} - {FormatCurrency(highEstimate)} ({confidenceRange}).",
                Severity = InsightSeverity.Info,
                Category = InsightCategory.Forecast,
                MetricValue = forecast.ForecastedRevenue
            });
        }

        // Cash flow projection
        if (forecast.ForecastedProfit != 0)
        {
            var isPositive = forecast.ForecastedProfit > 0;
            insights.Add(new InsightItem
            {
                Title = "Cash Flow Projection",
                Description = $"Projected cash flow for the next 30 days is {(isPositive ? "positive" : "negative")}. Expected {(isPositive ? "surplus" : "shortfall")}: {FormatCurrency(Math.Abs(forecast.ForecastedProfit))}.",
                Severity = isPositive ? InsightSeverity.Success : InsightSeverity.Warning,
                Category = InsightCategory.Forecast,
                MetricValue = forecast.ForecastedProfit
            });
        }

        // Inventory depletion alert
        var inventoryAlerts = CheckInventoryDepletion(companyData, dateRange);
        if (inventoryAlerts.Count > 0)
        {
            insights.Add(new InsightItem
            {
                Title = "Inventory Depletion Alert",
                Description = $"At current sales velocity, {inventoryAlerts.Count} product(s) will reach reorder point within 2 weeks.",
                Recommendation = "Review and place orders for low-stock items: " + string.Join(", ", inventoryAlerts.Take(3)),
                Severity = InsightSeverity.Warning,
                Category = InsightCategory.Inventory,
                MetricValue = inventoryAlerts.Count
            });
        }

        return insights;
    }

    private (decimal Value, double Confidence) ForecastNextPeriod(List<decimal> monthlyData)
    {
        if (monthlyData.Count < 2)
        {
            return (monthlyData.FirstOrDefault(), 0);
        }

        // Use weighted combination of linear regression and exponential smoothing
        var linearForecast = LinearRegressionForecast(monthlyData);
        var expForecast = ExponentialSmoothingForecast(monthlyData, 0.3);

        // Weight more recent method higher when data is limited
        var weight = monthlyData.Count >= 6 ? 0.6 : 0.4;
        var combined = (linearForecast * (decimal)weight) + (expForecast * (decimal)(1 - weight));

        return (combined, CalculateDataVariance(monthlyData));
    }

    private decimal LinearRegressionForecast(List<decimal> data)
    {
        var n = data.Count;
        if (n < 2) return data.FirstOrDefault();

        // Simple linear regression: y = mx + b
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += (double)data[i];
            sumXY += i * (double)data[i];
            sumX2 += i * i;
        }

        var denominator = (n * sumX2 - sumX * sumX);
        if (Math.Abs(denominator) < 0.0001) return data.Last();

        var m = (n * sumXY - sumX * sumY) / denominator;
        var b = (sumY - m * sumX) / n;

        // Forecast next period (x = n)
        var forecast = m * n + b;
        return (decimal)Math.Max(0, forecast);
    }

    private decimal ExponentialSmoothingForecast(List<decimal> data, double alpha)
    {
        if (data.Count == 0) return 0;
        if (data.Count == 1) return data[0];

        var smoothed = (double)data[0];
        foreach (var value in data.Skip(1))
        {
            smoothed = alpha * (double)value + (1 - alpha) * smoothed;
        }

        return (decimal)smoothed;
    }

    private double CalculateConfidenceScore(int monthsOfRevenue, double variance, int monthsOfExpenses)
    {
        // Base score from data quantity
        var dataScore = Math.Min(40, (monthsOfRevenue + monthsOfExpenses) * 3);

        // Stability score from variance (lower variance = higher confidence)
        var stabilityScore = variance < 0.1 ? 40 : variance < 0.3 ? 30 : variance < 0.5 ? 20 : 10;

        // Recency bonus if we have recent data
        var recencyBonus = monthsOfRevenue >= 3 ? 20 : monthsOfRevenue >= 1 ? 10 : 0;

        return Math.Min(100, dataScore + stabilityScore + recencyBonus);
    }

    private double CalculateDataVariance(List<decimal> data)
    {
        if (data.Count < 2) return 1.0;

        var mean = data.Average();
        if (mean == 0) return 1.0;

        var variance = data.Select(x => Math.Pow((double)(x - mean), 2)).Average();
        var cv = Math.Sqrt(variance) / (double)mean; // Coefficient of variation

        return cv;
    }

    private List<string> CheckInventoryDepletion(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var alerts = new List<string>();

        // Calculate average daily sales per product over the last 30 days
        var thirtyDaysAgo = dateRange.EndDate.AddDays(-30);
        var recentSales = companyData.Sales
            .Where(s => s.Date >= thirtyDaysAgo && s.Date <= dateRange.EndDate)
            .SelectMany(s => s.LineItems)
            .GroupBy(li => li.ProductId)
            .Select(g => new { ProductId = g.Key, DailyVelocity = g.Sum(li => li.Quantity) / 30m })
            .ToList();

        foreach (var item in recentSales.Where(x => x.DailyVelocity > 0))
        {
            var inventory = companyData.Inventory.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (inventory != null && inventory.InStock > 0)
            {
                var daysUntilEmpty = inventory.InStock / item.DailyVelocity;
                if (daysUntilEmpty <= 14) // 2 weeks or less
                {
                    var product = companyData.GetProduct(item.ProductId ?? "");
                    if (product != null)
                    {
                        alerts.Add(product.Name);
                    }
                }
            }
        }

        return alerts;
    }

    #endregion

    #region Recommendations

    private List<InsightItem> GenerateRecommendations(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var recommendations = new List<InsightItem>();

        // Top performing product
        var topProductRec = AnalyzeTopProducts(companyData, dateRange);
        if (topProductRec != null)
        {
            recommendations.Add(topProductRec);
        }

        // Inactive customers
        var inactiveCustomerRec = AnalyzeInactiveCustomers(companyData, dateRange);
        if (inactiveCustomerRec != null)
        {
            recommendations.Add(inactiveCustomerRec);
        }

        // Overdue invoices
        var overdueRec = AnalyzeOverdueInvoices(companyData);
        if (overdueRec != null)
        {
            recommendations.Add(overdueRec);
        }

        // Supplier optimization
        var supplierRec = AnalyzeSupplierOptimization(companyData, dateRange);
        if (supplierRec != null)
        {
            recommendations.Add(supplierRec);
        }

        // Customer concentration risk
        var concentrationRec = AnalyzeCustomerConcentration(companyData, dateRange);
        if (concentrationRec != null)
        {
            recommendations.Add(concentrationRec);
        }

        // Profit margin analysis
        var marginRec = AnalyzeProfitMargins(companyData, dateRange);
        if (marginRec != null)
        {
            recommendations.Add(marginRec);
        }

        return recommendations;
    }

    private InsightItem? AnalyzeTopProducts(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var productSales = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .SelectMany(s => s.LineItems)
            .GroupBy(li => li.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Revenue = g.Sum(li => li.Amount),
                Cost = g.Sum(li => li.Quantity * (companyData.GetProduct(li.ProductId ?? "")?.CostPrice ?? 0)),
                Quantity = g.Sum(li => li.Quantity)
            })
            .ToList();

        if (!productSales.Any()) return null;

        var topProduct = productSales
            .Where(p => p.Cost > 0)
            .Select(p => new { p.ProductId, p.Revenue, Margin = (p.Revenue - p.Cost) / p.Revenue * 100 })
            .OrderByDescending(p => p.Margin)
            .FirstOrDefault();

        if (topProduct == null) return null;

        var product = companyData.GetProduct(topProduct.ProductId ?? "");
        if (product == null) return null;

        return new InsightItem
        {
            Title = "Top Performing Product",
            Description = $"\"{product.Name}\" has the highest profit margin at {topProduct.Margin:F0}%. Revenue this period: {FormatCurrency(topProduct.Revenue)}.",
            Recommendation = "Consider featuring this product more prominently in marketing or bundling it with other items.",
            Severity = InsightSeverity.Info,
            Category = InsightCategory.Product,
            MetricValue = topProduct.Revenue,
            PercentageChange = topProduct.Margin
        };
    }

    private InsightItem? AnalyzeInactiveCustomers(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var inactivityThreshold = 60; // days

        var lastPurchaseByCustomer = companyData.Sales
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, LastPurchase = g.Max(s => s.Date) })
            .ToList();

        var inactiveCustomers = lastPurchaseByCustomer
            .Where(c => (dateRange.EndDate - c.LastPurchase).TotalDays > inactivityThreshold)
            .ToList();

        // Only flag if they were previously active (had at least 2 purchases in the past)
        var previouslyActiveCount = 0;
        foreach (var inactive in inactiveCustomers)
        {
            var purchaseCount = companyData.Sales.Count(s => s.CustomerId == inactive.CustomerId);
            if (purchaseCount >= 2)
            {
                previouslyActiveCount++;
            }
        }

        if (previouslyActiveCount == 0) return null;

        return new InsightItem
        {
            Title = "Customer Retention Opportunity",
            Description = $"{previouslyActiveCount} previously active customer(s) haven't made a purchase in over {inactivityThreshold} days.",
            Recommendation = "Consider sending re-engagement emails, special offers, or conducting a satisfaction survey.",
            Severity = InsightSeverity.Info,
            Category = InsightCategory.Customer,
            MetricValue = previouslyActiveCount
        };
    }

    private InsightItem? AnalyzeOverdueInvoices(CompanyData companyData)
    {
        var overdueInvoices = companyData.Invoices
            .Where(i => i.IsOverdue && i.Balance > 0)
            .OrderByDescending(i => (DateTime.Today - i.DueDate).TotalDays)
            .ToList();

        if (!overdueInvoices.Any()) return null;

        var totalOverdue = overdueInvoices.Sum(i => i.EffectiveBalanceUSD);
        var oldestDaysOverdue = (int)(DateTime.Today - overdueInvoices.First().DueDate).TotalDays;

        return new InsightItem
        {
            Title = "Payment Collection Needed",
            Description = $"{overdueInvoices.Count} invoice(s) totaling {FormatCurrency(totalOverdue)} are overdue. Oldest is {oldestDaysOverdue} days past due.",
            Recommendation = "Send payment reminders and follow up with these customers to improve cash flow.",
            Severity = oldestDaysOverdue > 30 ? InsightSeverity.Warning : InsightSeverity.Info,
            Category = InsightCategory.Payment,
            MetricValue = totalOverdue
        };
    }

    private InsightItem? AnalyzeSupplierOptimization(CompanyData companyData, AnalysisDateRange dateRange)
    {
        // Analyze purchase patterns by supplier
        var supplierPurchases = companyData.Purchases
            .Where(p => p.Date >= dateRange.StartDate && p.Date <= dateRange.EndDate)
            .GroupBy(p => p.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                TotalSpent = g.Sum(p => p.EffectiveTotalUSD),
                Count = g.Count(),
                AvgPerPurchase = g.Average(p => p.EffectiveTotalUSD)
            })
            .OrderByDescending(s => s.TotalSpent)
            .ToList();

        if (supplierPurchases.Count < 2) return null;

        var topSupplier = supplierPurchases.First();
        var supplier = companyData.GetSupplier(topSupplier.SupplierId ?? "");

        if (supplier == null) return null;

        var concentrationPercent = supplierPurchases.Sum(s => s.TotalSpent) > 0
            ? topSupplier.TotalSpent / supplierPurchases.Sum(s => s.TotalSpent) * 100
            : 0;

        if (concentrationPercent > 60) // More than 60% with one supplier
        {
            return new InsightItem
            {
                Title = "Supplier Concentration Risk",
                Description = $"{concentrationPercent:F0}% of your purchases ({FormatCurrency(topSupplier.TotalSpent)}) are from {supplier.Name}.",
                Recommendation = "Consider diversifying suppliers to reduce risk and potentially negotiate better terms.",
                Severity = InsightSeverity.Info,
                Category = InsightCategory.Recommendation,
                PercentageChange = concentrationPercent
            };
        }

        return null;
    }

    private InsightItem? AnalyzeCustomerConcentration(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var customerRevenue = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Revenue = g.Sum(s => s.EffectiveTotalUSD) })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        if (customerRevenue.Count < 3) return null;

        var totalRevenue = customerRevenue.Sum(c => c.Revenue);
        if (totalRevenue == 0) return null;

        var topCustomer = customerRevenue.First();
        var concentrationPercent = topCustomer.Revenue / totalRevenue * 100;

        if (concentrationPercent > 40) // More than 40% from one customer
        {
            var customer = companyData.GetCustomer(topCustomer.CustomerId ?? "");
            var customerName = customer?.Name ?? "your top customer";

            return new InsightItem
            {
                Title = "Revenue Concentration Risk",
                Description = $"{concentrationPercent:F0}% of revenue comes from {customerName}. This creates business risk if that relationship changes.",
                Recommendation = "Work on diversifying your customer base through acquisition and marketing efforts.",
                Severity = InsightSeverity.Warning,
                Category = InsightCategory.Customer,
                PercentageChange = concentrationPercent
            };
        }

        return null;
    }

    private InsightItem? AnalyzeProfitMargins(CompanyData companyData, AnalysisDateRange dateRange)
    {
        var totalRevenue = companyData.Sales
            .Where(s => s.Date >= dateRange.StartDate && s.Date <= dateRange.EndDate)
            .Sum(s => s.EffectiveTotalUSD);

        var totalExpenses = companyData.Purchases
            .Where(p => p.Date >= dateRange.StartDate && p.Date <= dateRange.EndDate)
            .Sum(p => p.EffectiveTotalUSD);

        if (totalRevenue == 0) return null;

        var profitMargin = (totalRevenue - totalExpenses) / totalRevenue * 100;

        if (profitMargin < 10) // Low margin warning
        {
            return new InsightItem
            {
                Title = "Low Profit Margin Alert",
                Description = $"Your current profit margin is {profitMargin:F1}%. Industry benchmarks typically suggest 15-20% for healthy businesses.",
                Recommendation = "Review pricing strategy and look for cost reduction opportunities to improve profitability.",
                Severity = InsightSeverity.Warning,
                Category = InsightCategory.Recommendation,
                PercentageChange = profitMargin
            };
        }

        if (profitMargin > 30) // Good margin
        {
            return new InsightItem
            {
                Title = "Strong Profit Margins",
                Description = $"Your profit margin of {profitMargin:F1}% is excellent. You're maintaining healthy profitability.",
                Severity = InsightSeverity.Success,
                Category = InsightCategory.Recommendation,
                PercentageChange = profitMargin
            };
        }

        return null;
    }

    #endregion

    #region Helper Methods

    private List<decimal> GetMonthlyTotals<T>(List<T> transactions, Func<T, decimal> amountSelector)
        where T : Transaction
    {
        return transactions
            .Where(t => t.Date >= DateTime.Now.AddMonths(-12))
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => g.Sum(amountSelector))
            .ToList();
    }

    private List<int> GetMonthlyNewCustomers(CompanyData companyData)
    {
        var firstPurchaseByCustomer = companyData.Sales
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, FirstPurchase = g.Min(s => s.Date) })
            .ToList();

        return firstPurchaseByCustomer
            .Where(c => c.FirstPurchase >= DateTime.Now.AddMonths(-12))
            .GroupBy(c => new { c.FirstPurchase.Year, c.FirstPurchase.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => g.Count())
            .ToList();
    }

    private static decimal CalculatePercentChange(decimal oldValue, decimal newValue)
    {
        if (oldValue == 0) return newValue > 0 ? 100 : 0;
        return (newValue - oldValue) / Math.Abs(oldValue) * 100;
    }

    private static (double Mean, double StandardDeviation) CalculateStatistics(List<double> values)
    {
        if (!values.Any()) return (0, 0);

        var mean = values.Average();
        var variance = values.Select(x => Math.Pow(x - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        return (mean, stdDev);
    }

    private static int GetWeekNumber(DateTime date)
    {
        return date.Year * 100 + (date.DayOfYear / 7);
    }

    private static string FormatCurrency(decimal amount)
    {
        return amount.ToString("C0");
    }

    #endregion
}
