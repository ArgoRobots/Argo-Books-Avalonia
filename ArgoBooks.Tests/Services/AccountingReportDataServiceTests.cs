using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the AccountingReportDataService class.
/// </summary>
public class AccountingReportDataServiceTests
{
    private static ReportFilters CreateDefaultFilters() => new()
    {
        StartDate = new DateTime(2024, 1, 1),
        EndDate = new DateTime(2024, 12, 31)
    };

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCompanyData_CreatesInstance()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithEmptyCompanyData_CreatesInstance()
    {
        var data = new CompanyData();
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        Assert.NotNull(service);
    }

    #endregion

    #region Income Statement Tests

    [Fact]
    public void GetReportData_IncomeStatement_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.IncomeStatement);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_IncomeStatement_EmptyData_ReturnsValidResult()
    {
        var data = new CompanyData();
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.IncomeStatement);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_IncomeStatement_IncludesEndDayTransactionEnteredWithATime()
    {
        var data = new CompanyData();
        // A revenue entered at 14:30 on the last day of a custom range whose end date is stored at
        // midnight (as the date picker does). It must still be counted, not dropped by a time compare.
        data.Revenues.Add(new Revenue
        {
            Id = "REV-1",
            Date = new DateTime(2024, 6, 30, 14, 30, 0),
            Total = 100m
        });
        var filters = new ReportFilters
        {
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 30) // midnight
        };

        var result = new AccountingReportDataService(data, filters).GetReportData(AccountingReportType.IncomeStatement);

        var totalRevenue = result.Rows.Find(r => r.Label == "Total Revenue");
        Assert.NotNull(totalRevenue);
        Assert.Contains("100", totalRevenue!.Values[0]);
    }

    #endregion

    #region Balance Sheet Tests

    [Fact]
    public void GetReportData_BalanceSheet_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.BalanceSheet);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_BalanceSheet_WithInventory_AddsInventoryRow()
    {
        var data = new CompanyData();
        // 100 units @ $5 via a single manual Add adjustment dated mid-period.
        // No revenue/expense rows => Cash = 0 and AR = 0, so Total Current
        // Assets equals the inventory value alone.
        data.Inventory.Add(new InventoryItem { Id = "I1", InStock = 100, UnitCost = 5m });
        data.StockAdjustments.Add(new StockAdjustment
        {
            InventoryItemId = "I1",
            AdjustmentType = AdjustmentType.Add,
            Quantity = 100,
            PreviousStock = 0,
            NewStock = 100,
            Timestamp = new DateTime(2024, 6, 1)
        });
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.BalanceSheet);

        var inventoryRow = result.Rows.Find(r => r.Label == "Inventory");
        Assert.NotNull(inventoryRow);
        var totalCurrentAssetsRow = result.Rows.Find(r => r.Label == "Total Current Assets");
        Assert.NotNull(totalCurrentAssetsRow);
        // Cash and AR are zero, so the subtotal must equal the inventory value.
        Assert.Equal(inventoryRow!.Values[0], totalCurrentAssetsRow!.Values[0]);
    }

    [Fact]
    public void GetReportData_BalanceSheet_NoInventory_OmitsInventoryRow()
    {
        var data = new CompanyData();
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.BalanceSheet);

        Assert.Null(result.Rows.Find(r => r.Label == "Inventory"));
    }

    #endregion

    #region Cash Flow Tests

    [Fact]
    public void GetReportData_CashFlow_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.CashFlowStatement);

        Assert.NotNull(result);
    }

    #endregion

    #region General Ledger Tests

    [Fact]
    public void GetReportData_GeneralLedger_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.GeneralLedger);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_GeneralLedger_RefundPayment_ShowsAmountInLedgerRow()
    {
        // A refund is stored as a Payment with a negative Amount. The ledger records every payment as
        // a Debit and only renders a column when its value is > 0, so a refund's negative debit shows
        // blank in BOTH columns even though the running balance still drops. The amount should be
        // visible (it belongs in the Credit column).
        var data = new CompanyData();
        data.Payments.Add(new Payment
        {
            Id = "PMT-1",
            CustomerId = "CUST-1",
            Date = new DateTime(2024, 6, 1),
            Amount = 250m,
            OriginalCurrency = "USD"
        });
        data.Payments.Add(new Payment
        {
            Id = "PMT-2",
            CustomerId = "CUST-1",
            Date = new DateTime(2024, 6, 15),
            Amount = -50m,        // refunds are stored negative
            OriginalCurrency = "USD",
            IsRefund = true,
            RefundedFromPaymentId = "PMT-1"
        });

        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.GeneralLedger);

        // The refund's ledger row is keyed by its payment id in the Reference column (Values[1]).
        var refundRow = result.Rows.Find(r => r.Values.Count >= 4 && r.Values[1] == "PMT-2");
        Assert.NotNull(refundRow);
        var debit = refundRow!.Values[2];
        var credit = refundRow.Values[3];
        Assert.True(
            debit.Contains("50") || credit.Contains("50"),
            $"Refund amount missing from the ledger row. Debit='{debit}', Credit='{credit}'.");
    }

    #endregion

    #region AR/AP Aging Tests

    [Fact]
    public void GetReportData_ARAging_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.AccountsReceivableAging);

        Assert.NotNull(result);
    }

    #endregion

    #region Tax Summary Tests

    [Fact]
    public void GetReportData_TaxSummary_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.TaxSummary);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_TaxSummary_CountsTransactionLevelTaxWhenLineItemsHaveNoRate()
    {
        // Regression: manually-entered transactions always carry a line item with TaxRate 0 but
        // record their tax at the transaction level. The Tax Summary used to read tax only off line
        // items whenever any existed, so it reported $0 for every UI-entered sale and expense.
        var data = new CompanyData();
        data.Revenues.Add(new Revenue
        {
            Id = "REV-2024-00001",
            Date = new DateTime(2024, 6, 1),
            OriginalCurrency = "USD",
            Amount = 100m,
            TaxRate = 8m,        // stored as a percentage (8%)
            TaxAmount = 8m,
            TaxAmountUSD = 8m,
            Total = 108m,
            TotalUSD = 108m,
            LineItems = [new LineItem { Description = "Sale", Quantity = 1, UnitPrice = 100m, TaxRate = 0 }]
        });
        data.Expenses.Add(new Expense
        {
            Id = "EXP-2024-00001",
            Date = new DateTime(2024, 6, 1),
            OriginalCurrency = "USD",
            Amount = 50m,
            TaxRate = 10m,
            TaxAmount = 5m,
            TaxAmountUSD = 5m,
            Total = 55m,
            TotalUSD = 55m,
            LineItems = [new LineItem { Description = "Supplies", Quantity = 1, UnitPrice = 50m, TaxRate = 0 }]
        });

        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.TaxSummary);

        // The two subtotal rows are "Total Tax Collected" then "Total Tax Paid". Before the fix both
        // formatted as $0.00 because the per-line-item tax is always 0 for manual entries.
        var subtotals = result.Rows.FindAll(r => r.RowType == AccountingRowType.SubtotalRow);
        Assert.Equal(2, subtotals.Count);
        Assert.Contains("8", subtotals[0].Values[0]);   // tax collected = $8
        Assert.Contains("5", subtotals[1].Values[0]);   // tax paid = $5
    }

    #endregion

    #region Date-Filtering Regressions

    [Fact]
    public void GetReportData_BalanceSheet_ExcludesInvoicesIssuedAfterEndDate()
    {
        // AR on the Balance Sheet is an "as of the end date" balance, so an open invoice issued AFTER
        // the report end date must not be counted. Every other current-asset/liability line is date
        // gated via IsOnOrBeforeEndDate; AR was the one that wasn't, so a future-dated open invoice
        // inflated AR (and, since Retained Earnings is the balancing figure, equity too).
        static Invoice MakeInvoice(string id, DateTime issue, decimal amount) => new()
        {
            Id = id,
            IssueDate = issue,
            Total = amount,
            Balance = amount,
            Status = InvoiceStatus.Sent,
            OriginalCurrency = "USD"
        };

        static string TotalCurrentAssets(AccountingTableData r) =>
            r.Rows.Find(x => x.Label == "Total Current Assets")!.Values[0];

        // Only the in-period invoice (issued mid-2024, within the 2024 report window).
        var dataInPeriodOnly = new CompanyData();
        dataInPeriodOnly.Invoices.Add(MakeInvoice("INV-IN", new DateTime(2024, 6, 1), 1000m));

        // Same invoice plus one issued in 2025, after the 2024-12-31 end date.
        var dataWithFuture = new CompanyData();
        dataWithFuture.Invoices.Add(MakeInvoice("INV-IN", new DateTime(2024, 6, 1), 1000m));
        dataWithFuture.Invoices.Add(MakeInvoice("INV-FUTURE", new DateTime(2025, 3, 1), 5000m));

        var without = new AccountingReportDataService(dataInPeriodOnly, CreateDefaultFilters())
            .GetReportData(AccountingReportType.BalanceSheet);
        var with = new AccountingReportDataService(dataWithFuture, CreateDefaultFilters())
            .GetReportData(AccountingReportType.BalanceSheet);

        // The future invoice is outside the report window, so AR (and thus Total Current Assets,
        // with cash and inventory both zero) must be identical with or without it.
        Assert.Equal(TotalCurrentAssets(without), TotalCurrentAssets(with));
    }

    [Fact]
    public void GetReportData_BalanceSheet_ExcludesPurchaseOrdersOrderedAfterEndDate()
    {
        // AP mirrors AR: an open purchase order placed AFTER the report end date must not count toward
        // "as of" Accounts Payable. The AP line lacked the IsOnOrBeforeEndDate gate its neighbors have.
        static PurchaseOrder MakePO(string id, DateTime order, decimal amount) => new()
        {
            Id = id,
            OrderDate = order,
            Total = amount,
            Status = PurchaseOrderStatus.Sent,
            OriginalCurrency = "USD"
        };

        static string TotalLiabilities(AccountingTableData r) =>
            r.Rows.Find(x => x.Label == "TOTAL LIABILITIES")!.Values[0];

        var dataInPeriodOnly = new CompanyData();
        dataInPeriodOnly.PurchaseOrders.Add(MakePO("PO-IN", new DateTime(2024, 6, 1), 1000m));

        var dataWithFuture = new CompanyData();
        dataWithFuture.PurchaseOrders.Add(MakePO("PO-IN", new DateTime(2024, 6, 1), 1000m));
        dataWithFuture.PurchaseOrders.Add(MakePO("PO-FUTURE", new DateTime(2025, 1, 5), 3000m));

        var without = new AccountingReportDataService(dataInPeriodOnly, CreateDefaultFilters())
            .GetReportData(AccountingReportType.BalanceSheet);
        var with = new AccountingReportDataService(dataWithFuture, CreateDefaultFilters())
            .GetReportData(AccountingReportType.BalanceSheet);

        // No revenue/expense => sales tax payable is zero, so Total Liabilities equals AP. The future
        // PO is outside the window and must not change it.
        Assert.Equal(TotalLiabilities(without), TotalLiabilities(with));
    }

    [Fact]
    public void GetReportData_IncomeStatement_CountsTransactionWhenAllLineItemsNetToZero()
    {
        // A fully-discounted sale has line items that net to a $0 subtotal but still carries a
        // transaction-level Total. The category allocator divides each line item's share by the sum
        // of line-item subtotals; when that sum is 0 it returned $0 for the whole sale, dropping it
        // from Total Revenue (and the General Ledger) entirely.
        var data = new CompanyData();
        data.Revenues.Add(new Revenue
        {
            Id = "REV-DISC",
            Date = new DateTime(2024, 6, 1),
            OriginalCurrency = "USD",
            Total = 4321m,
            TaxAmount = 0m,
            LineItems = [new LineItem { Description = "Item", Quantity = 1, UnitPrice = 4321m, Discount = 4321m }]
        });

        var result = new AccountingReportDataService(data, CreateDefaultFilters())
            .GetReportData(AccountingReportType.IncomeStatement);

        // The first subtotal row is "Total Revenue". Strip formatting to compare digits only.
        var totalRevenue = result.Rows.Find(r => r.RowType == AccountingRowType.SubtotalRow)!.Values[0];
        var digits = new string(totalRevenue.Where(char.IsDigit).ToArray());
        Assert.Contains("4321", digits);
    }

    #endregion
}
