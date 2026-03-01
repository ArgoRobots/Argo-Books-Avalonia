using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the NullableDecimalConverter class.
/// </summary>
public class NullableDecimalConverterTests
{
    private readonly NullableDecimalConverter _converter = NullableDecimalConverter.Instance;

    #region Convert Tests

    [Fact]
    public void Convert_DecimalValue_ToStringTarget_ReturnsFormattedString()
    {
        var result = _converter.Convert(123.45m, typeof(string), null, null!);

        Assert.NotNull(result);
        Assert.Contains("123", result.ToString()!);
    }

    [Fact]
    public void Convert_DecimalValue_ToDecimalTarget_ReturnsDecimal()
    {
        var result = _converter.Convert(123.45m, typeof(decimal), null, null!);

        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void Convert_NullableDecimalTarget_ReturnsDecimal()
    {
        var result = _converter.Convert(50.0m, typeof(decimal?), null, null!);

        Assert.Equal(50.0m, result);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_ValidDecimalString_ReturnsDecimal()
    {
        var result = _converter.ConvertBack("123.45", typeof(decimal), null, null!);

        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void ConvertBack_EmptyString_ReturnsZero()
    {
        var result = _converter.ConvertBack("", typeof(decimal), null, null!);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ConvertBack_WhitespaceString_ReturnsZero()
    {
        var result = _converter.ConvertBack("   ", typeof(decimal), null, null!);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ConvertBack_NullValue_ReturnsZero()
    {
        var result = _converter.ConvertBack(null, typeof(decimal), null, null!);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ConvertBack_DecimalValue_ReturnsAsIs()
    {
        var result = _converter.ConvertBack(99.99m, typeof(decimal), null, null!);

        Assert.Equal(99.99m, result);
    }

    [Fact]
    public void ConvertBack_InvalidString_ReturnsZero()
    {
        var result = _converter.ConvertBack("not a number", typeof(decimal), null, null!);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ConvertBack_IntegerString_ReturnsDecimal()
    {
        var result = _converter.ConvertBack("100", typeof(decimal), null, null!);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void ConvertBack_NegativeString_ReturnsNegativeDecimal()
    {
        var result = _converter.ConvertBack("-50.5", typeof(decimal), null, null!);

        Assert.Equal(-50.5m, result);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = NullableDecimalConverter.Instance;
        var instance2 = NullableDecimalConverter.Instance;

        Assert.Same(instance1, instance2);
    }

    #endregion
}
