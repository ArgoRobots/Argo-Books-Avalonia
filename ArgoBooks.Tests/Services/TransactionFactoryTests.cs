using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class TransactionFactoryTests
{
    private static CompanyData NewCompany() => new();

    [Fact]
    public void CreateExpense_GeneratesPurIdAndSetsFields()
    {
        var data = NewCompany();
        var draft = new TransactionDraft(
            Date: new DateTime(2026, 4, 5),
            Description: "AMZN MKTP US",
            Total: 38.20m,
            CounterpartyId: "SUP-001",
            Notes: "from statement");

        var expense = TransactionFactory.CreateExpense(data, draft);

        Assert.Equal("PUR-2026-00001", expense.Id);
        Assert.Equal("SUP-001", expense.SupplierId);
        Assert.Equal(38.20m, expense.Total);
        Assert.Equal(38.20m, expense.Amount);
        Assert.Equal("AMZN MKTP US", expense.Description);
        Assert.Single(expense.LineItems);
        Assert.Equal(1, data.IdCounters.Expense);
    }

    [Fact]
    public void CreateRevenue_GeneratesRevIdAndMarksPaid()
    {
        var data = NewCompany();
        var draft = new TransactionDraft(
            Date: new DateTime(2026, 4, 6),
            Description: "STRIPE TRANSFER",
            Total: 1200m,
            CounterpartyId: "CUS-001",
            Notes: null);

        var revenue = TransactionFactory.CreateRevenue(data, draft);

        Assert.Equal("REV-2026-00001", revenue.Id);
        Assert.Equal("CUS-001", revenue.CustomerId);
        Assert.Equal(1200m, revenue.Total);
        Assert.Equal(1200m, revenue.Subtotal);
        Assert.Equal(Core.Enums.RevenuePaymentStatus.Paid, revenue.PaymentStatus);
    }
}
