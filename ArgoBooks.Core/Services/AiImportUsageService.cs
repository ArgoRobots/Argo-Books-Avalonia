using System.Text;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for tracking and enforcing AI import usage limits via server-side API.
/// Communicates with the server API to track usage per license key.
/// </summary>
public class AiImportUsageService : IAiImportUsageService
{
    private const string UsageApiUrl = "https://argorobots.com/api/ai-import/usage.php";
    private const string ApiHostUrl = "https://argorobots.com";

    private readonly HttpClient _httpClient;
    private readonly LicenseService? _licenseService;
    private readonly IConnectivityService _connectivityService;
    private readonly IErrorLogger? _errorLogger;

    // Cache the last known usage to reduce API calls
    private AiImportUsageStatus? _cachedUsage;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Creates a new instance of the AiImportUsageService.
    /// </summary>
    public AiImportUsageService(LicenseService? licenseService = null, IErrorLogger? errorLogger = null)
        : this(licenseService, new HttpClient { Timeout = TimeSpan.FromSeconds(15) }, new ConnectivityService(), errorLogger)
    {
    }

    /// <summary>
    /// Creates a new instance with custom dependencies (for testing).
    /// </summary>
    public AiImportUsageService(LicenseService? licenseService, HttpClient httpClient, IConnectivityService connectivityService, IErrorLogger? errorLogger = null)
    {
        _licenseService = licenseService;
        _httpClient = httpClient;
        _connectivityService = connectivityService;
        _errorLogger = errorLogger;
    }

