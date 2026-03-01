using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the FullscreenMinMaxConverter class.
/// </summary>
public class FullscreenMinMaxConverterTests
{
    #region Fullscreen Min Tests

    [Fact]
    public void Convert_Fullscreen_IsMin_ReturnsZero()
    {
        var converter = new FullscreenMinMaxConverter(800, true);

        var result = converter.Convert(true, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    #endregion

    #region Fullscreen Max Tests

    [Fact]
    public void Convert_Fullscreen_IsMax_ReturnsPositiveInfinity()
    {
        var converter = new FullscreenMinMaxConverter(1200, false);

        var result = converter.Convert(true, typeof(double), null, null!);

        Assert.Equal(double.PositiveInfinity, result);
    }

    #endregion

    #region Normal Mode Tests

    [Fact]
    public void Convert_NotFullscreen_IsMin_ReturnsParameterOrDefault()
    {
        var converter = new FullscreenMinMaxConverter(800, true);

        var result = converter.Convert(false, typeof(double), null, null!);

        Assert.Equal(800.0, result);
    }

    [Fact]
    public void Convert_NotFullscreen_WithDoubleParameter_ReturnsParameter()
    {
        var converter = new FullscreenMinMaxConverter(800, true);

        var result = converter.Convert(false, typeof(double), 600.0, null!);

        Assert.Equal(600.0, result);
    }

    [Fact]
    public void Convert_NotFullscreen_WithStringParameter_ReturnsParsedValue()
    {
        var converter = new FullscreenMinMaxConverter(800, true);

        var result = converter.Convert(false, typeof(double), "600", null!);

        Assert.Equal(600.0, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Convert_NonBoolValue_ReturnsDefault()
    {
        var converter = new FullscreenMinMaxConverter(800, true);

        var result = converter.Convert("not bool", typeof(double), null, null!);

        Assert.Equal(800.0, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsDefault()
    {
        var converter = new FullscreenMinMaxConverter(1200, false);

        var result = converter.Convert(null, typeof(double), null, null!);

        Assert.Equal(1200.0, result);
    }

    #endregion
}
