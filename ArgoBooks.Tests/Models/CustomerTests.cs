using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Customer model.
/// </summary>
public class CustomerTests
{
    #region Default Value Tests

    [Fact]
    public void Customer_DefaultValues_AreCorrect()
    {
        var customer = new Customer();

        Assert.Equal(string.Empty, customer.Name);
        Assert.Null(customer.CompanyName);
        Assert.NotNull(customer.Address);
        Assert.Equal(string.Empty, customer.Notes);
        Assert.Equal(EntityStatus.Active, customer.Status);
        Assert.Equal(0m, customer.TotalPurchases);
        Assert.Null(customer.LastTransactionDate);
    }

    #endregion

    #region Name and Company Tests

    [Fact]
    public void Customer_Name_CanBeSet()
    {
        var customer = new Customer
        {
            Name = "John Doe"
        };

        Assert.Equal("John Doe", customer.Name);
    }

    [Fact]
    public void Customer_CompanyName_CanBeSet()
    {
        var customer = new Customer
        {
            Name = "Jane Smith",
            CompanyName = "Acme Corporation"
        };

        Assert.Equal("Jane Smith", customer.Name);
        Assert.Equal("Acme Corporation", customer.CompanyName);
    }

    [Fact]
    public void Customer_WithCompanyOnly_IsValid()
    {
        var customer = new Customer
        {
            Name = "Acme Corporation",
            CompanyName = null
        };

        Assert.Equal("Acme Corporation", customer.Name);
        Assert.Null(customer.CompanyName);
    }

    #endregion

    #region Address Tests

    [Fact]
    public void Customer_Address_IsInitialized()
    {
        var customer = new Customer();

        Assert.NotNull(customer.Address);
    }

    [Fact]
    public void Customer_Address_CanBeFullyPopulated()
    {
        var customer = new Customer
        {
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701",
                Country = "USA"
            }
        };

        Assert.Equal("123 Main St", customer.Address.Street);
        Assert.Equal("Springfield", customer.Address.City);
        Assert.Equal("IL", customer.Address.State);
        Assert.Equal("62701", customer.Address.ZipCode);
        Assert.Equal("USA", customer.Address.Country);
    }

    #endregion

    #region Status Tests

    [Theory]
    [InlineData(EntityStatus.Active)]
    [InlineData(EntityStatus.Inactive)]
    public void Customer_Status_SupportsExpectedValues(EntityStatus status)
    {
        var customer = new Customer
        {
            Status = status
        };

        Assert.Equal(status, customer.Status);
    }

    [Fact]
    public void Customer_Status_DefaultsToActive()
    {
        var customer = new Customer();

        Assert.Equal(EntityStatus.Active, customer.Status);
    }

    #endregion

    #region Total Purchases Tests

    [Fact]
    public void Customer_TotalPurchases_DefaultsToZero()
    {
        var customer = new Customer();

        Assert.Equal(0m, customer.TotalPurchases);
    }

    [Fact]
    public void Customer_TotalPurchases_CanBeSet()
    {
        var customer = new Customer
        {
            TotalPurchases = 15000.50m
        };

        Assert.Equal(15000.50m, customer.TotalPurchases);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(50000)]
    [InlineData(999999.99)]
    public void Customer_TotalPurchases_SupportsVariousAmounts(decimal amount)
    {
        var customer = new Customer
        {
            TotalPurchases = amount
        };

        Assert.Equal(amount, customer.TotalPurchases);
    }

    #endregion

    #region Last Transaction Date Tests

    [Fact]
    public void Customer_LastTransactionDate_DefaultsToNull()
    {
        var customer = new Customer();

        Assert.Null(customer.LastTransactionDate);
    }

    [Fact]
    public void Customer_LastTransactionDate_CanBeSet()
    {
        var date = new DateTime(2024, 6, 15);
        var customer = new Customer
        {
            LastTransactionDate = date
        };

        Assert.Equal(date, customer.LastTransactionDate);
    }

    [Fact]
    public void Customer_NewCustomer_HasNoTransactionHistory()
    {
        var customer = new Customer
        {
            Name = "New Customer",
            TotalPurchases = 0m,
            LastTransactionDate = null
        };

        Assert.Equal(0m, customer.TotalPurchases);
        Assert.Null(customer.LastTransactionDate);
    }

    #endregion

    #region Notes Tests

    [Fact]
    public void Customer_Notes_DefaultsToEmpty()
    {
        var customer = new Customer();

        Assert.Equal(string.Empty, customer.Notes);
    }

    [Fact]
    public void Customer_Notes_CanStoreText()
    {
        var customer = new Customer
        {
            Notes = "Prefers email communication. Payment terms: Net 30."
        };

        Assert.Contains("email communication", customer.Notes);
        Assert.Contains("Net 30", customer.Notes);
    }

    #endregion

    #region BaseEntity Inheritance Tests

    [Fact]
    public void Customer_InheritsFromBaseEntity()
    {
        var customer = new Customer();

        // BaseEntity should provide Id and timestamps
        Assert.NotNull(customer);
    }

    #endregion
}
