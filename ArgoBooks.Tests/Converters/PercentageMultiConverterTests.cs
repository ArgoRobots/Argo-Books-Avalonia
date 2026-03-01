using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the PercentageMultiConverter class.
/// </summary>
public class PercentageMultiConverterTests
{
    private readonly PercentageMultiConverter _converter = new();

    #region Basic Calculation Tests

    [Fact]
    public void Convert_50PercentOf200_Returns100()
    {
        var values = new List<object?> { 50.0, 200.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(100.0, result);
    }

    [Fact]
    public void Convert_100PercentOf200_Returns200()
    {
        var values = new List<object?> { 100.0, 200.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(200.0, result);
    }

    [Fact]
    public void Convert_0PercentOf200_Returns0()
    {
        var values = new List<object?> { 0.0, 200.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_25PercentOf400_Returns100()
    {
        var values = new List<object?> { 25.0, 400.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(100.0, result);
    }

    #endregion

    #region Integer Input Tests

    [Fact]
    public void Convert_IntPercentage_ReturnsCorrectValue()
    {
        var values = new List<object?> { 50, 200.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(100.0, result);
    }

    [Fact]
    public void Convert_IntTotalWidth_ReturnsCorrectValue()
    {
        var values = new List<object?> { 50.0, 200 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(100.0, result);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void Convert_PercentageExceeds100_ClampsToTotal()
    {
        var values = new List<object?> { 150.0, 200.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(200.0, result);
    }

    [Fact]
    public void Convert_NegativePercentage_Returns0()
    {
        var values = new List<object?> { -10.0, 200.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_ZeroTotalWidth_Returns0()
    {
        var values = new List<object?> { 50.0, 0.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_NegativeTotalWidth_Returns0()
    {
        var values = new List<object?> { 50.0, -100.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    #endregion

    #region Insufficient Values Tests

    [Fact]
    public void Convert_EmptyValues_Returns0()
    {
        var values = new List<object?>();

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_SingleValue_Returns0()
    {
        var values = new List<object?> { 50.0 };

        var result = _converter.Convert(values, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    #endregion
}
