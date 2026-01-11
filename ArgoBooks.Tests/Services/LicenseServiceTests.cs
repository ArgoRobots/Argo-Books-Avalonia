using ArgoBooks.Core.Models;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the LicenseService class.
/// </summary>
public class LicenseServiceTests
{
    private readonly MockEncryptionService _encryptionService;
    private readonly MockGlobalSettingsService _settingsService;
    private readonly MockPlatformService _platformService;
    private readonly LicenseService _licenseService;

    public LicenseServiceTests()
    {
        _encryptionService = new MockEncryptionService();
        _settingsService = new MockGlobalSettingsService();
        _platformService = new MockPlatformService();
        _licenseService = new LicenseService(_encryptionService, _settingsService, _platformService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullEncryptionService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LicenseService(null!, _settingsService, _platformService));
    }

    [Fact]
    public void Constructor_NullSettingsService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LicenseService(_encryptionService, null!, _platformService));
    }

    [Fact]
    public void Constructor_NullPlatformService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LicenseService(_encryptionService, _settingsService, null!));
    }

    #endregion

    #region SaveLicenseAsync Tests

    [Fact]
    public async Task SaveLicenseAsync_ValidLicense_SavesSuccessfully()
    {
        await _licenseService.SaveLicenseAsync(true, false, "TEST-LICENSE-KEY");

        Assert.NotNull(_settingsService.SavedSettings);
        Assert.NotNull(_settingsService.SavedSettings.License.LicenseData);
        Assert.NotNull(_settingsService.SavedSettings.License.Salt);
        Assert.NotNull(_settingsService.SavedSettings.License.Iv);
    }

    [Fact]
    public async Task SaveLicenseAsync_NullSettings_DoesNotThrow()
    {
        _settingsService.ReturnNullSettings = true;

        await _licenseService.SaveLicenseAsync(true, false, "TEST-KEY");

        // Should not throw and should not save
        Assert.Null(_settingsService.SavedSettings);
    }

    [Fact]
    public async Task SaveLicenseAsync_SetsLastValidationDate()
    {
        var beforeSave = DateTime.UtcNow;

        await _licenseService.SaveLicenseAsync(true, true, "TEST-KEY");

        var afterSave = DateTime.UtcNow;
        Assert.NotNull(_settingsService.SavedSettings?.License.LastValidationDate);
        Assert.True(_settingsService.SavedSettings.License.LastValidationDate >= beforeSave);
        Assert.True(_settingsService.SavedSettings.License.LastValidationDate <= afterSave);
    }

    [Fact]
    public async Task SaveLicenseAsync_StandardLicense_SavesCorrectly()
    {
        await _licenseService.SaveLicenseAsync(true, false, "STANDARD-KEY");

        var loaded = _licenseService.LoadLicense();

        Assert.True(loaded.HasStandard);
        Assert.False(loaded.HasPremium);
    }

    [Fact]
    public async Task SaveLicenseAsync_PremiumLicense_SavesCorrectly()
    {
        await _licenseService.SaveLicenseAsync(false, true, "PREMIUM-KEY");

        var loaded = _licenseService.LoadLicense();

        Assert.False(loaded.HasStandard);
        Assert.True(loaded.HasPremium);
    }

    [Fact]
    public async Task SaveLicenseAsync_BothLicenses_SavesCorrectly()
    {
        await _licenseService.SaveLicenseAsync(true, true, "BOTH-KEY");

        var loaded = _licenseService.LoadLicense();

        Assert.True(loaded.HasStandard);
        Assert.True(loaded.HasPremium);
    }

    [Fact]
    public async Task SaveLicenseAsync_NoLicense_SavesCorrectly()
    {
        await _licenseService.SaveLicenseAsync(false, false, null);

        var loaded = _licenseService.LoadLicense();

        Assert.False(loaded.HasStandard);
        Assert.False(loaded.HasPremium);
    }

    #endregion

    #region LoadLicense Tests

    [Fact]
    public void LoadLicense_NoSavedLicense_ReturnsFalseFalse()
    {
        _settingsService.ClearLicenseData();

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    [Fact]
    public void LoadLicense_NullSettings_ReturnsFalseFalse()
    {
        _settingsService.ReturnNullSettings = true;

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    [Fact]
    public void LoadLicense_NullLicenseData_ReturnsFalseFalse()
    {
        _settingsService.GetSettings()!.License.LicenseData = null;

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    [Fact]
    public void LoadLicense_NullSalt_ReturnsFalseFalse()
    {
        _settingsService.GetSettings()!.License.Salt = null;

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    [Fact]
    public void LoadLicense_NullIv_ReturnsFalseFalse()
    {
        _settingsService.GetSettings()!.License.Iv = null;

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    [Fact]
    public async Task LoadLicense_AfterSave_ReturnsCorrectValues()
    {
        await _licenseService.SaveLicenseAsync(true, true, "TEST-KEY");

        var result = _licenseService.LoadLicense();

        Assert.True(result.HasStandard);
        Assert.True(result.HasPremium);
    }

    [Fact]
    public void LoadLicense_InvalidEncryptedData_ReturnsFalseFalse()
    {
        _encryptionService.ThrowOnDecrypt = true;
        var settings = _settingsService.GetSettings()!;
        settings.License.LicenseData = "invalid-base64-data==";
        settings.License.Salt = "test-salt";
        settings.License.Iv = "test-iv";

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    #endregion

    #region ClearLicenseAsync Tests

    [Fact]
    public async Task ClearLicenseAsync_ClearsLicenseData()
    {
        await _licenseService.SaveLicenseAsync(true, true, "TEST-KEY");
        await _licenseService.ClearLicenseAsync();

        Assert.Null(_settingsService.SavedSettings?.License.LicenseData);
    }

    [Fact]
    public async Task ClearLicenseAsync_NullSettings_DoesNotThrow()
    {
        _settingsService.ReturnNullSettings = true;

        await _licenseService.ClearLicenseAsync();

        // Should not throw
    }

    [Fact]
    public async Task ClearLicenseAsync_AfterClear_LoadReturnsNoLicense()
    {
        await _licenseService.SaveLicenseAsync(true, true, "TEST-KEY");
        await _licenseService.ClearLicenseAsync();

        var result = _licenseService.LoadLicense();

        Assert.False(result.HasStandard);
        Assert.False(result.HasPremium);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task SaveAndLoad_RoundTrip_PreservesValues(bool hasStandard, bool hasPremium)
    {
        await _licenseService.SaveLicenseAsync(hasStandard, hasPremium, "LICENSE-KEY");

        var result = _licenseService.LoadLicense();

        Assert.Equal(hasStandard, result.HasStandard);
        Assert.Equal(hasPremium, result.HasPremium);
    }

    #endregion

    #region Mock Classes

    private class MockEncryptionService : IEncryptionService
    {
        private readonly Dictionary<string, byte[]> _encryptedData = new();
        public bool ThrowOnDecrypt { get; set; }

        public string GenerateSalt() => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        public string GenerateIv() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()[..12]);

        public string HashPassword(string password, string salt) =>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + salt));

        public bool ValidatePassword(string password, string storedHash, string salt) =>
            HashPassword(password, salt) == storedHash;

        public byte[] Encrypt(byte[] data, string password, string salt, string iv)
        {
            // Simple mock: just store the data and return a marker
            var key = $"{salt}:{iv}";
            _encryptedData[key] = data;
            return System.Text.Encoding.UTF8.GetBytes(key);
        }

        public byte[] Decrypt(byte[] encryptedData, string password, string salt, string iv)
        {
            if (ThrowOnDecrypt)
                throw new System.Security.Cryptography.CryptographicException("Mock decryption failure");

            var key = $"{salt}:{iv}";
            if (_encryptedData.TryGetValue(key, out var data))
                return data;

            throw new System.Security.Cryptography.CryptographicException("Data not found");
        }

        public Task<MemoryStream> EncryptAsync(Stream inputStream, string password, string salt, string iv)
        {
            using var ms = new MemoryStream();
            inputStream.CopyTo(ms);
            var encrypted = Encrypt(ms.ToArray(), password, salt, iv);
            return Task.FromResult(new MemoryStream(encrypted));
        }

        public Task<MemoryStream> DecryptAsync(Stream encryptedStream, string password, string salt, string iv)
        {
            using var ms = new MemoryStream();
            encryptedStream.CopyTo(ms);
            var decrypted = Decrypt(ms.ToArray(), password, salt, iv);
            return Task.FromResult(new MemoryStream(decrypted));
        }

        public bool IsPasswordValid(string password) => password.Length >= 8;
        public string? GetPasswordValidationError(string password) =>
            password.Length >= 8 ? null : "Password must be at least 8 characters";
    }

    private class MockGlobalSettingsService : IGlobalSettingsService
    {
        private GlobalSettings _settings = new();
        public GlobalSettings? SavedSettings { get; private set; }
        public bool ReturnNullSettings { get; set; }

        public GlobalSettings? GetSettings() => ReturnNullSettings ? null : _settings;

        public void SaveSettings(GlobalSettings settings)
        {
            SavedSettings = settings;
            _settings = settings;
        }

        public Task<GlobalSettings> LoadAsync() => Task.FromResult(_settings);

        public Task SaveAsync(GlobalSettings settings)
        {
            SaveSettings(settings);
            return Task.CompletedTask;
        }

        public IReadOnlyList<string> GetRecentCompanies() => _settings.RecentCompanies.AsReadOnly();
        public void AddRecentCompany(string filePath) => _settings.RecentCompanies.Add(filePath);
        public void RemoveRecentCompany(string filePath) => _settings.RecentCompanies.Remove(filePath);

        public void ClearLicenseData()
        {
            _settings.License = new LicenseSettings();
        }
    }

    private class MockPlatformService : IPlatformService
    {
        public PlatformType Platform => PlatformType.Windows;
        public bool SupportsFileSystem => true;
        public bool SupportsNativeDialogs => true;
        public bool SupportsBiometrics => false;
        public bool SupportsAutoUpdate => true;
        public int MaxRecentCompanies => 10;

        public string GetAppDataPath() => "/mock/appdata";
        public string GetTempPath() => "/mock/temp";
        public string GetDefaultDocumentsPath() => "/mock/documents";
        public string GetLogsPath() => "/mock/logs";
        public string GetCachePath() => "/mock/cache";
        public string NormalizePath(string path) => path;
        public string CombinePaths(params string[] paths) => string.Join("/", paths);
        public void EnsureDirectoryExists(string path) { }
        public Task<bool> AuthenticateWithBiometricAsync(string reason) => Task.FromResult(false);
        public void ClearPasswordForBiometric(string key) { }
        public string? GetPasswordForBiometric(string key) => null;
        public Task<bool> IsBiometricAvailableAsync() => Task.FromResult(false);
        public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;
        public void StorePasswordForBiometric(string key, string password) { }
        
        /// <summary>
        /// Returns a stable mock machine ID for testing.
        /// </summary>
        public string GetMachineId() => "MOCK-MACHINE-ID-12345";

        /// <summary>
        /// Mock implementation that does nothing.
        /// </summary>
        public void RegisterFileTypeAssociations(string iconPath) { }
    }

    #endregion
}
