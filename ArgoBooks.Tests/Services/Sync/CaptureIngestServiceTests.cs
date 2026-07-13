using System.Linq;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services.Sync;
using Xunit;

namespace ArgoBooks.Tests.Services.Sync;

public class CaptureIngestServiceTests
{
    private static CompanyData NewCompany() => new();

    private static CapturedTransaction NewExpenseDto() => new()
    {
        Type = CapturedTransactionType.Expense,
        SupplierOrCustomer = "Office Depot",
        Date = new DateTime(2026, 6, 1),
        Total = 54.00m,
        Tax = 4.00m,
        LineItems =
        [
            new CapturedLineItem { Description = "Printer paper", Quantity = 2, UnitPrice = 25.00m, Total = 50.00m, ProductName = "Printer paper" }
        ],
        ImageBase64 = Convert.ToBase64String([1, 2, 3, 4])
    };

    private static CapturedTransaction NewRevenueDto() => new()
    {
        Type = CapturedTransactionType.Revenue,
        SupplierOrCustomer = "Acme Corp",
        Date = new DateTime(2026, 6, 2),
        Total = 220.00m,
        Tax = 20.00m,
        LineItems =
        [
            new CapturedLineItem { Description = "Consulting", Quantity = 1, UnitPrice = 200.00m, Total = 200.00m, ProductName = null }
        ]
    };

    [Fact]
    public void Ingest_Expense_AddsExpenseAndLinkedMobileReceipt()
    {
        var data = NewCompany();
        var tx = NewExpenseDto();

        var expenseId = CaptureIngestService.Ingest(data, tx);

        Assert.Single(data.Expenses);
        var expense = data.Expenses.Single();
        Assert.Equal(expenseId, expense.Id);
        Assert.StartsWith("PUR-", expense.Id);
        Assert.Equal(54.00m, expense.Total);
        Assert.Equal(4.00m, expense.TaxAmount);
        Assert.Equal(50.00m, expense.Amount);
        Assert.Equal(new DateTime(2026, 6, 1), expense.Date);
        Assert.Single(expense.LineItems);

        Assert.Single(data.Receipts);
        var receipt = data.Receipts.Single();
        Assert.Equal("Mobile", receipt.Source);
        Assert.Equal(expense.Id, receipt.TransactionId);
        Assert.Equal("Expense", receipt.TransactionType);
        Assert.Equal(54.00m, receipt.Amount);
        Assert.Equal(expense.ReceiptId, receipt.Id);
        Assert.False(string.IsNullOrEmpty(receipt.FileData));

        Assert.Equal(1, data.IdCounters.Expense);
        Assert.Equal(1, data.IdCounters.Receipt);
    }

    [Fact]
    public void Ingest_Revenue_AddsRevenueAndLinkedMobileReceipt()
    {
        var data = NewCompany();
        var tx = NewRevenueDto();

        var revenueId = CaptureIngestService.Ingest(data, tx);

        Assert.Single(data.Revenues);
        var revenue = data.Revenues.Single();
        Assert.Equal(revenueId, revenue.Id);
        Assert.StartsWith("REV-", revenue.Id);
        Assert.Equal(220.00m, revenue.Total);
        Assert.Equal(20.00m, revenue.TaxAmount);
        Assert.Equal(200.00m, revenue.Amount);
        Assert.Equal(Core.Enums.RevenuePaymentStatus.Paid, revenue.PaymentStatus);

        Assert.Single(data.Receipts);
        var receipt = data.Receipts.Single();
        Assert.Equal("Mobile", receipt.Source);
        Assert.Equal(revenue.Id, receipt.TransactionId);
        Assert.Equal("Revenue", receipt.TransactionType);
        Assert.Equal(220.00m, receipt.Amount);
        Assert.Null(receipt.FileData);

        Assert.Equal(1, data.IdCounters.Revenue);
        Assert.Equal(1, data.IdCounters.Receipt);
    }

    [Fact]
    public void Ingest_MatchesLineItemToExistingProductByName()
    {
        var data = NewCompany();
        var product = new Core.Models.Entities.Product { Id = "PRD-00001", Name = "Printer paper" };
        data.Products.Add(product);

        var tx = NewExpenseDto();
        CaptureIngestService.Ingest(data, tx);

        var expense = data.Expenses.Single();
        Assert.Equal(product.Id, expense.LineItems.Single().ProductId);
    }
}
