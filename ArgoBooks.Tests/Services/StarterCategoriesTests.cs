using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Starter categories are written with the same ID counter the category form uses, so a
/// category made by hand afterwards has to land on a fresh ID.
/// </summary>
public class StarterCategoriesTests
{
    [Theory]
    [InlineData("Construction")]
    [InlineData("Services")]
    [InlineData(null)]
    [InlineData("Something new")]
    public void AddTo_GivesBothSidesAndLeavesTheCounterPastEveryId(string? industry)
    {
        var data = new CompanyData();

        StarterCategories.AddTo(data, industry);

        Assert.Contains(data.Categories, c => c.Type == CategoryType.Revenue);
        Assert.Contains(data.Categories, c => c.Type == CategoryType.Expense);
        Assert.Equal(data.Categories.Count, data.Categories.Select(c => c.Id).Distinct().Count());
        Assert.Equal(data.Categories.Count, data.IdCounters.Category);
    }
}
