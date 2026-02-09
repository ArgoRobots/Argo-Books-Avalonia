using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks;

/// <summary>
/// Converters for the Version History modal.
/// </summary>
public static class VersionHistoryConverters
{
    /// <summary>
    /// Converts IsUndone (bool) to opacity (1.0 or 0.5).
    /// </summary>
    public static readonly IValueConverter UndoneOpacity =
        new FuncValueConverter<bool, double>(isUndone => isUndone ? 0.5 : 1.0);

    /// <summary>
    /// Converts IsUndone (bool) to TextDecorations (Strikethrough or none).
    /// </summary>
    public static readonly IValueConverter UndoneStrikethrough =
        new FuncValueConverter<bool, TextDecorationCollection?>(isUndone =>
            isUndone ? TextDecorations.Strikethrough : null);
}
