using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the IntConverters static converter instances.
/// </summary>
public class IntConvertersTests
{
    #region IsZero Tests

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(100, false)]
    public void IsZero_ReturnsCorrectValue(int input, bool expected)
    {
        var converter = IntConverters.IsZero;
        Assert.NotNull(converter);

        var result = converter.Convert(input, typeof(bool), null, null!);

        Assert.Equal(expected, result);
    }

    #endregion

    #region IsPositive Tests

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    [InlineData(100, true)]
    public void IsPositive_ReturnsCorrectValue(int input, bool expected)
    {
        var converter = IntConverters.IsPositive;
        Assert.NotNull(converter);

        var result = converter.Convert(input, typeof(bool), null, null!);

        Assert.Equal(expected, result);
    }

    #endregion

    #region IsGreaterThanOne Tests

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(-1, false)]
    [InlineData(100, true)]
    public void IsGreaterThanOne_ReturnsCorrectValue(int input, bool expected)
    {
        var converter = IntConverters.IsGreaterThanOne;
        Assert.NotNull(converter);

        var result = converter.Convert(input, typeof(bool), null, null!);

        Assert.Equal(expected, result);
    }

    #endregion

    #region IsNotZero Tests

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    [InlineData(100, true)]
    public void IsNotZero_ReturnsCorrectValue(int input, bool expected)
    {
        var converter = IntConverters.IsNotZero;
        Assert.NotNull(converter);

        var result = converter.Convert(input, typeof(bool), null, null!);

        Assert.Equal(expected, result);
    }

    #endregion
}
