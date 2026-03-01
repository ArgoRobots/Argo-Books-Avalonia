using Avalonia.Media;
using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the DepartmentColorToBrushConverter class.
/// </summary>
public class DepartmentColorToBrushConverterTests
{
    private readonly DepartmentColorToBrushConverter _converter = new();

    #region Color Mapping Tests

    [Theory]
    [InlineData("blue", "#dbeafe")]
    [InlineData("green", "#dcfce7")]
    [InlineData("yellow", "#fef3c7")]
    [InlineData("purple", "#f3e8ff")]
    [InlineData("red", "#fee2e2")]
    [InlineData("cyan", "#cffafe")]
    [InlineData("orange", "#ffedd5")]
    [InlineData("pink", "#fce7f3")]
    public void Convert_KnownColor_ReturnsCorrectBrush(string colorName, string expectedHex)
    {
        var result = _converter.Convert(colorName, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        var expected = Color.Parse(expectedHex);
        Assert.Equal(expected, result.Color);
    }

    [Fact]
    public void Convert_UnknownColor_ReturnsBlueFallback()
    {
        var result = _converter.Convert("magenta", typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        var expected = Color.Parse("#dbeafe");
        Assert.Equal(expected, result.Color);
    }

    [Fact]
    public void Convert_NullValue_ReturnsBlueFallback()
    {
        var result = _converter.Convert(null, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        var expected = Color.Parse("#dbeafe");
        Assert.Equal(expected, result.Color);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsBlueFallback()
    {
        var result = _converter.Convert("", typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        var expected = Color.Parse("#dbeafe");
        Assert.Equal(expected, result.Color);
    }

    #endregion
}
