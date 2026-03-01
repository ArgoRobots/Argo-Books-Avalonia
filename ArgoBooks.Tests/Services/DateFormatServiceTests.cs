using System.Globalization;
using System.Reflection;
using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the DateFormatService class.
/// Since DateFormatService is a static class that depends on App.CompanyManager for CurrentFormat,
/// we test the private GetDotNetFormat method directly via reflection, and the pure static methods.
/// </summary>
public class DateFormatServiceTests
{
    private static readonly MethodInfo GetDotNetFormatMethod =
        typeof(DateFormatService).GetMethod("GetDotNetFormat", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string InvokeGetDotNetFormat(string userFormat) =>
        (string)GetDotNetFormatMethod.Invoke(null, [userFormat])!;

    #region Format with MM/DD/YYYY Tests

    [Fact]
    public void GetDotNetFormat_MMDDYYYY_ReturnsCorrectDotNetFormat()
    {
        var result = InvokeGetDotNetFormat("MM/DD/YYYY");

        Assert.Equal("MM/dd/yyyy", result);
    }

    [Fact]
    public void Format_MMDDYYYY_FormatsDateCorrectly()
    {
        var date = new DateTime(2025, 3, 15);
        var format = InvokeGetDotNetFormat("MM/DD/YYYY");

        var result = date.ToString(format, CultureInfo.InvariantCulture);

        Assert.Equal("03/15/2025", result);
    }

    #endregion

    #region Format with DD/MM/YYYY Tests

    [Fact]
    public void GetDotNetFormat_DDMMYYYY_ReturnsCorrectDotNetFormat()
    {
        var result = InvokeGetDotNetFormat("DD/MM/YYYY");

        Assert.Equal("dd/MM/yyyy", result);
    }

    [Fact]
    public void Format_DDMMYYYY_FormatsDateCorrectly()
    {
        var date = new DateTime(2025, 3, 15);
        var format = InvokeGetDotNetFormat("DD/MM/YYYY");

        var result = date.ToString(format, CultureInfo.InvariantCulture);

        Assert.Equal("15/03/2025", result);
    }

    #endregion

    #region Format with YYYY-MM-DD Tests

    [Fact]
    public void GetDotNetFormat_YYYYMMDD_ReturnsCorrectDotNetFormat()
    {
        var result = InvokeGetDotNetFormat("YYYY-MM-DD");

        Assert.Equal("yyyy-MM-dd", result);
    }

    [Fact]
    public void Format_YYYYMMDD_FormatsDateCorrectly()
    {
        var date = new DateTime(2025, 3, 15);
        var format = InvokeGetDotNetFormat("YYYY-MM-DD");

        var result = date.ToString(format);

        Assert.Equal("2025-03-15", result);
    }

    #endregion

    #region Format with null/unknown Tests

    [Fact]
    public void Format_NullableDate_NullValue_ReturnsEmpty()
    {
        DateTime? date = null;

        var result = DateFormatService.Format(date);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetDotNetFormat_UnknownFormat_FallsBackToMMDDYYYY()
    {
        var result = InvokeGetDotNetFormat("UNKNOWN");

        Assert.Equal("MM/dd/yyyy", result);
    }

    [Fact]
    public void GetDotNetFormat_MMMDYYYY_ReturnsCorrectDotNetFormat()
    {
        var result = InvokeGetDotNetFormat("MMM D, YYYY");

        Assert.Equal("MMM d, yyyy", result);
    }

    #endregion

    #region GetCurrentDotNetFormat Tests

    [Fact]
    public void GetDotNetFormat_AllSupportedFormats_ReturnValidDotNetFormats()
    {
        var formats = new[]
        {
            ("MM/DD/YYYY", "MM/dd/yyyy"),
            ("DD/MM/YYYY", "dd/MM/yyyy"),
            ("YYYY-MM-DD", "yyyy-MM-dd"),
            ("MMM D, YYYY", "MMM d, yyyy")
        };

        foreach (var (input, expected) in formats)
        {
            var result = InvokeGetDotNetFormat(input);
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [InlineData("MM/DD/YYYY", "MM/dd/yyyy")]
    [InlineData("DD/MM/YYYY", "dd/MM/yyyy")]
    [InlineData("YYYY-MM-DD", "yyyy-MM-dd")]
    [InlineData("MMM D, YYYY", "MMM d, yyyy")]
    public void GetDotNetFormat_VariousFormats_ReturnsExpected(string input, string expected)
    {
        var result = InvokeGetDotNetFormat(input);

        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatMonthYear Tests

    [Fact]
    public void FormatMonthYear_ValidDate_ReturnsMMMYYYYFormat()
    {
        var date = new DateTime(2025, 6, 15);

        var result = DateFormatService.FormatMonthYear(date);

        Assert.Equal("Jun 2025", result);
    }

    [Fact]
    public void FormatMonthYear_January_ReturnsJan()
    {
        var date = new DateTime(2024, 1, 1);

        var result = DateFormatService.FormatMonthYear(date);

        Assert.Equal("Jan 2024", result);
    }

    [Fact]
    public void FormatMonthYear_December_ReturnsDec()
    {
        var date = new DateTime(2025, 12, 31);

        var result = DateFormatService.FormatMonthYear(date);

        Assert.Equal("Dec 2025", result);
    }

    #endregion
}
