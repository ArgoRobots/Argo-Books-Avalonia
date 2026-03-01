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
    public void IsConfigured_WithoutCredentials_ReturnsFalse()
    {
        // Remove the keys from both the in-memory cache and environment
        // so the service sees no credentials.
        DotEnv.Unset("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT");
        DotEnv.Unset("AZURE_DOCUMENT_INTELLIGENCE_API_KEY");

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
    public async Task ValidateConfigurationAsync_WithoutCredentials_ReturnsFalse()
    {
        // Remove the keys from both the in-memory cache and environment.
        DotEnv.Unset("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT");
        DotEnv.Unset("AZURE_DOCUMENT_INTELLIGENCE_API_KEY");

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
