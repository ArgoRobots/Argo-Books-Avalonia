using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Payment model.
/// </summary>
public class PaymentTests
{
    #region Default Value Tests

    [Fact]
    public void Payment_DefaultValues_AreCorrect()
    {
        var payment = new Payment();

        Assert.Equal(string.Empty, payment.Id);
        Assert.Equal(string.Empty, payment.InvoiceId);
        Assert.Equal(string.Empty, payment.CustomerId);
        Assert.Equal(0m, payment.Amount);
        Assert.Null(payment.ReferenceNumber);
        Assert.Equal(string.Empty, payment.Notes);
        Assert.Equal("USD", payment.OriginalCurrency);
        Assert.Equal(0m, payment.AmountUSD);
    }

    [Fact]
    public void Payment_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var payment = new Payment();
        var after = DateTime.UtcNow;

        Assert.InRange(payment.CreatedAt, before, after);
    }

    #endregion

    #region Invoice Association Tests

    [Fact]
    public void Payment_InvoiceAssociation_WorksCorrectly()
    {
        var payment = new Payment
        {
            Id = "PAY-2024-00001",
            InvoiceId = "INV-2024-00001",
            CustomerId = "CUST-001"
        };

        Assert.Equal("PAY-2024-00001", payment.Id);
        Assert.Equal("INV-2024-00001", payment.InvoiceId);
        Assert.Equal("CUST-001", payment.CustomerId);
    }

    #endregion

    #region Payment Method Tests

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.Check)]
    [InlineData(PaymentMethod.PayPal)]
    [InlineData(PaymentMethod.Other)]
    public void Payment_PaymentMethod_SupportsAllValues(PaymentMethod method)
    {
        var payment = new Payment
        {
            PaymentMethod = method
        };

        Assert.Equal(method, payment.PaymentMethod);
    }

    [Fact]
    public void Payment_PaymentMethod_DefaultIsCash()
    {
        var payment = new Payment();

        Assert.Equal(PaymentMethod.Cash, payment.PaymentMethod);
    }

    #endregion

    #region Amount Tests

    [Fact]
    public void Payment_Amount_CanBeSet()
    {
        var payment = new Payment
        {
            Amount = 1500.50m
        };

        Assert.Equal(1500.50m, payment.Amount);
    }

    [Theory]
    [InlineData(0.01)]  // Minimum payment
    [InlineData(100.00)]
    [InlineData(999999.99)]  // Large payment
    public void Payment_Amount_SupportsVariousValues(decimal amount)
    {
        var payment = new Payment
        {
            Amount = amount
        };

        Assert.Equal(amount, payment.Amount);
    }

    [Fact]
    public void Payment_PartialPayment_WorksCorrectly()
    {
        // Simulating a partial payment against a $1000 invoice
        var payment = new Payment
        {
            Id = "PAY-2024-00001",
            InvoiceId = "INV-2024-00001",
            Amount = 250.00m  // Partial payment
        };

        Assert.Equal(250.00m, payment.Amount);
    }

    #endregion

    #region Currency Tests

    [Fact]
    public void Payment_Currency_DefaultsToUSD()
    {
        var payment = new Payment();

        Assert.Equal("USD", payment.OriginalCurrency);
    }

    [Theory]
    [InlineData("USD", 100.00, 100.00)]
    [InlineData("EUR", 100.00, 110.00)]
    [InlineData("GBP", 100.00, 125.00)]
    [InlineData("CAD", 100.00, 75.00)]
    public void Payment_Currency_StoresConvertedAmount(string currency, decimal originalAmount, decimal usdAmount)
    {
        var payment = new Payment
        {
            OriginalCurrency = currency,
            Amount = originalAmount,
            AmountUSD = usdAmount
        };

        Assert.Equal(currency, payment.OriginalCurrency);
        Assert.Equal(originalAmount, payment.Amount);
        Assert.Equal(usdAmount, payment.AmountUSD);
    }

    [Fact]
    public void Payment_EffectiveAmountUSD_ReturnsConvertedValueWhenSet()
    {
        var payment = new Payment
        {
            OriginalCurrency = "EUR",
            Amount = 100.00m,
            AmountUSD = 110.00m
        };

        Assert.Equal(110.00m, payment.EffectiveAmountUSD);
    }

    [Fact]
    public void Payment_EffectiveAmountUSD_FallsBackToAmountWhenNotSet()
    {
        var payment = new Payment
        {
            OriginalCurrency = "USD",
            Amount = 100.00m,
            AmountUSD = 0m
        };

        Assert.Equal(100.00m, payment.EffectiveAmountUSD);
    }

    #endregion

    #region Reference Number Tests

    [Fact]
    public void Payment_ReferenceNumber_IsOptional()
    {
        var payment = new Payment
        {
            Id = "PAY-2024-00001",
            ReferenceNumber = null
        };

        Assert.Null(payment.ReferenceNumber);
    }

    [Theory]
    [InlineData("TXN-12345678")]
    [InlineData("CHK-001-2024")]
    [InlineData("WIRE-20240115-ABC")]
    public void Payment_ReferenceNumber_SupportsVariousFormats(string refNumber)
    {
        var payment = new Payment
        {
            ReferenceNumber = refNumber
        };

        Assert.Equal(refNumber, payment.ReferenceNumber);
    }

    #endregion

    #region Date Tests

    [Fact]
    public void Payment_Date_CanBeSet()
    {
        var paymentDate = new DateTime(2024, 6, 15, 10, 30, 0);
        var payment = new Payment
        {
            Date = paymentDate
        };

        Assert.Equal(paymentDate, payment.Date);
    }

    [Fact]
    public void Payment_Date_TracksFuturePayments()
    {
        var futureDate = DateTime.UtcNow.AddDays(30);
        var payment = new Payment
        {
            Date = futureDate
        };

        Assert.True(payment.Date > DateTime.UtcNow);
    }

    #endregion

    #region Notes Tests

    [Fact]
    public void Payment_Notes_DefaultsToEmpty()
    {
        var payment = new Payment();

        Assert.Equal(string.Empty, payment.Notes);
    }

    [Fact]
    public void Payment_Notes_CanStoreText()
    {
        var payment = new Payment
        {
            Notes = "Payment received via wire transfer. Transaction confirmed."
        };

        Assert.Contains("wire transfer", payment.Notes);
    }

    #endregion

    #region Over-Payment Scenario Tests

    [Fact]
    public void Payment_OverPayment_IsAllowed()
    {
        // In some business scenarios, overpayments may be intentional (credits)
        var payment = new Payment
        {
            InvoiceId = "INV-2024-00001",
            Amount = 1500.00m,  // Invoice was for $1000
            Notes = "Customer overpaid. Credit to be applied to next invoice."
        };

        Assert.Equal(1500.00m, payment.Amount);
    }

    #endregion
}
