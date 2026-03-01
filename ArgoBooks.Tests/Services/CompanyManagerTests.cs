using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the CompanyManager class.
/// </summary>
public class CompanyManagerTests : IDisposable
{
    private readonly CompanyManager _manager;

    public CompanyManagerTests()
    {
        var platformService = new MockPlatformService();
        var footerService = new FooterService();
        var compressionService = new CompressionService();
        var fileService = new FileService(compressionService, footerService);
        var settingsService = new GlobalSettingsService(platformService);
        _manager = new CompanyManager(fileService, settingsService, footerService);
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    #region VerifyCurrentPassword Tests

    [Fact]
    public void VerifyCurrentPassword_WhenNoCompanyOpen_ReturnsFalse()
    {
        Assert.False(_manager.VerifyCurrentPassword("test"));
    }

    [Fact]
    public void VerifyCurrentPassword_WhenNoCompanyOpen_WithNull_ReturnsFalse()
    {
        Assert.False(_manager.VerifyCurrentPassword(null));
    }

    #endregion

    #region PendingRename Tests

    [Fact]
    public void ClearPendingRename_ClearsPath()
    {
        _manager.SetPendingRename("/new/path.argo");
        _manager.ClearPendingRename();

        Assert.Null(_manager.PendingRenamePath);
    }

    [Fact]
    public void SetPendingRename_OverwritesPrevious()
    {
        _manager.SetPendingRename("/first/path.argo");
        _manager.SetPendingRename("/second/path.argo");

        Assert.Equal("/second/path.argo", _manager.PendingRenamePath);
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_NullFileService_Throws()
    {
        var platformService = new MockPlatformService();
        var footerService = new FooterService();
        var settingsService = new GlobalSettingsService(platformService);

        Assert.Throws<ArgumentNullException>(() =>
            new CompanyManager(null!, settingsService, footerService));
    }

    [Fact]
    public void Constructor_NullSettingsService_Throws()
    {
        var footerService = new FooterService();
        var compressionService = new CompressionService();
        var fileService = new FileService(compressionService, footerService);

        Assert.Throws<ArgumentNullException>(() =>
            new CompanyManager(fileService, null!, footerService));
    }

    [Fact]
    public void Constructor_NullFooterService_Throws()
    {
        var platformService = new MockPlatformService();
        var compressionService = new CompressionService();
        var footerService = new FooterService();
        var fileService = new FileService(compressionService, footerService);
        var settingsService = new GlobalSettingsService(platformService);

        Assert.Throws<ArgumentNullException>(() =>
            new CompanyManager(fileService, settingsService, null!));
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
