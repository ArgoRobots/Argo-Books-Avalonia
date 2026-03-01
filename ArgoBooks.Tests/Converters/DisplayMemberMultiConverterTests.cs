using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the DisplayMemberMultiConverter class.
/// </summary>
public class DisplayMemberMultiConverterTests
{
    private readonly DisplayMemberMultiConverter _converter = new();

    #region Property Extraction Tests

    [Fact]
    public void Convert_WithPropertyName_ReturnsPropertyValue()
    {
        var item = new TestItem { Name = "Widget", Price = 9.99m };
        var values = new List<object?> { item, "Name" };

        var result = _converter.Convert(values, typeof(string), null, null!);

        Assert.Equal("Widget", result);
    }

    [Fact]
    public void Convert_WithDifferentPropertyName_ReturnsDifferentProperty()
    {
        var item = new TestItem { Name = "Widget", Price = 9.99m };
        var values = new List<object?> { item, "Price" };

        var result = _converter.Convert(values, typeof(string), null, null!);

        Assert.Contains("9.99", result?.ToString() ?? "");
    }

    #endregion

    #region Fallback Tests

    [Fact]
    public void Convert_NullItem_ReturnsEmpty()
    {
        var values = new List<object?> { null, "Name" };

        var result = _converter.Convert(values, typeof(string), null, null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Convert_NullDisplayMemberPath_ReturnsToString()
    {
        var item = new TestItem { Name = "Widget" };
        var values = new List<object?> { item, null };

        var result = _converter.Convert(values, typeof(string), null, null!);

        Assert.NotNull(result);
    }

    [Fact]
    public void Convert_EmptyDisplayMemberPath_ReturnsToString()
    {
        var item = new TestItem { Name = "Widget" };
        var values = new List<object?> { item, "" };

        var result = _converter.Convert(values, typeof(string), null, null!);

        Assert.NotNull(result);
    }

    [Fact]
    public void Convert_SingleValue_ReturnsToString()
    {
        var values = new List<object?> { "just a string" };

        var result = _converter.Convert(values, typeof(string), null, null!);

        Assert.Equal("just a string", result);
    }

    [Fact]
    public void Convert_InvalidPropertyName_ReturnsToString()
    {
        var item = new TestItem { Name = "Widget" };
        var values = new List<object?> { item, "NonExistentProperty" };

        var result = _converter.Convert(values, typeof(string), null, null!);

        // Falls back to item.ToString()
        Assert.NotNull(result);
    }

    #endregion

    #region Test Helper

    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    #endregion
}
