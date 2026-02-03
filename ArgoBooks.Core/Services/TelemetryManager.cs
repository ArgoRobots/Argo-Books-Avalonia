using System.Diagnostics;
using System.Runtime.InteropServices;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Central telemetry management service that coordinates all telemetry operations.
/// </summary>
public class TelemetryManager : ITelemetryManager
{
    private readonly ITelemetryStorageService _storageService;
    private readonly ITelemetryUploadService _uploadService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IGlobalSettingsService _settingsService;
    private readonly IErrorLogger _errorLogger;

    private readonly string _appVersion;
    private readonly string _platform;
    private readonly string _userAgent;

    private DateTime _sessionStartTime;
    private GeoLocationData? _cachedGeoLocation;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the TelemetryManager.
    /// </summary>
    public TelemetryManager(
        ITelemetryStorageService storageService,
        ITelemetryUploadService uploadService,
        IGeoLocationService geoLocationService,
        IGlobalSettingsService settingsService,
        IErrorLogger errorLogger,
        string? appVersion = null)
    {
        _storageService = storageService;
        _uploadService = uploadService;
        _geoLocationService = geoLocationService;
        _settingsService = settingsService;
        _errorLogger = errorLogger;

        _appVersion = appVersion ?? AppInfo.VersionNumber;
        _platform = GetPlatform();
        _userAgent = GetUserAgent();

        // Wire up error logger to report errors to telemetry
        if (_errorLogger is ErrorLogger errorLoggerImpl)
        {
            errorLoggerImpl.TelemetryManager = this;
        }
    }

    /// <inheritdoc />
    public bool IsConsentGranted => _settingsService.GetSettings()?.Privacy?.AnonymousDataCollectionConsent ?? false;

    /// <inheritdoc />
    public void SetConsent(bool granted)
    {
        var settings = _settingsService.GetSettings();
        if (settings?.Privacy == null)
            return;
        settings.Privacy.AnonymousDataCollectionConsent = granted;
        settings.Privacy.ConsentDate = granted ? DateTime.UtcNow : null;
        _settingsService.SaveSettings(settings);

        _errorLogger.LogInfo($"Telemetry consent {(granted ? "granted" : "revoked")}");
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        _sessionStartTime = DateTime.UtcNow;
        _isInitialized = true;

        if (!IsConsentGranted)
            return;

        try
        {
            // Prefetch geolocation in background
            _ = Task.Run(async () =>
            {
                try
                {
                    _cachedGeoLocation = await _geoLocationService.GetLocationAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to get geolocation: {ex.Message}");
                }
            }, cancellationToken);

            // Record session start
            var sessionEvent = await CreateEventAsync<SessionEvent>(cancellationToken);
            sessionEvent.Action = SessionAction.SessionStart;
            await _storageService.RecordEventAsync(sessionEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogError(ex, ErrorCategory.Unknown, "Failed to initialize telemetry");
        }
    }

    /// <inheritdoc />
    public async Task EndSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || !IsConsentGranted)
            return;

        try
        {
            var duration = (long)(DateTime.UtcNow - _sessionStartTime).TotalSeconds;

            var sessionEvent = await CreateEventAsync<SessionEvent>(cancellationToken);
            sessionEvent.Action = SessionAction.SessionEnd;
            sessionEvent.DurationSeconds = duration;
            await _storageService.RecordEventAsync(sessionEvent, cancellationToken);

            // Attempt to upload pending data on shutdown - must await to ensure upload completes before app closes
            await _uploadService.UploadPendingDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogError(ex, ErrorCategory.Unknown, "Failed to end telemetry session");
        }
    }

    /// <inheritdoc />
    public async Task TrackFeatureAsync(FeatureName featureName, string? context = null, CancellationToken cancellationToken = default)
    {
        if (!IsConsentGranted)
            return;

        try
        {
            var featureEvent = await CreateEventAsync<FeatureUsageEvent>(cancellationToken);
            featureEvent.FeatureName = featureName;
            featureEvent.Context = context;
            await _storageService.RecordEventAsync(featureEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogDebug($"Failed to track feature: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task TrackPageViewAsync(string pageName, CancellationToken cancellationToken = default)
    {
        await TrackFeatureAsync(FeatureName.PageView, pageName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TrackExportAsync(ExportType exportType, long durationMs, long fileSize, CancellationToken cancellationToken = default)
    {
        if (!IsConsentGranted)
            return;

        try
        {
            var exportEvent = await CreateEventAsync<ExportEvent>(cancellationToken);
            exportEvent.ExportType = exportType;
            exportEvent.DurationMs = durationMs;
            exportEvent.FileSize = fileSize;
            await _storageService.RecordEventAsync(exportEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogDebug($"Failed to track export: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task TrackApiCallAsync(ApiName apiName, long durationMs, bool success, string? model = null, int? tokensUsed = null, CancellationToken cancellationToken = default)
    {
        if (!IsConsentGranted)
            return;

        try
        {
            var apiEvent = await CreateEventAsync<ApiUsageEvent>(cancellationToken);
            apiEvent.ApiName = apiName;
            apiEvent.DurationMs = durationMs;
            apiEvent.Success = success;
            apiEvent.Model = model;
            apiEvent.TokensUsed = tokensUsed;
            await _storageService.RecordEventAsync(apiEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogDebug($"Failed to track API call: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task TrackErrorAsync(ErrorLogEntry errorEntry, CancellationToken cancellationToken = default)
    {
        if (!IsConsentGranted)
            return;

        try
        {
            var errorEvent = await CreateEventAsync<ErrorEvent>(cancellationToken);
            errorEvent.ErrorCode = errorEntry.ErrorCode ?? "Unknown";
            errorEvent.ErrorCategory = errorEntry.Category;
            errorEvent.Message = errorEntry.Message;
            errorEvent.SourceFile = errorEntry.SourceFile;
            errorEvent.LineNumber = errorEntry.LineNumber;
            errorEvent.MethodName = errorEntry.MethodName;
            await _storageService.RecordEventAsync(errorEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't log errors about error tracking to avoid infinite loops
            Debug.WriteLine($"Failed to track error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<TelemetryUploadResult> UploadPendingDataAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConsentGranted)
        {
            return new TelemetryUploadResult
            {
                Success = false,
                ErrorMessage = "User consent not granted"
            };
        }

        return await _uploadService.UploadPendingDataAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ExportDataAsJsonAsync(CancellationToken cancellationToken = default)
    {
        return _storageService.ExportToJsonAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _errorLogger.LogInfo("User requested to clear all telemetry data");
        return _storageService.ClearAllDataAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TelemetryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _storageService.GetStatisticsAsync(cancellationToken);
    }

    private async Task<T> CreateEventAsync<T>(CancellationToken cancellationToken) where T : TelemetryEvent, new()
    {
        var telemetryEvent = new T
        {
            AppVersion = _appVersion,
            Platform = _platform,
            UserAgent = _userAgent
        };

        // Add geolocation if available
        if (_cachedGeoLocation != null)
        {
            telemetryEvent.GeoLocation = _cachedGeoLocation;
        }
        else if (IsConsentGranted)
        {
            try
            {
                _cachedGeoLocation = await _geoLocationService.GetLocationAsync(cancellationToken);
                telemetryEvent.GeoLocation = _cachedGeoLocation;
            }
            catch
            {
                // Ignore geolocation failures
            }
        }

        return telemetryEvent;
    }

    private static string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        return "Unknown";
    }

    private static string GetUserAgent()
    {
        var os = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.OSArchitecture.ToString();
        return $"{os} ({arch})";
    }
}
