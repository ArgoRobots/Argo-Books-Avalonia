using System.Runtime.InteropServices;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Central telemetry management service that coordinates all telemetry operations.
/// </summary>
public class TelemetryManager : ITelemetryManager
{
    /// <summary>
    /// How often the session sentinel is stamped. Also the tick that drives the periodic
    /// upload below, so it doubles as the granularity of a recovered session's duration.
    /// </summary>
    private const int HeartbeatIntervalSeconds = 60;

    /// <summary>
    /// Heartbeats between periodic uploads, i.e. one flush every 20 minutes.
    /// <para>
    /// Sized against the server's free-tier ceiling of 6 uploads per hour per device: at
    /// most 3 periodic flushes, plus the startup flush and the one on close, leaves
    /// headroom. Quiet ticks are free because UploadPendingDataAsync returns without a
    /// request when nothing is pending.
    /// </para>
    /// </summary>
    private const int HeartbeatsPerUpload = 20;

    private readonly ITelemetryStorageService _storageService;
    private readonly ITelemetryUploadService _uploadService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IErrorLogger _errorLogger;
    private readonly IPlatformService _platformService;

    private readonly string _appVersion;
    private readonly string _platform;
    private readonly string _userAgent;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private DateTime _sessionStartTime;
    private GeoLocationData? _cachedGeoLocation;
    private bool _isInitialized;

    private SessionSentinel? _sentinel;
    private System.Threading.Timer? _heartbeatTimer;
    private int _heartbeatTicks;
    private int _uploadInFlight;

