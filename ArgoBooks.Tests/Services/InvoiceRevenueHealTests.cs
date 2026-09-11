using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Invoices paid by hand before the fix left their revenue unpaid, and portal refunds marked the
/// revenue of a paid invoice unpaid. The open-time heal counts that revenue again, and only ever
/// upgrades: it never takes a revenue out of the totals.
/// </summary>
public class InvoiceRevenueHealTests
{
    private static (CompanyData Data, Invoice Invoice, Revenue Revenue) PaidInvoice(RevenuePaymentStatus revenueStatus)
    {
        var data = new CompanyData();
        var invoice = new Invoice
        {
            Id = "INV-1", OriginalCurrency = "USD", Total = 100m, TotalUSD = 100m, Balance = 100m,
            Status = InvoiceStatus.Sent
        };
        data.Invoices.Add(invoice);
        data.Payments.Add(new Payment
        {
            Id = "PAY-1", InvoiceId = "INV-1", OriginalCurrency = "USD", Amount = 100m, AmountUSD = 100m
        });
        var revenue = new Revenue
        {
            Id = "REV-1", InvoiceId = "INV-1", OriginalCurrency = "USD", Total = 100m, TotalUSD = 100m,
            PaymentStatus = revenueStatus
        };
        data.Revenues.Add(revenue);
        return (data, invoice, revenue);
    }

    [Fact]
    public void Heal_CountsTheRevenueOfAnInvoicePaidInFull()
    {
        var (data, _, revenue) = PaidInvoice(RevenuePaymentStatus.Unpaid);

        CompanyManager.HealInvoiceTotalsIfNeeded(data);

        Assert.Equal(RevenuePaymentStatus.Paid, revenue.PaymentStatus);
        Assert.True(data.ChangesMade);
    }

    [Fact]
    public void Heal_CountsTheRevenueOfAPaidInvoiceRefundedSince()
    {
        var (data, _, revenue) = PaidInvoice(RevenuePaymentStatus.Unpaid);
        data.Payments.Add(new Payment
        {
            Id = "PAY-2", InvoiceId = "INV-1", OriginalCurrency = "USD", Amount = -100m, AmountUSD = -100m, IsRefund = true
        });

        CompanyManager.HealInvoiceTotalsIfNeeded(data);

        Assert.Equal(RevenuePaymentStatus.Paid, revenue.PaymentStatus);
    }

    [Fact]
    public void Heal_LeavesTheRevenueOfAPartPaidInvoiceAsItIs()
    {
        var (data, _, revenue) = PaidInvoice(RevenuePaymentStatus.Paid);
        data.Payments.Single().Amount = 40m;
        data.Payments.Single().AmountUSD = 40m;

        CompanyManager.HealInvoiceTotalsIfNeeded(data);

        // Marked paid by hand, and the heal never takes revenue out of the totals.
        Assert.Equal(RevenuePaymentStatus.Paid, revenue.PaymentStatus);
    }
}
