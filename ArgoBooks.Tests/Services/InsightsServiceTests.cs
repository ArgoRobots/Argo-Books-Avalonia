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

    #region FormatCurrency

    [Fact]
    public void FormatCurrency_IsCultureIndependent()
    {
        // FormatCurrency uses "C0", which formats with the OS-locale currency (e.g. euros and German
        // grouping on a German machine) instead of a stable, company-independent format. The result
        // must not depend on the machine locale.
        var method = typeof(InsightsService).GetMethod("FormatCurrency",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        string Run(string culture)
        {
            string result = null!;
            var thread = new System.Threading.Thread(() =>
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
                result = (string)method.Invoke(null, [1234m])!;
            });
            thread.Start();
            thread.Join();
            return result;
        }

        Assert.Equal(Run("en-US"), Run("de-DE"));
    }

    #endregion
}
