namespace ArgoBooks.Core.Models.Telemetry;

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
