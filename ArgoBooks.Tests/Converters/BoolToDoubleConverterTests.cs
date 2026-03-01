using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the BoolToDoubleConverter class.
/// </summary>
public class BoolToDoubleConverterTests
{
    private readonly BoolToDoubleConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_True_ReturnsTrueValue()
    {
        var result = _converter.Convert(true, typeof(double), "100,50", null!);

        Assert.Equal(100.0, result);
    }

    [Fact]
    public void Convert_False_ReturnsFalseValue()
    {
        var result = _converter.Convert(false, typeof(double), "100,50", null!);

        Assert.Equal(50.0, result);
    }

    [Theory]
    [InlineData("200,100", true, 200.0)]
    [InlineData("200,100", false, 100.0)]
    [InlineData("0,500", true, 0.0)]
    [InlineData("0,500", false, 500.0)]
    public void Convert_VariousValues_ReturnsCorrectDouble(string parameter, bool value, double expected)
    {
        var result = _converter.Convert(value, typeof(double), parameter, null!);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsZero()
    {
        var result = _converter.Convert("not bool", typeof(double), "100,50", null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsZero()
    {
        var result = _converter.Convert(true, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_InvalidParameter_ReturnsZero()
    {
        var result = _converter.Convert(true, typeof(double), "notanumber", null!);

        Assert.Equal(0.0, result);
    }

    #endregion
}
