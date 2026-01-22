using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Centralized error logging service with telemetry integration.
/// </summary>
public partial class ErrorLogger : IErrorLogger
{
    private readonly ConcurrentQueue<ErrorLogEntry> _logEntries = new();
    private readonly int _maxEntries;
    private readonly object _trimLock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    // Patterns for sanitizing PII from error messages and stack traces
    [GeneratedRegex(@"[A-Za-z]:\\Users\\[^\\]+", RegexOptions.IgnoreCase)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"/Users/[^/]+", RegexOptions.IgnoreCase)]
    private static partial Regex MacUserPathRegex();

    [GeneratedRegex(@"/home/[^/]+", RegexOptions.IgnoreCase)]
    private static partial Regex LinuxUserPathRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b")]
    private static partial Regex PhoneRegex();

    /// <summary>
    /// Gets or sets the telemetry manager for reporting errors to telemetry.
    /// This is set after construction to avoid circular dependencies.
    /// </summary>
    public ITelemetryManager? TelemetryManager { get; set; }

    /// <inheritdoc />
    public event EventHandler<ErrorLogEntry>? ErrorLogged;

    /// <summary>
    /// Initializes a new instance of the ErrorLogger.
    /// </summary>
    /// <param name="maxEntries">Maximum number of log entries to keep in memory.</param>
    public ErrorLogger(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public void LogError(Exception exception, ErrorCategory category, string? context = null)
    {
        var entry = CreateLogEntry(LogLevel.Error, category, context);
        entry.ErrorCode = exception.GetType().Name;
        entry.Message = SanitizeMessage(exception.Message);

        // Extract location from stack trace
        var stackTrace = new StackTrace(exception, true);
        var frame = stackTrace.GetFrame(0);
        if (frame != null)
        {
            var fileName = frame.GetFileName();
            entry.SourceFile = fileName != null ? Path.GetFileName(fileName) : null;
            entry.LineNumber = frame.GetFileLineNumber();
            entry.MethodName = frame.GetMethod()?.Name;
        }

        // Sanitize and truncate stack trace
        entry.StackTrace = SanitizeStackTrace(exception.StackTrace, maxFrames: 5);

        AddEntry(entry);

        // Log inner exceptions recursively (but don't create separate entries)
        if (exception.InnerException != null)
        {
            entry.Message += $" | Inner: {SanitizeMessage(exception.InnerException.Message)}";
        }

        Debug.WriteLine($"[ERROR] [{category}] {entry.Message}");
    }

    /// <inheritdoc />
    public void LogError(string message, ErrorCategory category, string? context = null)
    {
        var entry = CreateLogEntry(LogLevel.Error, category, context);
        entry.Message = SanitizeMessage(message);

        AddEntry(entry);
        Debug.WriteLine($"[ERROR] [{category}] {message}");
    }

    /// <inheritdoc />
    public void LogWarning(string message, string? context = null)
    {
        var entry = CreateLogEntry(LogLevel.Warning, ErrorCategory.Unknown, context);
        entry.Message = SanitizeMessage(message);

        AddEntry(entry);
        Debug.WriteLine($"[WARNING] {message}");
    }

    /// <inheritdoc />
    public void LogInfo(string message)
    {
        var entry = CreateLogEntry(LogLevel.Info, ErrorCategory.Unknown, null);
        entry.Message = message;

        AddEntry(entry);
        Debug.WriteLine($"[INFO] {message}");
    }

    /// <inheritdoc />
    public void LogDebug(string message)
    {
#if DEBUG
        var entry = CreateLogEntry(LogLevel.Debug, ErrorCategory.Unknown, null);
        entry.Message = message;

        AddEntry(entry);
        Debug.WriteLine($"[DEBUG] {message}");
#endif
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorLogEntry> GetRecentErrors(int count = 50)
    {
        return _logEntries
            .Where(e => e.Level >= LogLevel.Warning)
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorLogEntry> GetAllErrors()
    {
        return _logEntries
            .Where(e => e.Level >= LogLevel.Warning)
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public Task<string> ExportErrorLogAsync()
    {
        var entries = _logEntries.OrderByDescending(e => e.Timestamp).ToList();
        var json = JsonSerializer.Serialize(entries, _jsonOptions);
        return Task.FromResult(json);
    }

    /// <inheritdoc />
    public void ClearLogs()
    {
        while (_logEntries.TryDequeue(out _))
        {
            // Clear all entries
        }
    }

    private ErrorLogEntry CreateLogEntry(
        LogLevel level,
        ErrorCategory category,
        string? context,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = "")
    {
        return new ErrorLogEntry
        {
            Level = level,
            Category = category,
            Context = context,
            SourceFile = Path.GetFileName(callerFile),
            LineNumber = callerLine,
            MethodName = callerMember
        };
    }

    private void AddEntry(ErrorLogEntry entry)
    {
        _logEntries.Enqueue(entry);

        // Trim if over capacity
        if (_logEntries.Count > _maxEntries)
        {
            lock (_trimLock)
            {
                while (_logEntries.Count > _maxEntries * 0.9) // Trim to 90%
                {
                    _logEntries.TryDequeue(out _);
                }
            }
        }

        // Raise event
        ErrorLogged?.Invoke(this, entry);

        // Report to telemetry if available and this is an error
        if (entry.Level == LogLevel.Error && TelemetryManager != null)
        {
            _ = TelemetryManager.TrackErrorAsync(entry);
        }
    }

    private static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        var sanitized = message;

        // Remove user paths
        sanitized = WindowsUserPathRegex().Replace(sanitized, "[USER_PATH]");
        sanitized = MacUserPathRegex().Replace(sanitized, "[USER_PATH]");
        sanitized = LinuxUserPathRegex().Replace(sanitized, "[USER_PATH]");

        // Remove emails
        sanitized = EmailRegex().Replace(sanitized, "[EMAIL]");

        // Remove phone numbers
        sanitized = PhoneRegex().Replace(sanitized, "[PHONE]");

        // Truncate very long messages
        if (sanitized.Length > 500)
        {
            sanitized = sanitized[..497] + "...";
        }

        return sanitized;
    }

    private static string? SanitizeStackTrace(string? stackTrace, int maxFrames = 5)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return null;

        var lines = stackTrace.Split('\n')
            .Take(maxFrames)
            .Select(line =>
            {
                var sanitized = WindowsUserPathRegex().Replace(line, "[USER_PATH]");
                sanitized = MacUserPathRegex().Replace(sanitized, "[USER_PATH]");
                sanitized = LinuxUserPathRegex().Replace(sanitized, "[USER_PATH]");
                return sanitized.Trim();
            })
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(" -> ", lines);
    }
}
