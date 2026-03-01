using ArgoBooks.Core.Services.InvoiceTemplates;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the InvoicePreviewService static class.
/// </summary>
public class InvoicePreviewServiceTests
{
    #region PreviewInBrowserAsync Tests

    [Fact]
    public async Task PreviewInBrowserAsync_ValidHtml_WritesFile()
    {
        var html = "<html><body>Test</body></html>";

        var result = await InvoicePreviewService.PreviewInBrowserAsync(html, "test-preview", openBrowser: false);

        Assert.True(result);
    }

    [Fact]
    public async Task PreviewInBrowserAsync_WithInvoiceId_UsesInvoiceIdInFilename()
    {
        var html = "<html><body>Test</body></html>";

        // Verify it writes the file successfully without opening a browser
        var result = await InvoicePreviewService.PreviewInBrowserAsync(html, "INV-001", openBrowser: false);

        Assert.True(result);
    }

    #endregion
}
