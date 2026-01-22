using System.Text.Json;
using System.Text.Json.Serialization;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for storing telemetry events locally.
/// </summary>
public class TelemetryStorageService : ITelemetryStorageService
{
    private const string TelemetryDirectory = "telemetry";
    private const string EventsFileName = "events.json";
    private const string UploadStateFileName = "upload_state.json";
    private const int MaxEventsInMemory = 10000;

    private readonly IPlatformService _platformService;
    private readonly IErrorLogger? _errorLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<TelemetryEventWrapper> _events = [];
    private UploadState _uploadState = new();
    private bool _isLoaded;

    /// <summary>
    /// Initializes a new instance of the TelemetryStorageService.
    /// </summary>
    public TelemetryStorageService(IPlatformService? platformService = null, IErrorLogger? errorLogger = null)
    {
        _platformService = platformService ?? PlatformServiceFactory.GetPlatformService();
        _errorLogger = errorLogger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc />
    public async Task RecordEventAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var wrapper = new TelemetryEventWrapper
            {
                DataType = telemetryEvent.DataType,
                Event = telemetryEvent
            };

            _events.Add(wrapper);

            // Trim if over capacity
            if (_events.Count > MaxEventsInMemory)
            {
                // Remove oldest non-uploaded events first, then oldest uploaded
                var toRemove = _events
                    .OrderBy(e => e.Event.IsUploaded)
                    .ThenBy(e => e.Event.Timestamp)
                    .Take(_events.Count - (int)(MaxEventsInMemory * 0.9))
                    .ToList();

                foreach (var item in toRemove)
                {
                    _events.Remove(item);
                }
            }

            await SaveEventsAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TelemetryEvent>> GetPendingEventsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return _events
                .Where(e => !e.Event.IsUploaded)
                .Select(e => e.Event)
                .OrderBy(e => e.Timestamp)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task MarkEventsUploadedAsync(IEnumerable<string> dataIds, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var idSet = dataIds.ToHashSet();
            foreach (var wrapper in _events.Where(e => idSet.Contains(e.Event.DataId)))
            {
                wrapper.Event.IsUploaded = true;
            }

            _uploadState.LastUploadTime = DateTime.UtcNow;
            _uploadState.TotalEventsUploaded += idSet.Count;

            await SaveEventsAsync(cancellationToken);
            await SaveUploadStateAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var exportData = new TelemetryExport
            {
                ExportTime = DateTime.UtcNow,
                TotalEvents = _events.Count,
                Events = _events.Select(e => e.Event).OrderByDescending(e => e.Timestamp).ToList()
            };

            return JsonSerializer.Serialize(exportData, _jsonOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _events.Clear();
            _uploadState = new UploadState();

            var eventsPath = GetEventsFilePath();
            var statePath = GetUploadStatePath();

            if (File.Exists(eventsPath))
                File.Delete(eventsPath);

            if (File.Exists(statePath))
                File.Delete(statePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TelemetryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            return new TelemetryStatistics
            {
                TotalEvents = _events.Count,
                PendingEvents = _events.Count(e => !e.Event.IsUploaded),
                UploadedEvents = _events.Count(e => e.Event.IsUploaded),
                EventsByType = _events
                    .GroupBy(e => e.DataType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                OldestEventTime = _events.MinBy(e => e.Event.Timestamp)?.Event.Timestamp,
                NewestEventTime = _events.MaxBy(e => e.Event.Timestamp)?.Event.Timestamp,
                LastUploadTime = _uploadState.LastUploadTime,
                TotalEventsEverUploaded = _uploadState.TotalEventsUploaded
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_isLoaded)
            return;

        await LoadEventsAsync(cancellationToken);
        await LoadUploadStateAsync(cancellationToken);
        _isLoaded = true;
    }

    private async Task LoadEventsAsync(CancellationToken cancellationToken)
    {
        var path = GetEventsFilePath();
        if (!File.Exists(path))
        {
            _events = [];
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer.DeserializeAsync<List<TelemetryEventWrapper>>(stream, _jsonOptions, cancellationToken);
            _events = loaded ?? [];
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to load telemetry events");
            _events = [];
        }
    }

    private async Task SaveEventsAsync(CancellationToken cancellationToken)
    {
        var path = GetEventsFilePath();
        EnsureDirectoryExists(path);

        try
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, _events, _jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save telemetry events");
        }
    }

    private async Task LoadUploadStateAsync(CancellationToken cancellationToken)
    {
        var path = GetUploadStatePath();
        if (!File.Exists(path))
        {
            _uploadState = new UploadState();
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer.DeserializeAsync<UploadState>(stream, _jsonOptions, cancellationToken);
            _uploadState = loaded ?? new UploadState();
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to load upload state");
            _uploadState = new UploadState();
        }
    }

    private async Task SaveUploadStateAsync(CancellationToken cancellationToken)
    {
        var path = GetUploadStatePath();
        EnsureDirectoryExists(path);

        try
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, _uploadState, _jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save upload state");
        }
    }

    private string GetEventsFilePath()
    {
        return _platformService.CombinePaths(
            _platformService.GetAppDataPath(),
            TelemetryDirectory,
            EventsFileName);
    }

    private string GetUploadStatePath()
    {
        return _platformService.CombinePaths(
            _platformService.GetAppDataPath(),
            TelemetryDirectory,
            UploadStateFileName);
    }

    private void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            _platformService.EnsureDirectoryExists(directory);
        }
    }

