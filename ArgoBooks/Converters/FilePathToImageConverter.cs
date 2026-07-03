using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that loads a Bitmap image from a file path.
/// </summary>
public class FilePathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
            return null;

        try
        {
            if (!File.Exists(filePath))
                return null;

            // Optional target width (ConverterParameter): decode a downscaled thumbnail instead of
            // the full image. Receipt photos are often multi-MB, and decoding them at full resolution
            // for a small card is slow and memory-heavy. With no parameter it decodes full-size (used
            // by the full-screen receipt viewer).
            if (parameter is not null && int.TryParse(parameter.ToString(), out var width) && width > 0)
            {
                using var stream = File.OpenRead(filePath);
                return Bitmap.DecodeToWidth(stream, width);
            }

            return new Bitmap(filePath);
        }
        catch
        {
            // Failed to load image
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
