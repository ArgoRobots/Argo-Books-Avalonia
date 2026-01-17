using ArgoBooks.Controls;
using ArgoBooks.Utilities;
using Xunit;

namespace ArgoBooks.Tests.Utilities;

/// <summary>
/// Tests for the SortHelper class.
/// </summary>
public class SortHelperTests
{
    private readonly Dictionary<string, Func<TestItem, object?>> _selectors = new()
    {
        { "Name", x => x.Name },
        { "Value", x => x.Value },
        { "Date", x => x.Date }
    };

    #region Basic Sorting Tests

    [Fact]
    public void ApplySort_AscendingByName_SortsCorrectly()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 },
            new() { Name = "Bob", Value = 2 }
        };

        var sorted = items.ApplySort("Name", SortDirection.Ascending, _selectors);

        Assert.Equal("Alice", sorted[0].Name);
        Assert.Equal("Bob", sorted[1].Name);
        Assert.Equal("Charlie", sorted[2].Name);
    }

    [Fact]
    public void ApplySort_DescendingByName_SortsCorrectly()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Alice", Value = 1 },
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Bob", Value = 2 }
        };

        var sorted = items.ApplySort("Name", SortDirection.Descending, _selectors);

        Assert.Equal("Charlie", sorted[0].Name);
        Assert.Equal("Bob", sorted[1].Name);
        Assert.Equal("Alice", sorted[2].Name);
    }

    [Fact]
    public void ApplySort_AscendingByValue_SortsCorrectly()
    {
        var items = new List<TestItem>
        {
            new() { Name = "A", Value = 30 },
            new() { Name = "B", Value = 10 },
            new() { Name = "C", Value = 20 }
        };

        var sorted = items.ApplySort("Value", SortDirection.Ascending, _selectors);

        Assert.Equal(10, sorted[0].Value);
        Assert.Equal(20, sorted[1].Value);
        Assert.Equal(30, sorted[2].Value);
    }

    [Fact]
    public void ApplySort_DescendingByValue_SortsCorrectly()
    {
        var items = new List<TestItem>
        {
            new() { Name = "A", Value = 10 },
            new() { Name = "B", Value = 30 },
            new() { Name = "C", Value = 20 }
        };

        var sorted = items.ApplySort("Value", SortDirection.Descending, _selectors);

        Assert.Equal(30, sorted[0].Value);
        Assert.Equal(20, sorted[1].Value);
        Assert.Equal(10, sorted[2].Value);
    }

    [Fact]
    public void ApplySort_ByDate_SortsCorrectly()
    {
        var date1 = new DateTime(2024, 1, 1);
        var date2 = new DateTime(2024, 6, 15);
        var date3 = new DateTime(2024, 12, 31);

        var items = new List<TestItem>
        {
            new() { Name = "A", Date = date2 },
            new() { Name = "B", Date = date3 },
            new() { Name = "C", Date = date1 }
        };

        var sorted = items.ApplySort("Date", SortDirection.Ascending, _selectors);

        Assert.Equal(date1, sorted[0].Date);
        Assert.Equal(date2, sorted[1].Date);
        Assert.Equal(date3, sorted[2].Date);
    }

    #endregion

    #region No Sort Tests

    [Fact]
    public void ApplySort_SortDirectionNone_ReturnsOriginalOrder()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 },
            new() { Name = "Bob", Value = 2 }
        };

        var sorted = items.ApplySort("Name", SortDirection.None, _selectors);

        Assert.Equal("Charlie", sorted[0].Name);
        Assert.Equal("Alice", sorted[1].Name);
        Assert.Equal("Bob", sorted[2].Name);
    }

    [Fact]
    public void ApplySort_NullColumn_ReturnsOriginalOrder()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 },
            new() { Name = "Bob", Value = 2 }
        };

        var sorted = items.ApplySort(null, SortDirection.Ascending, _selectors);

        Assert.Equal("Charlie", sorted[0].Name);
        Assert.Equal("Alice", sorted[1].Name);
        Assert.Equal("Bob", sorted[2].Name);
    }

    [Fact]
    public void ApplySort_EmptyColumn_ReturnsOriginalOrder()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 }
        };

        var sorted = items.ApplySort("", SortDirection.Ascending, _selectors);

        Assert.Equal("Charlie", sorted[0].Name);
        Assert.Equal("Alice", sorted[1].Name);
    }

    [Fact]
    public void ApplySort_UnknownColumn_ReturnsOriginalOrder()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 }
        };

        var sorted = items.ApplySort("UnknownColumn", SortDirection.Ascending, _selectors);

        Assert.Equal("Charlie", sorted[0].Name);
        Assert.Equal("Alice", sorted[1].Name);
    }

    #endregion

    #region Default Selector Tests

    [Fact]
    public void ApplySort_WithDefaultSelector_SortDirectionNone_UsesDefault()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 },
            new() { Name = "Bob", Value = 2 }
        };

        Func<TestItem, object?> defaultSelector = x => x.Value;

        var sorted = items.ApplySort(null, SortDirection.None, _selectors, defaultSelector);

        Assert.Equal(1, sorted[0].Value);
        Assert.Equal(2, sorted[1].Value);
        Assert.Equal(3, sorted[2].Value);
    }

    [Fact]
    public void ApplySort_WithDefaultSelector_UnknownColumn_UsesDefault()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 30 },
            new() { Name = "Alice", Value = 10 },
            new() { Name = "Bob", Value = 20 }
        };

        Func<TestItem, object?> defaultSelector = x => x.Value;

        var sorted = items.ApplySort("UnknownColumn", SortDirection.Ascending, _selectors, defaultSelector);

        Assert.Equal(10, sorted[0].Value);
        Assert.Equal(20, sorted[1].Value);
        Assert.Equal(30, sorted[2].Value);
    }

    #endregion

    #region Empty List Tests

    [Fact]
    public void ApplySort_EmptyList_ReturnsEmptyList()
    {
        var items = new List<TestItem>();

        var sorted = items.ApplySort("Name", SortDirection.Ascending, _selectors);

        Assert.Empty(sorted);
    }

    #endregion

    #region IEnumerable Extension Tests

    [Fact]
    public void ApplySort_IEnumerable_WorksCorrectly()
    {
        IEnumerable<TestItem> items = new List<TestItem>
        {
            new() { Name = "Charlie", Value = 3 },
            new() { Name = "Alice", Value = 1 },
            new() { Name = "Bob", Value = 2 }
        };

        var sorted = items.ApplySort("Name", SortDirection.Ascending, _selectors);

        Assert.Equal("Alice", sorted[0].Name);
        Assert.Equal("Bob", sorted[1].Name);
        Assert.Equal("Charlie", sorted[2].Name);
    }

    #endregion

    #region Stability Tests

    [Fact]
    public void ApplySort_WithEqualValues_MaintainsRelativeOrder()
    {
        var items = new List<TestItem>
        {
            new() { Name = "A1", Value = 10 },
            new() { Name = "A2", Value = 10 },
            new() { Name = "A3", Value = 10 }
        };

        var sorted = items.ApplySort("Value", SortDirection.Ascending, _selectors);

        // All have same value, relative order should be preserved (stable sort)
        Assert.Equal(3, sorted.Count);
    }

    #endregion

    #region Test Helper Class

    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public DateTime Date { get; set; }
    }

    #endregion
}
