using ArgoBooks.Core;
using ArgoBooks.Core.Enums;
using Avalonia.Data.Converters;
using Avalonia.Media;

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

    /// <summary>
    /// Converts a ProcessingTier enum to a color for tier badge display.
    /// </summary>
    public static readonly IValueConverter TierToColor =
        new FuncValueConverter<ProcessingTier, Color>(tier => tier switch
        {
            ProcessingTier.Tier1_Mapping => Color.Parse(AppColors.Primary),
            ProcessingTier.Tier2_LlmProcessing => Color.Parse("#7C3AED"),
            _ => Color.Parse(AppColors.GrayText)
        });

    /// <summary>
    /// Converts a confidence level string ("High", "Medium", "Low") to a color.
    /// </summary>
    public static readonly IValueConverter ConfidenceLevelToColor =
        new FuncValueConverter<string, Color>(level => level switch
        {
            "High" => Color.Parse(AppColors.SuccessText),
            "Medium" => Color.Parse(AppColors.WarningText),
            "Low" => Color.Parse(AppColors.Error),
            _ => Color.Parse(AppColors.GrayText)
        });
}
