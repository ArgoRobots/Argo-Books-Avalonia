using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for PageEqualsConverter and PageActiveBackgroundConverter.
/// </summary>
public class PageConvertersTests
{
    #region PageEqualsConverter Tests

    [Fact]
    public void PageEquals_EqualValues_ReturnsTrue()
    {
        var converter = PageEqualsConverter.Instance;
        var values = new List<object?> { "Dashboard", "Dashboard" };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void PageEquals_DifferentValues_ReturnsFalse()
    {
        var converter = PageEqualsConverter.Instance;
        var values = new List<object?> { "Dashboard", "Settings" };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void PageEquals_InsufficientValues_ReturnsFalse()
    {
        var converter = PageEqualsConverter.Instance;
        var values = new List<object?> { "Dashboard" };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void PageEquals_EmptyValues_ReturnsFalse()
    {
        var converter = PageEqualsConverter.Instance;
        var values = new List<object?>();

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void PageEquals_BothNull_ReturnsTrue()
    {
        var converter = PageEqualsConverter.Instance;
        var values = new List<object?> { null, null };

        var result = converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void PageEquals_Instance_ReturnsSameInstance()
    {
        Assert.Same(PageEqualsConverter.Instance, PageEqualsConverter.Instance);
    }

    [Fact]
    public void PageActiveBackground_Instance_ReturnsSameInstance()
    {
        Assert.Same(PageActiveBackgroundConverter.Instance, PageActiveBackgroundConverter.Instance);
    }

    #endregion
}
