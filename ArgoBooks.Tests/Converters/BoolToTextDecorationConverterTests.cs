using Avalonia.Media;
using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the BoolToTextDecorationConverter class.
/// </summary>
public class BoolToTextDecorationConverterTests
{
    private readonly BoolToTextDecorationConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_True_ReturnsStrikethrough()
    {
        var result = _converter.Convert(true, typeof(TextDecorationCollection), null, null!);

        Assert.Equal(TextDecorations.Strikethrough, result);
    }

    [Fact]
    public void Convert_False_ReturnsNull()
    {
        var result = _converter.Convert(false, typeof(TextDecorationCollection), null, null!);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsNull()
    {
        var result = _converter.Convert("not bool", typeof(TextDecorationCollection), null, null!);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        var result = _converter.Convert(null, typeof(TextDecorationCollection), null, null!);

        Assert.Null(result);
    }

    #endregion
}
