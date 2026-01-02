using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Charts;
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
            .GroupBy(s => s.CategoryId ?? "Unknown")
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
    /// Gets sales by customer country.
    /// </summary>
    public List<ChartDataPoint> GetSalesByCountryOfOrigin()
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s =>
            {
                var customer = _companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address?.Country ?? "Unknown";
            })
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
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s =>
            {
                var customer = _companyData.GetCustomer(s.CustomerId ?? "");
                return customer?.Address?.Country;
            })
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => (double)g.Sum(s => s.Total));
    }

    /// <summary>
    /// Gets purchases by supplier country.
    /// </summary>
    public List<ChartDataPoint> GetPurchasesByCountryOfDestination()
    {
        if (_companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p =>
            {
                var supplier = _companyData.GetSupplier(p.SupplierId ?? "");
                return supplier?.Address?.Country ?? "Unknown";
            })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = (double)g.Sum(p => p.Total)
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
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => _companyData.GetCustomer(s.CustomerId ?? "")?.Name ?? "Unknown")
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
    /// Gets average shipping costs over time.
    /// </summary>
    public List<ChartDataPoint> GetAverageShippingCosts()
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate);

        return months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var shippingCosts = new List<decimal>();

            if (_companyData.Sales != null)
            {
                shippingCosts.AddRange(_companyData.Sales
                    .Where(s => s.Date >= monthStart && s.Date <= monthEnd)
                    .Select(s => s.ShippingCost));
            }

            if (_companyData.Purchases != null)
            {
                shippingCosts.AddRange(_companyData.Purchases
                    .Where(p => p.Date >= monthStart && p.Date <= monthEnd)
                    .Select(p => p.ShippingCost));
            }

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = shippingCosts.Count > 0 ? (double)shippingCosts.Average() : 0,
                Date = month
            };
        }).ToList();
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

        // Returns have Items, each with a Reason - group by all item reasons
        return _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .SelectMany(r => r.Items ?? [])
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

    /// <summary>
    /// Gets returns by category.
    /// </summary>
    public List<ChartDataPoint> GetReturnsByCategory()
    {
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Returns have Items with ProductId - group by category of each returned item
        return _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .SelectMany(r => r.Items ?? [])
            .GroupBy(item =>
            {
                var product = _companyData.GetProduct(item.ProductId ?? "");
                var category = product != null ? _companyData.GetCategory(product.CategoryId ?? "") : null;
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
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        // Returns have Items with ProductId - group by product name of each returned item
        return _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .SelectMany(r => r.Items ?? [])
            .GroupBy(item => _companyData.GetProduct(item.ProductId ?? "")?.Name ?? "Unknown")
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
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate);

        // Current Return model represents returns from sales (customer returns)
        var saleReturns = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = _companyData.Returns
                    .Count(r => r.ReturnDate >= monthStart && r.ReturnDate <= monthEnd),
                Date = month
            };
        }).ToList();

        // No purchase returns in current model - return empty series
        var purchaseReturns = months.Select(month => new ChartDataPoint
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
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
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
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
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
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
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
    /// Gets losses by category.
    /// </summary>
    public List<ChartDataPoint> GetLossesByCategory()
    {
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l =>
            {
                var product = _companyData.GetProduct(l.ProductId ?? "");
                var category = product != null ? _companyData.GetCategory(product.CategoryId ?? "") : null;
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
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        return _companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate)
            .GroupBy(l => _companyData.GetProduct(l.ProductId ?? "")?.Name ?? "Unknown")
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
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        var months = GetMonthsBetween(startDate, endDate);

        // Group by reason type - show Damaged vs Lost as the two main categories
        var damagedLosses = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = _companyData.LostDamaged
                    .Count(l => l.DateDiscovered >= monthStart && l.DateDiscovered <= monthEnd &&
                           l.Reason == LostDamagedReason.Damaged),
                Date = month
            };
        }).ToList();

        var lostLosses = months.Select(month =>
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new ChartDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = _companyData.LostDamaged
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
            ChartDataType.AverageShippingCosts => GetAverageShippingCosts(),

            // Geographic charts
            ChartDataType.WorldMap => GetWorldMapData(),
            ChartDataType.CountriesOfOrigin => GetSalesByCountryOfOrigin(),
            ChartDataType.CountriesOfDestination => GetPurchasesByCountryOfDestination(),
            ChartDataType.CompaniesOfOrigin => GetSalesByCompanyOfOrigin(),

            // Accountant charts
            ChartDataType.AccountantsTransactions => GetTransactionsByAccountant(),

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
