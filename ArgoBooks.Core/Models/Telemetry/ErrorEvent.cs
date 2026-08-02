namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Error event for tracking application errors.
/// </summary>
public class ErrorEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.Error;

    /// <summary>
    /// Error code or exception type name.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Category of the error.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ErrorCategory ErrorCategory { get; set; }

    /// <summary>
    /// Sanitized error message (PII removed).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Source file where the error occurred (filename only, no path).
    /// </summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// Line number where the error occurred.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Method name where the error occurred.
    /// </summary>
    public string? MethodName { get; set; }
}
