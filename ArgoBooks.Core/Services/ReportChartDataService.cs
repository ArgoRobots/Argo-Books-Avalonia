using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for generating chart data for reports.
/// </summary>
public class ReportChartDataService(CompanyData? companyData, ReportFilters filters)
{
    private readonly Dictionary<ChartDataType, object> _cache = new();

    /// <summary>
    /// Gets the date range based on filters (delegates to shared ReportFilters.GetDateRange).
    /// </summary>
    private (DateTime Start, DateTime End) GetDateRange() => filters.GetDateRange();

    /// <summary>
    /// Gets the effective date range, constrained by actual sales data.
    /// For unbounded ranges (like All Time), uses the actual min/max dates from sales.
    /// </summary>
    private (DateTime Start, DateTime End) GetEffectiveDateRange()
    {
        var (start, end) = GetDateRange();

        if (companyData?.Revenues == null || !companyData.Revenues.Any())
            return (start, end);

        // If date range is very large (e.g., All Time), constrain to actual data range
        var minDataDate = companyData.Revenues.Min(s => s.Date).Date;
        var maxDataDate = companyData.Revenues.Max(s => s.Date).Date;

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
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(s => s.EffectiveSubtotalUSD),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets revenue distribution by category (in USD).
    /// </summary>
    public List<ChartDataPoint> GetRevenueDistribution()
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var sales = companyData.Revenues
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
                    Value = (double)g.Sum(s => s.EffectiveSubtotalUSD)
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
        if (companyData?.Revenues == null)
            return 0;

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.EffectiveSubtotalUSD);
    }

    #endregion

    #region Expense Charts

    /// <summary>
    /// Gets expenses over time data (in USD for consistent calculations).
    /// </summary>
    public List<ChartDataPoint> GetExpensesOverTime()
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString("MMM dd"),
                Value = (double)g.Sum(p => p.EffectiveSubtotalUSD),
                Date = g.Key
            })
            .ToList();
    }

    /// <summary>
    /// Gets expense distribution by category (in USD).
    /// </summary>
    public List<ChartDataPoint> GetExpenseDistribution()
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Expenses
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
                    Value = (double)g.Sum(p => p.EffectiveSubtotalUSD)
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
        if (companyData?.Expenses == null)
            return 0;

        var (startDate, endDate) = GetDateRange();

        return companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .Sum(p => p.EffectiveSubtotalUSD);
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

        var revenueByDate = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.EffectiveSubtotalUSD));

        var expensesByDate = companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.EffectiveSubtotalUSD));

        var allDates = revenueByDate.Keys.Union(expensesByDate.Keys).OrderBy(d => d);

        return allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = (double)((revenueByDate.GetValueOrDefault(date, 0) - expensesByDate.GetValueOrDefault(date, 0))),
            Date = date
        }).ToList();
    }

    /// <summary>
    /// Gets revenue vs expenses comparison data.
    /// </summary>
    public List<ChartSeriesData> GetRevenueVsExpenses()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var allMonths = GetMonthsBetween(startDate, endDate).ToList();

        // Filter to only months that have actual revenue or expense data
        var monthsWithData = allMonths.Where(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var hasRevenue = companyData.Revenues.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasExpenses = companyData.Expenses.Any(p => p.Date >= monthStart && p.Date <= monthEnd);
            return hasRevenue || hasExpenses;
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
                Value = (double)companyData.Revenues
                    .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                    .Sum(s => s.EffectiveSubtotalUSD),
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
                Value = (double)companyData.Expenses
                    .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                    .Sum(p => p.EffectiveSubtotalUSD),
                Date = month
            };
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData },
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData }
        ];
    }

    /// <summary>
    /// Gets revenue vs expenses data grouped by day for granular display.
    /// </summary>
    public List<ChartSeriesData> GetRevenueVsExpensesDaily()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Get all dates with data
        var salesByDate = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => (double)g.Sum(s => s.EffectiveSubtotalUSD));

        var purchasesByDate = companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => (double)g.Sum(p => p.EffectiveSubtotalUSD));

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
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData },
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData }
        ];
    }

    #endregion

    #region Transaction Charts

    /// <summary>
    /// Gets average transaction value over time with separate series for revenue and expense transactions.
    /// </summary>
    public List<ChartSeriesData> GetAverageTransactionValueBySeries()
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

            var hasRevenue = companyData.Revenues.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasExpenses = companyData.Expenses.Any(p => p.Date >= monthStart && p.Date <= monthEnd);

            return hasRevenue || hasExpenses;
        }).ToList();

        if (monthsWithData.Count == 0)
            return [];

        var revenueData = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var transactions = companyData.Revenues
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .Select(s => s.EffectiveSubtotalUSD)
                .ToList();

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = transactions.Count > 0 ? (double)transactions.Average() : 0,
                Date = month
            };
        }).ToList();

        var expenseData = monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var transactions = companyData.Expenses
                .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                .Select(p => p.EffectiveSubtotalUSD)
                .ToList();

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = transactions.Count > 0 ? (double)transactions.Average() : 0,
                Date = month
            };
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData },
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData }
        ];
    }

    /// <summary>
    /// Gets transaction count over time with separate series for revenue and expense transactions.
    /// </summary>
    public List<ChartSeriesData> GetTransactionCountBySeries()
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

            var hasRevenue = companyData.Revenues.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasExpenses = companyData.Expenses.Any(p => p.Date >= monthStart && p.Date <= monthEnd);

            return hasRevenue || hasExpenses;
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
                Value = companyData.Revenues.Count(s => s.Date >= monthStart && s.Date <= monthEnd),
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
                Value = companyData.Expenses.Count(p => p.Date >= monthStart && p.Date <= monthEnd),
                Date = month
            };
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData },
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData }
        ];
    }

    /// <summary>
    /// Gets average transaction value over time grouped by day with separate series for revenue and expense transactions.
    /// </summary>
    public List<ChartSeriesData> GetAverageTransactionValueDailyBySeries()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var revenueValuesByDate = new Dictionary<DateTime, List<decimal>>();
        var expenseValuesByDate = new Dictionary<DateTime, List<decimal>>();

        foreach (var revenue in companyData.Revenues.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var date = revenue.Date.Date;
            if (!revenueValuesByDate.ContainsKey(date))
                revenueValuesByDate[date] = [];
            revenueValuesByDate[date].Add(revenue.EffectiveSubtotalUSD);
        }

        foreach (var expense in companyData.Expenses.Where(p => p.Date >= startDate && p.Date <= endDate))
        {
            var date = expense.Date.Date;
            if (!expenseValuesByDate.ContainsKey(date))
                expenseValuesByDate[date] = [];
            expenseValuesByDate[date].Add(expense.EffectiveSubtotalUSD);
        }

        // Combine all dates
        var allDates = revenueValuesByDate.Keys.Union(expenseValuesByDate.Keys).OrderBy(d => d).ToList();

        if (allDates.Count == 0)
            return [];

        var revenueData = allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = revenueValuesByDate.TryGetValue(date, out var values) && values.Count > 0 ? (double)values.Average() : 0,
            Date = date
        }).ToList();

        var expenseData = allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = expenseValuesByDate.TryGetValue(date, out var values) && values.Count > 0 ? (double)values.Average() : 0,
            Date = date
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData },
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData }
        ];
    }

    /// <summary>
    /// Gets transaction count over time grouped by day with separate series for revenue and expense transactions.
    /// </summary>
    public List<ChartSeriesData> GetTransactionCountDailyBySeries()
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var revenueCountsByDate = new Dictionary<DateTime, int>();
        var expenseCountsByDate = new Dictionary<DateTime, int>();

        foreach (var revenue in companyData.Revenues.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var date = revenue.Date.Date;
            revenueCountsByDate[date] = revenueCountsByDate.GetValueOrDefault(date, 0) + 1;
        }

        foreach (var expense in companyData.Expenses.Where(p => p.Date >= startDate && p.Date <= endDate))
        {
            var date = expense.Date.Date;
            expenseCountsByDate[date] = expenseCountsByDate.GetValueOrDefault(date, 0) + 1;
        }

        // Combine all dates
        var allDates = revenueCountsByDate.Keys.Union(expenseCountsByDate.Keys).OrderBy(d => d).ToList();

        if (allDates.Count == 0)
            return [];

        var revenueData = allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = revenueCountsByDate.GetValueOrDefault(date, 0),
            Date = date
        }).ToList();

        var expenseData = allDates.Select(date => new ChartDataPoint
        {
            Label = date.ToString("MMM dd"),
            Value = expenseCountsByDate.GetValueOrDefault(date, 0),
            Date = date
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Expenses", Color = "#EF4444", DataPoints = expenseData },
            new ChartSeriesData { Name = "Revenue", Color = "#22C55E", DataPoints = revenueData }
        ];
    }

    #endregion

    #region Geographic Charts

    /// <summary>
    /// Gets sales by customer country.
    /// </summary>
    public List<ChartDataPoint> GetRevenueByCountryOfOrigin()
    {
        return GetRevenueByCustomerCountry();
    }

    /// <summary>
    /// Gets sales grouped by customer country.
    /// Used for geographic distribution charts showing where customers are located.
    /// </summary>
    public List<ChartDataPoint> GetRevenueByCustomerCountry()
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s =>
            {
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address.Country ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(s => s.EffectiveSubtotalUSD)
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
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s =>
            {
                var customer = companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address.Country;
            })
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => (double)g.Sum(s => s.EffectiveSubtotalUSD));
    }

    /// <summary>
    /// Gets world map data by supplier country from purchases.
    /// Returns country names as keys.
    /// </summary>
    public Dictionary<string, double> GetWorldMapDataBySupplier()
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p =>
            {
                var supplier = companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address.Country;
            })
            .Where(g => g.Key != null && !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key!, g => (double)g.Sum(p => p.EffectiveSubtotalUSD));
    }

    /// <summary>
    /// Gets purchases by supplier country.
    /// </summary>
    public List<ChartDataPoint> GetExpensesByCountryOfDestination()
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p =>
            {
                var supplier = companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address.Country ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(p => p.EffectiveSubtotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets purchases by supplier company name.
    /// </summary>
    public List<ChartDataPoint> GetExpensesBySupplierCompany()
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => companyData.GetSupplier(p.SupplierId ?? "")?.Name ?? "Unknown")
            .Where(g => g.Key != "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(p => p.EffectiveSubtotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets sales by customer.
    /// </summary>
    public List<ChartDataPoint> GetRevenueByCompanyOfOrigin()
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => companyData.GetCustomer(s.CustomerId ?? "")?.Name ?? "Unknown")
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(s => s.EffectiveSubtotalUSD)
            })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Gets sales by customer company (destination companies).
    /// Only includes customers with CompanyName set.
    /// </summary>
    public List<ChartDataPoint> GetRevenueByCompanyOfDestination()
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .Select(s => new { Revenue = s, Customer = companyData.GetCustomer(s.CustomerId ?? "") })
            .Where(x => x.Customer != null && !string.IsNullOrEmpty(x.Customer.CompanyName))
            .GroupBy(x => x.Customer!.CompanyName)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key!,
                Value = (double)g.Sum(x => x.Revenue.EffectiveSubtotalUSD)
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

            var hasSales = companyData.Revenues.Any(s => s.Date >= monthStart && s.Date <= monthEnd);
            var hasPurchases = companyData.Expenses.Any(p => p.Date >= monthStart && p.Date <= monthEnd);

            return hasSales || hasPurchases;
        }).ToList();

        return monthsWithData.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var shippingCosts = new List<decimal>();

            shippingCosts.AddRange(companyData.Revenues
                .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                .Select(s => s.ShippingCost));

            shippingCosts.AddRange(companyData.Expenses
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

        foreach (var revenue in companyData.Revenues.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var date = revenue.Date.Date;
            if (!shippingByDate.ContainsKey(date))
                shippingByDate[date] = [];
            shippingByDate[date].Add(revenue.ShippingCost);
        }

        foreach (var purchase in companyData.Expenses.Where(p => p.Date >= startDate && p.Date <= endDate))
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

        // Revenue by accountant
        foreach (var revenue in companyData.Revenues.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var accountantName = companyData.GetAccountant(revenue.AccountantId ?? "")?.Name ?? "Unknown";
            accountantData.TryAdd(accountantName, 0);
            accountantData[accountantName] += 1;
        }

        // Expenses by accountant
        foreach (var expense in companyData.Expenses.Where(p => p.Date >= startDate && p.Date <= endDate))
        {
            var accountantName = companyData.GetAccountant(expense.AccountantId ?? "")?.Name ?? "Unknown";
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
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g =>
            {
                var customer = companyData.GetCustomer(g.Key ?? "");
                var customerName = customer?.Name ?? customer?.CompanyName ?? "Unknown";
                return new ChartDataPoint
                {
                    Label = customerName,
                    Value = (double)g.Sum(s => s.EffectiveSubtotalUSD)
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
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var salesInRange = companyData.Revenues
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

        // Group customers by their first transaction date (approximated by earliest revenue)
        var customerFirstTransaction = companyData.Revenues
            .Where(s => !string.IsNullOrEmpty(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .ToDictionary(g => g.Key!, g => g.Min(s => s.Date));

        return customerFirstTransaction
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
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var customerRevenue = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate && !string.IsNullOrEmpty(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalRevenue = g.Sum(s => s.EffectiveSubtotalUSD)
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

        var activeCustomerIds = companyData.Revenues
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
    /// Note: Current model only tracks revenue returns (returns from customers).
    /// </summary>
    public List<ChartSeriesData> GetExpenseVsRevenueReturns()
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

        // Current Return model represents returns from revenue (customer returns)
        var revenueReturns = monthsWithData.Select(month =>
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

        // No expense returns in current model - return empty series
        var expenseReturns = monthsWithData.Select(month => new ChartDataPoint
        {
            Label = month.ToString("MMM yyyy"),
            Value = 0,
            Date = month
        }).ToList();

        return
        [
            new ChartSeriesData { Name = "Revenue Returns", Color = "#EF4444", DataPoints = revenueReturns },
            new ChartSeriesData { Name = "Expense Returns", Color = "#F59E0B", DataPoints = expenseReturns }
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
    public List<ChartSeriesData> GetExpenseVsRevenueLosses()
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
    /// Results are cached per chart type for the lifetime of this service instance.
    /// </summary>
    public object GetChartData(ChartDataType chartType)
    {
        if (_cache.TryGetValue(chartType, out var cached))
            return cached;

        var result = ComputeChartData(chartType);
        _cache[chartType] = result;
        return result;
    }

    /// <summary>
    /// Computes chart data for a specific chart type.
    /// </summary>
    private object ComputeChartData(ChartDataType chartType)
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
            ChartDataType.RevenueVsExpenses => GetRevenueVsExpenses(),

            // Transaction charts
            ChartDataType.AverageTransactionValue => GetAverageTransactionValueBySeries(),
            ChartDataType.TotalTransactions => GetTransactionCountBySeries(),
            ChartDataType.AverageShippingCosts => GetAverageShippingCosts(),

            // Geographic charts
            ChartDataType.WorldMap => GetWorldMapData(),
            ChartDataType.CountriesOfOrigin => GetRevenueByCountryOfOrigin(),
            ChartDataType.CountriesOfDestination => GetExpensesByCountryOfDestination(),
            ChartDataType.CompaniesOfOrigin => GetRevenueByCompanyOfOrigin(),
            ChartDataType.CompaniesOfDestination => GetRevenueByCompanyOfDestination(),

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
            ChartDataType.ExpenseVsRevenueReturns => GetExpenseVsRevenueReturns(),

            // Losses charts
            ChartDataType.LossesOverTime => GetLossesOverTime(),
            ChartDataType.LossReasons => GetLossReasons(),
            ChartDataType.LossFinancialImpact => GetLossFinancialImpact(),
            ChartDataType.LossesByCategory => GetLossesByCategory(),
            ChartDataType.LossesByProduct => GetLossesByProduct(),
            ChartDataType.ExpenseVsRevenueLosses => GetExpenseVsRevenueLosses(),

            _ => new List<ChartDataPoint>()
        };
    }

    #endregion
}
