using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the BoolToParameterConverter class.
/// </summary>
public class BoolToParameterConverterTests
{
    #region ReturnWhen True Tests

    [Fact]
    public void Convert_TrueWhenReturnWhenTrue_ReturnsParameter()
    {
        var converter = new BoolToParameterConverter(true);

        var result = converter.Convert(true, typeof(object), "MyParam", null!);

        Assert.Equal("MyParam", result);
    }

    [Fact]
    public void Convert_FalseWhenReturnWhenTrue_ReturnsNull()
    {
        var converter = new BoolToParameterConverter(true);

        var result = converter.Convert(false, typeof(object), "MyParam", null!);

        Assert.Null(result);
    }

    #endregion

    #region ReturnWhen False Tests

    [Fact]
    public void Convert_FalseWhenReturnWhenFalse_ReturnsParameter()
    {
        var converter = new BoolToParameterConverter(false);

        var result = converter.Convert(false, typeof(object), "MyParam", null!);

        Assert.Equal("MyParam", result);
    }

    [Fact]
    public void Convert_TrueWhenReturnWhenFalse_ReturnsNull()
    {
        var converter = new BoolToParameterConverter(false);

        var result = converter.Convert(true, typeof(object), "MyParam", null!);

        Assert.Null(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Convert_NonBoolValue_ReturnsNull()
    {
        var converter = new BoolToParameterConverter(true);

        var result = converter.Convert("not bool", typeof(object), "MyParam", null!);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        var converter = new BoolToParameterConverter(true);

        var result = converter.Convert(null, typeof(object), "MyParam", null!);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_MatchingBoolNullParameter_ReturnsNull()
    {
        var converter = new BoolToParameterConverter(true);

        var result = converter.Convert(true, typeof(object), null, null!);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_ObjectParameter_ReturnsObjectParameter()
    {
        var converter = new BoolToParameterConverter(true);
        var param = new object();

        var result = converter.Convert(true, typeof(object), param, null!);

        Assert.Same(param, result);
    }

    #endregion
}
