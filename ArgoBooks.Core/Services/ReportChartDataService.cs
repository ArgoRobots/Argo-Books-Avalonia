using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for generating chart data for reports.
/// </summary>
public class ReportChartDataService
{
    private readonly CompanyData? _companyData;
    private readonly ReportFilters _filters;

    public ReportChartDataService(CompanyData? companyData, ReportFilters filters)
    {
        _companyData = companyData;
        _filters = filters;
    }

    /// <summary>
    /// Gets the date range based on filters.
    /// </summary>
    private (DateTime Start, DateTime End) GetDateRange()
    {
        if (!string.IsNullOrEmpty(_filters.DatePresetName) &&
            _filters.DatePresetName != DatePresetNames.Custom)
        {
            return DatePresetNames.GetDateRange(_filters.DatePresetName);
        }

        var start = _filters.StartDate ?? DateTime.MinValue;
        var end = _filters.EndDate ?? DateTime.MaxValue;
        return (start, end);
    }

    #region Revenue Charts

    /// <summary>
    /// Gets revenue over time data.
    /// </summary>
    public List<ChartDataPoint> GetRevenueOverTime()
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(s => s.Total),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets revenue distribution by category.
    /// </summary>
    public List<ChartDataPoint> GetRevenueDistribution()
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var sales = _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate);

        return sales
            .GroupBy(s => s.Items?.FirstOrDefault()?.CategoryId ?? "Unknown")
            .Select(g =>
            {
                var categoryName = _companyData.GetCategory(g.Key)?.Name ?? "Other";
                return new ChartDataPoint
                {
                    Label = categoryName,
                    Value = (double)g.Sum(s => s.Total)
                };
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets total revenue value.
    /// </summary>
    public decimal GetTotalRevenue()
    {
        if (_companyData?.Sales == null)
            return 0;

        var (startDate, endDate) = GetDateRange();

        return _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.Total);
    }

    #endregion

    #region Expense Charts

    /// <summary>
    /// Gets expenses over time data.
    /// </summary>
    public List<ChartDataPoint> GetExpensesOverTime()
    {
        if (_companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(p => p.Total),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets expense distribution by category.
    /// </summary>
    public List<ChartDataPoint> GetExpenseDistribution()
    {
        if (_companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.CategoryId ?? "Unknown")
            .Select(g =>
            {
                var categoryName = _companyData.GetCategory(g.Key)?.Name ?? "Other";
                return new ChartDataPoint
                {
                    Label = categoryName,
                    Value = (double)g.Sum(p => p.Total)
                };
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets total expenses value.
    /// </summary>
    public decimal GetTotalExpenses()
    {
        if (_companyData?.Purchases == null)
            return 0;

        var (startDate, endDate) = GetDateRange();

        return _companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .Sum(p => p.Total);
    }

    #endregion

    #region Financial Charts

    /// <summary>
    /// Gets profit over time data.
    /// </summary>
    public List<ChartDataPoint> GetProfitOverTime()
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var revenueByDate = _companyData.Sales?
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Total)) ?? [];

        var expensesByDate = _companyData.Purchases?
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Total)) ?? [];

        var allDates = revenueByDate.Keys.Union(expensesByDate.Keys).OrderBy(d => d);

        return allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = (double)((revenueByDate.GetValueOrDefault(date, 0) - expensesByDate.GetValueOrDefault(date, 0))),
            Date = date
        }).ToList();
    }

    /// <summary>
    /// Gets sales vs expenses comparison data.
    /// </summary>
    public List<ChartSeriesData> GetSalesVsExpenses()
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate);

        var revenueData = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = (double)(_companyData.Sales?
                    .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                    .Sum(s => s.Total) ?? 0),
                Date = month
            };
        }).ToList();

        var expenseData = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = (double)(_companyData.Purchases?
                    .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                    .Sum(p => p.Total) ?? 0),
                Date = month
            };
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData },
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData }
        ];
    }

    /// <summary>
    /// Gets growth rates over time.
    /// </summary>
    public List<ChartDataPoint> GetGrowthRates()
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate).ToList();
        var result = new List<ChartDataPoint>();

        for (int i = 1; i < months.Count; i++)
        {
            var currentMonth = months[i];
            var previousMonth = months[i - 1];

            var currentMonthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
            var previousMonthStart = new DateTime(previousMonth.Year, previousMonth.Month, 1);
            var previousMonthEnd = previousMonthStart.AddMonths(1).AddDays(-1);

            var currentRevenue = _companyData.Sales
                .Where(s => s.Date >= currentMonthStart && s.Date <= currentMonthEnd)
                .Sum(s => s.Total);

            var previousRevenue = _companyData.Sales
                .Where(s => s.Date >= previousMonthStart && s.Date <= previousMonthEnd)
                .Sum(s => s.Total);

            double growthRate = 0;
            if (previousRevenue != 0)
            {
                growthRate = (double)((currentRevenue - previousRevenue) / previousRevenue * 100);
            }
            else if (currentRevenue > 0)
            {
                growthRate = 100;
            }

            result.Add(new ChartDataPoint
            {
                Label = currentMonth.ToString("MMM yyyy"),
                Value = growthRate,
                Date = currentMonth
            });
        }

        return result;
    }

    #endregion

    #region Transaction Charts

    /// <summary>
    /// Gets average transaction value over time.
    /// </summary>
    public List<ChartDataPoint> GetAverageTransactionValue()
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate);

        return months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var transactions = new List<decimal>();

            if (_filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
            {
                transactions.AddRange(_companyData.Sales?
                    .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                    .Select(s => s.Total) ?? []);
            }

            if (_filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
            {
                transactions.AddRange(_companyData.Purchases?
                    .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                    .Select(p => p.Total) ?? []);
            }

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = transactions.Count > 0 ? (double)transactions.Average() : 0,
                Date = month
            };
        }).ToList();
    }

    /// <summary>
    /// Gets transaction count over time.
    /// </summary>
    public List<ChartDataPoint> GetTransactionCount()
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate);

        return months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            int count = 0;

            if (_filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
            {
                count += _companyData.Sales?.Count(s => s.Date >= monthStart && s.Date <= monthEnd) ?? 0;
            }

            if (_filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
            {
                count += _companyData.Purchases?.Count(p => p.Date >= monthStart && p.Date <= monthEnd) ?? 0;
            }

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = count,
                Date = month
            };
        }).ToList();
    }

    #endregion

    #region Geographic Charts

    /// <summary>
    /// Gets sales by country of origin.
    /// </summary>
    public List<ChartDataPoint> GetSalesByCountryOfOrigin()
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.ShippingAddress?.Country ?? "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(s => s.Total)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets world map data (countries with sales totals).
    /// </summary>
    public Dictionary<string, double> GetWorldMapData()
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate && s.ShippingAddress?.Country != null)
            .GroupBy(s => s.ShippingAddress!.Country!)
            .ToDictionary(g => g.Key, g => (double)g.Sum(s => s.Total));
    }

    #endregion

    #region Accountant Charts

    /// <summary>
    /// Gets transactions by accountant.
    /// </summary>
    public List<ChartDataPoint> GetTransactionsByAccountant()
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var accountantData = new Dictionary<string, decimal>();

        // Sales by accountant
        if (_companyData.Sales != null)
        {
            foreach (var sale in _companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate))
            {
                var accountantName = _companyData.GetAccountant(sale.AccountantId ?? "")?.Name ?? "Unknown";
                accountantData.TryAdd(accountantName, 0);
                accountantData[accountantName] += sale.Total;
            }
        }

        // Purchases by accountant
        if (_companyData.Purchases != null)
        {
            foreach (var purchase in _companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate))
            {
                var accountantName = _companyData.GetAccountant(purchase.AccountantId ?? "")?.Name ?? "Unknown";
                accountantData.TryAdd(accountantName, 0);
                accountantData[accountantName] += purchase.Total;
            }
        }

        return accountantData
            .Select(kvp => new ChartDataPoint { Label = kvp.Key, Value = (double)kvp.Value })
            .OrderByDescending(p => p.Value)
            .ToList();
    }

    #endregion

    #region Returns Charts

    /// <summary>
    /// Gets returns over time.
    /// </summary>
    public List<ChartDataPoint> GetReturnsOverTime()
    {
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .GroupBy(r => r.ReturnDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = g.Count(),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets returns by reason.
    /// </summary>
    public List<ChartDataPoint> GetReturnReasons()
    {
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .GroupBy(r => r.Reason ?? "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(p => p.Value)
            .ToList();
    }

    /// <summary>
    /// Gets return financial impact (refund amounts).
    /// </summary>
    public List<ChartDataPoint> GetReturnFinancialImpact()
    {
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .GroupBy(r => new DateTime(r.ReturnDate.Year, r.ReturnDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM yyyy"),
                Value = (double)g.Sum(r => r.RefundAmount),
                Date = g.Key
            })
            .ToList();
    }

    #endregion

    #region Losses Charts

    /// <summary>
    /// Gets losses over time.
    /// </summary>
    public List<ChartDataPoint> GetLossesOverTime()
    {
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
            .Where(l => l.ReportedDate >= startDate && l.ReportedDate <= endDate)
            .GroupBy(l => l.ReportedDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = g.Count(),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets loss reasons.
    /// </summary>
    public List<ChartDataPoint> GetLossReasons()
    {
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
            .Where(l => l.ReportedDate >= startDate && l.ReportedDate <= endDate)
            .GroupBy(l => l.Reason.ToString())
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(p => p.Value)
            .ToList();
    }

    /// <summary>
    /// Gets loss financial impact.
    /// </summary>
    public List<ChartDataPoint> GetLossFinancialImpact()
    {
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
            .Where(l => l.ReportedDate >= startDate && l.ReportedDate <= endDate)
            .GroupBy(l => new DateTime(l.ReportedDate.Year, l.ReportedDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM yyyy"),
                Value = (double)g.Sum(l => l.EstimatedValue),
                Date = g.Key
            })
            .ToList();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the months between two dates.
    /// </summary>
    private static IEnumerable<DateTime> GetMonthsBetween(DateTime startDate, DateTime endDate)
    {
        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var end = new DateTime(endDate.Year, endDate.Month, 1);

        while (current <= end)
        {
            yield return current;
            current = current.AddMonths(1);
        }
    }

    /// <summary>
    /// Gets chart data for a specific chart type.
    /// </summary>
    public object GetChartData(ChartDataType chartType)
    {
        return chartType switch
        {
            ChartDataType.TotalRevenue => GetRevenueOverTime(),
            ChartDataType.RevenueDistribution => GetRevenueDistribution(),
            ChartDataType.TotalExpenses => GetExpensesOverTime(),
            ChartDataType.ExpensesDistribution => GetExpenseDistribution(),
            ChartDataType.TotalProfits => GetProfitOverTime(),
            ChartDataType.SalesVsExpenses => GetSalesVsExpenses(),
            ChartDataType.GrowthRates => GetGrowthRates(),
            ChartDataType.AverageTransactionValue => GetAverageTransactionValue(),
            ChartDataType.TotalTransactions => GetTransactionCount(),
            ChartDataType.WorldMap => GetWorldMapData(),
            ChartDataType.CountriesOfOrigin => GetSalesByCountryOfOrigin(),
            ChartDataType.AccountantsTransactions => GetTransactionsByAccountant(),
            ChartDataType.ReturnsOverTime => GetReturnsOverTime(),
            ChartDataType.ReturnReasons => GetReturnReasons(),
            ChartDataType.ReturnFinancialImpact => GetReturnFinancialImpact(),
            ChartDataType.LossesOverTime => GetLossesOverTime(),
            ChartDataType.LossReasons => GetLossReasons(),
            ChartDataType.LossFinancialImpact => GetLossFinancialImpact(),
            _ => new List<ChartDataPoint>()
        };
    }

    #endregion
}

/// <summary>
/// Represents a single data point in a chart.
/// </summary>
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime? Date { get; set; }
    public string? Color { get; set; }
}

/// <summary>
/// Represents a series of data points for multi-series charts.
/// </summary>
public class ChartSeriesData
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public List<ChartDataPoint> DataPoints { get; set; } = [];
}
