using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the PercentDollarConverter class.
/// </summary>
public class PercentDollarConverterTests
{
    private readonly PercentDollarConverter _converter = PercentDollarConverter.Instance;

    #region Convert Tests

    [Fact]
    public void Convert_True_ReturnsPercent()
    {
        var result = _converter.Convert(true, typeof(string), null, null!);

        Assert.Equal("%", result);
    }

    [Fact]
    public void Convert_False_ReturnsDollar()
    {
        var result = _converter.Convert(false, typeof(string), null, null!);

        Assert.Equal("$", result);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = PercentDollarConverter.Instance;
        var instance2 = PercentDollarConverter.Instance;

        Assert.Same(instance1, instance2);
    }

    #endregion
}
