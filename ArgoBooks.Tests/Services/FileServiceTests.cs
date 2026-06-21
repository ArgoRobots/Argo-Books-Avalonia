using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the FileService class.
/// </summary>
public class FileServiceTests
{
    private static FileService CreateService() =>
        new(new CompressionService(), new FooterService(), new EncryptionService());

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

    #region Encrypted Open Round-Trip Tests

    [Fact]
    public async Task EncryptedSaveThenOpen_CorrectPassword_RoundTripsCompanyData()
    {
        var service = CreateService();
        const string password = "Sup3r$ecret!";
        var filePath = Path.Combine(Path.GetTempPath(), $"argo-test-{Guid.NewGuid():N}.argo");
        string? tempForSave = null;
        string? tempForOpen = null;

        try
        {
            // Create a company, then save it encrypted.
            await service.CreateCompanyAsync(filePath, "Round Trip Co");
            tempForSave = await service.OpenCompanyAsync(filePath);
            await service.SaveCompanyAsync(filePath, tempForSave, password);

            Assert.True(await service.IsFileEncryptedAsync(filePath));

            // Open with the correct password (exercises DecryptWithVerificationAsync).
            tempForOpen = await service.OpenCompanyAsync(filePath, password);
            var data = await service.LoadCompanyDataAsync(tempForOpen);

            Assert.Equal("Round Trip Co", data.Settings.Company.Name);
        }
        finally
        {
            CleanupTemp(tempForSave);
            CleanupTemp(tempForOpen);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public async Task EncryptedOpen_WrongPassword_ThrowsUnauthorized()
    {
        var service = CreateService();
        const string password = "CorrectHorse1!";
        var filePath = Path.Combine(Path.GetTempPath(), $"argo-test-{Guid.NewGuid():N}.argo");
        string? tempForSave = null;

        try
        {
            await service.CreateCompanyAsync(filePath, "Locked Co");
            tempForSave = await service.OpenCompanyAsync(filePath);
            await service.SaveCompanyAsync(filePath, tempForSave, password);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await service.OpenCompanyAsync(filePath, "wrong-password"));
        }
        finally
        {
            CleanupTemp(tempForSave);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    private static void CleanupTemp(string? dir)
    {
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }

    #endregion
}
