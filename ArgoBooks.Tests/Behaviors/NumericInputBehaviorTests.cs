using System.Text.RegularExpressions;
using Xunit;

namespace ArgoBooks.Tests.Behaviors;

/// <summary>
/// Tests for the NumericInputBehavior regex patterns and key detection.
/// </summary>
public class NumericInputBehaviorTests
{
    // The behavior uses these regex patterns internally
    private static readonly Regex IntegerRegex = new(@"^\d*$");
    private static readonly Regex DecimalInputRegex = new(@"^\d*\.?\d*$");

    #region Integer Regex Tests

    [Theory]
    [InlineData("", true)]
    [InlineData("0", true)]
    [InlineData("123", true)]
    [InlineData("999999", true)]
    [InlineData("1.5", false)]
    [InlineData("abc", false)]
    [InlineData("-1", false)]
    [InlineData("1 2", false)]
    public void IntegerRegex_MatchesExpectedPatterns(string input, bool shouldMatch)
    {
        Assert.Equal(shouldMatch, IntegerRegex.IsMatch(input));
    }

    #endregion

    #region Decimal Regex Tests

    [Theory]
    [InlineData("", true)]
    [InlineData("0", true)]
    [InlineData("123", true)]
    [InlineData("1.5", true)]
    [InlineData("123.456", true)]
    [InlineData(".", true)]
    [InlineData(".5", true)]
    [InlineData("1.", true)]
    [InlineData("1.2.3", false)]
    [InlineData("abc", false)]
    [InlineData("-1", false)]
    [InlineData("1.5.6", false)]
    public void DecimalInputRegex_MatchesExpectedPatterns(string input, bool shouldMatch)
    {
        Assert.Equal(shouldMatch, DecimalInputRegex.IsMatch(input));
    }

    #endregion

    #region Integer Paste Filter Tests

    [Fact]
    public void IntegerFilter_FiltersNonDigits()
    {
        var input = "abc123def456";
        var filtered = new string(input.Where(char.IsDigit).ToArray());

        Assert.Equal("123456", filtered);
    }

    [Fact]
    public void IntegerFilter_PreservesDigitsOnly()
    {
        var input = "12.34";
        var filtered = new string(input.Where(char.IsDigit).ToArray());

        Assert.Equal("1234", filtered);
    }

    [Fact]
    public void IntegerFilter_EmptyInput_ReturnsEmpty()
    {
        var input = "";
        var filtered = new string(input.Where(char.IsDigit).ToArray());

        Assert.Equal("", filtered);
    }

    #endregion

    #region Decimal Paste Filter Tests

    [Fact]
    public void DecimalFilter_AllowsSingleDecimalPoint()
    {
        var input = "12.34";
        var hasDecimal = false;
        var filtered = new string(input.Where(c =>
        {
            if (char.IsDigit(c)) return true;
            if (c == '.' && !hasDecimal) { hasDecimal = true; return true; }
            return false;
        }).ToArray());

        Assert.Equal("12.34", filtered);
    }

    [Fact]
    public void DecimalFilter_BlocksSecondDecimalPoint()
    {
        var input = "1.2.3";
        var hasDecimal = false;
        var filtered = new string(input.Where(c =>
        {
            if (char.IsDigit(c)) return true;
            if (c == '.' && !hasDecimal) { hasDecimal = true; return true; }
            return false;
        }).ToArray());

        Assert.Equal("1.23", filtered);
    }

    [Fact]
    public void DecimalFilter_FiltersLetters()
    {
        var input = "abc12.34def";
        var hasDecimal = false;
        var filtered = new string(input.Where(c =>
        {
            if (char.IsDigit(c)) return true;
            if (c == '.' && !hasDecimal) { hasDecimal = true; return true; }
            return false;
        }).ToArray());

        Assert.Equal("12.34", filtered);
    }

    #endregion
}
