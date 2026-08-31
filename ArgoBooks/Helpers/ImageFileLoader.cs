using ArgoBooks.Core.Services;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace ArgoBooks.Helpers;

/// <summary>
/// Prepares an image the user picked, for a logo or an avatar.
///
/// The picked file is copied into the company as-is, so a format Skia cannot decode has to be
/// converted here rather than only for the preview: otherwise the stored image would fail again
/// every time the header or an invoice tried to draw it.
/// </summary>
public static class ImageFileLoader
{
    /// <summary>The file to store, and the bitmap to show. Path differs from the picked one
    /// when the image had to be converted.</summary>
    public sealed record PreparedImage(string Path, Bitmap Bitmap);

    /// <summary>
    /// Returns null when the file cannot be turned into something drawable, which the caller
    /// must report rather than swallow.
    /// </summary>
    public static PreparedImage? TryPrepare(string pickedPath)
    {
        try
        {
            var data = File.ReadAllBytes(pickedPath);

            if (CanDecode(data))
                return new PreparedImage(pickedPath, new Bitmap(pickedPath));

            // Detected by the decode failing rather than by the extension, so a HEIC someone
            // renamed to .jpg is still handled. Mirrors ReceiptImageHelper.PreprocessForOcr.
            var asJpeg = HeifImageDecoder.TryConvertToJpeg(data);
            if (asJpeg == null)
                return null;

            var converted = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ArgoBooks",
                "PickedImages",
                $"{System.IO.Path.GetFileNameWithoutExtension(pickedPath)}_{Guid.NewGuid():N}.jpg");

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(converted)!);
            File.WriteAllBytes(converted, asJpeg);

            return new PreparedImage(converted, new Bitmap(converted));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns bytes Skia can decode: the originals when they already decode, a JPEG when they
    /// had to be converted, or null when neither is possible.
    /// </summary>
    public static byte[]? TryMakeDecodable(byte[] data)
    {
        if (CanDecode(data))
            return data;

        return HeifImageDecoder.TryConvertToJpeg(data);
    }

    private static bool CanDecode(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            using var codec = SKCodec.Create(stream);
            return codec != null;
        }
        catch
        {
            return false;
        }
    }
}
