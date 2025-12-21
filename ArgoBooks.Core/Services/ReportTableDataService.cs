using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for generating table data for reports.
/// </summary>
public class ReportTableDataService
{
    private readonly CompanyData? _companyData;
    private readonly ReportFilters _filters;

    public ReportTableDataService(CompanyData? companyData, ReportFilters filters)
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

    #region Sales Data

    /// <summary>
    /// Gets sales transaction data for table display.
    /// </summary>
    public List<TransactionTableRow> GetSalesTableData(TableReportElement tableConfig)
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate);

        // Apply data selection
        query = tableConfig.DataSelection switch
        {
            TableDataSelection.TopByAmount => query.OrderByDescending(s => s.Total),
            TableDataSelection.BottomByAmount => query.OrderBy(s => s.Total),
            TableDataSelection.ReturnsOnly => query.Where(s => s.HasReturn),
            _ => query.OrderByDescending(s => s.Date)
        };

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(s => s.Date),
            TableSortOrder.DateDescending => query.OrderByDescending(s => s.Date),
            TableSortOrder.AmountAscending => query.OrderBy(s => s.Total),
            TableSortOrder.AmountDescending => query.OrderByDescending(s => s.Total),
            _ => query
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(s => CreateSalesRow(s)).ToList();
    }

    private TransactionTableRow CreateSalesRow(Sale sale)
    {
        var company = _companyData?.GetCompany(sale.CompanyId ?? "");
        var accountant = _companyData?.GetAccountant(sale.AccountantId ?? "");
        var primaryProduct = sale.Items?.FirstOrDefault();
        var product = primaryProduct != null ? _companyData?.GetProduct(primaryProduct.ProductId ?? "") : null;

        return new TransactionTableRow
        {
            Id = sale.Id,
            TransactionId = sale.TransactionNumber ?? sale.Id,
            Date = sale.Date,
            TransactionType = "Sale",
            CompanyName = company?.Name ?? "Unknown",
            ProductName = product?.Name ?? (sale.Items?.Count > 1 ? $"Multiple ({sale.Items.Count} items)" : "Unknown"),
            Quantity = sale.Items?.Sum(i => i.Quantity) ?? 0,
            UnitPrice = primaryProduct?.UnitPrice ?? 0,
            Total = sale.Total,
            Status = sale.Status.ToString(),
            AccountantName = accountant?.Name ?? "Unknown",
            ShippingCost = sale.ShippingCost,
            Country = sale.ShippingAddress?.Country ?? "",
            HasReturn = sale.HasReturn,
            ReturnAmount = sale.ReturnAmount,
            Notes = sale.Notes ?? ""
        };
    }

    #endregion

    #region Purchase Data

    /// <summary>
    /// Gets purchase transaction data for table display.
    /// </summary>
    public List<TransactionTableRow> GetPurchasesTableData(TableReportElement tableConfig)
    {
        if (_companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = _companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate);

        // Apply data selection
        query = tableConfig.DataSelection switch
        {
            TableDataSelection.TopByAmount => query.OrderByDescending(p => p.Total),
            TableDataSelection.BottomByAmount => query.OrderBy(p => p.Total),
            TableDataSelection.ReturnsOnly => query.Where(p => p.HasReturn),
            _ => query.OrderByDescending(p => p.Date)
        };

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(p => p.Date),
            TableSortOrder.DateDescending => query.OrderByDescending(p => p.Date),
            TableSortOrder.AmountAscending => query.OrderBy(p => p.Total),
            TableSortOrder.AmountDescending => query.OrderByDescending(p => p.Total),
            _ => query
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(p => CreatePurchaseRow(p)).ToList();
    }

    private TransactionTableRow CreatePurchaseRow(Purchase purchase)
    {
        var supplier = _companyData?.GetSupplier(purchase.SupplierId ?? "");
        var accountant = _companyData?.GetAccountant(purchase.AccountantId ?? "");
        var primaryProduct = purchase.Items?.FirstOrDefault();
        var product = primaryProduct != null ? _companyData?.GetProduct(primaryProduct.ProductId ?? "") : null;

        return new TransactionTableRow
        {
            Id = purchase.Id,
            TransactionId = purchase.TransactionNumber ?? purchase.Id,
            Date = purchase.Date,
            TransactionType = "Purchase",
            CompanyName = supplier?.Name ?? "Unknown",
            ProductName = product?.Name ?? (purchase.Items?.Count > 1 ? $"Multiple ({purchase.Items.Count} items)" : "Unknown"),
            Quantity = purchase.Items?.Sum(i => i.Quantity) ?? 0,
            UnitPrice = primaryProduct?.UnitPrice ?? 0,
            Total = purchase.Total,
            Status = purchase.Status.ToString(),
            AccountantName = accountant?.Name ?? "Unknown",
            ShippingCost = purchase.ShippingCost,
            Country = purchase.SupplierAddress?.Country ?? "",
            HasReturn = purchase.HasReturn,
            ReturnAmount = purchase.ReturnAmount,
            Notes = purchase.Notes ?? ""
        };
    }

    #endregion

    #region Combined Data

    /// <summary>
    /// Gets combined transaction data (sales and purchases) for table display.
    /// </summary>
    public List<TransactionTableRow> GetAllTransactionsTableData(TableReportElement tableConfig)
    {
        var sales = _filters.TransactionType is TransactionType.Revenue or TransactionType.Both
            ? GetSalesTableData(tableConfig with { MaxRows = 0 })
            : [];

        var purchases = _filters.TransactionType is TransactionType.Expenses or TransactionType.Both
            ? GetPurchasesTableData(tableConfig with { MaxRows = 0 })
            : [];

        var combined = sales.Concat(purchases).ToList();

        // Apply sort order
        combined = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => combined.OrderBy(t => t.Date).ToList(),
            TableSortOrder.DateDescending => combined.OrderByDescending(t => t.Date).ToList(),
            TableSortOrder.AmountAscending => combined.OrderBy(t => t.Total).ToList(),
            TableSortOrder.AmountDescending => combined.OrderByDescending(t => t.Total).ToList(),
            _ => combined.OrderByDescending(t => t.Date).ToList()
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            combined = combined.Take(tableConfig.MaxRows).ToList();
        }

        return combined;
    }

    #endregion

    #region Returns Data

    /// <summary>
    /// Gets returns data for table display.
    /// </summary>
    public List<ReturnTableRow> GetReturnsTableData(TableReportElement tableConfig)
    {
        if (_companyData?.Returns == null || !_filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = _companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate);

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(r => r.ReturnDate),
            TableSortOrder.DateDescending => query.OrderByDescending(r => r.ReturnDate),
            TableSortOrder.AmountAscending => query.OrderBy(r => r.RefundAmount),
            TableSortOrder.AmountDescending => query.OrderByDescending(r => r.RefundAmount),
            _ => query.OrderByDescending(r => r.ReturnDate)
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(r => CreateReturnRow(r)).ToList();
    }

    private ReturnTableRow CreateReturnRow(Return returnRecord)
    {
        var product = _companyData?.GetProduct(returnRecord.ProductId ?? "");
        var category = product != null ? _companyData?.GetCategory(product.CategoryId ?? "") : null;

        return new ReturnTableRow
        {
            Id = returnRecord.Id,
            ReturnDate = returnRecord.ReturnDate,
            OriginalTransactionId = returnRecord.OriginalTransactionId ?? "",
            ReturnType = returnRecord.ReturnType ?? "Unknown",
            ProductName = product?.Name ?? "Unknown",
            CategoryName = category?.Name ?? "Unknown",
            Quantity = returnRecord.Quantity,
            RefundAmount = returnRecord.RefundAmount,
            Reason = returnRecord.Reason ?? "Not specified",
            Status = returnRecord.Status.ToString(),
            Notes = returnRecord.Notes ?? ""
        };
    }

    #endregion

    #region Losses Data

    /// <summary>
    /// Gets losses data for table display.
    /// </summary>
    public List<LossTableRow> GetLossesTableData(TableReportElement tableConfig)
    {
        if (_companyData?.LostDamaged == null || !_filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = _companyData.LostDamaged
            .Where(l => l.ReportedDate >= startDate && l.ReportedDate <= endDate);

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(l => l.ReportedDate),
            TableSortOrder.DateDescending => query.OrderByDescending(l => l.ReportedDate),
            TableSortOrder.AmountAscending => query.OrderBy(l => l.EstimatedValue),
            TableSortOrder.AmountDescending => query.OrderByDescending(l => l.EstimatedValue),
            _ => query.OrderByDescending(l => l.ReportedDate)
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(l => CreateLossRow(l)).ToList();
    }

    private LossTableRow CreateLossRow(LostDamaged lossRecord)
    {
        var product = _companyData?.GetProduct(lossRecord.ProductId ?? "");
        var category = product != null ? _companyData?.GetCategory(product.CategoryId ?? "") : null;

        return new LossTableRow
        {
            Id = lossRecord.Id,
            ReportedDate = lossRecord.ReportedDate,
            LossType = lossRecord.LossType ?? "Unknown",
            ProductName = product?.Name ?? "Unknown",
            CategoryName = category?.Name ?? "Unknown",
            Quantity = lossRecord.Quantity,
            EstimatedValue = lossRecord.EstimatedValue,
            Reason = lossRecord.Reason.ToString(),
            Location = lossRecord.Location ?? "",
            Notes = lossRecord.Notes ?? ""
        };
    }

    #endregion

    #region Summary Statistics

    /// <summary>
    /// Gets summary statistics for the filtered data.
    /// </summary>
    public ReportSummaryStatistics GetSummaryStatistics()
    {
        var (startDate, endDate) = GetDateRange();

        var stats = new ReportSummaryStatistics
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Calculate revenue statistics
        if (_companyData?.Sales != null &&
            _filters.TransactionType is TransactionType.Revenue or TransactionType.Both)
        {
            var sales = _companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate).ToList();
            stats.TotalRevenue = sales.Sum(s => s.Total);
            stats.RevenueTransactionCount = sales.Count;
            stats.AverageRevenueTransaction = sales.Count > 0 ? stats.TotalRevenue / sales.Count : 0;
            stats.LargestSale = sales.Count > 0 ? sales.Max(s => s.Total) : 0;
            stats.SmallestSale = sales.Count > 0 ? sales.Min(s => s.Total) : 0;
        }

        // Calculate expense statistics
        if (_companyData?.Purchases != null &&
            _filters.TransactionType is TransactionType.Expenses or TransactionType.Both)
        {
            var purchases = _companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate).ToList();
            stats.TotalExpenses = purchases.Sum(p => p.Total);
            stats.ExpenseTransactionCount = purchases.Count;
            stats.AverageExpenseTransaction = purchases.Count > 0 ? stats.TotalExpenses / purchases.Count : 0;
            stats.LargestPurchase = purchases.Count > 0 ? purchases.Max(p => p.Total) : 0;
            stats.SmallestPurchase = purchases.Count > 0 ? purchases.Min(p => p.Total) : 0;
        }

        // Calculate profit
        stats.NetProfit = stats.TotalRevenue - stats.TotalExpenses;
        stats.ProfitMargin = stats.TotalRevenue > 0 ? (stats.NetProfit / stats.TotalRevenue) * 100 : 0;

        // Calculate returns statistics
        if (_companyData?.Returns != null && _filters.IncludeReturns)
        {
            var returns = _companyData.Returns.Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate).ToList();
            stats.TotalReturns = returns.Count;
            stats.TotalReturnAmount = returns.Sum(r => r.RefundAmount);
            stats.ReturnRate = stats.RevenueTransactionCount > 0
                ? (decimal)stats.TotalReturns / stats.RevenueTransactionCount * 100
                : 0;
        }

        // Calculate losses statistics
        if (_companyData?.LostDamaged != null && _filters.IncludeLosses)
        {
            var losses = _companyData.LostDamaged.Where(l => l.ReportedDate >= startDate && l.ReportedDate <= endDate).ToList();
            stats.TotalLosses = losses.Count;
            stats.TotalLossAmount = losses.Sum(l => l.EstimatedValue);
        }

        // Calculate shipping statistics
        if (_companyData != null)
        {
            var shippingCosts = new List<decimal>();

            if (_companyData.Sales != null)
            {
                shippingCosts.AddRange(_companyData.Sales
                    .Where(s => s.Date >= startDate && s.Date <= endDate)
                    .Select(s => s.ShippingCost));
            }

            if (_companyData.Purchases != null)
            {
                shippingCosts.AddRange(_companyData.Purchases
                    .Where(p => p.Date >= startDate && p.Date <= endDate)
                    .Select(p => p.ShippingCost));
            }

            stats.TotalShippingCosts = shippingCosts.Sum();
            stats.AverageShippingCost = shippingCosts.Count > 0 ? shippingCosts.Average() : 0;
        }

        // Calculate growth rate
        stats.GrowthRate = CalculateGrowthRate(startDate, endDate);

        return stats;
    }

    private decimal CalculateGrowthRate(DateTime startDate, DateTime endDate)
    {
        if (_companyData?.Sales == null)
            return 0;

        var periodLength = (endDate - startDate).Days;
        var previousStartDate = startDate.AddDays(-periodLength);
        var previousEndDate = startDate.AddDays(-1);

        var currentRevenue = _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.Total);

        var previousRevenue = _companyData.Sales
            .Where(s => s.Date >= previousStartDate && s.Date <= previousEndDate)
            .Sum(s => s.Total);

        if (previousRevenue == 0)
            return currentRevenue > 0 ? 100 : 0;

        return (currentRevenue - previousRevenue) / previousRevenue * 100;
    }

    #endregion

    #region Top/Bottom Analysis

    /// <summary>
    /// Gets top products by revenue.
    /// </summary>
    public List<ProductAnalysisRow> GetTopProductsByRevenue(int count = 10)
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var productSales = _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .SelectMany(s => s.Items ?? [])
            .GroupBy(i => i.ProductId)
            .Select(g =>
            {
                var product = _companyData.GetProduct(g.Key ?? "");
                var category = product != null ? _companyData.GetCategory(product.CategoryId ?? "") : null;

                return new ProductAnalysisRow
                {
                    ProductId = g.Key ?? "",
                    ProductName = product?.Name ?? "Unknown",
                    CategoryName = category?.Name ?? "Unknown",
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Total),
                    TransactionCount = g.Count(),
                    AveragePrice = g.Average(i => i.UnitPrice)
                };
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(count)
            .ToList();

        return productSales;
    }

    /// <summary>
    /// Gets top customers by revenue.
    /// </summary>
    public List<CustomerAnalysisRow> GetTopCustomersByRevenue(int count = 10)
    {
        if (_companyData?.Sales == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var customerSales = _companyData.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.CompanyId)
            .Select(g =>
            {
                var company = _companyData.GetCompany(g.Key ?? "");

                return new CustomerAnalysisRow
                {
                    CustomerId = g.Key ?? "",
                    CustomerName = company?.Name ?? "Unknown",
                    Country = company?.Address?.Country ?? "",
                    TotalRevenue = g.Sum(s => s.Total),
                    TransactionCount = g.Count(),
                    AverageTransaction = g.Average(s => s.Total),
                    FirstPurchase = g.Min(s => s.Date),
                    LastPurchase = g.Max(s => s.Date)
                };
            })
            .OrderByDescending(c => c.TotalRevenue)
            .Take(count)
            .ToList();

        return customerSales;
    }

    /// <summary>
    /// Gets top suppliers by purchase volume.
    /// </summary>
    public List<SupplierAnalysisRow> GetTopSuppliersByVolume(int count = 10)
    {
        if (_companyData?.Purchases == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var supplierPurchases = _companyData.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.SupplierId)
            .Select(g =>
            {
                var supplier = _companyData.GetSupplier(g.Key ?? "");

                return new SupplierAnalysisRow
                {
                    SupplierId = g.Key ?? "",
                    SupplierName = supplier?.Name ?? "Unknown",
                    Country = supplier?.Address?.Country ?? "",
                    TotalPurchases = g.Sum(p => p.Total),
                    TransactionCount = g.Count(),
                    AverageTransaction = g.Average(p => p.Total),
                    FirstPurchase = g.Min(p => p.Date),
                    LastPurchase = g.Max(p => p.Date)
                };
            })
            .OrderByDescending(s => s.TotalPurchases)
            .Take(count)
            .ToList();

        return supplierPurchases;
    }

    /// <summary>
    /// Gets top accountants by transaction volume.
    /// </summary>
    public List<AccountantAnalysisRow> GetTopAccountantsByVolume(int count = 10)
    {
        if (_companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var accountantData = new Dictionary<string, (decimal Sales, decimal Purchases, int Count)>();

        if (_companyData.Sales != null)
        {
            foreach (var sale in _companyData.Sales.Where(s => s.Date >= startDate && s.Date <= endDate))
            {
                var accountantId = sale.AccountantId ?? "";
                if (!accountantData.ContainsKey(accountantId))
                    accountantData[accountantId] = (0, 0, 0);

                var current = accountantData[accountantId];
                accountantData[accountantId] = (current.Sales + sale.Total, current.Purchases, current.Count + 1);
            }
        }

        if (_companyData.Purchases != null)
        {
            foreach (var purchase in _companyData.Purchases.Where(p => p.Date >= startDate && p.Date <= endDate))
            {
                var accountantId = purchase.AccountantId ?? "";
                if (!accountantData.ContainsKey(accountantId))
                    accountantData[accountantId] = (0, 0, 0);

                var current = accountantData[accountantId];
                accountantData[accountantId] = (current.Sales, current.Purchases + purchase.Total, current.Count + 1);
            }
        }

        return accountantData
            .Select(kvp =>
            {
                var accountant = _companyData.GetAccountant(kvp.Key);
                return new AccountantAnalysisRow
                {
                    AccountantId = kvp.Key,
                    AccountantName = accountant?.Name ?? "Unknown",
                    TotalSales = kvp.Value.Sales,
                    TotalPurchases = kvp.Value.Purchases,
                    TotalVolume = kvp.Value.Sales + kvp.Value.Purchases,
                    TransactionCount = kvp.Value.Count
                };
            })
            .OrderByDescending(a => a.TotalVolume)
            .Take(count)
            .ToList();
    }

    #endregion
}