    /// <inheritdoc />
    public async Task<AiImportCheckResult> CheckUsageAsync(CancellationToken cancellationToken = default)
    {
        var licenseKey = _licenseService?.GetLicenseKey();
        if (string.IsNullOrEmpty(licenseKey))
        {
            return new AiImportCheckResult
            {
                CanImport = false,
                ErrorMessage = "No license key found. Please activate your license.",
                ImportCount = 0,
                MonthlyLimit = 0,
                Remaining = 0
            };
        }

        // Check cache first
        if (_cachedUsage != null && DateTime.UtcNow < _cacheExpiry)
        {
            return new AiImportCheckResult
            {
                CanImport = _cachedUsage.CanImport,
                ImportCount = _cachedUsage.ImportCount,
                MonthlyLimit = _cachedUsage.MonthlyLimit,
                Remaining = _cachedUsage.Remaining,
                Tier = _cachedUsage.Tier,
                ResetsAt = _cachedUsage.ResetsAt
            };
        }

        try
        {
            var response = await CallApiAsync("check", licenseKey, cancellationToken);

            if (response.Success)
            {
                // Update cache
                _cachedUsage = new AiImportUsageStatus
                {
                    CanImport = response.CanImport,
                    ImportCount = response.ImportCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining,
                    Tier = response.Tier,
                    ResetsAt = response.ResetsAt
                };
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                return new AiImportCheckResult
                {
                    CanImport = response.CanImport,
                    ImportCount = response.ImportCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining,
                    Tier = response.Tier,
                    ResetsAt = response.ResetsAt
                };
            }

            return new AiImportCheckResult
            {
                CanImport = false,
                ErrorMessage = response.Error ?? "Failed to check usage"
            };
        }
        catch (HttpRequestException)
        {
            // Network error - allow import if we have cached data showing capacity
            if (_cachedUsage != null && _cachedUsage.CanImport)
            {
                return new AiImportCheckResult
                {
                    CanImport = true,
                    ImportCount = _cachedUsage.ImportCount,
                    MonthlyLimit = _cachedUsage.MonthlyLimit,
                    Remaining = _cachedUsage.Remaining,
                    Tier = _cachedUsage.Tier,
                    ResetsAt = _cachedUsage.ResetsAt,
                    IsOffline = true
                };
            }

            var errorMessage = await GetConnectivityErrorMessageAsync(cancellationToken);
            return new AiImportCheckResult
            {
                CanImport = false,
                ErrorMessage = errorMessage
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            var errorMessage = await GetConnectivityErrorMessageAsync(cancellationToken);
            return new AiImportCheckResult
            {
                CanImport = false,
                ErrorMessage = errorMessage
            };
        }
        catch (TaskCanceledException)
        {
            return new AiImportCheckResult
            {
                CanImport = false,
                ErrorMessage = "Request was cancelled."
            };
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "AI import usage check failed");
            return new AiImportCheckResult
            {
                CanImport = false,
                ErrorMessage = $"Error checking usage: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<AiImportIncrementResult> IncrementUsageAsync(CancellationToken cancellationToken = default)
    {
        var licenseKey = _licenseService?.GetLicenseKey();
        if (string.IsNullOrEmpty(licenseKey))
        {
            return new AiImportIncrementResult
            {
                Success = false,
                ErrorMessage = "No license key found"
            };
        }

        try
        {
            var response = await CallApiAsync("increment", licenseKey, cancellationToken);

            if (response.Success)
            {
                // Update cache with new values
                _cachedUsage = new AiImportUsageStatus
                {
                    CanImport = response.Remaining > 0,
                    ImportCount = response.ImportCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining,
                    Tier = response.Tier,
                    ResetsAt = response.ResetsAt
                };
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                return new AiImportIncrementResult
                {
                    Success = true,
                    ImportCount = response.ImportCount,
                    MonthlyLimit = response.MonthlyLimit,
                    Remaining = response.Remaining
                };
            }

            return new AiImportIncrementResult
            {
                Success = false,
                ErrorMessage = response.Error ?? "Failed to record usage"
            };
        }
        catch (HttpRequestException)
        {
            // Network error - don't block the user
            return new AiImportIncrementResult
            {
                Success = true,
                IsOffline = true
            };
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "AI import usage increment failed");
            return new AiImportIncrementResult
            {
                Success = false,
                ErrorMessage = $"Error recording usage: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _cachedUsage = null;
        _cacheExpiry = DateTime.MinValue;
    }

    /// <inheritdoc />
    public AiImportUsageStatus? GetCachedUsage() => _cachedUsage;

    private async Task<string> GetConnectivityErrorMessageAsync(CancellationToken cancellationToken)
    {
        try
        {
            var hasInternet = await _connectivityService.IsInternetAvailableAsync(cancellationToken);

            if (!hasInternet)
            {
                return "No internet connection. Please check your network and try again.";
            }

            var isApiReachable = await _connectivityService.IsHostReachableAsync(ApiHostUrl, cancellationToken);

            if (!isApiReachable)
            {
                return "Unable to reach Argo Books servers. The service may be temporarily unavailable. Please try again later.";
            }

            return "Unable to verify usage. Please try again.";
        }
        catch
        {
            return "Unable to verify usage. Please check your internet connection.";
        }
    }

    private async Task<AiImportApiResponse> CallApiAsync(string action, string licenseKey, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            license_key = licenseKey,
            action
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(UsageApiUrl, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<AiImportApiResponse>(responseJson) ?? new AiImportApiResponse();
    }

    private class AiImportApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("can_import")]
        public bool CanImport { get; init; }

        [JsonPropertyName("import_count")]
        public int ImportCount { get; init; }

        [JsonPropertyName("monthly_limit")]
        public int MonthlyLimit { get; init; }

        [JsonPropertyName("remaining")]
        public int Remaining { get; init; }

        [JsonPropertyName("tier")]
        public string? Tier { get; init; }

        [JsonPropertyName("usage_month")]
        public string? UsageMonth { get; init; }

        [JsonPropertyName("resets_at")]
        public string? ResetsAt { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}

/// <summary>
/// Interface for AI import usage tracking service.
/// </summary>
public interface IAiImportUsageService
{
    /// <summary>
    /// Checks current usage and whether the user can perform more AI imports.
    /// </summary>
    Task<AiImportCheckResult> CheckUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the usage count after a successful import.
    /// </summary>
    Task<AiImportIncrementResult> IncrementUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached usage data.
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Gets the cached usage status (may be stale).
    /// </summary>
    AiImportUsageStatus? GetCachedUsage();
}

/// <summary>
/// Result of checking AI import usage status.
/// </summary>
public class AiImportCheckResult
{
    public bool CanImport { get; init; }
    public string? ErrorMessage { get; init; }
    public int ImportCount { get; init; }
    public int MonthlyLimit { get; init; }
    public int Remaining { get; init; }
    public string? Tier { get; init; }
    public string? ResetsAt { get; init; }
    public bool IsOffline { get; init; }
}

/// <summary>
/// Result of incrementing AI import usage.
/// </summary>
public class AiImportIncrementResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int ImportCount { get; init; }
    public int MonthlyLimit { get; init; }
    public int Remaining { get; init; }
    public bool IsOffline { get; init; }
}

/// <summary>
/// Cached AI import usage status.
/// </summary>
public class AiImportUsageStatus
{
    public bool CanImport { get; init; }
    public int ImportCount { get; init; }
    public int MonthlyLimit { get; init; }
    public int Remaining { get; init; }
    public string? Tier { get; init; }
    public string? ResetsAt { get; init; }
}
