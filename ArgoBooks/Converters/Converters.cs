using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Static class providing common converter instances for XAML binding.
/// </summary>
public static class Converters
{
    /// <summary>
    /// Multi-value converter for theme border brush that updates when accent color changes.
    /// </summary>
    public static readonly IMultiValueConverter ThemeBorderBrushMulti = new ThemeBorderBrushMultiConverter();

    /// <summary>
    /// Converts a boolean to "Finish" or "Next" text.
    /// </summary>
    public static readonly IValueConverter BoolToFinishNext = new BoolToFixedStringConverter("Finish", "Next");
}
