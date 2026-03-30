using SkiaSharp;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Shared image compression and format utilities for receipt scanning services.
/// </summary>
internal static class ReceiptImageHelper
{
    /// <summary>
    /// Compresses an image to fit within the max file size for scanning.
    /// Strategy: re-encode as JPEG (handles BMP/PNG/TIFF), then progressively
    /// reduce resolution if still too large. PDFs are returned unchanged.
    /// </summary>
    internal static (byte[] Data, string FileName) CompressImageForScanning(byte[] imageData, string fileName, int maxSizeBytes)
    {
        // Already within limit — no work needed
        if (imageData.Length <= maxSizeBytes)
            return (imageData, fileName);

        // PDFs cannot be image-compressed
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf")
            return (imageData, fileName);

        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            return (imageData, fileName);

        var outputFileName = Path.ChangeExtension(fileName, ".jpg");

        // For BMP/PNG/TIFF, simply re-encoding as JPEG at high quality is often enough
        const int highQuality = 95;
        var encoded = EncodeAsJpeg(original, highQuality);
        if (encoded.Length <= maxSizeBytes)
            return (encoded, outputFileName);

        // Progressively reduce resolution until it fits
        float[] scales = [0.85f, 0.7f, 0.55f, 0.4f, 0.3f];
        foreach (var scale in scales)
        {
            var newWidth = Math.Max(1, (int)(original.Width * scale));
            var newHeight = Math.Max(1, (int)(original.Height * scale));

            using var resized = ResizeBitmap(original, newWidth, newHeight);
            encoded = EncodeAsJpeg(resized, highQuality);
            if (encoded.Length <= maxSizeBytes)
                return (encoded, outputFileName);
        }

        // Last resort: smallest scale with reduced quality
        var minWidth = Math.Max(1, (int)(original.Width * 0.3f));
        var minHeight = Math.Max(1, (int)(original.Height * 0.3f));
        using var smallest = ResizeBitmap(original, minWidth, minHeight);

        foreach (var quality in (int[])[85, 75, 65])
        {
            encoded = EncodeAsJpeg(smallest, quality);
            if (encoded.Length <= maxSizeBytes)
                return (encoded, outputFileName);
        }

        // Could not compress enough — return original and let the size check fail
        return (imageData, fileName);
    }

    internal static string? GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            _ => null
        };
    }

    internal static byte[] EncodeAsJpeg(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }

    internal static SKBitmap ResizeBitmap(SKBitmap source, int width, int height)
    {
        var resized = new SKBitmap(width, height);
        using var canvas = new SKCanvas(resized);
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height));
        return resized;
    }
}
