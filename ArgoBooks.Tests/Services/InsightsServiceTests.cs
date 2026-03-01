using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the InsightsService class.
/// </summary>
public class InsightsServiceTests
{
    #region GenerateInsightsAsync Tests

    [Fact]
    public async Task GenerateInsightsAsync_EmptyCompanyData_ReturnsInsufficientData()
    {
        var service = new InsightsService();
        var companyData = new ArgoBooks.Core.Data.CompanyData();
        var dateRange = new ArgoBooks.Core.Models.Insights.AnalysisDateRange
        {
            StartDate = DateTime.Now.AddMonths(-12),
            EndDate = DateTime.Now
        };

        var result = await service.GenerateInsightsAsync(companyData, dateRange);

        Assert.NotNull(result);
        Assert.False(result.HasSufficientData);
        Assert.NotNull(result.InsufficientDataMessage);
    }

    #endregion
}
