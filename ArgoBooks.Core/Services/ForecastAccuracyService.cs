using ArgoBooks.Core.Data;
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
}
