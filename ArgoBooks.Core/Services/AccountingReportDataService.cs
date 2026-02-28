using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for computing accounting report data from transaction records.
/// Aggregates from Revenue, Expense, Invoice, Payment, PurchaseOrder, and InventoryItem data
/// without using a Chart of Accounts or double-entry bookkeeping.
/// </summary>
public class AccountingReportDataService
{
    private readonly CompanyData? _companyData;
    private readonly ReportFilters _filters;

    public AccountingReportDataService(CompanyData? companyData, ReportFilters filters)
    {
        _companyData = companyData;
        _filters = filters;
    }

    /// <summary>
    /// Formats a currency amount using the company's configured currency.
    /// </summary>
    private string FormatCurrency(decimal amount)
    {
        var currencyCode = _companyData?.Settings.Localization.Currency ?? "USD";
        return CurrencyInfo.FormatAmount(amount, currencyCode);
    }

    /// <summary>
    /// Formats a currency amount, wrapping negative values in parentheses.
    /// </summary>
    private string FormatCurrencyWithSign(decimal amount)
    {
        if (amount < 0)
            return $"({FormatCurrency(Math.Abs(amount))})";
        return FormatCurrency(amount);
    }

    /// <summary>
    /// Checks whether a date falls within the configured filter range.
    /// </summary>
    private bool IsInDateRange(DateTime date)
    {
        if (_filters.StartDate.HasValue && date < _filters.StartDate.Value)
            return false;
        if (_filters.EndDate.HasValue && date > _filters.EndDate.Value)
            return false;
        return true;
    }

    /// <summary>
    /// Checks whether a date falls on or before the end date filter.
    /// Used for cumulative/balance calculations.
    /// </summary>
    private bool IsOnOrBeforeEndDate(DateTime date)
    {
        if (_filters.EndDate.HasValue && date > _filters.EndDate.Value)
            return false;
        return true;
    }

    /// <summary>
    /// Gets the earliest transaction date across all data in the company.
    /// Used to replace the hardcoded "All Time" sentinel with the actual start of data.
    /// </summary>
    private DateTime GetEarliestTransactionDate()
    {
        if (_companyData == null) return DateTime.Today;

        var dates = new List<DateTime>();
        if (_companyData.Revenues.Count > 0) dates.Add(_companyData.Revenues.Min(r => r.Date));
        if (_companyData.Expenses.Count > 0) dates.Add(_companyData.Expenses.Min(e => e.Date));
        if (_companyData.Payments.Count > 0) dates.Add(_companyData.Payments.Min(p => p.Date));

        return dates.Count > 0 ? dates.Min() : DateTime.Today;
    }

    /// <summary>
    /// Gets the effective start date, replacing the "All Time" sentinel (year 2000) with
    /// the actual earliest transaction date from the company data.
    /// </summary>
    private DateTime GetEffectiveStartDate()
    {
        if (_filters.StartDate.HasValue && _filters.StartDate.Value.Year <= 2000)
            return GetEarliestTransactionDate();
        return _filters.StartDate ?? DateTime.Today;
    }


    /// <summary>
    /// Dispatches to the appropriate report generation method based on report type.
    /// </summary>
    public AccountingTableData GetReportData(AccountingReportType reportType)
    {
        return reportType switch
        {
            AccountingReportType.IncomeStatement => GetIncomeStatementData(),
            AccountingReportType.BalanceSheet => GetBalanceSheetData(),
            AccountingReportType.CashFlowStatement => GetCashFlowData(),
            AccountingReportType.TrialBalance => GetTrialBalanceData(),
            AccountingReportType.GeneralLedger => GetGeneralLedgerData(),
            AccountingReportType.AccountsReceivableAging => GetARAgingData(),
            AccountingReportType.AccountsPayableAging => GetAPAgingData(),
            AccountingReportType.TaxSummary => GetTaxSummaryData(),
            _ => new AccountingTableData { Title = "Unknown Report" }
        };
    }

    /// <summary>
    /// Resolves a category name from a product ID by looking up the product's category.
    /// </summary>
    private string GetCategoryNameForProduct(string? productId)
    {
        if (string.IsNullOrEmpty(productId) || _companyData == null)
            return "Uncategorized";

        var product = _companyData.GetProduct(productId);
        if (product == null || string.IsNullOrEmpty(product.CategoryId))
            return "Uncategorized";

        var category = _companyData.GetCategory(product.CategoryId);
        return category?.Name ?? "Uncategorized";
    }

    /// <summary>
    /// Groups transaction pre-tax totals by category, derived from line items' product IDs.
    /// Uses Subtotal (pre-tax) because sales tax is a liability, not revenue/expense.
    /// </summary>
    private Dictionary<string, decimal> GroupTransactionsByCategory(
        IEnumerable<Models.Transactions.Transaction> transactions)
    {
        var result = new Dictionary<string, decimal>();

        foreach (var txn in transactions)
        {
            if (txn.LineItems.Count > 0)
            {
                foreach (var lineItem in txn.LineItems)
                {
                    var categoryName = GetCategoryNameForProduct(lineItem.ProductId);
                    if (!result.ContainsKey(categoryName))
                        result[categoryName] = 0;
                    result[categoryName] += lineItem.Subtotal;
                }
            }
            else
            {
                // No line items — use the transaction amount (pre-tax) and try to infer category
                var categoryName = "Uncategorized";
                if (!result.ContainsKey(categoryName))
                    result[categoryName] = 0;
                result[categoryName] += txn.Amount;
            }
        }

        return result;
    }

    /// <summary>
    /// Checks whether an expense's line items reference products that have inventory records,
    /// indicating it is a cost of goods sold rather than an operating expense.
    /// </summary>
    private bool IsInventoryRelatedExpense(Models.Transactions.Transaction expense)
    {
        if (_companyData == null) return false;

        if (expense.LineItems.Count > 0)
        {
            foreach (var li in expense.LineItems)
            {
                if (!string.IsNullOrEmpty(li.ProductId))
                {
                    var product = _companyData.GetProduct(li.ProductId);
                    if (product != null)
                    {
                        // Check if product has inventory records
                        var hasInventory = _companyData.Inventory.Any(i => i.ProductId == li.ProductId);
                        if (hasInventory) return true;
                    }
                }
            }
        }

        return false;
    }

    #region Income Statement

