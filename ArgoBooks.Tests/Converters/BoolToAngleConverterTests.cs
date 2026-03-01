using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the BoolToAngleConverter class.
/// </summary>
public class BoolToAngleConverterTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultTrueAngle_Is90()
    {
        var converter = new BoolToAngleConverter();

        Assert.Equal(90, converter.TrueAngle);
    }

    [Fact]
    public void DefaultFalseAngle_Is0()
    {
        var converter = new BoolToAngleConverter();

        Assert.Equal(0, converter.FalseAngle);
    }

    #endregion

    #region Convert Tests

    [Fact]
    public void Convert_True_ReturnsTrueAngle()
    {
        var converter = new BoolToAngleConverter { TrueAngle = 180, FalseAngle = 0 };

        var result = converter.Convert(true, typeof(double), null, null!);

        Assert.Equal(180.0, result);
    }

    [Fact]
    public void Convert_False_ReturnsFalseAngle()
    {
        var converter = new BoolToAngleConverter { TrueAngle = 180, FalseAngle = 45 };

        var result = converter.Convert(false, typeof(double), null, null!);

        Assert.Equal(45.0, result);
    }

    [Fact]
    public void Convert_TrueWithDefaults_Returns90()
    {
        var converter = new BoolToAngleConverter();

        var result = converter.Convert(true, typeof(double), null, null!);

        Assert.Equal(90.0, result);
    }

    [Fact]
    public void Convert_FalseWithDefaults_Returns0()
    {
        var converter = new BoolToAngleConverter();

        var result = converter.Convert(false, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsFalseAngle()
    {
        var converter = new BoolToAngleConverter { TrueAngle = 90, FalseAngle = 0 };

        var result = converter.Convert("not a bool", typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalseAngle()
    {
        var converter = new BoolToAngleConverter { TrueAngle = 90, FalseAngle = 0 };

        var result = converter.Convert(null, typeof(double), null, null!);

        Assert.Equal(0.0, result);
    }

    [Theory]
    [InlineData(0, 360)]
    [InlineData(-90, 90)]
    [InlineData(45, 135)]
    public void Convert_CustomAngles_ReturnsCorrectAngle(double falseAngle, double trueAngle)
    {
        var converter = new BoolToAngleConverter { TrueAngle = trueAngle, FalseAngle = falseAngle };

        var trueResult = converter.Convert(true, typeof(double), null, null!);
        var falseResult = converter.Convert(false, typeof(double), null, null!);

        Assert.Equal(trueAngle, trueResult);
        Assert.Equal(falseAngle, falseResult);
    }

    #endregion
}
