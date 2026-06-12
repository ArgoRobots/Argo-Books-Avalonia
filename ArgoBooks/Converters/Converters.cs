using ArgoBooks.Core;
using ArgoBooks.Core.Models.AI;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

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
    /// Converts a boolean to "Finish" or "Next" text. Used by tutorial overlays
    /// where the last step actually ends the flow (Products, Categories).
    /// </summary>
    public static readonly IValueConverter BoolToFinishNext = new BoolToFixedStringConverter("Finish", "Next");

    /// <summary>
    /// Whether the app is currently using the dark theme. Badge colors that are tuned
    /// for light surfaces look muddy on dark backgrounds, so we brighten them in dark mode.
    /// </summary>
    private static bool IsDarkTheme =>
        Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>
    /// Converts a ProcessingTier enum to a color for tier badge display.
    /// </summary>
    public static readonly IValueConverter TierToColor =
        new FuncValueConverter<ProcessingTier, Color>(tier => tier switch
        {
            ProcessingTier.Tier1_Mapping => Color.Parse(IsDarkTheme ? "#60A5FA" : AppColors.Primary),
            ProcessingTier.Tier2_LlmProcessing => Color.Parse(IsDarkTheme ? "#A78BFA" : "#7C3AED"),
            _ => Color.Parse(IsDarkTheme ? "#9CA3AF" : AppColors.GrayText)
        });

    /// <summary>
    /// Converts a confidence level string ("High", "Medium", "Low") to a color.
    /// </summary>
    public static readonly IValueConverter ConfidenceLevelToColor =
        new FuncValueConverter<string, Color>(level => level switch
        {
            "High" => Color.Parse(IsDarkTheme ? "#4ADE80" : AppColors.SuccessDark),
            "Medium" => Color.Parse(IsDarkTheme ? "#FBBF24" : AppColors.WarningDark),
            "Low" => Color.Parse(IsDarkTheme ? "#F87171" : AppColors.Error),
            _ => Color.Parse(IsDarkTheme ? "#9CA3AF" : AppColors.GrayText)
        });
}
