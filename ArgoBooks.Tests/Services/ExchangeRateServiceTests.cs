using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ExchangeRateService class.
/// </summary>
public class ExchangeRateServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNoApiKey_HasApiKeyIsFalse()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService("", new MockPlatformService(), httpClient);

        Assert.False(service.HasApiKey);
    }

    [Fact]
    public void Constructor_WithApiKey_HasApiKeyIsTrue()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService("test-key", new MockPlatformService(), httpClient);

        Assert.True(service.HasApiKey);
    }

    #endregion

    #region GetExchangeRate Sync Tests

    [Fact]
    public void GetExchangeRate_SameCurrency_ReturnsOne()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService(null, new MockPlatformService(), httpClient);

        var rate = service.GetExchangeRate("USD", "USD", DateTime.Today);

        Assert.Equal(1m, rate);
    }

    [Fact]
    public void GetExchangeRate_SameCurrencyCaseInsensitive_ReturnsOne()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService(null, new MockPlatformService(), httpClient);

        var rate = service.GetExchangeRate("usd", "USD", DateTime.Today);

        Assert.Equal(1m, rate);
    }

    [Fact]
    public void GetExchangeRate_UncachedRate_ReturnsNegativeOne()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService(null, new MockPlatformService(), httpClient);

        var rate = service.GetExchangeRate("USD", "EUR", DateTime.Today);

        Assert.Equal(-1m, rate);
    }

    #endregion

    #region GetExchangeRateAsync Tests

    [Fact]
    public async Task GetExchangeRateAsync_SameCurrency_ReturnsOne()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService(null, new MockPlatformService(), httpClient);

        var rate = await service.GetExchangeRateAsync("USD", "USD", DateTime.Today);

        Assert.Equal(1m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_NoApiKey_ReturnsNegativeOne()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService(null, new MockPlatformService(), httpClient);

        var rate = await service.GetExchangeRateAsync("USD", "EUR", DateTime.Today, false);

        Assert.Equal(-1m, rate);
    }

    #endregion

    #region ConvertAsync Tests

    [Fact]
    public async Task ConvertAsync_SameCurrency_ReturnsSameAmount()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService(null, new MockPlatformService(), httpClient);

        var result = await service.ConvertAsync(100m, "USD", "USD", DateTime.Today);

        Assert.Equal(100m, result);
    }

    [Fact]
    public async Task ConvertAsync_UnavailableRate_ReturnsOriginalAmount()
    {
        var httpClient = new HttpClient();
        var service = new ExchangeRateService("", new MockPlatformService(), httpClient);

        var result = await service.ConvertAsync(100m, "USD", "EUR", DateTime.Today);

        Assert.Equal(100m, result);
    }

    #endregion

    #region Mock Classes

    private class MockPlatformService : IPlatformService
    {
        public PlatformType Platform => PlatformType.Linux;
        public string GetAppDataPath() => Path.GetTempPath();
        public string GetTempPath() => Path.GetTempPath();
        public string GetDefaultDocumentsPath() => Path.GetTempPath();
        public string GetLogsPath() => Path.GetTempPath();
        public string GetCachePath() => Path.GetTempPath();
        public void EnsureDirectoryExists(string path) { }
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
