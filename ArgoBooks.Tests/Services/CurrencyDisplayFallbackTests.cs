using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Regression: an imported foreign-currency row dated in the past (e.g. £1,200 on 2026-01-05)
/// must still convert to the company currency for display. The importer only seeds *today's*
/// rate, and the display path looked up the rate by the transaction's exact date with a
/// cache-only call and no fallback. That exact-date lookup missed, so the converter silently
/// returned the raw USD amount, e.g. showing the USD value "$1,611.40" labelled as CAD instead
/// of the real ~$2,255.96 CAD. The display converter must mirror the importer and fall back to
/// the nearest-known cached rate (<see cref="ExchangeRateService.GetLatestCachedRate"/>).
/// </summary>
public class CurrencyDisplayFallbackTests
{
    [Fact]
    public async Task ConvertFromUSD_ExactDateUncached_FallsBackToLatestCachedRate()
    {
        // Only "today's" USD->CAD rate is cached (as the import flow seeds it).
        var seededDate = new DateTime(2026, 6, 16);
        var service = new ExchangeRateService(new MockPlatformService(), new HttpClient(new StubCadHandler(1.40m)));
        await service.GetExchangeRateAsync("USD", "CAD", seededDate);

        // A historical transaction whose exact-date rate was never cached.
        var txnDate = new DateTime(2026, 1, 5);
        Assert.Equal(-1m, service.GetExchangeRate("USD", "CAD", txnDate)); // precondition: exact-date miss

        // £1,200 stored as 1,611.40 USD should display as ~2,255.96 CAD, not the raw USD amount.
        var result = service.ConvertFromUSD(1611.40m, "CAD", txnDate);

        Assert.Equal(Math.Round(1611.40m * 1.40m, 2), result);
        Assert.NotEqual(1611.40m, result);
    }

    private sealed class StubCadHandler(decimal usdToCad) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = $$"""{ "success": true, "base": "USD", "rates": { "CAD": {{usdToCad}} } }""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class MockPlatformService : IPlatformService
    {
        public PlatformType Platform => PlatformType.Linux;
        public string GetAppDataPath() => Path.GetTempPath();
        public string GetTempPath() => Path.GetTempPath();
        public string GetDefaultDocumentsPath() => Path.GetTempPath();
        public string GetLogsPath() => Path.GetTempPath();
        public string GetCachePath() => Path.GetTempPath();
        public void EnsureDirectoryExists(string path) { }
        public bool SupportsFileSystem => false; // skip disk persistence in tests
        public bool SupportsNativeDialogs => false;
        public bool SupportsBiometrics => false;
        public Task<bool> IsBiometricAvailableAsync() => Task.FromResult(false);
        public Task<string> GetBiometricAvailabilityDetailsAsync() => Task.FromResult("Not supported");
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
}
