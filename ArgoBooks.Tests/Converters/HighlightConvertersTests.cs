using Avalonia;
using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the highlight multi-converter classes.
/// </summary>
public class HighlightConvertersTests
{
    #region ObjectEqualsMultiConverter Tests

    [Fact]
    public void ObjectEquals_EqualValues_ReturnsTrue()
    {
        var converter = new ObjectEqualsMultiConverter();
        var values = new List<object?> { "test", "test" };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void ObjectEquals_DifferentValues_ReturnsFalse()
    {
        var converter = new ObjectEqualsMultiConverter();
        var values = new List<object?> { "test", "other" };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void ObjectEquals_BothNull_ReturnsTrue()
    {
        var converter = new ObjectEqualsMultiConverter();
        var values = new List<object?> { null, null };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void ObjectEquals_OneNull_ReturnsFalse()
    {
        var converter = new ObjectEqualsMultiConverter();
        var values = new List<object?> { "test", null };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void ObjectEquals_InsufficientValues_ReturnsFalse()
    {
        var converter = new ObjectEqualsMultiConverter();
        var values = new List<object?> { "test" };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void ObjectEquals_EqualIntegers_ReturnsTrue()
    {
        var converter = new ObjectEqualsMultiConverter();
        var values = new List<object?> { 42, 42 };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    #endregion

    #region HighlightBorderThicknessMultiConverter Tests

    [Fact]
    public void HighlightBorderThickness_EqualValues_ReturnsBorderThickness()
    {
        var converter = new HighlightBorderThicknessMultiConverter();
        var values = new List<object?> { "page1", "page1" };

        var result = (Thickness)converter.Convert(values, typeof(Thickness), null, null!);

        Assert.Equal(new Thickness(2, 0, 0, 0), result);
    }

    [Fact]
    public void HighlightBorderThickness_DifferentValues_ReturnsZeroThickness()
    {
        var converter = new HighlightBorderThicknessMultiConverter();
        var values = new List<object?> { "page1", "page2" };

        var result = (Thickness)converter.Convert(values, typeof(Thickness), null, null!);

        Assert.Equal(new Thickness(0), result);
    }

    [Fact]
    public void HighlightBorderThickness_InsufficientValues_ReturnsZeroThickness()
    {
        var converter = new HighlightBorderThicknessMultiConverter();
        var values = new List<object?> { "page1" };

        var result = (Thickness)converter.Convert(values, typeof(Thickness), null, null!);

        Assert.Equal(new Thickness(0), result);
    }

    #endregion
}
