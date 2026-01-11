using System.Globalization;
using Avalonia.Data.Converters;
using ArgoBooks.Services;

namespace ArgoBooks.Localization;

/// <summary>
/// Value converter that translates strings using the LanguageService.
/// Use this in ItemTemplates for ComboBox and other list controls.
/// </summary>
/// <example>
/// <![CDATA[
/// <ComboBox ItemsSource="{Binding Options}">
///     <ComboBox.ItemTemplate>
///         <DataTemplate>
///             <TextBlock Text="{Binding Converter={StaticResource TranslateConverter}}" />
///         </DataTemplate>
///     </ComboBox.ItemTemplate>
/// </ComboBox>
/// ]]>
/// </example>
public class TranslateConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for easy use in XAML.
    /// </summary>
    public static readonly TranslateConverter Instance = new();

    /// <summary>
    /// Converts a string value to its translated equivalent.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && !string.IsNullOrEmpty(text))
        {
            return LanguageService.Instance.Translate(text);
        }
        return value;
    }

    /// <summary>
    /// Not supported - translations are one-way.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("TranslateConverter only supports one-way conversion");
    }
}
