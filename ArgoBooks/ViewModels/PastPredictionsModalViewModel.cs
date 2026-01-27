using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Insights;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a single past prediction record for display in the modal.
/// </summary>
public partial class PastPredictionItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _periodLabel = string.Empty;

    [ObservableProperty]
    private string _forecastedRevenue = string.Empty;

    [ObservableProperty]
    private string _actualRevenue = string.Empty;

    [ObservableProperty]
    private string _revenueAccuracy = string.Empty;

    [ObservableProperty]
    private string _forecastedExpenses = string.Empty;

    [ObservableProperty]
    private string _actualExpenses = string.Empty;

    [ObservableProperty]
    private string _expensesAccuracy = string.Empty;

    [ObservableProperty]
    private string _confidenceScore = string.Empty;

    [ObservableProperty]
    private string _forecastMethod = string.Empty;

    [ObservableProperty]
    private bool _isBacktested;

    [ObservableProperty]
    private bool _isValidated;

    [ObservableProperty]
    private string _accuracyColor = "#6B7280"; // Default gray

    [ObservableProperty]
    private string _typeLabel = "Live";

    [ObservableProperty]
    private string _typeBadgeColor = "#3B82F6";

    /// <summary>
    /// Creates a view model from a ForecastAccuracyRecord.
    /// </summary>
    public static PastPredictionItemViewModel FromRecord(ForecastAccuracyRecord record)
    {
        var isBacktested = record.ForecastDate.Date == record.PeriodStartDate.Date;

        var revenueAccuracy = record.RevenueAccuracyPercent;
        var expensesAccuracy = record.ExpensesAccuracyPercent;

        // Determine color based on accuracy
        var accuracyColor = revenueAccuracy switch
        {
            >= 90 => "#22C55E", // Green - Excellent
            >= 80 => "#3B82F6", // Blue - Good
            >= 70 => "#F59E0B", // Orange - Moderate
            _ => "#EF4444" // Red - Low
        };

        return new PastPredictionItemViewModel
        {
            PeriodLabel = $"{record.PeriodStartDate:MMM yyyy}",
            ForecastedRevenue = record.ForecastedRevenue.ToString("C0"),
            ActualRevenue = record.ActualRevenue?.ToString("C0") ?? "—",
            RevenueAccuracy = revenueAccuracy.HasValue ? $"{revenueAccuracy.Value:F0}%" : "—",
            ForecastedExpenses = record.ForecastedExpenses.ToString("C0"),
            ActualExpenses = record.ActualExpenses?.ToString("C0") ?? "—",
            ExpensesAccuracy = expensesAccuracy.HasValue ? $"{expensesAccuracy.Value:F0}%" : "—",
            ConfidenceScore = $"{record.ConfidenceScore:F0}%",
            ForecastMethod = record.ForecastMethod,
            IsBacktested = isBacktested,
            IsValidated = record.IsValidated,
            AccuracyColor = revenueAccuracy.HasValue ? accuracyColor : "#6B7280",
            TypeLabel = isBacktested ? "Backtest" : "Live",
            TypeBadgeColor = isBacktested ? "#8B5CF6" : "#3B82F6"
        };
    }
}

/// <summary>
/// ViewModel for the Past Predictions modal that shows historical forecast accuracy.
/// </summary>
public partial class PastPredictionsModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _overallAccuracy = "—";

    [ObservableProperty]
    private string _accuracyTrend = "Stable";

    [ObservableProperty]
    private string _accuracyTrendIcon = "→";

    [ObservableProperty]
    private string _accuracyTrendColor = "#6B7280";

    [ObservableProperty]
    private string _validatedCount = "0";

    [ObservableProperty]
    private string _backtestCount = "0";

    [ObservableProperty]
    private bool _hasPredictions;

    [ObservableProperty]
    private string _noPredictionsMessage = "No past predictions yet. Predictions will appear here after forecast periods have completed.";

    /// <summary>
    /// Collection of past predictions for display.
    /// </summary>
    public ObservableCollection<PastPredictionItemViewModel> Predictions { get; } = [];

    /// <summary>
    /// Opens the modal and loads data.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        LoadPredictions();
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Loads prediction data from the company data.
    /// </summary>
    private void LoadPredictions()
    {
        Predictions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null || companyData.ForecastRecords.Count == 0)
        {
            HasPredictions = false;
            return;
        }

        // Get all records, ordered by period date descending
        var records = companyData.ForecastRecords
            .OrderByDescending(r => r.PeriodStartDate)
            .ToList();

        // Calculate statistics
        var validatedRecords = records.Where(r => r.IsValidated).ToList();
        var backtestRecords = records.Where(r => r.ForecastDate.Date == r.PeriodStartDate.Date && r.IsValidated).ToList();
        var realPredictions = validatedRecords.Except(backtestRecords).ToList();

        ValidatedCount = realPredictions.Count.ToString();
        BacktestCount = backtestRecords.Count.ToString();

        // Calculate overall accuracy
        if (validatedRecords.Count > 0)
        {
            var accuracies = validatedRecords
                .Where(r => r.RevenueAccuracyPercent.HasValue)
                .Select(r => r.RevenueAccuracyPercent!.Value)
                .ToList();

            if (accuracies.Count > 0)
            {
                var avgAccuracy = accuracies.Average();
                OverallAccuracy = $"{avgAccuracy:F0}%";

                // Determine trend if we have enough data
                if (accuracies.Count >= 4)
                {
                    var half = accuracies.Count / 2;
                    var firstHalf = accuracies.Skip(half).Take(half).Average(); // Older predictions
                    var secondHalf = accuracies.Take(half).Average(); // Newer predictions

                    if (secondHalf > firstHalf + 5)
                    {
                        AccuracyTrend = "Improving";
                        AccuracyTrendIcon = "↑";
                        AccuracyTrendColor = "#22C55E";
                    }
                    else if (secondHalf < firstHalf - 5)
                    {
                        AccuracyTrend = "Declining";
                        AccuracyTrendIcon = "↓";
                        AccuracyTrendColor = "#EF4444";
                    }
                    else
                    {
                        AccuracyTrend = "Stable";
                        AccuracyTrendIcon = "→";
                        AccuracyTrendColor = "#6B7280";
                    }
                }
            }
        }
        else
        {
            OverallAccuracy = "—";
            AccuracyTrend = "Stable";
            AccuracyTrendIcon = "→";
            AccuracyTrendColor = "#6B7280";
        }

        // Add records to the collection
        foreach (var record in records)
        {
            if (record.IsValidated)
            {
                Predictions.Add(PastPredictionItemViewModel.FromRecord(record));
            }
        }

        HasPredictions = Predictions.Count > 0;

        if (!HasPredictions)
        {
            NoPredictionsMessage = "No validated predictions yet. Predictions will appear here after forecast periods have completed and can be compared to actual results.";
        }
    }
}
