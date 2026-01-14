using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Insights;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for tracking and comparing forecast accuracy over time.
/// Stores historical forecasts and validates them against actual outcomes.
/// </summary>
public class ForecastAccuracyService : IForecastAccuracyService
{
    /// <inheritdoc />
    public void SaveForecast(CompanyData companyData, ForecastData forecast, AnalysisDateRange forecastPeriod)
    {
        // Check if we already have a forecast for this exact period
        var existingRecord = companyData.ForecastRecords.FirstOrDefault(r =>
            r.PeriodStartDate == forecastPeriod.StartDate &&
            r.PeriodEndDate == forecastPeriod.EndDate &&
            !r.IsValidated);

        if (existingRecord != null)
        {
            // Update existing unvalidated record
            existingRecord.ForecastedRevenue = forecast.ForecastedRevenue;
            existingRecord.ForecastedExpenses = forecast.ForecastedExpenses;
            existingRecord.ForecastedProfit = forecast.ForecastedProfit;
            existingRecord.ForecastedNewCustomers = forecast.ExpectedNewCustomers;
            existingRecord.ConfidenceScore = forecast.ConfidenceScore;
            existingRecord.ForecastDate = DateTime.Now;
        }
        else
        {
            // Create new record
            var record = new ForecastAccuracyRecord
            {
                ForecastDate = DateTime.Now,
                PeriodStartDate = forecastPeriod.StartDate,
                PeriodEndDate = forecastPeriod.EndDate,
                ForecastedRevenue = forecast.ForecastedRevenue,
                ForecastedExpenses = forecast.ForecastedExpenses,
                ForecastedProfit = forecast.ForecastedProfit,
                ForecastedNewCustomers = forecast.ExpectedNewCustomers,
                ConfidenceScore = forecast.ConfidenceScore,
                ForecastMethod = forecast.ForecastMethod ?? "Combined",
                IsValidated = false
            };

            companyData.ForecastRecords.Add(record);
        }

        companyData.MarkAsModified();
    }

    /// <inheritdoc />
    public void ValidatePastForecasts(CompanyData companyData)
    {
        var today = DateTime.Today;
        var unvalidatedRecords = companyData.ForecastRecords
            .Where(r => !r.IsValidated && r.PeriodEndDate < today)
            .ToList();

        foreach (var record in unvalidatedRecords)
        {
            // Calculate actual values for the forecast period
            var actualRevenue = companyData.Sales
                .Where(s => s.Date >= record.PeriodStartDate && s.Date <= record.PeriodEndDate)
                .Sum(s => s.EffectiveTotalUSD);

            var actualExpenses = companyData.Purchases
                .Where(p => p.Date >= record.PeriodStartDate && p.Date <= record.PeriodEndDate)
                .Sum(p => p.EffectiveTotalUSD);

            var actualProfit = actualRevenue - actualExpenses;

            // Calculate actual new customers (first purchase within the period)
            var firstPurchaseByCustomer = companyData.Sales
                .GroupBy(s => s.CustomerId)
                .Select(g => new { CustomerId = g.Key, FirstPurchase = g.Min(s => s.Date) })
                .ToList();

            var actualNewCustomers = firstPurchaseByCustomer
                .Count(c => c.FirstPurchase >= record.PeriodStartDate && c.FirstPurchase <= record.PeriodEndDate);

            // Update the record with actual values
            record.ActualRevenue = actualRevenue;
            record.ActualExpenses = actualExpenses;
            record.ActualProfit = actualProfit;
            record.ActualNewCustomers = actualNewCustomers;
            record.IsValidated = true;
        }

        if (unvalidatedRecords.Any())
        {
            companyData.MarkAsModified();
        }
    }

    /// <inheritdoc />
    public ForecastAccuracyData GetAccuracyData(CompanyData companyData)
    {
        // First, validate any past forecasts that haven't been validated yet
        ValidatePastForecasts(companyData);

        var accuracyData = new ForecastAccuracyData
        {
            HistoricalRecords = companyData.ForecastRecords
                .OrderByDescending(r => r.PeriodStartDate)
                .ToList()
        };

        accuracyData.CalculateStatistics();

        return accuracyData;
    }