    /// <summary>
    /// Generates Income Statement data showing revenue, expenses, and net income.
    /// </summary>
    private AccountingTableData GetIncomeStatementData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = t.IncomeStatementTitle,
            Subtitle = "",
            ColumnHeaders = [],
            ColumnWidthRatios = [0.65, 0.35]
        };

        if (_companyData == null)
        {
            AddEmptyIncomeStatement(data, t);
            return data;
        }

        // Filter revenues and expenses by date
        var revenues = _companyData.Revenues
            .Where(r => IsInDateRange(r.Date))
            .ToList();

        var expenses = _companyData.Expenses
            .Where(e => IsInDateRange(e.Date))
            .ToList();

        // Split expenses into COGS (inventory-related) and operating expenses
        var cogsExpenses = expenses.Where(IsInventoryRelatedExpense).ToList();
        var operatingExpenses = expenses.Where(e => !IsInventoryRelatedExpense(e)).ToList();

        // Group by category
        var revenueByCategory = GroupTransactionsByCategory(revenues);
        var cogsByCategory = GroupTransactionsByCategory(cogsExpenses);
        var opexByCategory = GroupTransactionsByCategory(operatingExpenses);

        var totalRevenue = revenueByCategory.Values.Sum();
        var totalCogs = cogsByCategory.Values.Sum();
        var grossProfit = totalRevenue - totalCogs;
        var totalOpex = opexByCategory.Values.Sum();
        var netIncome = grossProfit - totalOpex;

        // Revenue section
        data.Rows.Add(new AccountingRow
        {
            Label = t.Revenue,
            RowType = AccountingRowType.SectionHeader,
            Values = ["Amount"]
        });

        foreach (var kvp in revenueByCategory.OrderBy(k => k.Key))
        {
            data.Rows.Add(new AccountingRow
            {
                Label = kvp.Key,
                Values = [FormatCurrency(kvp.Value)],
                IndentLevel = 1,
                RowType = AccountingRowType.DataRow
            });
        }

        data.Rows.Add(new AccountingRow
        {
            Label = t.TotalRevenue,
            Values = [FormatCurrency(totalRevenue)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Cost of Goods Sold section
        data.Rows.Add(new AccountingRow
        {
            Label = t.CostOfGoodsSold,
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        if (cogsByCategory.Count > 0)
        {
            foreach (var kvp in cogsByCategory.OrderBy(k => k.Key))
            {
                data.Rows.Add(new AccountingRow
                {
                    Label = kvp.Key,
                    Values = [FormatCurrency(kvp.Value)],
                    IndentLevel = 1,
                    RowType = AccountingRowType.DataRow
                });
            }
        }

        data.Rows.Add(new AccountingRow
        {
            Label = t.TotalCostOfGoodsSold,
            Values = [FormatCurrency(totalCogs)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Gross Profit
        data.Rows.Add(new AccountingRow
        {
            Label = t.GrossProfit,
            Values = [FormatCurrencyWithSign(grossProfit)],
            RowType = AccountingRowType.TotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Operating Expenses section
        data.Rows.Add(new AccountingRow
        {
            Label = t.OperatingExpenses,
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        foreach (var kvp in opexByCategory.OrderBy(k => k.Key))
        {
            data.Rows.Add(new AccountingRow
            {
                Label = kvp.Key,
                Values = [FormatCurrency(kvp.Value)],
                IndentLevel = 1,
                RowType = AccountingRowType.DataRow
            });
        }

        data.Rows.Add(new AccountingRow
        {
            Label = t.TotalOperatingExpenses,
            Values = [FormatCurrency(totalOpex)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Net Income
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = t.NetIncome,
            Values = [FormatCurrencyWithSign(netIncome)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    private void AddEmptyIncomeStatement(AccountingTableData data, AccountingTerms t)
    {
        data.Rows.Add(new AccountingRow { Label = t.Revenue, RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.TotalRevenue, Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.CostOfGoodsSold, RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.TotalCostOfGoodsSold, Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.GrossProfit, Values = [FormatCurrency(0)], RowType = AccountingRowType.TotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.OperatingExpenses, RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.TotalOperatingExpenses, Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.NetIncome, Values = [FormatCurrency(0)], RowType = AccountingRowType.GrandTotalRow });
    }

    #endregion

    #region Balance Sheet

    /// <summary>
    /// Generates Balance Sheet data showing assets, liabilities, and equity.
    /// </summary>
    private AccountingTableData GetBalanceSheetData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = t.BalanceSheetTitle,
            Subtitle = "",
            ColumnHeaders = [],
            ColumnWidthRatios = [0.65, 0.35],
            Footnote = "Cash balance estimated from recorded transactions."
        };

        if (_companyData == null)
        {
            AddEmptyBalanceSheet(data, t);
            return data;
        }

        // Cash = Revenue (Paid, no invoice) + Payments - Expenses, all filtered by date
        // Uses pre-tax amounts to match Income Statement (tax is a liability, not revenue/expense)
        var cashFromRevenue = _companyData.Revenues
            .Where(r => r.PaymentStatus == "Paid"
                        && string.IsNullOrEmpty(r.InvoiceId)
                        && IsOnOrBeforeEndDate(r.Date))
            .Sum(r => r.EffectiveSubtotalUSD);

        var cashFromPayments = _companyData.Payments
            .Where(p => IsOnOrBeforeEndDate(p.Date))
            .Sum(p => p.EffectiveAmountUSD);

        var cashPaidForExpenses = _companyData.Expenses
            .Where(e => IsOnOrBeforeEndDate(e.Date))
            .Sum(e => e.EffectiveSubtotalUSD);

        var cash = cashFromRevenue + cashFromPayments - cashPaidForExpenses;

        // Accounts Receivable = unpaid/uncancelled invoices
        var accountsReceivable = _companyData.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.EffectiveBalanceUSD);

        var totalCurrentAssets = cash + accountsReceivable;
        var totalAssets = totalCurrentAssets;

        // Accounts Payable = purchase orders not received and not cancelled
        var accountsPayable = _companyData.PurchaseOrders
            .Where(po => po.Status != PurchaseOrderStatus.Received
                         && po.Status != PurchaseOrderStatus.Cancelled)
            .Sum(po => po.Total);

        var totalLiabilities = accountsPayable;

        // Retained Earnings derived as balancing figure so Assets = Liabilities + Equity.
        // This is standard for simplified bookkeeping systems without full double-entry.
        var retainedEarnings = totalAssets - totalLiabilities;
        var totalEquity = retainedEarnings;

        // ASSETS
        data.Rows.Add(new AccountingRow
        {
            Label = "ASSETS",
            RowType = AccountingRowType.SectionHeader,
            Values = ["Amount"]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Current Assets",
            RowType = AccountingRowType.SectionHeader,
            IndentLevel = 0,
            Values = [""]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Cash (Estimated)",
            Values = [FormatCurrencyWithSign(cash)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = t.AccountsReceivable,
            Values = [FormatCurrency(accountsReceivable)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Total Current Assets",
            Values = [FormatCurrencyWithSign(totalCurrentAssets)],
            IndentLevel = 0,
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTAL ASSETS",
            Values = [FormatCurrencyWithSign(totalAssets)],
            RowType = AccountingRowType.TotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // LIABILITIES
        data.Rows.Add(new AccountingRow
        {
            Label = "LIABILITIES",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = t.AccountsPayable,
            Values = [FormatCurrency(accountsPayable)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTAL LIABILITIES",
            Values = [FormatCurrency(totalLiabilities)],
            RowType = AccountingRowType.TotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // EQUITY
        data.Rows.Add(new AccountingRow
        {
            Label = "EQUITY",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Retained Earnings",
            Values = [FormatCurrencyWithSign(retainedEarnings)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTAL EQUITY",
            Values = [FormatCurrencyWithSign(totalEquity)],
            RowType = AccountingRowType.TotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // TOTAL LIABILITIES & EQUITY
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTAL LIABILITIES & EQUITY",
            Values = [FormatCurrencyWithSign(totalLiabilities + totalEquity)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    private void AddEmptyBalanceSheet(AccountingTableData data, AccountingTerms t)
    {
        data.Rows.Add(new AccountingRow { Label = "ASSETS", RowType = AccountingRowType.SectionHeader, Values = ["Amount"] });
        data.Rows.Add(new AccountingRow { Label = "Current Assets", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Cash (Estimated)", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = t.AccountsReceivable, Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Total Current Assets", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "TOTAL ASSETS", Values = [FormatCurrency(0)], RowType = AccountingRowType.TotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "LIABILITIES", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.AccountsPayable, Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "TOTAL LIABILITIES", Values = [FormatCurrency(0)], RowType = AccountingRowType.TotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "EQUITY", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Retained Earnings", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "TOTAL EQUITY", Values = [FormatCurrency(0)], RowType = AccountingRowType.TotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "TOTAL LIABILITIES & EQUITY", Values = [FormatCurrency(0)], RowType = AccountingRowType.GrandTotalRow });
    }

    #endregion

    #region Cash Flow Statement

    /// <summary>
    /// Generates Cash Flow Statement data showing operating, investing, and financing activities.
    /// </summary>
    private AccountingTableData GetCashFlowData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = "Cash Flow Statement",
            Subtitle = "",
            ColumnHeaders = [],
            ColumnWidthRatios = [0.65, 0.35]
        };

        if (_companyData == null)
        {
            AddEmptyCashFlow(data, t);
            return data;
        }

        // Operating Activities
        // Exclude invoice-linked revenue to avoid double counting with Payments
        // Uses pre-tax amounts to match Income Statement (tax is a liability, not revenue/expense)
        var cashFromSales = _companyData.Revenues
            .Where(r => r.PaymentStatus == "Paid"
                        && string.IsNullOrEmpty(r.InvoiceId)
                        && IsInDateRange(r.Date))
            .Sum(r => r.EffectiveSubtotalUSD);

        var cashFromInvoicePayments = _companyData.Payments
            .Where(p => IsInDateRange(p.Date))
            .Sum(p => p.EffectiveAmountUSD);

        var cashPaidForExpenses = _companyData.Expenses
            .Where(e => IsInDateRange(e.Date))
            .Sum(e => e.EffectiveSubtotalUSD);

        var totalOperating = cashFromSales + cashFromInvoicePayments - cashPaidForExpenses;

        var netChange = totalOperating;

        // Operating section
        data.Rows.Add(new AccountingRow
        {
            Label = "OPERATING ACTIVITIES",
            RowType = AccountingRowType.SectionHeader,
            Values = ["Amount"]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Cash from Sales",
            Values = [FormatCurrency(cashFromSales)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Cash from Invoice Payments",
            Values = [FormatCurrency(cashFromInvoicePayments)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Cash Paid for Expenses",
            Values = [FormatCurrencyWithSign(-cashPaidForExpenses)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Net Cash from Operating Activities",
            Values = [FormatCurrencyWithSign(totalOperating)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Investing section
        data.Rows.Add(new AccountingRow
        {
            Label = "INVESTING ACTIVITIES",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Net Cash from Investing Activities",
            Values = [FormatCurrency(0)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Financing section
        data.Rows.Add(new AccountingRow
        {
            Label = "FINANCING ACTIVITIES",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Net Cash from Financing Activities",
            Values = [FormatCurrency(0)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Net change
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "NET CHANGE IN CASH",
            Values = [FormatCurrencyWithSign(netChange)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    private void AddEmptyCashFlow(AccountingTableData data, AccountingTerms t)
    {
        data.Rows.Add(new AccountingRow { Label = "OPERATING ACTIVITIES", RowType = AccountingRowType.SectionHeader, Values = ["Amount"] });
        data.Rows.Add(new AccountingRow { Label = "Cash from Sales", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Cash from Invoice Payments", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Cash Paid for Expenses", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Net Cash from Operating Activities", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "INVESTING ACTIVITIES", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Net Cash from Investing Activities", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "FINANCING ACTIVITIES", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Net Cash from Financing Activities", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "NET CHANGE IN CASH", Values = [FormatCurrency(0)], RowType = AccountingRowType.GrandTotalRow });
    }

    #endregion

    #region Trial Balance

    /// <summary>
    /// Generates Trial Balance data showing debit and credit balances for all accounts.
    /// </summary>
    private AccountingTableData GetTrialBalanceData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = "Trial Balance",
            Subtitle = "",
            ColumnHeaders = ["Account", "Debit", "Credit"],
            ColumnWidthRatios = [0.5, 0.25, 0.25]
        };

        if (_companyData == null)
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "TOTALS",
                Values = [FormatCurrency(0), FormatCurrency(0)],
                RowType = AccountingRowType.GrandTotalRow
            });
            return data;
        }

        var totalDebits = 0m;
        var totalCredits = 0m;

        // Cash (debit) = Revenue paid (no invoice) + Payments - Expenses
        // Uses pre-tax amounts to match Income Statement (tax is a liability, not revenue/expense)
        var cashFromRevenue = _companyData.Revenues
            .Where(r => r.PaymentStatus == "Paid"
                        && string.IsNullOrEmpty(r.InvoiceId)
                        && IsOnOrBeforeEndDate(r.Date))
            .Sum(r => r.EffectiveSubtotalUSD);

        var cashFromPayments = _companyData.Payments
            .Where(p => IsOnOrBeforeEndDate(p.Date))
            .Sum(p => p.EffectiveAmountUSD);

        var cashPaidExpenses = _companyData.Expenses
            .Where(e => IsOnOrBeforeEndDate(e.Date))
            .Sum(e => e.EffectiveSubtotalUSD);

        var cash = cashFromRevenue + cashFromPayments - cashPaidExpenses;

        if (cash >= 0)
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "Cash",
                Values = [FormatCurrency(cash), ""],
                RowType = AccountingRowType.DataRow
            });
            totalDebits += cash;
        }
        else
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "Cash",
                Values = ["", FormatCurrency(Math.Abs(cash))],
                RowType = AccountingRowType.DataRow
            });
            totalCredits += Math.Abs(cash);
        }

        // Accounts Receivable (debit)
        var ar = _companyData.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.EffectiveBalanceUSD);

        data.Rows.Add(new AccountingRow
        {
            Label = t.AccountsReceivable,
            Values = [FormatCurrency(ar), ""],
            RowType = AccountingRowType.DataRow
        });
        totalDebits += ar;

        // Accounts Payable (credit)
        var ap = _companyData.PurchaseOrders
            .Where(po => po.Status != PurchaseOrderStatus.Received
                         && po.Status != PurchaseOrderStatus.Cancelled)
            .Sum(po => po.Total);

        data.Rows.Add(new AccountingRow
        {
            Label = t.AccountsPayable,
            Values = ["", FormatCurrency(ap)],
            RowType = AccountingRowType.DataRow
        });
        totalCredits += ap;

        // Revenue categories (credit)
        var revenues = _companyData.Revenues
            .Where(r => IsOnOrBeforeEndDate(r.Date))
            .ToList();
        var revenueByCategory = GroupTransactionsByCategory(revenues);

        foreach (var kvp in revenueByCategory.OrderBy(k => k.Key))
        {
            data.Rows.Add(new AccountingRow
            {
                Label = $"{t.RevenuePrefix} - {kvp.Key}",
                Values = ["", FormatCurrency(kvp.Value)],
                RowType = AccountingRowType.DataRow
            });
            totalCredits += kvp.Value;
        }

        // Expense categories (debit)
        var expenses = _companyData.Expenses
            .Where(e => IsOnOrBeforeEndDate(e.Date))
            .ToList();
        var expenseByCategory = GroupTransactionsByCategory(expenses);

        foreach (var kvp in expenseByCategory.OrderBy(k => k.Key))
        {
            data.Rows.Add(new AccountingRow
            {
                Label = $"{t.ExpensePrefix} - {kvp.Key}",
                Values = [FormatCurrency(kvp.Value), ""],
                RowType = AccountingRowType.DataRow
            });
            totalDebits += kvp.Value;
        }

        // Retained Earnings as balancing entry so debits equal credits
        var retainedEarnings = totalDebits - totalCredits;
        if (retainedEarnings >= 0)
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "Retained Earnings",
                Values = ["", FormatCurrency(retainedEarnings)],
                RowType = AccountingRowType.DataRow
            });
            totalCredits += retainedEarnings;
        }
        else
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "Retained Earnings",
                Values = [FormatCurrency(Math.Abs(retainedEarnings)), ""],
                RowType = AccountingRowType.DataRow
            });
            totalDebits += Math.Abs(retainedEarnings);
        }

        // Separator and totals
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = ["", ""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTALS",
            Values = [FormatCurrency(totalDebits), FormatCurrency(totalCredits)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    #endregion

    #region General Ledger

    /// <summary>
    /// Generates General Ledger data showing all transactions chronologically, grouped by category.
    /// </summary>
    private AccountingTableData GetGeneralLedgerData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = "General Ledger",
            Subtitle = "",
            ColumnHeaders = ["Date", "Description", "Ref", "Debit", "Credit", "Balance"],
            ColumnWidthRatios = [0.12, 0.3, 0.1, 0.16, 0.16, 0.16]
        };

        if (_companyData == null)
            return data;

        // Build a list of all ledger entries grouped by category
        var entries = new Dictionary<string, List<LedgerEntry>>();

        // Revenue transactions (credits)
        foreach (var rev in _companyData.Revenues.Where(r => IsInDateRange(r.Date)))
        {
            if (rev.LineItems.Count > 0)
            {
                foreach (var li in rev.LineItems)
                {
                    var catName = GetCategoryNameForProduct(li.ProductId);
                    AddLedgerEntry(entries, catName, new LedgerEntry
                    {
                        Date = rev.Date,
                        Description = li.Description.Length > 0 ? li.Description : rev.Description,
                        Reference = rev.ReferenceNumber,
                        Debit = 0,
                        Credit = li.Subtotal
                    });
                }
            }
            else
            {
                AddLedgerEntry(entries, t.RevenueCategory, new LedgerEntry
                {
                    Date = rev.Date,
                    Description = rev.Description,
                    Reference = rev.ReferenceNumber,
                    Debit = 0,
                    Credit = rev.EffectiveSubtotalUSD
                });
            }
        }

        // Expense transactions (debits)
        foreach (var exp in _companyData.Expenses.Where(e => IsInDateRange(e.Date)))
        {
            if (exp.LineItems.Count > 0)
            {
                foreach (var li in exp.LineItems)
                {
                    var catName = GetCategoryNameForProduct(li.ProductId);
                    AddLedgerEntry(entries, catName, new LedgerEntry
                    {
                        Date = exp.Date,
                        Description = li.Description.Length > 0 ? li.Description : exp.Description,
                        Reference = exp.ReferenceNumber,
                        Debit = li.Subtotal,
                        Credit = 0
                    });
                }
            }
            else
            {
                AddLedgerEntry(entries, t.ExpensesCategory, new LedgerEntry
                {
                    Date = exp.Date,
                    Description = exp.Description,
                    Reference = exp.ReferenceNumber,
                    Debit = exp.EffectiveSubtotalUSD,
                    Credit = 0
                });
            }
        }

        // Payments (credits to AR / debits to cash)
        foreach (var pmt in _companyData.Payments.Where(p => IsInDateRange(p.Date)))
        {
            var customerName = _companyData.GetCustomer(pmt.CustomerId)?.Name ?? "Unknown";
            AddLedgerEntry(entries, t.PaymentsReceivedCategory, new LedgerEntry
            {
                Date = pmt.Date,
                Description = $"Payment from {customerName}",
                Reference = pmt.ReferenceNumber ?? "",
                Debit = pmt.EffectiveAmountUSD,
                Credit = 0
            });
        }

        // Render grouped entries
        foreach (var group in entries.OrderBy(g => g.Key))
        {
            data.Rows.Add(new AccountingRow
            {
                Label = group.Key.ToUpperInvariant(),
                RowType = AccountingRowType.SectionHeader,
                Values = ["", "", "", "", ""]
            });

            var sortedEntries = group.Value.OrderBy(e => e.Date).ToList();
            var runningBalance = 0m;

            foreach (var entry in sortedEntries)
            {
                runningBalance += entry.Debit - entry.Credit;

                data.Rows.Add(new AccountingRow
                {
                    Label = entry.Date.ToString("MMM dd, yyyy"),
                    Values =
                    [
                        entry.Description,
                        entry.Reference,
                        entry.Debit > 0 ? FormatCurrency(entry.Debit) : "",
                        entry.Credit > 0 ? FormatCurrency(entry.Credit) : "",
                        FormatCurrencyWithSign(runningBalance)
                    ],
                    RowType = AccountingRowType.DataRow
                });
            }

            data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = ["", "", "", "", ""] });
        }

        return data;
    }

    /// <summary>
    /// Helper to add a ledger entry to the grouped dictionary.
    /// </summary>
    private static void AddLedgerEntry(Dictionary<string, List<LedgerEntry>> entries, string category, LedgerEntry entry)
    {
        if (!entries.ContainsKey(category))
            entries[category] = [];
        entries[category].Add(entry);
    }

    /// <summary>
    /// Internal record for building general ledger data.
    /// </summary>
    private class LedgerEntry
    {
        public DateTime Date { get; init; }
        public string Description { get; init; } = "";
        public string Reference { get; init; } = "";
        public decimal Debit { get; init; }
        public decimal Credit { get; init; }
    }

    #endregion

    #region Accounts Receivable Aging

    /// <summary>
    /// Generates Accounts Receivable Aging data showing outstanding invoices grouped by customer and aging bucket.
    /// </summary>
    private AccountingTableData GetARAgingData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = t.ARAgingTitle,
            Subtitle = "",
            ColumnHeaders = [t.CustomerColumn, "Current", "1-30", "31-60", "61-90", "90+", "Total"],
            ColumnWidthRatios = [0.25, 0.125, 0.125, 0.125, 0.125, 0.125, 0.125]
        };

        if (_companyData == null)
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "TOTAL",
                Values = [FormatCurrency(0), FormatCurrency(0), FormatCurrency(0), FormatCurrency(0), FormatCurrency(0), FormatCurrency(0)],
                RowType = AccountingRowType.TotalRow
            });
            return data;
        }

        var today = DateTime.Today;

        // Filter to unpaid, uncancelled invoices
        var openInvoices = _companyData.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToList();

        // Group by customer
        var byCustomer = openInvoices
            .GroupBy(i => i.CustomerId)
            .OrderBy(g => _companyData.GetCustomer(g.Key)?.Name ?? "Unknown");

        var totalCurrent = 0m;
        var total1to30 = 0m;
        var total31to60 = 0m;
        var total61to90 = 0m;
        var total90Plus = 0m;
        var grandTotal = 0m;

        foreach (var group in byCustomer)
        {
            var customerName = _companyData.GetCustomer(group.Key)?.Name ?? "Unknown";
            var current = 0m;
            var days1to30 = 0m;
            var days31to60 = 0m;
            var days61to90 = 0m;
            var days90Plus = 0m;

            foreach (var invoice in group)
            {
                var daysPastDue = (today - invoice.DueDate.Date).Days;
                var balance = invoice.EffectiveBalanceUSD;

                if (daysPastDue <= 0)
                    current += balance;
                else if (daysPastDue <= 30)
                    days1to30 += balance;
                else if (daysPastDue <= 60)
                    days31to60 += balance;
                else if (daysPastDue <= 90)
                    days61to90 += balance;
                else
                    days90Plus += balance;
            }

            var customerTotal = current + days1to30 + days31to60 + days61to90 + days90Plus;

            data.Rows.Add(new AccountingRow
            {
                Label = customerName,
                Values =
                [
                    FormatCurrency(current),
                    FormatCurrency(days1to30),
                    FormatCurrency(days31to60),
                    FormatCurrency(days61to90),
                    FormatCurrency(days90Plus),
                    FormatCurrency(customerTotal)
                ],
                RowType = AccountingRowType.DataRow
            });

            totalCurrent += current;
            total1to30 += days1to30;
            total31to60 += days31to60;
            total61to90 += days61to90;
            total90Plus += days90Plus;
            grandTotal += customerTotal;
        }

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = ["", "", "", "", "", ""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTAL",
            Values =
            [
                FormatCurrency(totalCurrent),
                FormatCurrency(total1to30),
                FormatCurrency(total31to60),
                FormatCurrency(total61to90),
                FormatCurrency(total90Plus),
                FormatCurrency(grandTotal)
            ],
            RowType = AccountingRowType.TotalRow
        });

        return data;
    }

    #endregion

    #region Accounts Payable Aging

    /// <summary>
    /// Generates Accounts Payable Aging data showing outstanding purchase orders grouped by supplier and aging bucket.
    /// </summary>
    private AccountingTableData GetAPAgingData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = t.APAgingTitle,
            Subtitle = "",
            ColumnHeaders = [t.SupplierColumn, "Current", "1-30", "31-60", "61-90", "90+", "Total"],
            ColumnWidthRatios = [0.25, 0.125, 0.125, 0.125, 0.125, 0.125, 0.125]
        };

        if (_companyData == null)
        {
            data.Rows.Add(new AccountingRow
            {
                Label = "TOTAL",
                Values = [FormatCurrency(0), FormatCurrency(0), FormatCurrency(0), FormatCurrency(0), FormatCurrency(0), FormatCurrency(0)],
                RowType = AccountingRowType.TotalRow
            });
            return data;
        }

        var today = DateTime.Today;

        // Filter to open purchase orders (not received, not cancelled)
        var openPOs = _companyData.PurchaseOrders
            .Where(po => po.Status != PurchaseOrderStatus.Received
                         && po.Status != PurchaseOrderStatus.Cancelled)
            .ToList();

        // Group by supplier
        var bySupplier = openPOs
            .GroupBy(po => po.SupplierId)
            .OrderBy(g => _companyData.GetSupplier(g.Key)?.Name ?? "Unknown");

        var totalCurrent = 0m;
        var total1to30 = 0m;
        var total31to60 = 0m;
        var total61to90 = 0m;
        var total90Plus = 0m;
        var grandTotal = 0m;

        foreach (var group in bySupplier)
        {
            var supplierName = _companyData.GetSupplier(group.Key)?.Name ?? "Unknown";
            var current = 0m;
            var days1to30 = 0m;
            var days31to60 = 0m;
            var days61to90 = 0m;
            var days90Plus = 0m;

            foreach (var po in group)
            {
                var daysPastDue = (today - po.ExpectedDeliveryDate.Date).Days;
                var amount = po.Total;

                if (daysPastDue <= 0)
                    current += amount;
                else if (daysPastDue <= 30)
                    days1to30 += amount;
                else if (daysPastDue <= 60)
                    days31to60 += amount;
                else if (daysPastDue <= 90)
                    days61to90 += amount;
                else
                    days90Plus += amount;
            }

            var supplierTotal = current + days1to30 + days31to60 + days61to90 + days90Plus;

            data.Rows.Add(new AccountingRow
            {
                Label = supplierName,
                Values =
                [
                    FormatCurrency(current),
                    FormatCurrency(days1to30),
                    FormatCurrency(days31to60),
                    FormatCurrency(days61to90),
                    FormatCurrency(days90Plus),
                    FormatCurrency(supplierTotal)
                ],
                RowType = AccountingRowType.DataRow
            });

            totalCurrent += current;
            total1to30 += days1to30;
            total31to60 += days31to60;
            total61to90 += days61to90;
            total90Plus += days90Plus;
            grandTotal += supplierTotal;
        }

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = ["", "", "", "", "", ""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "TOTAL",
            Values =
            [
                FormatCurrency(totalCurrent),
                FormatCurrency(total1to30),
                FormatCurrency(total31to60),
                FormatCurrency(total61to90),
                FormatCurrency(total90Plus),
                FormatCurrency(grandTotal)
            ],
            RowType = AccountingRowType.TotalRow
        });

        return data;
    }

    #endregion

    #region Country-Specific Accounting Terminology

    /// <summary>
    /// Holds all country-specific labels used across accounting reports.
    /// Defaults are US GAAP terminology.
    /// </summary>
    private class AccountingTerms
    {
        // Income Statement
        public string IncomeStatementTitle { get; set; } = "Income Statement";
        public string Revenue { get; set; } = "REVENUE";
        public string TotalRevenue { get; set; } = "Total Revenue";
        public string CostOfGoodsSold { get; set; } = "COST OF GOODS SOLD";
        public string TotalCostOfGoodsSold { get; set; } = "Total Cost of Goods Sold";
        public string GrossProfit { get; set; } = "GROSS PROFIT";
        public string OperatingExpenses { get; set; } = "OPERATING EXPENSES";
        public string TotalOperatingExpenses { get; set; } = "Total Operating Expenses";
        public string NetIncome { get; set; } = "NET INCOME";

        // Balance Sheet
        public string BalanceSheetTitle { get; set; } = "Balance Sheet";
        public string AccountsReceivable { get; set; } = "Accounts Receivable";
        public string AccountsPayable { get; set; } = "Accounts Payable";

        // Trial Balance
        public string RevenuePrefix { get; set; } = "Revenue";
        public string ExpensePrefix { get; set; } = "Expense";

        // AR/AP Aging
        public string ARAgingTitle { get; set; } = "Accounts Receivable Aging";
        public string APAgingTitle { get; set; } = "Accounts Payable Aging";
        public string CustomerColumn { get; set; } = "Customer";
        public string SupplierColumn { get; set; } = "Supplier";

        // General Ledger
        public string RevenueCategory { get; set; } = "Revenue";
        public string ExpensesCategory { get; set; } = "Expenses";
        public string PaymentsReceivedCategory { get; set; } = "Payments Received";

        // Tax Summary
        public string TaxCollectedHeader { get; set; } = "TAX COLLECTED";
        public string TaxCollectedLineFormat { get; set; } = "Tax Collected at {0}%";
        public string TaxCollectedTotal { get; set; } = "Total Tax Collected";
        public string TaxPaidHeader { get; set; } = "TAX PAID";
        public string TaxPaidLineFormat { get; set; } = "Tax Paid at {0}%";
        public string TaxPaidTotal { get; set; } = "Total Tax Paid";
        public string NetTaxLabel { get; set; } = "NET TAX LIABILITY";
    }

    /// <summary>
    /// Determines the accounting tradition based on the company's country and
    /// returns all report labels using the appropriate terminology.
    /// Three traditions: US GAAP (Americas), UK (Commonwealth), IFRS (EU + rest of world).
    /// </summary>
    private AccountingTerms GetAccountingTerms()
    {
        var country = _companyData?.Settings.Company.Country;
        var normalized = country?.Trim().ToUpperInvariant() ?? "";

        // Determine accounting tradition from country
        var tradition = normalized switch
        {
            "UNITED STATES" or "CANADA" or "PUERTO RICO" => "US",

            "UNITED KINGDOM" or "IRELAND" or "AUSTRALIA" or "NEW ZEALAND"
            or "SOUTH AFRICA" or "INDIA" or "SINGAPORE" or "MALAYSIA"
            or "HONG KONG" or "KENYA" or "NIGERIA" or "GHANA" or "PAKISTAN"
            or "BANGLADESH" or "SRI LANKA" or "ZIMBABWE" or "BOTSWANA"
            or "JAMAICA" or "TRINIDAD AND TOBAGO" => "UK",

            "FRANCE" or "GERMANY" or "ITALY" or "SPAIN" or "NETHERLANDS"
            or "BELGIUM" or "AUSTRIA" or "SWEDEN" or "NORWAY" or "DENMARK"
            or "FINLAND" or "PORTUGAL" or "GREECE" or "SWITZERLAND" or "POLAND"
            or "CZECH REPUBLIC" or "HUNGARY" or "ROMANIA" or "BULGARIA" or "CROATIA"
            or "SLOVAKIA" or "SLOVENIA" or "LITHUANIA" or "LATVIA" or "ESTONIA"
            or "LUXEMBOURG" or "MALTA" or "CYPRUS" or "ICELAND"
            or "TURKEY" or "RUSSIA" or "UKRAINE" or "BRAZIL" or "ARGENTINA"
            or "CHILE" or "COLOMBIA" or "MEXICO" or "PERU"
            or "ISRAEL" or "UNITED ARAB EMIRATES" or "SAUDI ARABIA"
            or "JAPAN" or "SOUTH KOREA" or "CHINA" or "TAIWAN"
            or "THAILAND" or "VIETNAM" or "INDONESIA" or "PHILIPPINES" => "IFRS",

            _ => "US"
        };

        // Determine tax system (orthogonal to accounting tradition)
        var taxSystem = normalized switch
        {
            "UNITED KINGDOM" or "FRANCE" or "GERMANY" or "ITALY" or "SPAIN" or "NETHERLANDS"
            or "BELGIUM" or "AUSTRIA" or "SWEDEN" or "NORWAY" or "DENMARK" or "FINLAND"
            or "IRELAND" or "PORTUGAL" or "GREECE" or "SWITZERLAND" or "POLAND"
            or "CZECH REPUBLIC" or "HUNGARY" or "ROMANIA" or "BULGARIA" or "CROATIA"
            or "SLOVAKIA" or "SLOVENIA" or "LITHUANIA" or "LATVIA" or "ESTONIA"
            or "LUXEMBOURG" or "MALTA" or "CYPRUS" or "SOUTH AFRICA" or "KENYA"
            or "NIGERIA" or "TURKEY" or "RUSSIA" or "UKRAINE" or "BRAZIL"
            or "ARGENTINA" or "CHILE" or "COLOMBIA" or "MEXICO" or "PERU"
            or "ISRAEL" or "UNITED ARAB EMIRATES" or "SAUDI ARABIA" or "THAILAND"
            or "VIETNAM" or "INDONESIA" or "PHILIPPINES" or "SOUTH KOREA"
            or "CHINA" or "TAIWAN" or "ICELAND" => "VAT",

            "CANADA" => "GST/HST",
            "INDIA" or "SINGAPORE" or "MALAYSIA" or "AUSTRALIA" or "NEW ZEALAND" => "GST",
            "JAPAN" => "JCT",
            "UNITED STATES" => "SALES_TAX",

            _ => "TAX"
        };

        var terms = new AccountingTerms();

        // Apply accounting tradition overrides
        switch (tradition)
        {
            case "UK":
                terms.IncomeStatementTitle = "Profit & Loss";
                terms.Revenue = "TURNOVER";
                terms.TotalRevenue = "Total Turnover";
                terms.CostOfGoodsSold = "COST OF SALES";
                terms.TotalCostOfGoodsSold = "Total Cost of Sales";
                terms.OperatingExpenses = "OVERHEADS";
                terms.TotalOperatingExpenses = "Total Overheads";
                terms.NetIncome = "NET PROFIT";
                terms.AccountsReceivable = "Trade Debtors";
                terms.AccountsPayable = "Trade Creditors";
                terms.RevenuePrefix = "Turnover";
                terms.ARAgingTitle = "Trade Debtors Aging";
                terms.APAgingTitle = "Trade Creditors Aging";
                terms.RevenueCategory = "Turnover";
                break;

            case "IFRS":
                terms.CostOfGoodsSold = "COST OF SALES";
                terms.TotalCostOfGoodsSold = "Total Cost of Sales";
                terms.NetIncome = "NET PROFIT";
                terms.AccountsReceivable = "Trade Receivables";
                terms.AccountsPayable = "Trade Payables";
                terms.ARAgingTitle = "Trade Receivables Aging";
                terms.APAgingTitle = "Trade Payables Aging";
                break;
        }

        // Apply tax system terminology
        switch (taxSystem)
        {
            case "VAT":
                terms.TaxCollectedHeader = "VAT COLLECTED";
                terms.TaxCollectedLineFormat = "VAT Collected at {0}%";
                terms.TaxCollectedTotal = "Total VAT Collected";
                terms.TaxPaidHeader = "VAT PAID (INPUT VAT)";
                terms.TaxPaidLineFormat = "Input VAT at {0}%";
                terms.TaxPaidTotal = "Total Input VAT";
                terms.NetTaxLabel = "NET VAT PAYABLE";
                break;
            case "GST/HST":
                terms.TaxCollectedHeader = "GST/HST COLLECTED";
                terms.TaxCollectedLineFormat = "GST/HST Collected at {0}%";
                terms.TaxCollectedTotal = "Total GST/HST Collected";
                terms.TaxPaidHeader = "GST/HST PAID (INPUT TAX CREDITS)";
                terms.TaxPaidLineFormat = "ITC at {0}%";
                terms.TaxPaidTotal = "Total Input Tax Credits";
                terms.NetTaxLabel = "NET GST/HST PAYABLE";
                break;
            case "GST":
                terms.TaxCollectedHeader = "GST COLLECTED";
                terms.TaxCollectedLineFormat = "GST Collected at {0}%";
                terms.TaxCollectedTotal = "Total GST Collected";
                terms.TaxPaidHeader = "GST PAID (INPUT TAX CREDITS)";
                terms.TaxPaidLineFormat = "Input Tax Credit at {0}%";
                terms.TaxPaidTotal = "Total Input Tax Credits";
                terms.NetTaxLabel = "NET GST PAYABLE";
                break;
            case "JCT":
                terms.TaxCollectedHeader = "CONSUMPTION TAX COLLECTED";
                terms.TaxCollectedLineFormat = "Consumption Tax at {0}%";
                terms.TaxCollectedTotal = "Total Consumption Tax Collected";
                terms.TaxPaidHeader = "CONSUMPTION TAX PAID";
                terms.TaxPaidLineFormat = "Consumption Tax Paid at {0}%";
                terms.TaxPaidTotal = "Total Consumption Tax Paid";
                terms.NetTaxLabel = "NET CONSUMPTION TAX LIABILITY";
                break;
            case "SALES_TAX":
                terms.TaxCollectedHeader = "SALES TAX COLLECTED";
                terms.TaxCollectedLineFormat = "Sales Tax Collected at {0}%";
                terms.TaxCollectedTotal = "Total Sales Tax Collected";
                terms.TaxPaidHeader = "SALES TAX PAID";
                terms.TaxPaidLineFormat = "Sales Tax Paid at {0}%";
                terms.TaxPaidTotal = "Total Sales Tax Paid";
                terms.NetTaxLabel = "NET SALES TAX LIABILITY";
                break;
        }

        return terms;
    }

    #endregion

    #region Tax Summary

    /// <summary>
    /// Generates Tax Summary data showing taxes collected, taxes paid, and net tax liability.
    /// </summary>
    private AccountingTableData GetTaxSummaryData()
    {
        var t = GetAccountingTerms();
        var data = new AccountingTableData
        {
            Title = "Tax Summary",
            Subtitle = "",
            ColumnHeaders = [],
            ColumnWidthRatios = [0.65, 0.35]
        };

        if (_companyData == null)
        {
            AddEmptyTaxSummary(data, t);
            return data;
        }

        // Tax collected from revenue, grouped by tax rate
        var filteredRevenues = _companyData.Revenues
            .Where(r => IsInDateRange(r.Date))
            .ToList();

        // Round tax rates to 4 decimal places to consolidate near-identical rates
        var taxCollectedByRate = new Dictionary<decimal, decimal>();
        foreach (var rev in filteredRevenues)
        {
            if (rev.LineItems.Count > 0)
            {
                foreach (var li in rev.LineItems)
                {
                    if (li.TaxRate > 0)
                    {
                        var rate = Math.Round(li.TaxRate, 4);
                        if (!taxCollectedByRate.ContainsKey(rate))
                            taxCollectedByRate[rate] = 0;
                        taxCollectedByRate[rate] += li.TaxAmount;
                    }
                }
            }
            else if (rev.TaxRate > 0)
            {
                var rate = Math.Round(rev.TaxRate, 4);
                if (!taxCollectedByRate.ContainsKey(rate))
                    taxCollectedByRate[rate] = 0;
                taxCollectedByRate[rate] += rev.TaxAmount;
            }
        }

        // Tax paid on expenses, grouped by tax rate
        var filteredExpenses = _companyData.Expenses
            .Where(e => IsInDateRange(e.Date))
            .ToList();

        var taxPaidByRate = new Dictionary<decimal, decimal>();
        foreach (var exp in filteredExpenses)
        {
            if (exp.LineItems.Count > 0)
            {
                foreach (var li in exp.LineItems)
                {
                    if (li.TaxRate > 0)
                    {
                        var rate = Math.Round(li.TaxRate, 4);
                        if (!taxPaidByRate.ContainsKey(rate))
                            taxPaidByRate[rate] = 0;
                        taxPaidByRate[rate] += li.TaxAmount;
                    }
                }
            }
            else if (exp.TaxRate > 0)
            {
                var rate = Math.Round(exp.TaxRate, 4);
                if (!taxPaidByRate.ContainsKey(rate))
                    taxPaidByRate[rate] = 0;
                taxPaidByRate[rate] += exp.TaxAmount;
            }
        }

        var totalTaxCollected = taxCollectedByRate.Values.Sum();
        var totalTaxPaid = taxPaidByRate.Values.Sum();
        var netTaxLiability = totalTaxCollected - totalTaxPaid;

        // Tax Collected section
        data.Rows.Add(new AccountingRow
        {
            Label = t.TaxCollectedHeader,
            RowType = AccountingRowType.SectionHeader,
            Values = ["Amount"]
        });

        foreach (var kvp in taxCollectedByRate.OrderBy(k => k.Key))
        {
            var ratePercent = kvp.Key * 100;
            data.Rows.Add(new AccountingRow
            {
                Label = string.Format(t.TaxCollectedLineFormat, ratePercent.ToString("0.##")),
                Values = [FormatCurrency(kvp.Value)],
                IndentLevel = 1,
                RowType = AccountingRowType.DataRow
            });
        }

        data.Rows.Add(new AccountingRow
        {
            Label = t.TaxCollectedTotal,
            Values = [FormatCurrency(totalTaxCollected)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Tax Paid section
        data.Rows.Add(new AccountingRow
        {
            Label = t.TaxPaidHeader,
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        foreach (var kvp in taxPaidByRate.OrderBy(k => k.Key))
        {
            var ratePercent = kvp.Key * 100;
            data.Rows.Add(new AccountingRow
            {
                Label = string.Format(t.TaxPaidLineFormat, ratePercent.ToString("0.##")),
                Values = [FormatCurrency(kvp.Value)],
                IndentLevel = 1,
                RowType = AccountingRowType.DataRow
            });
        }

        data.Rows.Add(new AccountingRow
        {
            Label = t.TaxPaidTotal,
            Values = [FormatCurrency(totalTaxPaid)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Net Tax Liability
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = t.NetTaxLabel,
            Values = [FormatCurrencyWithSign(netTaxLiability)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    private void AddEmptyTaxSummary(AccountingTableData data, AccountingTerms t)
    {
        data.Rows.Add(new AccountingRow { Label = t.TaxCollectedHeader, RowType = AccountingRowType.SectionHeader, Values = ["Amount"] });
        data.Rows.Add(new AccountingRow { Label = t.TaxCollectedTotal, Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.TaxPaidHeader, RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.TaxPaidTotal, Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = t.NetTaxLabel, Values = [FormatCurrency(0)], RowType = AccountingRowType.GrandTotalRow });
    }

    #endregion
}
