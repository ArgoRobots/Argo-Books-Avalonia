using PDFtoImage;
using SkiaSharp;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Shared image compression and format utilities for receipt scanning services.
/// </summary>
public static class ReceiptImageHelper
{
    /// <summary>
    /// Renders the first page of a PDF as a JPEG image.
    /// TODO: Replace with Avalonia WebView when it ships in the free tier to save exe size.
    /// Returns null if the PDF cannot be rendered.
    /// </summary>
    public static byte[]? RenderPdfFirstPage(byte[] pdfData, int dpi = 150)
    {
        try
        {
            using var output = new MemoryStream();
            using var pdfStream = new MemoryStream(pdfData);
            Conversion.SaveJpeg(output, pdfStream, page: 0, leaveOpen: false, password: null,
                options: new RenderOptions(Dpi: dpi));
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

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

    /// <summary>
    /// Applies EXIF orientation correction so the image displays right-side up.
    /// Returns the original bytes if no rotation is needed or the format is unsupported.
    /// </summary>
    public static byte[] FixOrientation(byte[] imageData)
    {
        using var codec = SKCodec.Create(new MemoryStream(imageData));
        if (codec == null)
            return imageData;

        var origin = codec.EncodedOrigin;
        if (origin == SKEncodedOrigin.TopLeft)
            return imageData; // Already correct

        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            return imageData;

        var swapDims = origin is SKEncodedOrigin.LeftBottom or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom;
        var outWidth = swapDims ? original.Height : original.Width;
        var outHeight = swapDims ? original.Width : original.Height;

        using var surface = SKSurface.Create(new SKImageInfo(outWidth, outHeight));
        var canvas = surface.Canvas;
        ApplyExifTransform(canvas, origin, original.Width, original.Height, outWidth, outHeight);
        canvas.DrawBitmap(original, 0, 0);

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, 90);
        return encoded.ToArray();
    }

    /// <summary>
    /// Preprocesses a receipt image to improve OCR accuracy.
    /// Applies EXIF orientation fix, contrast boost, and sharpening.
    /// PDFs are returned unchanged.
    /// </summary>
    public static byte[] PreprocessForOcr(byte[] imageData, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf")
            return imageData;

        // Use SKCodec to read EXIF orientation, then decode with correct rotation applied.
        using var codec = SKCodec.Create(new MemoryStream(imageData));
        if (codec == null)
            return imageData;

        var origin = codec.EncodedOrigin;
        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            return imageData;

        // Determine output dimensions — swap width/height for 90°/270° rotations.
        var swapDims = origin is SKEncodedOrigin.LeftBottom or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom;
        var outWidth = swapDims ? original.Height : original.Width;
        var outHeight = swapDims ? original.Width : original.Height;

        // Mild contrast boost (1.2x) to help faded thermal receipts.
        // Keeps color intact — vision models use color to parse receipts.
        const float contrast = 1.2f;
        const float bias = (1f - contrast) / 2f;
        float[] contrastMatrix =
        [
            contrast, 0,        0,        0, bias,
            0,        contrast, 0,        0, bias,
            0,        0,        contrast, 0, bias,
            0,        0,        0,        1, 0
        ];
        var colorFilter = SKColorFilter.CreateColorMatrix(contrastMatrix);

        // Light sharpen to improve text edge clarity for blurry phone photos.
        var sharpenKernel = new float[]
        {
             0, -0.5f,  0,
            -0.5f,  3, -0.5f,
             0, -0.5f,  0
        };
        var sharpenFilter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(3, 3),
            sharpenKernel,
            gain: 1f,
            bias: 0f,
            kernelOffset: new SKPointI(1, 1),
            tileMode: SKShaderTileMode.Clamp,
            convolveAlpha: false);

        using var surface = SKSurface.Create(new SKImageInfo(outWidth, outHeight));
        var canvas = surface.Canvas;

        ApplyExifTransform(canvas, origin, original.Width, original.Height, outWidth, outHeight);

        using var paint = new SKPaint();
        paint.ColorFilter = colorFilter;
        paint.ImageFilter = sharpenFilter;
        canvas.DrawBitmap(original, 0, 0, paint);

        using var snapshot = surface.Snapshot();
        // Match original file size — use quality 95 to avoid inflating compressed JPEGs.
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, 95);
        return encoded.ToArray();
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

    private static void ApplyExifTransform(SKCanvas canvas, SKEncodedOrigin origin,
        int srcWidth, int srcHeight, int outWidth, int outHeight)
    {
        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Scale(-1, 1, outWidth / 2f, 0);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.RotateDegrees(180, outWidth / 2f, outHeight / 2f);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Scale(1, -1, 0, outHeight / 2f);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90, 0, 0);
                canvas.Scale(1, -1, srcHeight / 2f, 0);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(outWidth, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(outWidth, 0);
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1, 0, srcWidth / 2f);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, outHeight);
                canvas.RotateDegrees(270);
                break;
        }
    }
}
