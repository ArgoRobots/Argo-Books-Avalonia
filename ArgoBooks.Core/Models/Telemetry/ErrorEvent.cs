using System.Text.Json.Serialization;

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

/// <summary>
/// Categories of errors for classification.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Unknown or uncategorized error.
    /// </summary>
    Unknown,

    /// <summary>
    /// Network connectivity or HTTP errors.
    /// </summary>
    Network,

    /// <summary>
    /// File system or database errors.
    /// </summary>
    FileSystem,

    /// <summary>
    /// JSON, XML, or data parsing errors.
    /// </summary>
    Parsing,

    /// <summary>
    /// Business logic validation errors.
    /// </summary>
    Validation,

    /// <summary>
    /// UI rendering or binding errors.
    /// </summary>
    UI,

    /// <summary>
    /// External API call failures.
    /// </summary>
    Api,

    /// <summary>
    /// Export operation errors.
    /// </summary>
    Export,

    /// <summary>
    /// Import operation errors.
    /// </summary>
    Import,

    /// <summary>
    /// License validation errors.
    /// </summary>
    License,

    /// <summary>
    /// Authentication or credential errors.
    /// </summary>
    Authentication,

    /// <summary>
    /// Encryption or decryption errors.
    /// </summary>
    Encryption
}