    /// <inheritdoc />
    public (double RevenueAccuracy, double ExpenseAccuracy)? GetRecentAccuracy(CompanyData companyData, int recentCount = 6)
    {
        var validatedRecords = companyData.ForecastRecords
            .Where(r => r.IsValidated)
            .OrderByDescending(r => r.PeriodEndDate)
            .Take(recentCount)
            .ToList();

        if (validatedRecords.Count == 0)
            return null;

        var revenueAccuracies = validatedRecords
            .Where(r => r.RevenueAccuracyPercent.HasValue)
            .Select(r => r.RevenueAccuracyPercent!.Value)
            .ToList();

        var expenseAccuracies = validatedRecords
            .Where(r => r.ExpensesAccuracyPercent.HasValue)
            .Select(r => r.ExpensesAccuracyPercent!.Value)
            .ToList();

        if (!revenueAccuracies.Any() && !expenseAccuracies.Any())
            return null;

        return (
            revenueAccuracies.Any() ? revenueAccuracies.Average() : 0,
            expenseAccuracies.Any() ? expenseAccuracies.Average() : 0
        );
    }

    /// <inheritdoc />
    public string GetAccuracySummary(CompanyData companyData)
    {
        var recentAccuracy = GetRecentAccuracy(companyData);
        if (!recentAccuracy.HasValue)
        {
            return "No validated forecasts yet. Check back after the current forecast period ends.";
        }

        var validatedCount = companyData.ForecastRecords.Count(r => r.IsValidated);
        var avgAccuracy = (recentAccuracy.Value.RevenueAccuracy + recentAccuracy.Value.ExpenseAccuracy) / 2;
        var errorMargin = 100 - avgAccuracy;

        return $"Based on {validatedCount} validated forecast(s), predictions were within ±{errorMargin:F0}% of actual values on average.";
    }

    /// <inheritdoc />
    public void CleanupOldRecords(CompanyData companyData, int maxRecords = 24)
    {
        // Keep only the most recent records, validated ones get priority
        var toKeep = companyData.ForecastRecords
            .OrderByDescending(r => r.IsValidated ? 1 : 0)
            .ThenByDescending(r => r.PeriodStartDate)
            .Take(maxRecords)
            .ToList();

        if (companyData.ForecastRecords.Count > toKeep.Count)
        {
            companyData.ForecastRecords.Clear();
            companyData.ForecastRecords.AddRange(toKeep);
            companyData.MarkAsModified();
        }
    }

    /// <inheritdoc />
    public bool ShouldRunBacktest(CompanyData companyData, CompanySettings settings, int minMonths = 4)
    {
        // Get all months with transaction data
        var allDates = companyData.Sales.Select(s => s.Date)
            .Concat(companyData.Purchases.Select(p => p.Date))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        if (!allDates.Any())
            return false;

        // Get distinct months
        var distinctMonths = allDates
            .Select(d => new DateTime(d.Year, d.Month, 1))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        // Need at least minMonths of data
        if (distinctMonths.Count < minMonths)
            return false;

        // Get the newest month with data
        var newestMonth = distinctMonths.Last();
        var newestMonthStr = newestMonth.ToString("yyyy-MM");

        // If never backtested, run backtest
        if (string.IsNullOrEmpty(settings.LastBacktestedMonth))
            return true;

        // If newest data is newer than last backtest, run incremental backtest
        return string.Compare(newestMonthStr, settings.LastBacktestedMonth, StringComparison.Ordinal) > 0;
    }

