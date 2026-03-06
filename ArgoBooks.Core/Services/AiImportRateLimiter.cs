using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Tracks AI import usage and enforces a daily rate limit.
/// Persists usage data to a JSON file in the app's local data directory.
/// </summary>
public class AiImportRateLimiter
{
    private const int MaxImportsPerDay = 10;
    private const string RateLimitFileName = "ai-import-usage.json";

    private readonly string _filePath;
    private readonly object _lock = new();

    public AiImportRateLimiter(string appDataPath)
    {
        _filePath = Path.Combine(appDataPath, RateLimitFileName);
    }

    /// <summary>
    /// Whether the user can perform an AI import right now.
    /// </summary>
    public bool CanImport()
    {
        return GetRemainingImportsToday() > 0;
    }

    /// <summary>
    /// Gets the number of remaining AI imports for today.
    /// </summary>
    public int GetRemainingImportsToday()
    {
        var usage = LoadUsage();
        var todayCount = CountTodayImports(usage);
        return Math.Max(0, MaxImportsPerDay - todayCount);
    }

    /// <summary>
    /// Gets the total allowed imports per day.
    /// </summary>
    public int MaxPerDay => MaxImportsPerDay;

    /// <summary>
    /// Records a successful AI import.
    /// </summary>
    public void RecordImport(string fileName = "")
    {
        lock (_lock)
        {
            var usage = LoadUsage();

            // Prune old entries (older than 24 hours)
            var cutoff = DateTime.UtcNow.AddHours(-24);
            usage.Imports.RemoveAll(i => i.Timestamp < cutoff);

            // Add new entry
            usage.Imports.Add(new AiImportUsageEntry
            {
                Timestamp = DateTime.UtcNow,
                FileName = fileName
            });

            SaveUsage(usage);
        }
    }

    private static int CountTodayImports(AiImportUsageData usage)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return usage.Imports.Count(i => i.Timestamp.Date == todayUtc);
    }

    private AiImportUsageData LoadUsage()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new AiImportUsageData();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AiImportUsageData>(json) ?? new AiImportUsageData();
        }
        catch
        {
            return new AiImportUsageData();
        }
    }

    private void SaveUsage(AiImportUsageData usage)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(usage, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Silently fail — rate limiting is a convenience, not a hard requirement
        }
    }
}

internal class AiImportUsageData
{
    [JsonPropertyName("imports")]
    public List<AiImportUsageEntry> Imports { get; set; } = [];
}

internal class AiImportUsageEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
}
