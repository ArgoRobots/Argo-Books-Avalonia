using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class ColumnProfilerTests
{
    [Fact]
    public void Profile_NumericColumn_InfersNumberAndRange()
    {
        var headers = new List<string> { "Qty", "Price", "Total" };
        var rows = new List<List<string>>
        {
            new() { "2", "10", "20" },
            new() { "3", "10", "30" },
            new() { "1", "5",  "5"  },
        };
        var profiles = ColumnProfiler.Profile(headers, rows);
        Assert.Equal("number", profiles[0].InferredType);
        Assert.Equal("1", profiles[0].Min);
        Assert.Equal("3", profiles[0].Max);
    }

    [Fact]
    public void DetectRelationships_FindsProduct()
    {
        var headers = new List<string> { "Qty", "Price", "Total" };
        var rows = new List<List<string>>
        {
            new() { "2", "10", "20" },
            new() { "3", "10", "30" },
            new() { "4", "5",  "20" },
        };
        var rels = ColumnProfiler.DetectRelationships(headers, rows);
        Assert.Contains(rels, r => r.Description.Contains("Total") && r.Description.Contains("*"));
    }

    [Fact]
    public void DetectRelationships_FindsSum()
    {
        var headers = new List<string> { "A", "B", "Total" };
        var rows = new List<List<string>>
        {
            new() { "10", "5",  "15" },
            new() { "20", "3",  "23" },
            new() { "7",  "8",  "15" },
        };
        var rels = ColumnProfiler.DetectRelationships(headers, rows);
        Assert.Contains(rels, r => r.Description.Contains("Total") && r.Description.Contains("+"));
    }
}
