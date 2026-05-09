using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class RefundAnalyticsServiceTests
{
    private static CompanyData BuildCompany(out Customer customer)
    {
        customer = new Customer { Id = "C1", Name = "Acme" };
        var company = new CompanyData();
        company.Customers.Add(customer);
        return company;
    }

    private static Payment Payment(string id, string customerId, decimal amount, DateTime? date = null, bool isRefund = false, string? refundReason = null, string? refundedFromId = null, PaymentMethod method = PaymentMethod.Stripe)
        => new()
        {
            Id = id,
            CustomerId = customerId,
            Amount = amount,
            Date = date ?? DateTime.Today,
            PaymentMethod = method,
            IsRefund = isRefund,
            RefundReason = refundReason,
            RefundedFromPaymentId = refundedFromId,
            InvoiceId = "INV-1",
        };

    [Fact]
    public void TotalRefunded_SumsAbsoluteValues_OnlyRefunds_InWindow()
    {
        var c = BuildCompany(out _);
        c.Payments.Add(Payment("p1", "C1", 100m));               // positive — excluded
        c.Payments.Add(Payment("p2", "C1", -25m, isRefund: true));
        c.Payments.Add(Payment("p3", "C1", -75m, isRefund: true));
        c.Payments.Add(Payment("p4", "C1", -10m, isRefund: true, date: DateTime.Today.AddYears(-2)));

        var total = RefundAnalyticsService.TotalRefunded(c, DateTime.Today.AddDays(-30));
        Assert.Equal(100m, total);
    }

    [Fact]
    public void RefundRate_DividesByPositivePaymentTotal()
    {
        var c = BuildCompany(out _);
        c.Payments.Add(Payment("p1", "C1", 200m));
        c.Payments.Add(Payment("p2", "C1", -50m, isRefund: true));
        var rate = RefundAnalyticsService.RefundRate(c, DateTime.Today.AddDays(-30));
        Assert.Equal(0.25m, rate);
    }

    [Fact]
    public void RefundRate_ReturnsZero_WhenNoPositivePayments()
    {
        var c = BuildCompany(out _);
        var rate = RefundAnalyticsService.RefundRate(c, DateTime.Today.AddDays(-30));
        Assert.Equal(0m, rate);
    }

    [Fact]
    public void TopRefundedCustomers_OrdersByTotalDesc()
    {
        var c = BuildCompany(out _);
        c.Customers.Add(new Customer { Id = "C2", Name = "Beta Inc" });
        c.Customers.Add(new Customer { Id = "C3", Name = "Charlie LLC" });
        c.Payments.Add(Payment("p1", "C1", -10m, isRefund: true));
        c.Payments.Add(Payment("p2", "C2", -50m, isRefund: true));
        c.Payments.Add(Payment("p3", "C3", -30m, isRefund: true));

        var top = RefundAnalyticsService.TopRefundedCustomers(c, DateTime.Today.AddDays(-30), 2);
        Assert.Equal(2, top.Count);
        Assert.Equal("Beta Inc", top[0].CustomerName);
        Assert.Equal(50m, top[0].Amount);
        Assert.Equal("Charlie LLC", top[1].CustomerName);
    }

    [Fact]
    public void TopReasons_GroupsCaseInsensitive_AndIgnoresEmpty()
    {
        var c = BuildCompany(out _);
        c.Payments.Add(Payment("p1", "C1", -10m, isRefund: true, refundReason: "Wrong color"));
        c.Payments.Add(Payment("p2", "C1", -10m, isRefund: true, refundReason: "wrong color"));   // case-insensitive merge
        c.Payments.Add(Payment("p3", "C1", -10m, isRefund: true, refundReason: "Defective"));
        c.Payments.Add(Payment("p4", "C1", -10m, isRefund: true, refundReason: ""));               // ignored
        c.Payments.Add(Payment("p5", "C1", -10m, isRefund: true, refundReason: null));             // ignored

        var top = RefundAnalyticsService.TopReasons(c, DateTime.Today.AddDays(-30), 5);
        Assert.Equal(2, top.Count);
        Assert.Equal("Wrong color", top[0].Reason);
        Assert.Equal(2, top[0].Count);
    }

    [Fact]
    public void ChannelBreakdown_BucketsByPaymentMethod()
    {
        var c = BuildCompany(out _);
        c.Payments.Add(Payment("p1", "C1", -50m, isRefund: true, method: PaymentMethod.Stripe));
        c.Payments.Add(Payment("p2", "C1", -30m, isRefund: true, method: PaymentMethod.PayPal));
        c.Payments.Add(Payment("p3", "C1", -20m, isRefund: true, method: PaymentMethod.Stripe));

        var breakdown = RefundAnalyticsService.ChannelBreakdown(c, DateTime.Today.AddDays(-30));
        Assert.Equal(70m, breakdown["Stripe"]);
        Assert.Equal(30m, breakdown["PayPal"]);
    }

    [Fact]
    public void AverageRefundLatencyDays_IgnoresUnlinkedRefunds()
    {
        var c = BuildCompany(out _);
        c.Payments.Add(Payment("p1", "C1", 100m, date: DateTime.Today.AddDays(-10)));
        c.Payments.Add(Payment("p2", "C1", -100m, isRefund: true, refundedFromId: "p1", date: DateTime.Today));
        c.Payments.Add(Payment("p3", "C1", -50m, isRefund: true, refundedFromId: null));   // unlinked, ignored
        var avg = RefundAnalyticsService.AverageRefundLatencyDays(c, DateTime.Today.AddDays(-30));
        Assert.Equal(10.0, avg, 2);
    }

    [Fact]
    public void MonthlyTotals_ReturnsConsecutiveMonthsIncludingZeroBuckets()
    {
        var c = BuildCompany(out _);
        c.Payments.Add(Payment("p1", "C1", -50m, isRefund: true, date: DateTime.Today.AddMonths(-2)));
        c.Payments.Add(Payment("p2", "C1", -25m, isRefund: true, date: DateTime.Today));

        var months = RefundAnalyticsService.MonthlyTotals(c, 3);
        Assert.Equal(3, months.Count);
        Assert.Equal(50m, months[0].Amount);
        Assert.Equal(0m, months[1].Amount);  // empty middle month still represented
        Assert.Equal(25m, months[2].Amount);
    }
}
