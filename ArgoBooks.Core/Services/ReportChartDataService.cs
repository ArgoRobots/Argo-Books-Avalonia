using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Granularity options for growth rate calculations.
/// </summary>
public enum GrowthRateGranularity
{
    /// <summary>Day-over-day comparison.</summary>
    Daily,
    /// <summary>Week-over-week comparison.</summary>
    Weekly,
    /// <summary>Month-over-month comparison.</summary>
    Monthly
}

/// <summary>
/// Service for generating chart data for reports.
/// </summary>
public class ReportChartDataService(CompanyData? companyData, ReportFilters filters)
{
    /// <summary>
    /// Gets the date range based on filters.
    /// Handles both preset names and custom ranges.
    /// </summary>
    private (DateTime Start, DateTime End) GetDateRange()
    {
        // For custom ranges, use the explicit start/end dates from filters
        if (string.IsNullOrEmpty(filters.DatePresetName) || IsCustomRange(filters.DatePresetName))
        {
            var start = filters.StartDate ?? DateTime.MinValue;
            var end = filters.EndDate ?? DateTime.MaxValue;
            return (start, end);
        }

        // For preset names, calculate the date range
        return DatePresetNames.GetDateRange(filters.DatePresetName);
    }

    /// <summary>
    /// Checks if the preset name indicates a custom range.
    /// </summary>
    private static bool IsCustomRange(string? presetName)
    {
        if (string.IsNullOrEmpty(presetName))
            return true;

        var lower = presetName.ToLowerInvariant();
        return lower is "custom" or "custom range";
    }

    /// <summary>
    /// Gets the effective date range, constrained by actual sales data.
    /// For unbounded ranges (like All Time), uses the actual min/max dates from sales.
    /// </summary>
    private (DateTime Start, DateTime End) GetEffectiveDateRange()
    {
        var (start, end) = GetDateRange();

        if (companyData?.Sales == null || !companyData.Sales.Any())
            return (start, end);

        // If date range is very large (e.g., All Time), constrain to actual data range
        var minDataDate = companyData.Sales.Min(s => s.Date).Date;
        var maxDataDate = companyData.Sales.Max(s => s.Date).Date;

        // Use the intersection of requested range and actual data range
        var effectiveStart = start < minDataDate ? minDataDate : start;
        var effectiveEnd = end > maxDataDate ? maxDataDate : end;

        return (effectiveStart, effectiveEnd);
    }

    #region Revenue Charts