    /// <summary>
    /// Initializes a new instance of the TelemetryManager.
    /// </summary>
    public TelemetryManager(
        ITelemetryStorageService storageService,
        ITelemetryUploadService uploadService,
        IGeoLocationService geoLocationService,
        IErrorLogger errorLogger,
        string? appVersion = null,
        IPlatformService? platformService = null)
    {
        _storageService = storageService;
        _uploadService = uploadService;
        _geoLocationService = geoLocationService;
        _errorLogger = errorLogger;
        _platformService = platformService ?? PlatformServiceFactory.GetPlatformService();

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
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            _sessionStartTime = DateTime.UtcNow;
            _isInitialized = true;

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
                        _errorLogger.LogDebug($"Failed to get geolocation: {ex.Message}");
                    }
                }, cancellationToken);

                // Close out any previous run that died without recording its own end.
                // Done before this session's start so the recovered events, which carry
                // their original timestamps, read in order against it.
                await RecoverUncleanSessionsAsync(cancellationToken);

                _sentinel = SessionSentinel.Begin(_platformService, _sessionStartTime, _appVersion, _errorLogger);

                // Record session start
                var sessionEvent = await CreateEventAsync<SessionEvent>(cancellationToken);
                sessionEvent.Action = SessionAction.SessionStart;
                await _storageService.RecordEventAsync(sessionEvent, cancellationToken);

                StartHeartbeat();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex, ErrorCategory.Unknown, "Failed to initialize telemetry");
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task EndSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            return;

        // Stopped first so a heartbeat can't race the sentinel's removal below.
        StopHeartbeat();

        try
        {
            var duration = (long)(DateTime.UtcNow - _sessionStartTime).TotalSeconds;

            var sessionEvent = await CreateEventAsync<SessionEvent>(cancellationToken);
            sessionEvent.Action = SessionAction.SessionEnd;
            sessionEvent.DurationSeconds = duration;
            sessionEvent.Clean = true;
            await _storageService.RecordEventAsync(sessionEvent, cancellationToken);

            // Only now is the session provably accounted for. Dropping the sentinel any
            // earlier would lose the session outright if the record above threw; dropping
            // it later would mean a failed upload got it reported as an unclean exit, even
            // though the event is safely stored for the next launch to deliver.
            _sentinel?.Complete();
            _sentinel = null;

            // Attempt to upload pending data on shutdown - must await to ensure upload completes before app closes
            await _uploadService.UploadPendingDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogError(ex, ErrorCategory.Unknown, "Failed to end telemetry session");
        }
        finally
        {
            // Releases the handle without deleting, so a shutdown that never got as far as
            // Complete() is reported as the unclean exit it was.
            _sentinel?.Dispose();
            _sentinel = null;
        }
    }

    /// <summary>
    /// Turns sentinels left by runs that never shut down cleanly into the SessionEnd events
    /// those runs could not record for themselves.
    /// </summary>
    private async Task RecoverUncleanSessionsAsync(CancellationToken cancellationToken)
    {
        foreach (var orphan in SessionSentinel.CollectOrphans(_platformService, _errorLogger))
        {
            var sessionEvent = await CreateEventAsync<SessionEvent>(cancellationToken);
            sessionEvent.Timestamp = orphan.LastHeartbeatUtc;
            sessionEvent.Action = SessionAction.SessionEnd;
            sessionEvent.DurationSeconds = orphan.DurationSeconds;
            sessionEvent.Clean = false;

            // The dead run may have been an older build, so attribute it to the version
            // that actually ran rather than the one doing the recovering.
            if (!string.IsNullOrEmpty(orphan.AppVersion))
            {
                sessionEvent.AppVersion = orphan.AppVersion;
            }

            await _storageService.RecordEventAsync(sessionEvent, cancellationToken);
        }
    }

    private void StartHeartbeat()
    {
        var period = TimeSpan.FromSeconds(HeartbeatIntervalSeconds);
        _heartbeatTimer = new System.Threading.Timer(OnHeartbeat, null, period, period);
    }

    private void StopHeartbeat()
    {
        var timer = Interlocked.Exchange(ref _heartbeatTimer, null);
        timer?.Dispose();
    }

    /// <summary>
    /// Stamps the sentinel every tick, and every <see cref="HeartbeatsPerUpload"/> ticks
    /// also flushes pending events. The flush is what makes a session that never closes
    /// cleanly still worth something: without it, a user who force-quits and never reopens
    /// the app takes every event on their machine with them.
    /// </summary>
    private void OnHeartbeat(object? state)
    {
        try
        {
            _sentinel?.Heartbeat(DateTime.UtcNow);

            if (Interlocked.Increment(ref _heartbeatTicks) % HeartbeatsPerUpload != 0)
            {
                return;
            }

            // Skip rather than queue when a flush is still running. A slow or retrying
            // upload must not stack requests up behind it and burn the hourly allowance.
            if (Interlocked.CompareExchange(ref _uploadInFlight, 1, 0) != 0)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _uploadService.UploadPendingDataAsync();
                }
                catch (Exception ex)
                {
                    _errorLogger.LogDebug($"Periodic telemetry upload failed: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _uploadInFlight, 0);
                }
            });
        }
        catch (Exception ex)
        {
            _errorLogger.LogDebug($"Telemetry heartbeat failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task TrackFeatureAsync(FeatureName featureName, string? context = null, long? durationMs = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureEvent = await CreateEventAsync<FeatureUsageEvent>(cancellationToken);
            featureEvent.FeatureName = featureName;
            featureEvent.Context = context;
            featureEvent.DurationMs = durationMs;
            await _storageService.RecordEventAsync(featureEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger.LogDebug($"Failed to track feature: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task TrackExportAsync(ExportType exportType, long durationMs, long fileSize, CancellationToken cancellationToken = default)
    {
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
        try
        {
            var errorEvent = await CreateEventAsync<ErrorEvent>(cancellationToken);
            // Anything below Warning never reaches here (see ErrorLogger.AddEntry), so
            // the only values that travel are Warning and Error.
            errorEvent.Severity = errorEntry.Level == LogLevel.Warning ? LogLevel.Warning : LogLevel.Error;
            errorEvent.ErrorCode = errorEntry.ErrorCode ?? "Unknown";
            errorEvent.ErrorCategory = errorEntry.Category;
            errorEvent.Message = errorEntry.Message;
            errorEvent.SourceFile = errorEntry.SourceFile;
            errorEvent.LineNumber = errorEntry.LineNumber;
            errorEvent.MethodName = errorEntry.MethodName;
            await _storageService.RecordEventAsync(errorEvent, cancellationToken);
        }
        catch
        {
            // Don't log errors about error tracking to avoid infinite loops
        }
    }

    /// <inheritdoc />
    public async Task<TelemetryUploadResult> UploadPendingDataAsync(CancellationToken cancellationToken = default)
    {
        return await _uploadService.UploadPendingDataAsync(cancellationToken);
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
        else
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
