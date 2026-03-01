using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the StringEqualsConverter class.
/// </summary>
public class StringEqualsConverterTests
{
    #region Basic Equality Tests

    [Fact]
    public void Convert_MatchingStrings_ReturnsTrue()
    {
        var converter = new StringEqualsConverter { CompareValue = "test" };

        var result = converter.Convert("test", typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_NonMatchingStrings_ReturnsFalse()
    {
        var converter = new StringEqualsConverter { CompareValue = "test" };

        var result = converter.Convert("other", typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_CaseInsensitive_ReturnsTrue()
    {
        var converter = new StringEqualsConverter { CompareValue = "Test" };

        var result = converter.Convert("test", typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var converter = new StringEqualsConverter { CompareValue = "test" };

        var result = converter.Convert(null, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsFalse()
    {
        var converter = new StringEqualsConverter { CompareValue = "test" };

        var result = converter.Convert(42, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    #endregion

    #region TrueValue/FalseValue Tests

    [Fact]
    public void Convert_WithTrueValue_MatchReturnsCustomValue()
    {
        var converter = new StringEqualsConverter
        {
            CompareValue = "active",
            TrueValue = "visible",
            FalseValue = "hidden"
        };

        var result = converter.Convert("active", typeof(object), null, null!);

        Assert.Equal("visible", result);
    }

    [Fact]
    public void Convert_WithFalseValue_NoMatchReturnsCustomValue()
    {
        var converter = new StringEqualsConverter
        {
            CompareValue = "active",
            TrueValue = "visible",
            FalseValue = "hidden"
        };

        var result = converter.Convert("inactive", typeof(object), null, null!);

        Assert.Equal("hidden", result);
    }

    #endregion
}
