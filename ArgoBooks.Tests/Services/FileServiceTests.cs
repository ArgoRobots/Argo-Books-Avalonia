using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the FileService class.
/// </summary>
public class FileServiceTests
{
    #region IsFileEncryptedAsync Tests

    [Fact]
    public async Task IsFileEncryptedAsync_NonExistentFile_ThrowsOrReturnsFalse()
    {
        var compressionService = new CompressionService();
        var footerService = new FooterService();
        var service = new FileService(compressionService, footerService);

        // Non-existent file should throw or handle gracefully
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.IsFileEncryptedAsync("/nonexistent/file.argo"));
    }

    #endregion
}
