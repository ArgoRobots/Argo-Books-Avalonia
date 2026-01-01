using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Invoice model properties.
/// </summary>
public class InvoiceTests
{
    #region IsOverdue Tests

    [Fact]
    public void IsOverdue_DraftStatus_PastDue_ReturnsFalse()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Draft,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.False(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_PaidStatus_PastDue_ReturnsFalse()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Paid,
            DueDate = DateTime.UtcNow.AddDays(-30)
        };

        Assert.False(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_CancelledStatus_PastDue_ReturnsFalse()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Cancelled,
            DueDate = DateTime.UtcNow.AddDays(-30)
        };

        Assert.False(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_PendingStatus_PastDue_ReturnsTrue()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_SentStatus_PastDue_ReturnsTrue()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Sent,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_ViewedStatus_PastDue_ReturnsTrue()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Viewed,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_PartialStatus_PastDue_ReturnsTrue()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Partial,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_OverdueStatus_PastDue_ReturnsTrue()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Overdue,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_PendingStatus_DueToday_ReturnsFalse()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Pending,
            DueDate = DateTime.UtcNow.Date
        };

        Assert.False(invoice.IsOverdue);
    }

    [Fact]
    public void IsOverdue_PendingStatus_FutureDue_ReturnsFalse()
    {
        var invoice = new Invoice
        {
            Status = InvoiceStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        Assert.False(invoice.IsOverdue);
    }

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Viewed)]
    [InlineData(InvoiceStatus.Partial)]
    [InlineData(InvoiceStatus.Overdue)]
    public void IsOverdue_UnpaidStatuses_WhenPastDue_ReturnsTrue(InvoiceStatus status)
    {
        var invoice = new Invoice
        {
            Status = status,
            DueDate = DateTime.UtcNow.AddDays(-10)
        };

        Assert.True(invoice.IsOverdue);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void IsOverdue_ClosedStatuses_WhenPastDue_ReturnsFalse(InvoiceStatus status)
    {
        var invoice = new Invoice
        {
            Status = status,
            DueDate = DateTime.UtcNow.AddDays(-10)
        };

        Assert.False(invoice.IsOverdue);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewInvoice_HasDraftStatus()
    {
        var invoice = new Invoice();

        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public void NewInvoice_HasEmptyLineItems()
    {
        var invoice = new Invoice();

        Assert.NotNull(invoice.LineItems);
        Assert.Empty(invoice.LineItems);
    }

    [Fact]
    public void NewInvoice_HasEmptyHistory()
    {
        var invoice = new Invoice();

        Assert.NotNull(invoice.History);
        Assert.Empty(invoice.History);
    }

    [Fact]
    public void NewInvoice_HasCreatedAtSet()
    {
        var beforeCreate = DateTime.UtcNow;
        var invoice = new Invoice();
        var afterCreate = DateTime.UtcNow;

        Assert.True(invoice.CreatedAt >= beforeCreate);
        Assert.True(invoice.CreatedAt <= afterCreate);
    }

    [Fact]
    public void NewInvoice_HasUpdatedAtSet()
    {
        var beforeCreate = DateTime.UtcNow;
        var invoice = new Invoice();
        var afterCreate = DateTime.UtcNow;

        Assert.True(invoice.UpdatedAt >= beforeCreate);
        Assert.True(invoice.UpdatedAt <= afterCreate);
    }

    [Fact]
    public void NewInvoice_HasDefaultReminderSettings()
    {
        var invoice = new Invoice();

        Assert.NotNull(invoice.ReminderSettings);
    }

    [Fact]
    public void NewInvoice_HasEmptyNotes()
    {
        var invoice = new Invoice();

        Assert.Equal(string.Empty, invoice.Notes);
    }

    #endregion

    #region Balance and Payment Tests

    [Fact]
    public void Invoice_BalanceTracking_WorksCorrectly()
    {
        var invoice = new Invoice
        {
            Total = 1000m,
            AmountPaid = 400m,
            Balance = 600m
        };

        Assert.Equal(1000m, invoice.Total);
        Assert.Equal(400m, invoice.AmountPaid);
        Assert.Equal(600m, invoice.Balance);
    }

    [Fact]
    public void Invoice_FullyPaid_ZeroBalance()
    {
        var invoice = new Invoice
        {
            Total = 500m,
            AmountPaid = 500m,
            Balance = 0m
        };

        Assert.Equal(0m, invoice.Balance);
    }

    #endregion
}