    /// <summary>
    /// Gets revenue over time data (in USD for consistent calculations).
    /// </summary>
    public List<ChartDataPoint> GetRevenueOverTime()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(s => s.EffectiveTotalUSD),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets revenue distribution by category (in USD).
    /// </summary>
    public List<ChartDataPoint> GetRevenueDistribution()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var sales = companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate);

        return sales
            .GroupBy(s =>
            {
                var productId = s.LineItems.FirstOrDefault()?.ProductId;
                var product = productId != null ? companyData.GetProduct(productId) : null;
                return product?.CategoryId ?? "Unknown";
            })
            .Select(g =>
            {
                var categoryName = companyData.GetCategory(g.Key)?.Name ?? "Other";
                return new ChartDataPoint
                {
                    Label = categoryName,
                    Value = (double)g.Sum(s => s.EffectiveTotalUSD)
                };
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets total revenue value (in USD).
    /// </summary>
    public decimal GetTotalRevenue()
    {
        if (companyData?.Sales == null)
            return 0;

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.EffectiveTotalUSD);
    }

    #endregion

    #region Expense Charts

    /// <summary>
    /// Gets expenses over time data (in USD for consistent calculations).
    /// </summary>
    public List<ChartDataPoint> GetExpensesOverTime()
    {
        if (companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(p => p.EffectiveTotalUSD),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets expense distribution by category (in USD).
    /// </summary>
    public List<ChartDataPoint> GetExpenseDistribution()
    {
        if (companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p =>
            {
                var productId = p.LineItems.FirstOrDefault()?.ProductId;
                var product = productId != null ? companyData.GetProduct(productId) : null;
                return product?.CategoryId ?? "Unknown";
            })
            .Select(g =>
            {
                var categoryName = companyData.GetCategory(g.Key)?.Name ?? "Other";
                return new ChartDataPoint
                {
                    Label = categoryName,
                    Value = (double)g.Sum(p => p.EffectiveTotalUSD)
                };
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets total expenses value (in USD).
    /// </summary>
    public decimal GetTotalExpenses()
    {
        if (companyData?.Purchases == null)
            return 0;

        var (startDate, endDate) = GetDateRange();

        return companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .Sum(p => p.EffectiveTotalUSD);
    }

    #endregion

    #region Financial Charts

    /// <summary>
    /// Gets profit over time data (in USD for consistent calculations).
    /// </summary>
    public List<ChartDataPoint> GetProfitOverTime()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var revenueByDate = companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.EffectiveTotalUSD));

        var expensesByDate = companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.EffectiveTotalUSD));

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
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months that have actual sales or purchase data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var hasSales = companyData.Sales.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasPurchases = companyData.Purchases.Any(p => p.Date >= monthStart && p.Date <= monthEnd);
            return hasSales || hasPurchases;
        }).ToList();

        if (monthsWithData.Count == 0)
            return [];

        var revenueData = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = (double)companyData.Sales
                    .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                    .Sum(s => s.EffectiveTotalUSD),
                Date = month
            };
        }).ToList();

        var expenseData = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = (double)companyData.Purchases
                    .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                    .Sum(p => p.EffectiveTotalUSD),
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
    /// Gets sales vs expenses data grouped by day for granular display.
    /// </summary>
    public List<ChartSeriesData> GetSalesVsExpensesDaily()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Get all dates with data
        var salesByDate = companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => (double)g.Sum(s => s.EffectiveTotalUSD));

        var purchasesByDate = companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => (double)g.Sum(p => p.EffectiveTotalUSD));

        // Combine all dates
        var allDates = salesByDate.Keys.Union(purchasesByDate.Keys).OrderBy(d => d).ToList();

        if (allDates.Count == 0)
            return [];

        var revenueData = allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = salesByDate.GetValueOrDefault(date, 0),
            Date = date
        }).ToList();

        var expenseData = allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = purchasesByDate.GetValueOrDefault(date, 0),
            Date = date
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData },
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData }
        ];
    }

    /// <summary>
    /// Gets growth rates over time with dynamic granularity based on the selected date range.
    /// Falls back to finer granularity if not enough data at the preferred level.
    /// - Short ranges (up to ~1 month): day-over-day
    /// - Medium ranges (quarter): week-over-week
    /// - Long ranges (year+): month-over-month
    /// </summary>
    public List<ChartDataPoint> GetGrowthRates()
    {
        if (companyData?.Sales == null)
            return [];

        var granularity = DetermineGrowthRateGranularity();

        // Try preferred granularity, fall back to finer if not enough data
        List<ChartDataPoint> result;

        if (granularity == GrowthRateGranularity.Monthly)
        {
            result = GetGrowthRatesMonthly();
            if (result.Count == 0)
            {
                result = GetGrowthRatesWeekly();
            }
            if (result.Count == 0)
            {
                result = GetGrowthRatesDaily();
            }
        }
        else if (granularity == GrowthRateGranularity.Weekly)
        {
            result = GetGrowthRatesWeekly();
            if (result.Count == 0)
            {
                result = GetGrowthRatesDaily();
            }
        }
        else
        {
            result = GetGrowthRatesDaily();
        }

        return result;
    }

    /// <summary>
    /// Gets day-over-day growth rates.
    /// </summary>
    private List<ChartDataPoint> GetGrowthRatesDaily()
    {
        var (startDate, endDate) = GetEffectiveDateRange();

        // Get days directly from sales data within the range
        var daysWithData = companyData!.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Select(s => s.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (daysWithData.Count < 2)
            return [];

        var result = new List<ChartDataPoint>();

        for (int i = 1; i < daysWithData.Count; i++)
        {
            var currentDay = daysWithData[i];
            var previousDay = daysWithData[i - 1];

            var currentRevenue = companyData!.Sales
                .Where(s => s.Date.Date == currentDay)
                .Sum(s => s.EffectiveTotalUSD);

            var previousRevenue = companyData.Sales
                .Where(s => s.Date.Date == previousDay)
                .Sum(s => s.EffectiveTotalUSD);

            double growthRate = CalculateGrowthRate(currentRevenue, previousRevenue);

            result.Add(new ChartDataPoint
            {
                Label = currentDay.ToString("MMM d"),
                Value = growthRate,
                Date = currentDay
            });
        }

        return result;
    }

    /// <summary>
    /// Gets week-over-week growth rates.
    /// </summary>
    private List<ChartDataPoint> GetGrowthRatesWeekly()
    {
        var (startDate, endDate) = GetEffectiveDateRange();

        // Get weeks directly from sales data - group by week start (Monday)
        var weeksWithData = companyData!.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Select(s => GetWeekStart(s.Date))
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        if (weeksWithData.Count < 2)
            return [];

        var result = new List<ChartDataPoint>();

        for (int i = 1; i < weeksWithData.Count; i++)
        {
            var currentWeekStart = weeksWithData[i];
            var currentWeekEnd = currentWeekStart.AddDays(6);
            var previousWeekStart = weeksWithData[i - 1];
            var previousWeekEnd = previousWeekStart.AddDays(6);

            var currentRevenue = companyData!.Sales
                .Where(s => s.Date >= currentWeekStart && s.Date <= currentWeekEnd)
                .Sum(s => s.EffectiveTotalUSD);

            var previousRevenue = companyData.Sales
                .Where(s => s.Date >= previousWeekStart && s.Date <= previousWeekEnd)
                .Sum(s => s.EffectiveTotalUSD);

            double growthRate = CalculateGrowthRate(currentRevenue, previousRevenue);

            // Format label as "Jan 6-12" for weekly ranges
            var label = $"{currentWeekStart:MMM d}-{currentWeekEnd:d}";

            result.Add(new ChartDataPoint
            {
                Label = label,
                Value = growthRate,
                Date = currentWeekStart
            });
        }

        return result;
    }

    /// <summary>
    /// Gets the Monday of the week containing the given date.
    /// </summary>
    private static DateTime GetWeekStart(DateTime date)
    {
        var daysToMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.AddDays(-daysToMonday).Date;
    }

    /// <summary>
    /// Gets month-over-month growth rates.
    /// </summary>
    private List<ChartDataPoint> GetGrowthRatesMonthly()
    {
        var (startDate, endDate) = GetEffectiveDateRange();

        // Get months directly from sales data
        var monthsWithData = companyData!.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Select(s => new DateTime(s.Date.Year, s.Date.Month, 1))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        if (monthsWithData.Count < 2)
            return [];

        var result = new List<ChartDataPoint>();

        for (int i = 1; i < monthsWithData.Count; i++)
        {
            var currentMonth = monthsWithData[i];
            var previousMonth = monthsWithData[i - 1];

            var currentMonthStart = currentMonth;
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
            var previousMonthStart = previousMonth;
            var previousMonthEnd = previousMonthStart.AddMonths(1).AddDays(-1);

            var currentRevenue = companyData!.Sales
                .Where(s => s.Date >= currentMonthStart && s.Date <= currentMonthEnd)
                .Sum(s => s.EffectiveTotalUSD);

            var previousRevenue = companyData.Sales
                .Where(s => s.Date >= previousMonthStart && s.Date <= previousMonthEnd)
                .Sum(s => s.EffectiveTotalUSD);

            double growthRate = CalculateGrowthRate(currentRevenue, previousRevenue);

            result.Add(new ChartDataPoint
            {
                Label = currentMonth.ToString("MMM yyyy"),
                Value = growthRate,
                Date = currentMonth
            });
        }

        return result;
    }

    /// <summary>
    /// Calculates the growth rate percentage between two revenue values.
    /// </summary>
    private static double CalculateGrowthRate(decimal currentRevenue, decimal previousRevenue)
    {
        if (previousRevenue != 0)
        {
            return (double)((currentRevenue - previousRevenue) / previousRevenue * 100);
        }
        else if (currentRevenue > 0)
        {
            return 100;
        }
        return 0;
    }

    #endregion

    #region Transaction Charts

    /// <summary>
    /// Gets average transaction value over time.
    /// </summary>
    public List<ChartDataPoint> GetAverageTransactionValue()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months with actual transaction data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var hasRevenue = filters.TransactionType is TransactionType.Revenue or TransactionType.Both &&
                companyData.Sales.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasExpenses = filters.TransactionType is TransactionType.Expenses or TransactionType.Both &&
                companyData.Purchases.Any(p => p.Date >= monthStart && p.Date <= monthEnd);

            return hasRevenue || hasExpenses;
        }).ToList();

        return monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var transactions = new List<decimal>();

            if (filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
            {
                transactions.AddRange(companyData.Sales
                    .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                    .Select(s => s.EffectiveTotalUSD));
            }

            if (filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
            {
                transactions.AddRange(companyData.Purchases
                    .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                    .Select(p => p.EffectiveTotalUSD));
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
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months with actual transaction data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var hasRevenue = filters.TransactionType is TransactionType.Revenue or TransactionType.Both &&
                companyData.Sales.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasExpenses = filters.TransactionType is TransactionType.Expenses or TransactionType.Both &&
                companyData.Purchases.Any(p => p.Date >= monthStart && p.Date <= monthEnd);

            return hasRevenue || hasExpenses;
        }).ToList();

        return monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            int count = 0;

            if (filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
            {
                count += companyData.Sales.Count(s => s.Date >= monthStart && s.Date <= monthEnd);
            }

            if (filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
            {
                count += companyData.Purchases.Count(p => p.Date >= monthStart && p.Date <= monthEnd);
            }

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = count,
                Date = month
            };
        }).ToList();
    }

    /// <summary>
    /// Gets average transaction value over time grouped by day.
    /// </summary>
    public List<ChartDataPoint> GetAverageTransactionValueDaily()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var transactionsByDate = new Dictionary<DateTime, List<decimal>>();

        if (filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
        {
            foreach (var sale in companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate))
            {
                var date = sale.Date.Date;
                if (!transactionsByDate.ContainsKey(date))
                    transactionsByDate[date] = [];
                transactionsByDate[date].Add(sale.EffectiveTotalUSD);
            }
        }

        if (filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
        {
            foreach (var purchase in companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate))
            {
                var date = purchase.Date.Date;
                if (!transactionsByDate.ContainsKey(date))
                    transactionsByDate[date] = [];
                transactionsByDate[date].Add(purchase.EffectiveTotalUSD);
            }
        }

        return transactionsByDate
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ChartDataPoint
            {
                Label = kvp.Key.ToString("MMM dd"),
                Value = kvp.Value.Count > 0 ? (double)kvp.Value.Average() : 0,
                Date = kvp.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets transaction count over time grouped by day.
    /// </summary>
    public List<ChartDataPoint> GetTransactionCountDaily()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var countsByDate = new Dictionary<DateTime, int>();

        if (filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
        {
            foreach (var sale in companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate))
            {
                var date = sale.Date.Date;
                countsByDate[date] = countsByDate.GetValueOrDefault(date, 0) + 1;
            }
        }

        if (filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
        {
            foreach (var purchase in companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate))
            {
                var date = purchase.Date.Date;
                countsByDate[date] = countsByDate.GetValueOrDefault(date, 0) + 1;
            }
        }

        return countsByDate
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ChartDataPoint
            {
                Label = kvp.Key.ToString("MMM dd"),
                Value = kvp.Value,
                Date = kvp.Key
            })
            .ToList();
    }

    #endregion

    #region Geographic Charts

    /// <summary>
    /// Gets sales by customer country.
    /// </summary>
    public List<ChartDataPoint> GetSalesByCountryOfOrigin()
    {
        return GetSalesByCustomerCountry();
    }

    /// <summary>
    /// Gets sales grouped by customer country.
    /// Used for geographic distribution charts showing where customers are located.
    /// </summary>
    public List<ChartDataPoint> GetSalesByCustomerCountry()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s =>
            {
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address.Country ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(s => s.EffectiveTotalUSD)
            })
            // Filter out "Unknown" entries with zero or negligible value
            .Where(p => p.Label != "Unknown" || p.Value > 0.01)
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets world map data (countries with sales totals) by customer country.
    /// Returns country names as keys.
    /// </summary>
    public Dictionary<string, double> GetWorldMapData()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s =>
            {
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address.Country;
            })
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => (double)g.Sum(s => s.EffectiveTotalUSD));
    }

    /// <summary>
    /// Gets world map data by supplier country from purchases.
    /// Returns country names as keys.
    /// </summary>
    public Dictionary<string, double> GetWorldMapDataBySupplier()
    {
        if (companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p =>
            {
                var supplier = companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address.Country;
            })
            .Where(g => g.Key != null && !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key!, g => (double)g.Sum(p => p.EffectiveTotalUSD));
    }

    /// <summary>
    /// Gets purchases by supplier country.
    /// </summary>
    public List<ChartDataPoint> GetPurchasesByCountryOfDestination()
    {
        if (companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p =>
            {
                var supplier = companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address.Country ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(p => p.EffectiveTotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets purchases by supplier company name.
    /// </summary>
    public List<ChartDataPoint> GetPurchasesBySupplierCompany()
    {
        if (companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => companyData.GetSupplier(p.SupplierId ?? "")?.Name ?? "Unknown")
            .Where(g => g.Key != "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(p => p.EffectiveTotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets sales by customer.
    /// </summary>
    public List<ChartDataPoint> GetSalesByCompanyOfOrigin()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => companyData.GetCustomer(s.CustomerId ?? "")?.Name ?? "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(s => s.EffectiveTotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets sales by customer company (destination companies).
    /// Only includes customers with CompanyName set.
    /// </summary>
    public List<ChartDataPoint> GetSalesByCompanyOfDestination()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .Select(s => new { Sale = s, Customer = companyData.GetCustomer(s.CustomerId ?? "") })
            .Where(x => x.Customer != null && !string.IsNullOrEmpty(x.Customer.CompanyName))
            .GroupBy(x => x.Customer!.CompanyName)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key!,
                Value = (double)g.Sum(x => x.Sale.EffectiveTotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets average shipping costs over time.
    /// </summary>
    public List<ChartDataPoint> GetAverageShippingCosts()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months with actual transaction data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var hasSales = companyData.Sales.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasPurchases = companyData.Purchases.Any(p => p.Date >= monthStart && p.Date <= monthEnd);

            return hasSales || hasPurchases;
        }).ToList();

        return monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var shippingCosts = new List<decimal>();

            shippingCosts.AddRange(companyData.Sales
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .Select(s => s.ShippingCost));

            shippingCosts.AddRange(companyData.Purchases
                .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                .Select(p => p.ShippingCost));

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = shippingCosts.Count > 0 ? (double)shippingCosts.Average() : 0,
                Date = month
            };
        }).ToList();
    }

    /// <summary>
    /// Gets average shipping costs over time grouped by day.
    /// </summary>
    public List<ChartDataPoint> GetAverageShippingCostsDaily()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var shippingByDate = new Dictionary<DateTime, List<decimal>>();

        foreach (var sale in companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var date = sale.Date.Date;
            if (!shippingByDate.ContainsKey(date))
                shippingByDate[date] = [];
            shippingByDate[date].Add(sale.ShippingCost);
        }

        foreach (var purchase in companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate))
        {
            var date = purchase.Date.Date;
            if (!shippingByDate.ContainsKey(date))
                shippingByDate[date] = [];
            shippingByDate[date].Add(purchase.ShippingCost);
        }

        return shippingByDate
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ChartDataPoint
            {
                Label = kvp.Key.ToString("MMM dd"),
                Value = kvp.Value.Count > 0 ? (double)kvp.Value.Average() : 0,
                Date = kvp.Key
            })
            .ToList();
    }

    #endregion

    #region Accountant Charts

    /// <summary>
    /// Gets transactions by accountant.
    /// </summary>
    public List<ChartDataPoint> GetTransactionsByAccountant()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var accountantData = new Dictionary<string, int>();

        // Sales by accountant
        foreach (var sale in companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var accountantName = companyData.GetAccountant(sale.AccountantId ?? "")?.Name ?? "Unknown";
            accountantData.TryAdd(accountantName, 0);
            accountantData[accountantName] += 1;
        }

        // Purchases by accountant
        foreach (var purchase in companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate))
        {
            var accountantName = companyData.GetAccountant(purchase.AccountantId ?? "")?.Name ?? "Unknown";
            accountantData.TryAdd(accountantName, 0);
            accountantData[accountantName] += 1;
        }

        return accountantData
            .Select(kvp => new ChartDataPoint { Label = kvp.Key, Value = kvp.Value })
            .OrderByDescending(p => p.Value)
            .ToList();
    }

    #endregion

    #region Customer Charts

    /// <summary>
    /// Gets top customers by revenue.
    /// </summary>
    public List<ChartDataPoint> GetTopCustomersByRevenue()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g =>
            {
                var customer = companyData.GetCustomer(g.Key ?? "");
                var customerName = customer?.Name ?? customer?.CompanyName ?? "Unknown";
                return new ChartDataPoint
                {
                    Label = customerName,
                    Value = (double)g.Sum(s => s.Total)
                };
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets customer payment status breakdown (Paid, Pending, Overdue).
    /// Uses Sales.PaymentStatus to match ChartLoaderService behavior.
    /// </summary>
    public List<ChartDataPoint> GetCustomerPaymentStatus()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var salesInRange = companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .ToList();

        if (salesInRange.Count == 0)
            return [];

        var paid = salesInRange.Count(s => s.PaymentStatus == "Paid" || s.PaymentStatus == "Complete");
        var pending = salesInRange.Count(s => s.PaymentStatus == "Pending" || string.IsNullOrEmpty(s.PaymentStatus));
        var overdue = salesInRange.Count(s => s.PaymentStatus == "Overdue");

        var result = new List<ChartDataPoint>();
        if (paid > 0) result.Add(new ChartDataPoint { Label = "Paid", Value = paid });
        if (pending > 0) result.Add(new ChartDataPoint { Label = "Pending", Value = pending });
        if (overdue > 0) result.Add(new ChartDataPoint { Label = "Overdue", Value = overdue });

        return result;
    }

    /// <summary>
    /// Gets customer growth over time (new customers acquired).
    /// </summary>
    public List<ChartDataPoint> GetCustomerGrowth()
    {
        if (companyData?.Customers == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Group customers by their first purchase date (approximated by earliest sale)
        var customerFirstPurchase = companyData.Sales
            .Where(s => !string.IsNullOrEmpty(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .ToDictionary(g => g.Key!, g => g.Min(s => s.Date));

        return customerFirstPurchase
            .Where(kvp => kvp.Value >= startDate && kvp.Value <= endDate)
            .GroupBy(kvp => new DateTime(kvp.Value.Year, kvp.Value.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM yyyy"),
                Value = g.Count(),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets customer lifetime value (average revenue per customer).
    /// </summary>
    public List<ChartDataPoint> GetCustomerLifetimeValue()
    {
        if (companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var customerRevenue = companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalRevenue = g.Sum(s => s.Total)
            })
            .ToList();

        if (!customerRevenue.Any())
            return [];

        // Return distribution of customer lifetime values
        var ranges = new[]
        {
            (0m, 100m, "$0-100"),
            (100m, 500m, "$100-500"),
            (500m, 1000m, "$500-1K"),
            (1000m, 5000m, "$1K-5K"),
            (5000m, decimal.MaxValue, "$5K+")
        };

        return ranges
            .Select(r => new ChartDataPoint
            {
                Label = r.Item3,
                Value = customerRevenue.Count(c => c.TotalRevenue >= r.Item1 && c.TotalRevenue < r.Item2)
            })
            .Where(p => p.Value > 0)
            .ToList();
    }

    /// <summary>
    /// Gets active vs inactive customers count.
    /// </summary>
    public List<ChartDataPoint> GetActiveVsInactiveCustomers()
    {
        if (companyData?.Customers == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var activeCustomerIds = companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .Select(s => s.CustomerId)
            .Distinct()
            .ToHashSet();

        var activeCount = activeCustomerIds.Count;
        var inactiveCount = companyData.Customers.Count - activeCount;

        var result = new List<ChartDataPoint>();
        if (activeCount > 0) result.Add(new ChartDataPoint { Label = "Active", Value = activeCount });
        if (inactiveCount > 0) result.Add(new ChartDataPoint { Label = "Inactive", Value = inactiveCount });

        return result;
    }

    /// <summary>
    /// Gets rentals per customer distribution.
    /// </summary>
    public List<ChartDataPoint> GetRentalsPerCustomer()
    {
        if (companyData?.Rentals == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Rentals
            .Where(r => r.StartDate >= startDate && r.StartDate <= endDate && !string.IsNullOrEmpty(r.CustomerId))
            .GroupBy(r => r.CustomerId)
            .Select(g =>
            {
                var customer = companyData.GetCustomer(g.Key);
                var customerName = customer?.Name ?? customer?.CompanyName ?? "Unknown";
                return new ChartDataPoint
                {
                    Label = customerName,
                    Value = g.Count()
                };
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    #endregion

    #region Returns Charts

    /// <summary>
    /// Gets returns over time.
    /// </summary>
    public List<ChartDataPoint> GetReturnsOverTime()
    {
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Returns
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
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Returns have Items, each with a Reason - group by all item reasons
        return companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .SelectMany(r => r.Items)
            .GroupBy(item => string.IsNullOrEmpty(item.Reason) ? "Unknown" : item.Reason)
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
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Returns
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

    /// <summary>
    /// Gets return financial impact (refund amounts) grouped by day.
    /// </summary>
    public List<ChartDataPoint> GetReturnFinancialImpactDaily()
    {
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .GroupBy(r => r.ReturnDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(r => r.RefundAmount),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets returns by category.
    /// </summary>
    public List<ChartDataPoint> GetReturnsByCategory()
    {
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Returns have Items with ProductId - group by category of each returned item
        return companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .SelectMany(r => r.Items)
            .GroupBy(item =>
            {
                var product = companyData.GetProduct(item.ProductId);
                var category = product != null ? companyData.GetCategory(product.CategoryId ?? "") : null;
                return category?.Name ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets returns by product.
    /// </summary>
    public List<ChartDataPoint> GetReturnsByProduct()
    {
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Returns have Items with ProductId - group by product name of each returned item
        return companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .SelectMany(r => r.Items)
            .GroupBy(item => companyData.GetProduct(item.ProductId)?.Name ?? "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets returns comparison over time.
    /// Note: Current model only tracks sale returns (returns from customers).
    /// </summary>
    public List<ChartSeriesData> GetPurchaseVsSaleReturns()
    {
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months with actual return data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return companyData.Returns.Any(r => r.ReturnDate >= monthStart && r.ReturnDate <= monthEnd);
        }).ToList();

        if (monthsWithData.Count == 0)
            return [];

        // Current Return model represents returns from sales (customer returns)
        var saleReturns = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = companyData.Returns
                    .Count(r => r.ReturnDate >= monthStart && r.ReturnDate <= monthEnd),
                Date = month
            };
        }).ToList();

        // No purchase returns in current model - return empty series
        var purchaseReturns = monthsWithData.Select(month => new ChartDataPoint
        {
            Label = month.ToString("MMM yyyy"),
            Value = 0,
            Date = month
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Sale Returns", Color = "#EF4444", DataPoints = saleReturns },
            new ChartSeriesData { Name = "Purchase Returns", Color = "#F59E0B", DataPoints = purchaseReturns }
        ];
    }

    #endregion

    #region Losses Charts

    /// <summary>
    /// Gets losses over time.
    /// </summary>
    public List<ChartDataPoint> GetLossesOverTime()
    {
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l => l.DateDiscovered.Date)
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
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
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
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l => new DateTime(l.DateDiscovered.Year, l.DateDiscovered.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM yyyy"),
                Value = (double)g.Sum(l => l.ValueLost),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets loss financial impact grouped by day.
    /// </summary>
    public List<ChartDataPoint> GetLossFinancialImpactDaily()
    {
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l => l.DateDiscovered.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(l => l.ValueLost),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets losses by category.
    /// </summary>
    public List<ChartDataPoint> GetLossesByCategory()
    {
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l =>
            {
                var product = companyData.GetProduct(l.ProductId);
                var category = product != null ? companyData.GetCategory(product.CategoryId ?? "") : null;
                return category?.Name ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets losses by product.
    /// </summary>
    public List<ChartDataPoint> GetLossesByProduct()
    {
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l => companyData.GetProduct(l.ProductId)?.Name ?? "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets losses by reason type over time.
    /// Note: Current model tracks inventory losses by reason (Lost, Damaged, Stolen, Expired, Other).
    /// </summary>
    public List<ChartSeriesData> GetPurchaseVsSaleLosses()
    {
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months with actual loss data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return companyData.LostDamaged.Any(l => l.DateDiscovered >= monthStart && l.DateDiscovered <= monthEnd);
        }).ToList();

        if (monthsWithData.Count == 0)
            return [];

        // Group by reason type - show Damaged vs Lost as the two main categories
        var damagedLosses = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = companyData.LostDamaged
                    .Count(l => l.DateDiscovered >= monthStart && l.DateDiscovered <= monthEnd &&
                           l.Reason == LostDamagedReason.Damaged),
                Date = month
            };
        }).ToList();

        var lostLosses = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = companyData.LostDamaged
                    .Count(l => l.DateDiscovered >= monthStart && l.DateDiscovered <= monthEnd &&
                           l.Reason == LostDamagedReason.Lost),
                Date = month
            };
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Damaged", Color = "#DC2626", DataPoints = damagedLosses },
            new ChartSeriesData { Name = "Lost", Color = "#9333EA", DataPoints = lostLosses }
        ];
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Determines the appropriate granularity for growth rate calculations based on the date range.
    /// Maps UI options: This Month, Last Month, This Quarter, Last Quarter, This Year, Last Year, All Time, Custom Range.
    /// </summary>
    private GrowthRateGranularity DetermineGrowthRateGranularity()
    {
        // For custom ranges or no preset, determine from actual date span
        if (IsCustomRange(filters.DatePresetName))
            return DetermineGranularityFromSpan();

        // Normalize to lowercase for case-insensitive comparison
        var presetLower = filters.DatePresetName!.ToLowerInvariant();

        // Short ranges (month) - day-over-day
        if (presetLower is "this month" or "last month")
            return GrowthRateGranularity.Daily;

        // Medium ranges (quarter) - week-over-week
        if (presetLower is "this quarter" or "last quarter")
            return GrowthRateGranularity.Weekly;

        // Long ranges (year+) - month-over-month
        if (presetLower is "this year" or "last year" or "all time")
            return GrowthRateGranularity.Monthly;

        // Fall back to calculating based on actual date span
        return DetermineGranularityFromSpan();
    }

    /// <summary>
    /// Determines granularity based on the actual date span.
    /// </summary>
    private GrowthRateGranularity DetermineGranularityFromSpan()
    {
        var (startDate, endDate) = GetDateRange();

        // Handle extreme dates
        if (startDate == DateTime.MinValue || endDate == DateTime.MaxValue)
            return GrowthRateGranularity.Monthly;

        var span = endDate - startDate;

        return span.TotalDays switch
        {
            <= 45 => GrowthRateGranularity.Daily,     // Up to ~1.5 months: daily
            <= 120 => GrowthRateGranularity.Weekly,   // Up to ~4 months: weekly
            _ => GrowthRateGranularity.Monthly        // Longer periods: monthly
        };
    }

    /// <summary>
    /// Gets the months between two dates.
    /// Handles extreme dates (DateTime.MinValue/MaxValue) by clamping to reasonable ranges.
    /// </summary>
    private static IEnumerable<DateTime> GetMonthsBetween(DateTime startDate, DateTime endDate)
    {
        // Clamp dates to avoid DateTime overflow when using MinValue/MaxValue
        var minSafeDate = new DateTime(1900, 1, 1);
        var maxSafeDate = new DateTime(2100, 12, 31);

        if (startDate < minSafeDate) startDate = minSafeDate;
        if (endDate > maxSafeDate) endDate = maxSafeDate;
        if (startDate > endDate) yield break;

        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var end = new DateTime(endDate.Year, endDate.Month, 1);

        // Safety limit to prevent infinite loops (max 1200 months = 100 years)
        var maxIterations = 1200;
        var iterations = 0;

        while (current <= end && iterations < maxIterations)
        {
            yield return current;
            current = current.AddMonths(1);
            iterations++;
        }
    }

    /// <summary>
    /// Gets chart data for a specific chart type.
    /// </summary>
    public object GetChartData(ChartDataType chartType)
    {
        return chartType switch
        {
            // Revenue charts
            ChartDataType.TotalRevenue => GetRevenueOverTime(),
            ChartDataType.RevenueDistribution => GetRevenueDistribution(),

            // Expense charts
            ChartDataType.TotalExpenses => GetExpensesOverTime(),
            ChartDataType.ExpensesDistribution => GetExpenseDistribution(),

            // Financial charts
            ChartDataType.TotalProfits => GetProfitOverTime(),
            ChartDataType.SalesVsExpenses => GetSalesVsExpenses(),
            ChartDataType.GrowthRates => GetGrowthRates(),

            // Transaction charts
            ChartDataType.AverageTransactionValue => GetAverageTransactionValue(),
            ChartDataType.TotalTransactions => GetTransactionCount(),
            ChartDataType.TotalTransactionsOverTime => GetTransactionCount(),
            ChartDataType.AverageShippingCosts => GetAverageShippingCosts(),

            // Geographic charts
            ChartDataType.WorldMap => GetWorldMapData(),
            ChartDataType.CountriesOfOrigin => GetSalesByCountryOfOrigin(),
            ChartDataType.CountriesOfDestination => GetPurchasesByCountryOfDestination(),
            ChartDataType.CompaniesOfOrigin => GetSalesByCompanyOfOrigin(),
            ChartDataType.CompaniesOfDestination => GetSalesByCompanyOfDestination(),

            // Accountant charts
            ChartDataType.AccountantsTransactions => GetTransactionsByAccountant(),

            // Customer charts
            ChartDataType.TopCustomersByRevenue => GetTopCustomersByRevenue(),
            ChartDataType.CustomerPaymentStatus => GetCustomerPaymentStatus(),
            ChartDataType.CustomerGrowth => GetCustomerGrowth(),
            ChartDataType.CustomerLifetimeValue => GetCustomerLifetimeValue(),
            ChartDataType.ActiveVsInactiveCustomers => GetActiveVsInactiveCustomers(),
            ChartDataType.RentalsPerCustomer => GetRentalsPerCustomer(),

            // Returns charts
            ChartDataType.ReturnsOverTime => GetReturnsOverTime(),
            ChartDataType.ReturnReasons => GetReturnReasons(),
            ChartDataType.ReturnFinancialImpact => GetReturnFinancialImpact(),
            ChartDataType.ReturnsByCategory => GetReturnsByCategory(),
            ChartDataType.ReturnsByProduct => GetReturnsByProduct(),
            ChartDataType.PurchaseVsSaleReturns => GetPurchaseVsSaleReturns(),

            // Losses charts
            ChartDataType.LossesOverTime => GetLossesOverTime(),
            ChartDataType.LossReasons => GetLossReasons(),
            ChartDataType.LossFinancialImpact => GetLossFinancialImpact(),
            ChartDataType.LossesByCategory => GetLossesByCategory(),
            ChartDataType.LossesByProduct => GetLossesByProduct(),
            ChartDataType.PurchaseVsSaleLosses => GetPurchaseVsSaleLosses(),

            _ => new List<ChartDataPoint>()
        };
    }

    #endregion
}
