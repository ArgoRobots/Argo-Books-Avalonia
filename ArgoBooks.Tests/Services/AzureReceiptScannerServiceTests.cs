using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the AzureReceiptScannerService class.
/// </summary>
[Collection("DotEnv")]
public class AzureReceiptScannerServiceTests
{
    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithoutPortalKey_ReturnsFalse()
    {
        // Remove the portal API key so IsConfigured returns false
        DotEnv.Unset("PAYMENT_PORTAL_API_KEY");

        try
        {
            var service = new AzureReceiptScannerService();

            Assert.False(service.IsConfigured);
        }
        finally
        {
            // Reload from .env to restore original state.
            DotEnv.Reload();
        }
    }

    #endregion

    #region ValidateConfiguration Tests

    [Fact]
    public async Task ValidateConfigurationAsync_WithoutPortalKey_ReturnsFalse()
    {
        // Remove the portal API key so IsConfigured returns false
        DotEnv.Unset("PAYMENT_PORTAL_API_KEY");

        try
        {
            var service = new AzureReceiptScannerService();

            var result = await service.ValidateConfigurationAsync();

            Assert.False(result);
        }
        finally
        {
            DotEnv.Reload();
        }
    }

    #endregion

    #region ScanReceiptFromFile Tests

    [Fact]
    public async Task ScanReceiptFromFileAsync_FileNotFound_ReturnsFailedResult()
    {
        var service = new AzureReceiptScannerService();

        var result = await service.ScanReceiptFromFileAsync("/nonexistent/file.jpg");

        Assert.False(result.IsSuccess);
    }

    #endregion
}