#region Table Row Models

/// <summary>
/// Represents a transaction row in a table.
/// </summary>
public class TransactionTableRow
{
    public string Id { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AccountantName { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool HasReturn { get; set; }
    public decimal ReturnAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents a return row in a table.
/// </summary>
public class ReturnTableRow
{
    public string Id { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public string OriginalTransactionId { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents a loss row in a table.
/// </summary>
public class LossTableRow
{
    public string Id { get; set; } = string.Empty;
    public DateTime ReportedDate { get; set; }
    public string LossType { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal EstimatedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents summary statistics for a report.
/// </summary>
public class ReportSummaryStatistics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Revenue
    public decimal TotalRevenue { get; set; }
    public int RevenueTransactionCount { get; set; }
    public decimal AverageRevenueTransaction { get; set; }
    public decimal LargestSale { get; set; }
    public decimal SmallestSale { get; set; }

    // Expenses
    public decimal TotalExpenses { get; set; }
    public int ExpenseTransactionCount { get; set; }
    public decimal AverageExpenseTransaction { get; set; }
    public decimal LargestPurchase { get; set; }
    public decimal SmallestPurchase { get; set; }

    // Profit
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal GrowthRate { get; set; }

    // Returns
    public int TotalReturns { get; set; }
    public decimal TotalReturnAmount { get; set; }
    public decimal ReturnRate { get; set; }

    // Losses
    public int TotalLosses { get; set; }
    public decimal TotalLossAmount { get; set; }

    // Shipping
    public decimal TotalShippingCosts { get; set; }
    public decimal AverageShippingCost { get; set; }
}

/// <summary>
/// Represents a product analysis row.
/// </summary>
public class ProductAnalysisRow
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AveragePrice { get; set; }
}

/// <summary>
/// Represents a customer analysis row.
/// </summary>
public class CustomerAnalysisRow
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public DateTime FirstPurchase { get; set; }
    public DateTime LastPurchase { get; set; }
}

/// <summary>
/// Represents a supplier analysis row.
/// </summary>
public class SupplierAnalysisRow
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal TotalPurchases { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public DateTime FirstPurchase { get; set; }
    public DateTime LastPurchase { get; set; }
}

/// <summary>
/// Represents an accountant analysis row.
/// </summary>
public class AccountantAnalysisRow
{
    public string AccountantId { get; set; } = string.Empty;
    public string AccountantName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalVolume { get; set; }
    public int TransactionCount { get; set; }
}

#endregion