    /// <inheritdoc />
    public async Task<int> RunBacktestAsync(
        CompanyData companyData,
        CompanySettings settings,
        ILocalMLForecastingService mlService,
        int minTrainingMonths = 3,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        return await Task.Run(() =>
        {
            // Get monthly revenue and expense aggregates
            var monthlyData = GetMonthlyAggregates(companyData);

            if (monthlyData.Count < minTrainingMonths + 1)
            {
                progress?.Report((0, 0, "Insufficient data for backtesting"));
                return 0;
            }

            // Determine starting point for backtest
            var existingPeriods = companyData.ForecastRecords
                .Where(r => r.IsValidated)
                .Select(r => r.PeriodStartDate.ToString("yyyy-MM"))
                .ToHashSet();

            var monthsToTest = monthlyData
                .Skip(minTrainingMonths)
                .Where(m => !existingPeriods.Contains(m.Month.ToString("yyyy-MM")))
                .ToList();

            if (!monthsToTest.Any())
            {
                progress?.Report((0, 0, "All periods already backtested"));
                settings.LastBacktestedMonth = monthlyData.Last().Month.ToString("yyyy-MM");
                return 0;
            }

            var total = monthsToTest.Count;
            var recordsCreated = 0;

            for (int i = 0; i < monthsToTest.Count; i++)
            {
                var targetMonth = monthsToTest[i];
                var targetIndex = monthlyData.IndexOf(targetMonth);

                progress?.Report((i + 1, total, $"Analyzing {targetMonth.Month:MMMM yyyy}..."));

                // Get training data (all months before the target)
                var trainingRevenue = monthlyData
                    .Take(targetIndex)
                    .Select(m => m.Revenue)
                    .ToList();

                var trainingExpenses = monthlyData
                    .Take(targetIndex)
                    .Select(m => m.Expenses)
                    .ToList();

                if (trainingRevenue.Count < minTrainingMonths)
                    continue;

                // Generate forecast using ML service
                var revenueForecast = mlService.GenerateEnhancedForecast(trainingRevenue, 1);
                var expenseForecast = mlService.GenerateEnhancedForecast(trainingExpenses, 1);

                var forecastedRevenue = revenueForecast.ForecastedValue;
                var forecastedExpenses = expenseForecast.ForecastedValue;
                var forecastedProfit = forecastedRevenue - forecastedExpenses;

                // Create validated record with both forecast and actual
                var record = new ForecastAccuracyRecord
                {
                    ForecastDate = targetMonth.Month, // Simulated forecast date
                    PeriodStartDate = targetMonth.Month,
                    PeriodEndDate = targetMonth.Month.AddMonths(1).AddDays(-1),
                    ForecastedRevenue = forecastedRevenue,
                    ForecastedExpenses = forecastedExpenses,
                    ForecastedProfit = forecastedProfit,
                    ForecastedNewCustomers = 0, // Not tracked in backtest
                    ConfidenceScore = (revenueForecast.ConfidenceScore + expenseForecast.ConfidenceScore) / 2,
                    ForecastMethod = revenueForecast.MethodUsed,
                    ActualRevenue = targetMonth.Revenue,
                    ActualExpenses = targetMonth.Expenses,
                    ActualProfit = targetMonth.Revenue - targetMonth.Expenses,
                    ActualNewCustomers = 0,
                    IsValidated = true
                };

                companyData.ForecastRecords.Add(record);
                recordsCreated++;
            }

            // Update last backtested month
            settings.LastBacktestedMonth = monthlyData.Last().Month.ToString("yyyy-MM");

            if (recordsCreated > 0)
            {
                companyData.MarkAsModified();
            }

            progress?.Report((total, total, $"Completed! {recordsCreated} forecast(s) validated."));
            return recordsCreated;
        });
    }

