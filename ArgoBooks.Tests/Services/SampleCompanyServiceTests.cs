using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the SampleCompanyService class.
/// Focuses on the static TimeShiftSampleData method which is testable without file I/O.
/// </summary>
public class SampleCompanyServiceTests
{
    #region TimeShiftSampleData Tests

    [Fact]
    public void TimeShiftSampleData_EmptyData_ReturnsFalse()
    {
        var data = new CompanyData();

        var result = SampleCompanyService.TimeShiftSampleData(data);

        Assert.False(result);
    }

    [Fact]
    public void TimeShiftSampleData_WithOldData_ShiftsDatesForward()
    {
        var data = new CompanyData();
        var oldDate = new DateTime(2024, 1, 15);
        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = oldDate,
            Total = 1000m,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        var result = SampleCompanyService.TimeShiftSampleData(data);

        Assert.True(result);
        // The max date should now be close to yesterday
        var targetDate = DateTime.Today.AddDays(-1);
        Assert.Equal(targetDate.Date, data.Revenues[0].Date.Date);
    }

    [Fact]
    public void TimeShiftSampleData_AlreadyCurrent_ReturnsFalse()
    {
        var data = new CompanyData();
        var yesterday = DateTime.Today.AddDays(-1);
        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = yesterday,
            Total = 1000m,
            CreatedAt = yesterday,
            UpdatedAt = yesterday
        });

        var result = SampleCompanyService.TimeShiftSampleData(data);

        Assert.False(result);
    }

    [Fact]
    public void TimeShiftSampleData_ShiftsMultipleRevenues_MaintainsRelativeOffsets()
    {
        var data = new CompanyData();
        var baseDate = new DateTime(2024, 1, 15);

        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = baseDate,
            Total = 1000m,
            CreatedAt = baseDate,
            UpdatedAt = baseDate
        });
        data.Revenues.Add(new Revenue
        {
            Id = "REV-002",
            Date = baseDate.AddDays(-10),
            Total = 2000m,
            CreatedAt = baseDate.AddDays(-10),
            UpdatedAt = baseDate.AddDays(-10)
        });

        SampleCompanyService.TimeShiftSampleData(data);

        // Both dates should be shifted by the same offset
        var dayDifference = (data.Revenues[0].Date - data.Revenues[1].Date).Days;
        Assert.Equal(10, dayDifference);
    }

    [Fact]
    public void TimeShiftSampleData_PreservesMinValueDates()
    {
        var data = new CompanyData();
        var oldDate = new DateTime(2024, 1, 15);

        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = oldDate,
            Total = 1000m,
            CreatedAt = DateTime.MinValue,
            UpdatedAt = oldDate
        });

        SampleCompanyService.TimeShiftSampleData(data);

        // MinValue dates should not be shifted
        Assert.Equal(DateTime.MinValue, data.Revenues[0].CreatedAt);
    }

    #endregion

    #region Invoice Date Shifting Tests

    [Fact]
    public void TimeShiftSampleData_InvoiceDates_AreShifted()
    {
        var data = new CompanyData();
        var oldDate = new DateTime(2024, 1, 15);

        // Add a revenue to establish the max date
        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = oldDate,
            Total = 1000m,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        // Add an invoice
        data.Invoices.Add(new Invoice
        {
            Id = "INV-001",
            InvoiceNumber = "#INV-001",
            CustomerId = "CUST-001",
            IssueDate = oldDate.AddDays(-5),
            DueDate = oldDate.AddDays(25),
            Total = 1000m,
            CreatedAt = oldDate.AddDays(-5),
            UpdatedAt = oldDate.AddDays(-5)
        });

        SampleCompanyService.TimeShiftSampleData(data);

        var targetDate = DateTime.Today.AddDays(-1);
        // Invoice issue date should be shifted (5 days before the max revenue date)
        Assert.Equal(targetDate.Date.AddDays(-5), data.Invoices[0].IssueDate.Date);
        // Due date should be shifted (25 days after the old date)
        Assert.Equal(targetDate.Date.AddDays(25), data.Invoices[0].DueDate.Date);
    }

    [Fact]
    public void TimeShiftSampleData_InvoiceCreatedAt_IsShifted()
    {
        var data = new CompanyData();
        var oldDate = new DateTime(2024, 6, 1);

        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = oldDate,
            Total = 500m,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        data.Invoices.Add(new Invoice
        {
            Id = "INV-002",
            InvoiceNumber = "#INV-002",
            CustomerId = "CUST-001",
            IssueDate = oldDate,
            DueDate = oldDate.AddDays(30),
            Total = 500m,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        var shiftResult = SampleCompanyService.TimeShiftSampleData(data);

        Assert.True(shiftResult);
        Assert.NotEqual(oldDate, data.Invoices[0].CreatedAt);
        Assert.NotEqual(oldDate, data.Invoices[0].UpdatedAt);
    }

    #endregion

    #region Revenue Date Shifting Tests

    [Fact]
    public void TimeShiftSampleData_RevenueDates_AreShifted()
    {
        var data = new CompanyData();
        var oldDate = new DateTime(2024, 3, 20);

        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = oldDate,
            Total = 5000m,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        SampleCompanyService.TimeShiftSampleData(data);

        var targetDate = DateTime.Today.AddDays(-1);
        Assert.Equal(targetDate.Date, data.Revenues[0].Date.Date);
        Assert.NotEqual(oldDate, data.Revenues[0].CreatedAt);
        Assert.NotEqual(oldDate, data.Revenues[0].UpdatedAt);
    }

    [Fact]
    public void TimeShiftSampleData_MultipleRevenues_AllDatesShifted()
    {
        var data = new CompanyData();
        var maxDate = new DateTime(2024, 6, 30);

        for (int i = 0; i < 5; i++)
        {
            data.Revenues.Add(new Revenue
            {
                Id = $"REV-{i:D3}",
                Date = maxDate.AddDays(-i * 7),
                Total = 1000m * (i + 1),
                CreatedAt = maxDate.AddDays(-i * 7),
                UpdatedAt = maxDate.AddDays(-i * 7)
            });
        }

        var result = SampleCompanyService.TimeShiftSampleData(data);

        Assert.True(result);
        var targetDate = DateTime.Today.AddDays(-1);

        // The most recent revenue should be at the target date
        Assert.Equal(targetDate.Date, data.Revenues[0].Date.Date);

        // All revenues should have been shifted
        foreach (var revenue in data.Revenues)
        {
            Assert.NotEqual(maxDate, revenue.Date);
        }
    }

    [Fact]
    public void TimeShiftSampleData_RevenueAndExpense_BothShifted()
    {
        var data = new CompanyData();
        var oldDate = new DateTime(2024, 4, 15);

        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = oldDate,
            Total = 5000m,
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        data.Expenses.Add(new Expense
        {
            Id = "EXP-001",
            Date = oldDate.AddDays(-3),
            Total = 2000m,
            CreatedAt = oldDate.AddDays(-3),
            UpdatedAt = oldDate.AddDays(-3)
        });

        SampleCompanyService.TimeShiftSampleData(data);

        var targetDate = DateTime.Today.AddDays(-1);
        Assert.Equal(targetDate.Date, data.Revenues[0].Date.Date);
        Assert.Equal(targetDate.Date.AddDays(-3), data.Expenses[0].Date.Date);
    }

    #endregion

    #region GetSampleCompanyPath Tests

    [Fact]
    public void GetSampleCompanyPath_ReturnsValidPath()
    {
        var path = SampleCompanyService.GetSampleCompanyPath();

        Assert.NotNull(path);
        Assert.EndsWith(".argo", path);
        Assert.Contains("SampleCompany", path);
    }

    [Fact]
    public void GetSampleCompanyPath_ContainsArgoBooksDirectory()
    {
        var path = SampleCompanyService.GetSampleCompanyPath();

        Assert.Contains("ArgoBooks", path);
    }

    #endregion
}
