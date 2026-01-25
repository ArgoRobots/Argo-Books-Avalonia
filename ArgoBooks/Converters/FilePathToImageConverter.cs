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
            if (File.Exists(filePath))
            {
                return new Bitmap(filePath);
            }
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
