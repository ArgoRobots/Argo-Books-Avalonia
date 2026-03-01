using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the BoolToFixedStringConverter class.
/// </summary>
public class BoolToFixedStringConverterTests
{
    #region Convert Tests

    [Fact]
    public void Convert_True_ReturnsTrueValue()
    {
        var converter = new BoolToFixedStringConverter("Finish", "Next");

        var result = converter.Convert(true, typeof(string), null, null!);

        Assert.Equal("Finish", result);
    }

    [Fact]
    public void Convert_False_ReturnsFalseValue()
    {
        var converter = new BoolToFixedStringConverter("Finish", "Next");

        var result = converter.Convert(false, typeof(string), null, null!);

        Assert.Equal("Next", result);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsFalseValue()
    {
        var converter = new BoolToFixedStringConverter("Yes", "No");

        var result = converter.Convert("not a bool", typeof(string), null, null!);

        Assert.Equal("No", result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalseValue()
    {
        var converter = new BoolToFixedStringConverter("Yes", "No");

        var result = converter.Convert(null, typeof(string), null, null!);

        Assert.Equal("No", result);
    }

    [Theory]
    [InlineData("Save", "Cancel")]
    [InlineData("Enable", "Disable")]
    [InlineData("", "")]
    public void Convert_VariousStrings_ReturnsCorrectValue(string trueValue, string falseValue)
    {
        var converter = new BoolToFixedStringConverter(trueValue, falseValue);

        Assert.Equal(trueValue, converter.Convert(true, typeof(string), null, null!));
        Assert.Equal(falseValue, converter.Convert(false, typeof(string), null, null!));
    }

    #endregion
}