    /// <inheritdoc />
    public async Task<string?> SaveBackupFileAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var pendingEvents = _events
                .Where(e => !e.Event.IsUploaded)
                .Select(e => e.Event)
                .OrderBy(e => e.Timestamp)
                .ToList();

            if (pendingEvents.Count == 0)
            {
                return null;
            }

            var backupData = new TelemetryBackup
            {
                BackupTime = DateTime.UtcNow,
                TotalEvents = pendingEvents.Count,
                Events = pendingEvents
            };

            var backupPath = GetBackupFilePath();
            EnsureDirectoryExists(backupPath);

            try
            {
                await using var stream = File.Create(backupPath);
                await JsonSerializer.SerializeAsync(stream, backupData, _jsonOptions, cancellationToken);
                _errorLogger?.LogInfo($"Telemetry backup saved to: {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save telemetry backup file");
                return null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetBackupFilePath()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return _platformService.CombinePaths(
            _platformService.GetAppDataPath(),
            TelemetryDirectory,
            "backups",
            $"telemetry_backup_{timestamp}.json");
    }

    #region Internal Types

    private class TelemetryEventWrapper
    {
        public TelemetryDataType DataType { get; set; }

        [JsonConverter(typeof(TelemetryEventConverter))]
        public TelemetryEvent Event { get; set; } = null!;
    }

    private class UploadState
    {
        public DateTime? LastUploadTime { get; set; }
        public int TotalEventsUploaded { get; set; }
    }

    private class TelemetryExport
    {
        public DateTime ExportTime { get; set; }
        public int TotalEvents { get; set; }
        public List<TelemetryEvent> Events { get; set; } = [];
    }

    private class TelemetryBackup
    {
        public DateTime BackupTime { get; set; }
        public int TotalEvents { get; set; }
        public List<TelemetryEvent> Events { get; set; } = [];
    }

    /// <summary>
    /// Custom JSON converter for polymorphic TelemetryEvent serialization.
    /// </summary>
    private class TelemetryEventConverter : JsonConverter<TelemetryEvent>
    {
        public override TelemetryEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("dataType", out var dataTypeElement))
            {
                return null;
            }

            var dataType = Enum.Parse<TelemetryDataType>(dataTypeElement.GetString()!, ignoreCase: true);
            var json = root.GetRawText();

            return dataType switch
            {
                TelemetryDataType.Session => JsonSerializer.Deserialize<SessionEvent>(json, options),
                TelemetryDataType.Export => JsonSerializer.Deserialize<ExportEvent>(json, options),
                TelemetryDataType.ApiUsage => JsonSerializer.Deserialize<ApiUsageEvent>(json, options),
                TelemetryDataType.Error => JsonSerializer.Deserialize<ErrorEvent>(json, options),
                TelemetryDataType.FeatureUsage => JsonSerializer.Deserialize<FeatureUsageEvent>(json, options),
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, TelemetryEvent value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    #endregion
}

/// <summary>
/// Interface for telemetry storage operations.
/// </summary>
public interface ITelemetryStorageService
{
    /// <summary>
    /// Records a telemetry event to local storage.
    /// </summary>
    Task RecordEventAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events that haven't been uploaded yet.
    /// </summary>
    Task<IReadOnlyList<TelemetryEvent>> GetPendingEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the specified events as uploaded.
    /// </summary>
    Task MarkEventsUploadedAsync(IEnumerable<string> dataIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all telemetry data as a JSON string for user review.
    /// </summary>
    Task<string> ExportToJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all stored telemetry data.
    /// </summary>
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about stored telemetry data.
    /// </summary>
    Task<TelemetryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves pending events to a local backup file. This is used when upload fails
    /// to ensure data is preserved locally for later upload attempts.
    /// </summary>
    /// <returns>The path to the saved backup file, or null if there was nothing to save.</returns>
    Task<string?> SaveBackupFileAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about stored telemetry data.
/// </summary>
public class TelemetryStatistics
{
    public int TotalEvents { get; set; }
    public int PendingEvents { get; set; }
    public int UploadedEvents { get; set; }
    public Dictionary<TelemetryDataType, int> EventsByType { get; set; } = new();
    public DateTime? OldestEventTime { get; set; }
    public DateTime? NewestEventTime { get; set; }
    public DateTime? LastUploadTime { get; set; }
    public int TotalEventsEverUploaded { get; set; }
}
