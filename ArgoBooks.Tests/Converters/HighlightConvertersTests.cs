using Avalonia;
using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the highlight multi-converter classes.
/// </summary>
public class HighlightConvertersTests
{
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
