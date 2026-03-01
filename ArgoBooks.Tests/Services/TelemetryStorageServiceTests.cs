using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the TelemetryStorageService class.
/// </summary>
public class TelemetryStorageServiceTests
{
    #region GetPendingEventsAsync Tests

    [Fact]
    public async Task GetPendingEventsAsync_NewService_ReturnsEmptyList()
    {
        var platformService = new MockPlatformService();
        var service = new TelemetryStorageService(platformService);

        var events = await service.GetPendingEventsAsync();

        Assert.NotNull(events);
        Assert.Empty(events);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_NewService_ReturnsStatistics()
    {
        var platformService = new MockPlatformService();
        var service = new TelemetryStorageService(platformService);

        var stats = await service.GetStatisticsAsync();

        Assert.NotNull(stats);
    }

    #endregion

    #region ClearAllDataAsync Tests

    [Fact]
    public async Task ClearAllDataAsync_EmptyService_DoesNotThrow()
    {
        var platformService = new MockPlatformService();
        var service = new TelemetryStorageService(platformService);

        await service.ClearAllDataAsync();

        var events = await service.GetPendingEventsAsync();
        Assert.Empty(events);
    }

    #endregion

    #region ExportToJsonAsync Tests

    [Fact]
    public async Task ExportToJsonAsync_EmptyService_ReturnsValidJson()
    {
        var platformService = new MockPlatformService();
        var service = new TelemetryStorageService(platformService);

        var json = await service.ExportToJsonAsync();

        Assert.NotNull(json);
        Assert.False(string.IsNullOrEmpty(json));
    }

    #endregion

    #region Mock Classes

    private class MockPlatformService : IPlatformService
    {
        public PlatformType Platform => PlatformType.Linux;
        public string GetAppDataPath() => Path.Combine(Path.GetTempPath(), "ArgoBooks_Test_" + Guid.NewGuid().ToString("N")[..8]);
        public string GetTempPath() => Path.GetTempPath();
        public string GetDefaultDocumentsPath() => Path.GetTempPath();
        public string GetLogsPath() => Path.GetTempPath();
        public string GetCachePath() => Path.GetTempPath();
        public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);
        public bool SupportsFileSystem => true;
        public bool SupportsNativeDialogs => false;
        public bool SupportsBiometrics => false;
        public Task<bool> IsBiometricAvailableAsync() => Task.FromResult(false);
        public Task<bool> AuthenticateWithBiometricAsync(string reason) => Task.FromResult(false);
        public void StorePasswordForBiometric(string fileId, string password) { }
        public string? GetPasswordForBiometric(string fileId) => null;
        public void ClearPasswordForBiometric(string fileId) { }
        public bool SupportsAutoUpdate => false;
        public int MaxRecentCompanies => 10;
        public string NormalizePath(string path) => path;
        public string CombinePaths(params string[] paths) => Path.Combine(paths);
        public string GetMachineId() => "test-machine-id";
        public void RegisterFileTypeAssociations(string iconPath) { }
        public StringComparer PathComparer => StringComparer.Ordinal;
    }

    #endregion
}
