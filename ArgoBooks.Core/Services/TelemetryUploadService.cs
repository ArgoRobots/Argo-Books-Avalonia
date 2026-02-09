using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for uploading telemetry data to the server.
/// </summary>
public class TelemetryUploadService : ITelemetryUploadService
{
    private const string UploadUrl = "https://argorobots.com/api/data/upload.php";
    private const string UserAgentPrefix = "ArgoSalesTracker";
    private const int MaxRetries = 3;
    private const int BatchSize = 500;

    private readonly HttpClient _httpClient;
    private readonly ITelemetryStorageService _storageService;
    private readonly IErrorLogger? _errorLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _appVersion;

    /// <summary>
    /// Initializes a new instance of the TelemetryUploadService.
    /// </summary>
    public TelemetryUploadService(
        ITelemetryStorageService storageService,
        HttpClient? httpClient = null,
        IErrorLogger? errorLogger = null,
        string? appVersion = null)
    {
        _storageService = storageService;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _errorLogger = errorLogger;
        _appVersion = appVersion ?? "1.0.0";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc />
    public async Task<TelemetryUploadResult> UploadPendingDataAsync(CancellationToken cancellationToken = default)
    {
        var result = new TelemetryUploadResult();

        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                result.Success = false;
                result.ErrorMessage = "API key not configured";
                // Save backup when API key is missing
                result.BackupFilePath = await _storageService.SaveBackupFileAsync(cancellationToken);
                return result;
            }

            var pendingEvents = await _storageService.GetPendingEventsAsync(cancellationToken);
            if (pendingEvents.Count == 0)
            {
                result.Success = true;
                result.EventsUploaded = 0;
                return result;
            }

            // Upload in batches
            var batches = pendingEvents
                .Select((e, i) => new { Event = e, Index = i })
                .GroupBy(x => x.Index / BatchSize)
                .Select(g => g.Select(x => x.Event).ToList())
                .ToList();

            var uploadedIds = new List<string>();

            foreach (var batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchResult = await UploadBatchWithRetryAsync(batch, apiKey, cancellationToken);
                if (batchResult.Success)
                {
                    uploadedIds.AddRange(batch.Select(e => e.DataId));
                }
                else
                {
                    // Stop on first failure
                    result.ErrorMessage = batchResult.ErrorMessage;
                    break;
                }
            }

            // Mark uploaded events
            if (uploadedIds.Count > 0)
            {
                await _storageService.MarkEventsUploadedAsync(uploadedIds, cancellationToken);
            }

            result.Success = uploadedIds.Count == pendingEvents.Count;
            result.EventsUploaded = uploadedIds.Count;
            result.TotalPending = pendingEvents.Count;

            // If upload failed or was partial, save a backup file locally
            if (!result.Success)
            {
                result.BackupFilePath = await _storageService.SaveBackupFileAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Upload cancelled";
            // Save backup on cancellation
            result.BackupFilePath = await _storageService.SaveBackupFileAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Network, "Failed to upload telemetry data");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            // Save backup on failure
            result.BackupFilePath = await _storageService.SaveBackupFileAsync(CancellationToken.None);
        }

        return result;
    }

    private async Task<TelemetryUploadResult> UploadBatchWithRetryAsync(
        List<TelemetryEvent> batch,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var result = new TelemetryUploadResult();

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    // Exponential backoff: 2s, 4s, 8s
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                }

                var uploadResult = await UploadBatchAsync(batch, apiKey, cancellationToken);
                if (uploadResult.Success)
                {
                    return uploadResult;
                }

                result.ErrorMessage = uploadResult.ErrorMessage;

                // Don't retry on client errors (4xx)
                if (uploadResult.ErrorMessage?.Contains("400") == true ||
                    uploadResult.ErrorMessage?.Contains("401") == true ||
                    uploadResult.ErrorMessage?.Contains("413") == true)
                {
                    break;
                }
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = ex.Message;
                _errorLogger?.LogDebug($"Upload attempt {attempt + 1} failed: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<TelemetryUploadResult> UploadBatchAsync(
        List<TelemetryEvent> batch,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var result = new TelemetryUploadResult();

        // Extract shared metadata from the first event for compact format
        var firstEvent = batch[0];
        var uploadData = new TelemetryUploadData
        {
            UploadTime = DateTime.UtcNow,
            AppVersion = _appVersion,
            Platform = firstEvent.Platform,
            UserAgent = firstEvent.UserAgent,
            GeoLocation = firstEvent.GeoLocation,
            EventCount = batch.Count,
            Events = batch
        };

        // Temporarily null metadata on events so WhenWritingNull excludes them
        var savedMetadata = batch.Select(e => (e.AppVersion, e.Platform, e.UserAgent, e.GeoLocation)).ToList();
        foreach (var e in batch)
        {
            e.AppVersion = null;
            e.Platform = null;
            e.UserAgent = null;
            e.GeoLocation = null;
        }

        string json;
        try
        {
            json = JsonSerializer.Serialize(uploadData, _jsonOptions);
        }
        finally
        {
            // Restore metadata on events for retry scenarios and local storage consistency
            for (int i = 0; i < batch.Count; i++)
            {
                batch[i].AppVersion = savedMetadata[i].AppVersion;
                batch[i].Platform = savedMetadata[i].Platform;
                batch[i].UserAgent = savedMetadata[i].UserAgent;
                batch[i].GeoLocation = savedMetadata[i].GeoLocation;
            }
        }
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jsonBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", $"telemetry_{DateTime.UtcNow:yyyyMMddHHmmss}.json");

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadUrl);
        request.Content = content;
        request.Headers.Add("X-API-Key", apiKey);
        request.Headers.UserAgent.ParseAdd($"{UserAgentPrefix}/{_appVersion}");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            result.Success = true;
            result.EventsUploaded = batch.Count;
        }
        else
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            result.Success = false;
            result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}";
        }

        return result;
    }

    private static string? GetApiKey()
    {
        // Try environment variable first
        var apiKey = Environment.GetEnvironmentVariable("ARGO_TELEMETRY_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
            return apiKey;

        // Try DotEnv
        return DotEnv.Get("UPLOAD_API_KEY");
    }

    private class TelemetryUploadData
    {
        public DateTime UploadTime { get; set; }
        public string? AppVersion { get; set; }
        public string? Platform { get; set; }
        public string? UserAgent { get; set; }
        public GeoLocationData? GeoLocation { get; set; }
        public int EventCount { get; set; }
        public List<TelemetryEvent> Events { get; set; } = [];
    }
}

/// <summary>
/// Interface for telemetry upload operations.
/// </summary>
public interface ITelemetryUploadService
{
    /// <summary>
    /// Uploads all pending telemetry data to the server.
    /// </summary>
    Task<TelemetryUploadResult> UploadPendingDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a telemetry upload operation.
/// </summary>
public class TelemetryUploadResult
{
    public bool Success { get; set; }
    public int EventsUploaded { get; set; }
    public int TotalPending { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Path to the local backup file saved when upload fails. Null if upload succeeded or no backup was needed.
    /// </summary>
    public string? BackupFilePath { get; set; }
}
