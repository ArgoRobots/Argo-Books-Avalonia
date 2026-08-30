using Avalonia.Platform.Storage;

namespace ArgoBooks.Utilities;

/// <summary>
/// Shared file picker type definitions used across multiple pages.
/// </summary>
public static class FilePickerTypes
{
    /// <summary>
    /// File extensions (lowercase, leading dot) the AI receipt scanner can process.
    /// Single source of truth for the file picker, drag-drop, and queue validation.
    /// </summary>
    public static readonly string[] SupportedReceiptExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".pdf"];

    /// <summary>
    /// Returns true if the path points to a file the receipt scanner accepts.
    /// </summary>
    public static bool IsSupportedReceiptFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return Array.IndexOf(SupportedReceiptExtensions, extension) >= 0;
    }

    public static readonly FilePickerFileType ImageFileType = new("Images")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.heic", "*.heif"],
        MimeTypes = ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"]
    };

    public static readonly FilePickerFileType PdfFileType = new("PDF Documents")
    {
        Patterns = ["*.pdf"],
        MimeTypes = ["application/pdf"]
    };

    public static readonly FilePickerFileType AllSupportedTypes = new("All Supported")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.heic", "*.heif", "*.pdf"],
        MimeTypes = ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif", "application/pdf"]
    };
}
