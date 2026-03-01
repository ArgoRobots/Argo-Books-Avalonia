using ArgoBooks.Core.Models.Common;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Address model.
/// </summary>
public class AddressTests
{
    #region ToString Tests

    [Fact]
    public void ToString_FullAddress_ReturnsCommaSeparatedValues()
    {
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            Country = "USA"
        };

        var result = address.ToString();

        Assert.Equal("123 Main St, Springfield, IL, 62701, USA", result);
    }

    [Fact]
    public void ToString_PartialAddress_OmitsEmptyParts()
    {
        var address = new Address
        {
            City = "Toronto",
            Country = "Canada"
        };

        var result = address.ToString();

        Assert.Equal("Toronto, Canada", result);
    }

    [Fact]
    public void ToString_EmptyAddress_ReturnsEmptyString()
    {
        var address = new Address();

        var result = address.ToString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_WhitespaceValues_AreTreatedAsEmpty()
    {
        var address = new Address
        {
            Street = "  ",
            City = "Vancouver",
            State = "",
            ZipCode = "V6B 1A1",
            Country = "Canada"
        };

        var result = address.ToString();

        Assert.Equal("Vancouver, V6B 1A1, Canada", result);
    }

    #endregion
}
