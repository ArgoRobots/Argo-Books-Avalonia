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
    /// Gets the subtitle string for period-based reports.
    /// </summary>
    private string GetPeriodSubtitle()
    {
        if (_filters.StartDate.HasValue && _filters.EndDate.HasValue)
            return $"For the period {_filters.StartDate.Value:MMM dd, yyyy} to {_filters.EndDate.Value:MMM dd, yyyy}";
        if (_filters.StartDate.HasValue)
            return $"From {_filters.StartDate.Value:MMM dd, yyyy}";
        if (_filters.EndDate.HasValue)
            return $"Through {_filters.EndDate.Value:MMM dd, yyyy}";
        return "As of All Time";
    }

    /// <summary>
    /// Gets the subtitle string for point-in-time reports (e.g., Balance Sheet).
    /// </summary>
    private string GetAsOfSubtitle()
    {
        if (_filters.EndDate.HasValue)
            return $"As of {_filters.EndDate.Value:MMM dd, yyyy}";
        return $"As of {DateTime.Today:MMM dd, yyyy}";
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
    /// Groups transaction totals by category, derived from line items' product IDs.
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
                    result[categoryName] += lineItem.Amount;
                }
            }
            else
            {
                // No line items — use the transaction total and try to infer category
                var categoryName = "Uncategorized";
                if (!result.ContainsKey(categoryName))
                    result[categoryName] = 0;
                result[categoryName] += txn.EffectiveTotalUSD;
            }
        }

        return result;
    }

    #region Income Statement

    /// <summary>
    /// Generates Income Statement data showing revenue, expenses, and net income.
    /// </summary>
    private AccountingTableData GetIncomeStatementData()
    {
        var data = new AccountingTableData
        {
            Title = "Income Statement",
            Subtitle = GetPeriodSubtitle(),
            ColumnHeaders = ["", "Amount"],
            ColumnWidthRatios = [0.65, 0.35]
        };

        if (_companyData == null)
        {
            AddEmptyIncomeStatement(data);
            return data;
        }

        // Filter revenues and expenses by date
        var revenues = _companyData.Revenues
            .Where(r => IsInDateRange(r.Date))
            .ToList();

        var expenses = _companyData.Expenses
            .Where(e => IsInDateRange(e.Date))
            .ToList();

        // Group by category
        var revenueByCategory = GroupTransactionsByCategory(revenues);
        var expenseByCategory = GroupTransactionsByCategory(expenses);

        var totalRevenue = revenueByCategory.Values.Sum();
        var totalExpenses = expenseByCategory.Values.Sum();
        var netIncome = totalRevenue - totalExpenses;

        // Revenue section
        data.Rows.Add(new AccountingRow
        {
            Label = "REVENUE",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
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
            Label = "Total Revenue",
            Values = [FormatCurrency(totalRevenue)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Expenses section
        data.Rows.Add(new AccountingRow
        {
            Label = "EXPENSES",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        foreach (var kvp in expenseByCategory.OrderBy(k => k.Key))
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
            Label = "Total Expenses",
            Values = [FormatCurrency(totalExpenses)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Net Income
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "NET INCOME",
            Values = [FormatCurrencyWithSign(netIncome)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    private void AddEmptyIncomeStatement(AccountingTableData data)
    {
        data.Rows.Add(new AccountingRow
        {
            Label = "REVENUE",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });
        data.Rows.Add(new AccountingRow
        {
            Label = "Total Revenue",
            Values = [FormatCurrency(0)],
            RowType = AccountingRowType.SubtotalRow
        });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow
        {
            Label = "EXPENSES",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });
        data.Rows.Add(new AccountingRow
        {
            Label = "Total Expenses",
            Values = [FormatCurrency(0)],
            RowType = AccountingRowType.SubtotalRow
        });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });
        data.Rows.Add(new AccountingRow
        {
            Label = "NET INCOME",
            Values = [FormatCurrency(0)],
            RowType = AccountingRowType.GrandTotalRow
        });
    }

    #endregion

    #region Balance Sheet

    /// <summary>
    /// Generates Balance Sheet data showing assets, liabilities, and equity.
    /// </summary>
    private AccountingTableData GetBalanceSheetData()
    {
        var data = new AccountingTableData
        {
            Title = "Balance Sheet",
            Subtitle = GetAsOfSubtitle(),
            ColumnHeaders = ["", "Amount"],
            ColumnWidthRatios = [0.65, 0.35],
            Footnote = "Cash balance estimated from recorded transactions."
        };

        if (_companyData == null)
        {
            AddEmptyBalanceSheet(data);
            return data;
        }

        // Cash = Revenue (Paid, no invoice) + Payments - Expenses, all filtered by date
        var cashFromRevenue = _companyData.Revenues
            .Where(r => r.PaymentStatus == "Paid"
                        && string.IsNullOrEmpty(r.InvoiceId)
                        && IsOnOrBeforeEndDate(r.Date))
            .Sum(r => r.EffectiveTotalUSD);

        var cashFromPayments = _companyData.Payments
            .Where(p => IsOnOrBeforeEndDate(p.Date))
            .Sum(p => p.EffectiveAmountUSD);

        var cashPaidForExpenses = _companyData.Expenses
            .Where(e => IsOnOrBeforeEndDate(e.Date))
            .Sum(e => e.EffectiveTotalUSD);

        var cash = cashFromRevenue + cashFromPayments - cashPaidForExpenses;

        // Accounts Receivable = unpaid/uncancelled invoices
        var accountsReceivable = _companyData.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.EffectiveBalanceUSD);

        // Inventory value
        var inventoryValue = _companyData.Inventory.Sum(i => i.TotalValue);

        var totalCurrentAssets = cash + accountsReceivable + inventoryValue;
        var totalAssets = totalCurrentAssets;

        // Accounts Payable = purchase orders not received and not cancelled
        var accountsPayable = _companyData.PurchaseOrders
            .Where(po => po.Status != PurchaseOrderStatus.Received
                         && po.Status != PurchaseOrderStatus.Cancelled)
            .Sum(po => po.Total);

        var totalLiabilities = accountsPayable;

        // Retained Earnings = cumulative revenue - expenses through end date
        var totalRevenue = _companyData.Revenues
            .Where(r => IsOnOrBeforeEndDate(r.Date))
            .Sum(r => r.EffectiveTotalUSD);

        var totalExpenses = _companyData.Expenses
            .Where(e => IsOnOrBeforeEndDate(e.Date))
            .Sum(e => e.EffectiveTotalUSD);

        var retainedEarnings = totalRevenue - totalExpenses;
        var totalEquity = retainedEarnings;

        // ASSETS
        data.Rows.Add(new AccountingRow
        {
            Label = "ASSETS",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
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
            Label = "Accounts Receivable",
            Values = [FormatCurrency(accountsReceivable)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Inventory",
            Values = [FormatCurrency(inventoryValue)],
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
            Label = "Accounts Payable",
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

    private void AddEmptyBalanceSheet(AccountingTableData data)
    {
        data.Rows.Add(new AccountingRow { Label = "ASSETS", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Current Assets", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Cash (Estimated)", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Accounts Receivable", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Inventory", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Total Current Assets", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "TOTAL ASSETS", Values = [FormatCurrency(0)], RowType = AccountingRowType.TotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "LIABILITIES", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Accounts Payable", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
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
        var data = new AccountingTableData
        {
            Title = "Cash Flow Statement",
            Subtitle = GetPeriodSubtitle(),
            ColumnHeaders = ["", "Amount"],
            ColumnWidthRatios = [0.65, 0.35]
        };

        if (_companyData == null)
        {
            AddEmptyCashFlow(data);
            return data;
        }

        // Operating Activities
        var cashFromSales = _companyData.Revenues
            .Where(r => r.PaymentStatus == "Paid" && IsInDateRange(r.Date))
            .Sum(r => r.EffectiveTotalUSD);

        var cashFromInvoicePayments = _companyData.Payments
            .Where(p => IsInDateRange(p.Date))
            .Sum(p => p.EffectiveAmountUSD);

        var cashPaidForExpenses = _companyData.Expenses
            .Where(e => IsInDateRange(e.Date))
            .Sum(e => e.EffectiveTotalUSD);

        var totalOperating = cashFromSales + cashFromInvoicePayments - cashPaidForExpenses;

        // Investing Activities
        var inventoryPurchases = _companyData.PurchaseOrders
            .Where(po => po.Status != PurchaseOrderStatus.Cancelled && IsInDateRange(po.OrderDate))
            .Sum(po => po.Total);

        var totalInvesting = -inventoryPurchases;

        var netChange = totalOperating + totalInvesting;

        // Operating section
        data.Rows.Add(new AccountingRow
        {
            Label = "OPERATING ACTIVITIES",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
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
            Label = "Inventory Purchases",
            Values = [FormatCurrencyWithSign(-inventoryPurchases)],
            IndentLevel = 1,
            RowType = AccountingRowType.DataRow
        });

        data.Rows.Add(new AccountingRow
        {
            Label = "Net Cash from Investing Activities",
            Values = [FormatCurrencyWithSign(totalInvesting)],
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

    private void AddEmptyCashFlow(AccountingTableData data)
    {
        data.Rows.Add(new AccountingRow { Label = "OPERATING ACTIVITIES", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Cash from Sales", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Cash from Invoice Payments", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Cash Paid for Expenses", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Net Cash from Operating Activities", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "INVESTING ACTIVITIES", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Inventory Purchases", Values = [FormatCurrency(0)], IndentLevel = 1, RowType = AccountingRowType.DataRow });
        data.Rows.Add(new AccountingRow { Label = "Net Cash from Investing Activities", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
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
        var data = new AccountingTableData
        {
            Title = "Trial Balance",
            Subtitle = GetAsOfSubtitle(),
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
        var cashFromRevenue = _companyData.Revenues
            .Where(r => r.PaymentStatus == "Paid"
                        && string.IsNullOrEmpty(r.InvoiceId)
                        && IsOnOrBeforeEndDate(r.Date))
            .Sum(r => r.EffectiveTotalUSD);

        var cashFromPayments = _companyData.Payments
            .Where(p => IsOnOrBeforeEndDate(p.Date))
            .Sum(p => p.EffectiveAmountUSD);

        var cashPaidExpenses = _companyData.Expenses
            .Where(e => IsOnOrBeforeEndDate(e.Date))
            .Sum(e => e.EffectiveTotalUSD);

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
            Label = "Accounts Receivable",
            Values = [FormatCurrency(ar), ""],
            RowType = AccountingRowType.DataRow
        });
        totalDebits += ar;

        // Inventory (debit)
        var inventoryValue = _companyData.Inventory.Sum(i => i.TotalValue);

        data.Rows.Add(new AccountingRow
        {
            Label = "Inventory",
            Values = [FormatCurrency(inventoryValue), ""],
            RowType = AccountingRowType.DataRow
        });
        totalDebits += inventoryValue;

        // Accounts Payable (credit)
        var ap = _companyData.PurchaseOrders
            .Where(po => po.Status != PurchaseOrderStatus.Received
                         && po.Status != PurchaseOrderStatus.Cancelled)
            .Sum(po => po.Total);

        data.Rows.Add(new AccountingRow
        {
            Label = "Accounts Payable",
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
                Label = $"Revenue - {kvp.Key}",
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
                Label = $"Expense - {kvp.Key}",
                Values = [FormatCurrency(kvp.Value), ""],
                RowType = AccountingRowType.DataRow
            });
            totalDebits += kvp.Value;
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
        var data = new AccountingTableData
        {
            Title = "General Ledger",
            Subtitle = GetPeriodSubtitle(),
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
                        Credit = li.Amount
                    });
                }
            }
            else
            {
                AddLedgerEntry(entries, "Revenue", new LedgerEntry
                {
                    Date = rev.Date,
                    Description = rev.Description,
                    Reference = rev.ReferenceNumber,
                    Debit = 0,
                    Credit = rev.EffectiveTotalUSD
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
                        Debit = li.Amount,
                        Credit = 0
                    });
                }
            }
            else
            {
                AddLedgerEntry(entries, "Expenses", new LedgerEntry
                {
                    Date = exp.Date,
                    Description = exp.Description,
                    Reference = exp.ReferenceNumber,
                    Debit = exp.EffectiveTotalUSD,
                    Credit = 0
                });
            }
        }

        // Payments (credits to AR / debits to cash)
        foreach (var pmt in _companyData.Payments.Where(p => IsInDateRange(p.Date)))
        {
            var customerName = _companyData.GetCustomer(pmt.CustomerId)?.Name ?? "Unknown";
            AddLedgerEntry(entries, "Payments Received", new LedgerEntry
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
        var data = new AccountingTableData
        {
            Title = "Accounts Receivable Aging",
            Subtitle = GetAsOfSubtitle(),
            ColumnHeaders = ["Customer", "Current", "1-30", "31-60", "61-90", "90+", "Total"],
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
        var data = new AccountingTableData
        {
            Title = "Accounts Payable Aging",
            Subtitle = GetAsOfSubtitle(),
            ColumnHeaders = ["Supplier", "Current", "1-30", "31-60", "61-90", "90+", "Total"],
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

    #region Tax Summary

    /// <summary>
    /// Generates Tax Summary data showing taxes collected, taxes paid, and net tax liability.
    /// </summary>
    private AccountingTableData GetTaxSummaryData()
    {
        var data = new AccountingTableData
        {
            Title = "Tax Summary",
            Subtitle = GetPeriodSubtitle(),
            ColumnHeaders = ["Description", "Amount"],
            ColumnWidthRatios = [0.65, 0.35]
        };

        if (_companyData == null)
        {
            AddEmptyTaxSummary(data);
            return data;
        }

        // Tax collected from revenue, grouped by tax rate
        var filteredRevenues = _companyData.Revenues
            .Where(r => IsInDateRange(r.Date))
            .ToList();

        var taxCollectedByRate = new Dictionary<decimal, decimal>();
        foreach (var rev in filteredRevenues)
        {
            if (rev.LineItems.Count > 0)
            {
                foreach (var li in rev.LineItems)
                {
                    if (li.TaxRate > 0)
                    {
                        if (!taxCollectedByRate.ContainsKey(li.TaxRate))
                            taxCollectedByRate[li.TaxRate] = 0;
                        taxCollectedByRate[li.TaxRate] += li.TaxAmount;
                    }
                }
            }
            else if (rev.TaxRate > 0)
            {
                if (!taxCollectedByRate.ContainsKey(rev.TaxRate))
                    taxCollectedByRate[rev.TaxRate] = 0;
                taxCollectedByRate[rev.TaxRate] += rev.TaxAmount;
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
                        if (!taxPaidByRate.ContainsKey(li.TaxRate))
                            taxPaidByRate[li.TaxRate] = 0;
                        taxPaidByRate[li.TaxRate] += li.TaxAmount;
                    }
                }
            }
            else if (exp.TaxRate > 0)
            {
                if (!taxPaidByRate.ContainsKey(exp.TaxRate))
                    taxPaidByRate[exp.TaxRate] = 0;
                taxPaidByRate[exp.TaxRate] += exp.TaxAmount;
            }
        }

        var totalTaxCollected = taxCollectedByRate.Values.Sum();
        var totalTaxPaid = taxPaidByRate.Values.Sum();
        var netTaxLiability = totalTaxCollected - totalTaxPaid;

        // Tax Collected section
        data.Rows.Add(new AccountingRow
        {
            Label = "TAX COLLECTED",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        foreach (var kvp in taxCollectedByRate.OrderBy(k => k.Key))
        {
            var ratePercent = kvp.Key * 100;
            data.Rows.Add(new AccountingRow
            {
                Label = $"Tax Collected at {ratePercent:0.##}%",
                Values = [FormatCurrency(kvp.Value)],
                IndentLevel = 1,
                RowType = AccountingRowType.DataRow
            });
        }

        data.Rows.Add(new AccountingRow
        {
            Label = "Total Tax Collected",
            Values = [FormatCurrency(totalTaxCollected)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Tax Paid section
        data.Rows.Add(new AccountingRow
        {
            Label = "TAX PAID",
            RowType = AccountingRowType.SectionHeader,
            Values = [""]
        });

        foreach (var kvp in taxPaidByRate.OrderBy(k => k.Key))
        {
            var ratePercent = kvp.Key * 100;
            data.Rows.Add(new AccountingRow
            {
                Label = $"Tax Paid at {ratePercent:0.##}%",
                Values = [FormatCurrency(kvp.Value)],
                IndentLevel = 1,
                RowType = AccountingRowType.DataRow
            });
        }

        data.Rows.Add(new AccountingRow
        {
            Label = "Total Tax Paid",
            Values = [FormatCurrency(totalTaxPaid)],
            RowType = AccountingRowType.SubtotalRow
        });

        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });

        // Net Tax Liability
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });

        data.Rows.Add(new AccountingRow
        {
            Label = "NET TAX LIABILITY",
            Values = [FormatCurrencyWithSign(netTaxLiability)],
            RowType = AccountingRowType.GrandTotalRow
        });

        return data;
    }

    private void AddEmptyTaxSummary(AccountingTableData data)
    {
        data.Rows.Add(new AccountingRow { Label = "TAX COLLECTED", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Total Tax Collected", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "TAX PAID", RowType = AccountingRowType.SectionHeader, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "Total Tax Paid", Values = [FormatCurrency(0)], RowType = AccountingRowType.SubtotalRow });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.BlankRow, Values = [""] });
        data.Rows.Add(new AccountingRow { RowType = AccountingRowType.SeparatorLine, Values = [""] });
        data.Rows.Add(new AccountingRow { Label = "NET TAX LIABILITY", Values = [FormatCurrency(0)], RowType = AccountingRowType.GrandTotalRow });
    }

    #endregion
}
