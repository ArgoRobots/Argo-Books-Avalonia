namespace ArgoBooks.Core.Models;

/// <summary>
/// Footer structure appended to .argo files.
/// Contains metadata needed to open the file without reading the entire contents.
/// </summary>
public class FileFooter
{
    /// <summary>
    /// Application version that created/last saved the file.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Whether the file contents are encrypted.
    /// </summary>
    [JsonPropertyName("isEncrypted")]
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Salt used for password-based key derivation (Base64 encoded).
    /// Only present if IsEncrypted is true.
    /// </summary>
    [JsonPropertyName("salt")]
    public string? Salt { get; set; }

    /// <summary>
    /// Hash of the password for verification (Base64 encoded).
    /// Only present if IsEncrypted is true.
    /// </summary>
    [JsonPropertyName("passwordHash")]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// IV/Nonce used for encryption (Base64 encoded).
    /// Only present if IsEncrypted is true.
    /// </summary>
    [JsonPropertyName("iv")]
    public string? Iv { get; set; }

    /// <summary>
    /// List of accountant names (for quick access without decryption).
    /// </summary>
    [JsonPropertyName("accountants")]
    public List<string> Accountants { get; set; } = [];

    /// <summary>
    /// Company name (for display in recent files without opening).
    /// </summary>
    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// When the file was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the file was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether biometric authentication (Windows Hello) is enabled for this file.
    /// Stored in footer so it can be read before decryption.
    /// </summary>
    [JsonPropertyName("biometricEnabled")]
    public bool BiometricEnabled { get; set; } = false;

    /// <summary>
    /// Small Base64-encoded PNG thumbnail of the company logo (64x64 max).
    /// Stored in footer for instant access without decompressing the archive.
    /// Null if the company has no logo.
    /// </summary>
    [JsonPropertyName("logoThumbnail")]
    public string? LogoThumbnail { get; set; }
}

/// <summary>
/// Marker bytes and constants for file format.
/// </summary>
public static class FileFormatConstants
{
    /// <summary>
    /// Magic bytes at the end of footer to identify Argo files.
    /// "ARGO" in ASCII.
    /// </summary>
    public static readonly byte[] MagicBytes = "ARGO"u8.ToArray();

    /// <summary>
    /// Current file format version.
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// File extension for company files.
    /// </summary>
    public const string CompanyFileExtension = ".argo";

    /// <summary>
    /// File extension for backup files.
    /// </summary>
    public const string BackupFileExtension = ".argobk";

    /// <summary>
    /// File extension for report templates.
    /// </summary>
    public const string TemplateFileExtension = ".argotemplate";

}
