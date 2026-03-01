using ArgoBooks.Controls;
using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the SortIndicatorConverter class.
/// </summary>
public class SortIndicatorConverterTests
{
    private readonly SortIndicatorConverter _converter = SortIndicatorConverter.Instance;

    #region Parameter Mode Tests

    [Fact]
    public void Convert_MatchingColumnAndDirection_ReturnsTrue()
    {
        var values = new List<object?> { "Price", SortDirection.Ascending };

        var result = _converter.Convert(values, typeof(bool), "Price:Ascending", null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_MatchingColumnDifferentDirection_ReturnsFalse()
    {
        var values = new List<object?> { "Price", SortDirection.Descending };

        var result = _converter.Convert(values, typeof(bool), "Price:Ascending", null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_DifferentColumn_ReturnsFalse()
    {
        var values = new List<object?> { "Name", SortDirection.Ascending };

        var result = _converter.Convert(values, typeof(bool), "Price:Ascending", null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_DescendingDirection_ReturnsTrue()
    {
        var values = new List<object?> { "Date", SortDirection.Descending };

        var result = _converter.Convert(values, typeof(bool), "Date:Descending", null!);

        Assert.Equal(true, result);
    }

    #endregion

    #region Values Mode Tests

    [Fact]
    public void Convert_ValuesMode_MatchingAll_ReturnsTrue()
    {
        var values = new List<object?> { "Name", SortDirection.Ascending, "Name", "Ascending" };

        var result = _converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_ValuesMode_DifferentColumn_ReturnsFalse()
    {
        var values = new List<object?> { "Price", SortDirection.Ascending, "Name", "Ascending" };

        var result = _converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_ValuesMode_DifferentDirection_ReturnsFalse()
    {
        var values = new List<object?> { "Name", SortDirection.Descending, "Name", "Ascending" };

        var result = _converter.Convert(values, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Convert_InsufficientValues_ReturnsFalse()
    {
        var values = new List<object?> { "Name" };

        var result = _converter.Convert(values, typeof(bool), "Name:Ascending", null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_NullSortColumn_ReturnsFalse()
    {
        var values = new List<object?> { null, SortDirection.Ascending };

        var result = _converter.Convert(values, typeof(bool), "Name:Ascending", null!);

        Assert.Equal(false, result);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        Assert.Same(SortIndicatorConverter.Instance, SortIndicatorConverter.Instance);
    }

    #endregion
}
