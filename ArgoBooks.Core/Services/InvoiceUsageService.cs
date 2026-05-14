using System.Text;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for tracking and enforcing invoice send usage limits via server-side API.
/// Free-tier users get a configurable monthly limit; premium users are unlimited.
/// </summary>
public class InvoiceUsageService : IDisposable
{
    private static readonly string UsageApiUrl = $"{ApiConfig.BaseUrl}/api/invoice/usage.php";
    // Must match the server's free-tier default (config/pricing.php
    // FREE_INVOICE_MONTHLY_LIMIT). Used only as a fallback when the
    // server check fails or hasn't completed yet.
    private const int DefaultFreeLimit = 25;

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly LicenseService? _licenseService;
    private readonly IErrorLogger? _errorLogger;
    private bool _disposed;

    // Cache the last known usage to reduce API calls
    private InvoiceUsageStatus? _cachedUsage;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public InvoiceUsageService(LicenseService? licenseService = null, IErrorLogger? errorLogger = null)
        : this(licenseService, new HttpClient { Timeout = TimeSpan.FromSeconds(15) }, errorLogger)
    {
        _ownsHttpClient = true;
    }

    public InvoiceUsageService(LicenseService? licenseService, HttpClient httpClient, IErrorLogger? errorLogger = null)
    {
        _licenseService = licenseService;
        _httpClient = httpClient;
        _errorLogger = errorLogger;
    }

    /// <summary>
    /// Gets the cached monthly limit, or the default if not yet fetched.
    /// </summary>
    public int MonthlyLimit => _cachedUsage?.MonthlyLimit ?? DefaultFreeLimit;

    /// <summary>
    /// Checks current invoice send usage with the server.
    /// </summary>
    public async Task<InvoiceUsageResult> CheckUsageAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cachedUsage != null && DateTime.UtcNow < _cacheExpiry)
        {
            return new InvoiceUsageResult
            {
                Success = true,
                CanSend = _cachedUsage.CanSend,
                SendCount = _cachedUsage.SendCount,
                MonthlyLimit = _cachedUsage.MonthlyLimit,
                Remaining = _cachedUsage.Remaining,
                Tier = _cachedUsage.Tier
            };
        }

        try
        {
            var response = await CallApiAsync("check", cancellationToken);

            if (response.Success)
            {
                _cachedUsage = new InvoiceUsageStatus
                {
                    CanSend = response.CanSend,
                    SendCount = response.SendCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining,
                    Tier = response.Tier
                };
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                return new InvoiceUsageResult
                {
                    Success = true,
                    CanSend = response.CanSend,
                    SendCount = response.SendCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining,
                    Tier = response.Tier
                };
            }

            // Fall back to cached data only if it's still within the expiry window —
            // otherwise a stale cache from a prior month could permit sends past quota.
            var hasFreshCache = _cachedUsage != null && DateTime.UtcNow < _cacheExpiry;
            return new InvoiceUsageResult
            {
                Success = false,
                ErrorMessage = response.Error ?? "Failed to check usage",
                CanSend = hasFreshCache && _cachedUsage!.CanSend,
                MonthlyLimit = hasFreshCache ? _cachedUsage!.MonthlyLimit : DefaultFreeLimit
            };
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "Invoice usage check failed");

            // Network error — allow sending if cache is fresh and shows capacity.
            // Without the expiry check a stale cache could permit sends past the server-side quota.
            var hasFreshCache = _cachedUsage != null && DateTime.UtcNow < _cacheExpiry;
            return new InvoiceUsageResult
            {
                Success = false,
                CanSend = hasFreshCache && _cachedUsage!.CanSend,
                SendCount = hasFreshCache ? _cachedUsage!.SendCount : 0,
                MonthlyLimit = hasFreshCache ? _cachedUsage!.MonthlyLimit : DefaultFreeLimit,
                Remaining = hasFreshCache ? _cachedUsage!.Remaining : DefaultFreeLimit,
                ErrorMessage = "Unable to verify usage."
            };
        }
    }

    /// <summary>
    /// Increments the send count after a successful invoice send.
    /// </summary>
    public async Task<InvoiceUsageResult> IncrementUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CallApiAsync("increment", cancellationToken);

            if (response.Success)
            {
                _cachedUsage = new InvoiceUsageStatus
                {
                    CanSend = response.CanSend,
                    SendCount = response.SendCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining,
                    Tier = response.Tier
                };
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            }

            return new InvoiceUsageResult
            {
                Success = response.Success,
                CanSend = response.CanSend,
                SendCount = response.SendCount,
                MonthlyLimit = response.MonthlyLimit,
                Remaining = response.Remaining,
                Tier = response.Tier,
                ErrorMessage = response.Error
            };
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "Invoice usage increment failed");
            return new InvoiceUsageResult
            {
                Success = false,
                ErrorMessage = "Unable to record usage."
            };
        }
    }

    /// <summary>
    /// Invalidates the cached usage data so the next check fetches fresh data.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedUsage = null;
        _cacheExpiry = DateTime.MinValue;
    }

    private async Task<InvoiceUsageApiResponse> CallApiAsync(string action, CancellationToken cancellationToken)
    {
        var licenseKey = _licenseService?.GetLicenseKey() ?? "";
        var deviceId = _licenseService?.GetDeviceId() ?? "";

        var requestBody = new
        {
            license_key = licenseKey,
            device_id = deviceId,
            action
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(UsageApiUrl, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<InvoiceUsageApiResponse>(responseJson) ?? new InvoiceUsageApiResponse();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _ownsHttpClient)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }

    private class InvoiceUsageApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("can_send")]
        public bool CanSend { get; init; }

        [JsonPropertyName("send_count")]
        public int SendCount { get; init; }

        [JsonPropertyName("monthly_limit")]
        public int MonthlyLimit { get; init; }

        [JsonPropertyName("remaining")]
        public int Remaining { get; init; }

        [JsonPropertyName("tier")]
        public string? Tier { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private class InvoiceUsageStatus
    {
        public bool CanSend { get; init; }
        public int SendCount { get; init; }
        public int MonthlyLimit { get; init; }
        public int Remaining { get; init; }
        public string? Tier { get; init; }
    }
}

/// <summary>
/// Result of checking or incrementing invoice send usage.
/// </summary>
public class InvoiceUsageResult
{
    public bool Success { get; init; }
    public bool CanSend { get; init; }
    public int SendCount { get; init; }
    public int MonthlyLimit { get; init; }
    public int Remaining { get; init; }
    public string? Tier { get; init; }
    public string? ErrorMessage { get; init; }
}
