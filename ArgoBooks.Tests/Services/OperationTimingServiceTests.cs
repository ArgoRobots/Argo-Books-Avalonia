using System.Net;
using System.Text;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for <see cref="OperationTimingService"/>: parsing the server priors payload (which
/// catches any property-name drift between this client and the PHP endpoint), the seed-priors
/// fallback on failure, and disk persistence of the learned calibration.
/// </summary>
public class OperationTimingServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly MockPlatformService _platform;

    public OperationTimingServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"OpTimingTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        _platform = new MockPlatformService(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* ignore */ }
    }

    private OperationTimingService Build(HttpStatusCode status, string body)
        => new(_platform, new HttpClient(new StubHandler(status, body)));

    [Fact]
    public async Task RefreshPriorsAsync_ValidPayload_AppliesFetchedPriors()
    {
        // The exact shape /api/ai/timing-priors.php returns. A property-name mismatch here
        // would surface as the estimate ignoring the fetched p50 (staying on the seed).
        var service = Build(HttpStatusCode.OK,
            "{\"success\":true,\"model\":\"gemini-2.5-flash\",\"window_days\":14,\"load_factor\":1.0," +
            "\"priors\":[{\"operation\":\"receipt_scan\",\"p50_ms\":3000,\"p90_ms\":7000," +
            "\"sample_count\":50,\"avg_size_feature\":1000000,\"avg_output_tokens\":600,\"per_page_ms\":null}]}");

        await service.RefreshPriorsAsync();

        var estimate = service.Estimate(OperationKind.ReceiptScan);
        Assert.InRange(estimate.ComputeMs, 2900, 3100);
        Assert.InRange(estimate.P90Ms, 6900, 7100);
    }

    [Fact]
    public async Task RefreshPriorsAsync_LoadFactor_ScalesEstimate()
    {
        var service = Build(HttpStatusCode.OK,
            "{\"success\":true,\"model\":\"m\",\"load_factor\":2.0," +
            "\"priors\":[{\"operation\":\"receipt_scan\",\"p50_ms\":3000,\"p90_ms\":7000}]}");

        await service.RefreshPriorsAsync();

        // p50 3000 * load factor 2.0 = 6000.
        Assert.InRange(service.Estimate(OperationKind.ReceiptScan).ComputeMs, 5900, 6100);
    }

    [Fact]
    public async Task RefreshPriorsAsync_NonSuccess_FallsBackToSeed()
    {
        var service = Build(HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}");

        await service.RefreshPriorsAsync();

        // Seed receipt p50 is 9000; the failed fetch must leave the estimator usable.
        Assert.InRange(service.Estimate(OperationKind.ReceiptScan).ComputeMs, 8900, 9100);
    }

    [Fact]
    public async Task RecordResult_PersistsCalibration_AcrossInstances()
    {
        var first = new OperationTimingService(_platform, new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")));
        // Seed receipt p50 is 9000; consistently ~18000 trains calibration toward ~2x.
        for (int i = 0; i < 30; i++)
            first.RecordResult(OperationKind.ReceiptScan, serverComputeMs: 18000, totalWallClockMs: 18500);
        Assert.True(first.Estimator.UserCalibration > 1.5);

        // A fresh instance over the same app-data dir loads the saved calibration on init.
        var second = new OperationTimingService(_platform, new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")));
        await second.InitializeAsync();
        Assert.InRange(second.Estimator.UserCalibration, first.Estimator.UserCalibration - 0.01, first.Estimator.UserCalibration + 0.01);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class MockPlatformService(string appDataPath) : IPlatformService
    {
        public PlatformType Platform => PlatformType.Linux;
        public string GetAppDataPath() => appDataPath;
        public string GetTempPath() => Path.Combine(appDataPath, "temp");
        public string GetDefaultDocumentsPath() => Path.Combine(appDataPath, "docs");
        public string GetLogsPath() => Path.Combine(appDataPath, "logs");
        public string GetCachePath() => Path.Combine(appDataPath, "cache");
        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        public bool SupportsFileSystem => true;
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
