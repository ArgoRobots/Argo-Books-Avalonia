using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ReceiptUsageService class.
/// </summary>
public class ReceiptUsageServiceTests
{
    #region GetCachedUsage Tests

    [Fact]
    public void GetCachedUsage_NoCachedData_ReturnsNull()
    {
        var service = new ReceiptUsageService();

        var result = service.GetCachedUsage();

        Assert.Null(result);
    }

    [Fact]
    public void GetCachedUsage_AfterInvalidate_ReturnsNull()
    {
        var service = new ReceiptUsageService();
        service.InvalidateCache();

        var result = service.GetCachedUsage();

        Assert.Null(result);
    }

    #endregion

    #region CheckUsageAsync Tests

    [Fact]
    public async Task CheckUsageAsync_NoLicenseKey_ReturnsCannotScan()
    {
        var service = new ReceiptUsageService();

        var result = await service.CheckUsageAsync();

        Assert.False(result.CanScan);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion
}
