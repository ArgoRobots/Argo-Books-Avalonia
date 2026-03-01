using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the BoolToStringConverter class.
/// </summary>
public class BoolToStringConverterTests
{
    private readonly BoolToStringConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_TrueWithParameter_ReturnsFirstPart()
    {
        var result = _converter.Convert(true, typeof(string), "Yes;No", null!);

        Assert.Equal("Yes", result);
    }

    [Fact]
    public void Convert_FalseWithParameter_ReturnsSecondPart()
    {
        var result = _converter.Convert(false, typeof(string), "Yes;No", null!);

        Assert.Equal("No", result);
    }

    [Fact]
    public void Convert_TrueWithDifferentStrings_ReturnsCorrectPart()
    {
        var result = _converter.Convert(true, typeof(string), "Active;Inactive", null!);

        Assert.Equal("Active", result);
    }

    [Fact]
    public void Convert_FalseWithDifferentStrings_ReturnsCorrectPart()
    {
        var result = _converter.Convert(false, typeof(string), "Active;Inactive", null!);

        Assert.Equal("Inactive", result);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsEmpty()
    {
        var result = _converter.Convert("not a bool", typeof(string), "Yes;No", null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsEmpty()
    {
        var result = _converter.Convert(null, typeof(string), "Yes;No", null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsEmpty()
    {
        var result = _converter.Convert(true, typeof(string), null, null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Convert_ParameterWithoutSemicolon_ReturnsEmpty()
    {
        var result = _converter.Convert(true, typeof(string), "NoSemicolon", null!);

        Assert.Equal(string.Empty, result);
    }

    #endregion
}
