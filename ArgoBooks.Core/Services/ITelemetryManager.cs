using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Interface for the central telemetry management service.
/// </summary>
public interface ITelemetryManager
{
    /// <summary>
    /// Gets whether the user has consented to anonymous data collection.
    /// </summary>
    bool IsConsentGranted { get; }

    /// <summary>
    /// Sets the user's consent preference for data collection.
    /// </summary>
    /// <param name="granted">Whether consent is granted.</param>
    void SetConsent(bool granted);

    /// <summary>
    /// Initializes the telemetry manager and starts a new session.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current session (called on app shutdown).
    /// </summary>
    Task EndSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a feature usage event.
    /// </summary>
    Task TrackFeatureAsync(FeatureName featureName, string? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a page view event.
    /// </summary>
    Task TrackPageViewAsync(string pageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an export operation.
    /// </summary>
    Task TrackExportAsync(ExportType exportType, long durationMs, long fileSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an API call.
    /// </summary>
    Task TrackApiCallAsync(ApiName apiName, long durationMs, bool success, string? model = null, int? tokensUsed = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an error event from the error logger.
    /// </summary>
    Task TrackErrorAsync(ErrorLogEntry errorEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads all pending telemetry data to the server (requires consent).
    /// </summary>
    Task<TelemetryUploadResult> UploadPendingDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all collected telemetry data as JSON for user review.
    /// </summary>
    Task<string> ExportDataAsJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all collected telemetry data.
    /// </summary>
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about collected telemetry data.
    /// </summary>
    Task<TelemetryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
