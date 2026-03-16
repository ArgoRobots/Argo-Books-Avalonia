using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the AzureReceiptScannerService class.
/// </summary>
public class AzureReceiptScannerServiceTests
{
    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithoutLicenseService_ReturnsFalse()
    {
        var service = new AzureReceiptScannerService();

        Assert.False(service.IsConfigured);
    }

    #endregion

    #region ValidateConfiguration Tests

    [Fact]
    public async Task ValidateConfigurationAsync_WithoutLicenseService_ReturnsFalse()
    {
        var service = new AzureReceiptScannerService();

        var result = await service.ValidateConfigurationAsync();

        Assert.False(result);
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