    /// <summary>
    /// Gets monthly aggregates of revenue and expenses.
    /// </summary>
    private List<MonthlyAggregate> GetMonthlyAggregates(CompanyData companyData)
    {
        // Aggregate sales by month
        var salesByMonth = companyData.Sales
            .Where(s => s.Date.HasValue)
            .GroupBy(s => new DateTime(s.Date!.Value.Year, s.Date.Value.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(s => s.EffectiveTotalUSD));

        // Aggregate purchases by month
        var purchasesByMonth = companyData.Purchases
            .Where(p => p.Date.HasValue)
            .GroupBy(p => new DateTime(p.Date!.Value.Year, p.Date.Value.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.EffectiveTotalUSD));

        // Get all months with any data
        var allMonths = salesByMonth.Keys
            .Concat(purchasesByMonth.Keys)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return allMonths.Select(month => new MonthlyAggregate
        {
            Month = month,
            Revenue = salesByMonth.TryGetValue(month, out var rev) ? rev : 0,
            Expenses = purchasesByMonth.TryGetValue(month, out var exp) ? exp : 0
        }).ToList();
    }

    /// <summary>
    /// Represents monthly aggregated data for backtesting.
    /// </summary>
    private class MonthlyAggregate
    {
        public DateTime Month { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
    }
}

/// <summary>
/// Interface for forecast accuracy tracking service.
/// </summary>
public interface IForecastAccuracyService
{
    /// <summary>
    /// Saves a forecast for later comparison with actual values.
    /// </summary>
    /// <param name="companyData">The company data to store the forecast in.</param>
    /// <param name="forecast">The forecast data to save.</param>
    /// <param name="forecastPeriod">The date range the forecast covers.</param>
    void SaveForecast(CompanyData companyData, ForecastData forecast, AnalysisDateRange forecastPeriod);

    /// <summary>
    /// Validates past forecasts by comparing them to actual values.
    /// Called automatically when generating new insights.
    /// </summary>
    /// <param name="companyData">The company data containing forecasts and actuals.</param>
    void ValidatePastForecasts(CompanyData companyData);

    /// <summary>
    /// Gets the complete forecast accuracy data including historical records and statistics.
    /// </summary>
    /// <param name="companyData">The company data to analyze.</param>
    /// <returns>Complete accuracy data with statistics.</returns>
    ForecastAccuracyData GetAccuracyData(CompanyData companyData);

    /// <summary>
    /// Gets recent forecast accuracy as a tuple.
    /// </summary>
    /// <param name="companyData">The company data to analyze.</param>
    /// <param name="recentCount">Number of recent forecasts to include.</param>
    /// <returns>Tuple of (RevenueAccuracy, ExpenseAccuracy) or null if no data.</returns>
    (double RevenueAccuracy, double ExpenseAccuracy)? GetRecentAccuracy(CompanyData companyData, int recentCount = 6);

    /// <summary>
    /// Gets a human-readable summary of forecast accuracy.
    /// </summary>
    /// <param name="companyData">The company data to analyze.</param>
    /// <returns>A description of forecast accuracy performance.</returns>
    string GetAccuracySummary(CompanyData companyData);

    /// <summary>
    /// Removes old forecast records beyond the maximum limit.
    /// </summary>
    /// <param name="companyData">The company data to clean up.</param>
    /// <param name="maxRecords">Maximum number of records to keep.</param>
    void CleanupOldRecords(CompanyData companyData, int maxRecords = 24);

    /// <summary>
    /// Determines if a backtest should be run based on available data and previous runs.
    /// </summary>
    /// <param name="companyData">The company data to check.</param>
    /// <param name="settings">The company settings with backtest tracking.</param>
    /// <param name="minMonths">Minimum months of data required.</param>
    /// <returns>True if backtest should be run.</returns>
    bool ShouldRunBacktest(CompanyData companyData, CompanySettings settings, int minMonths = 4);

    /// <summary>
    /// Runs walk-forward validation (backtesting) on historical data.
    /// Creates validated forecast records by testing predictions against known outcomes.
    /// </summary>
    /// <param name="companyData">The company data to backtest.</param>
    /// <param name="settings">The company settings to update with backtest status.</param>
    /// <param name="mlService">The ML forecasting service to use.</param>
    /// <param name="minTrainingMonths">Minimum months of training data before making predictions.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Number of forecast records created.</returns>
    Task<int> RunBacktestAsync(
        CompanyData companyData,
        CompanySettings settings,
        ILocalMLForecastingService mlService,
        int minTrainingMonths = 3,
        IProgress<(int current, int total, string message)>? progress = null);
}
