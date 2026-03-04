namespace ArgoBooks.Core.Services;

/// <summary>
/// Defines the available theme modes.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Follow system theme setting.
    /// </summary>
    System,

    /// <summary>
    /// Always use light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Always use dark theme.
    /// </summary>
    Dark
}

/// <summary>
/// Extension methods for ThemeMode.
/// </summary>
public static class ThemeModeExtensions
{
    /// <summary>
    /// Gets the display name for a theme mode.
    /// </summary>
    public static string GetDisplayName(this ThemeMode mode)
    {
        return mode switch
        {
            ThemeMode.Light => "Light",
            ThemeMode.Dark => "Dark",
            _ => "System"
        };
    }

    /// <summary>
    /// Parses a theme name string to a ThemeMode enum value.
    /// </summary>
    public static ThemeMode ParseThemeMode(string? name)
    {
        return name switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            "System" => ThemeMode.System,
            _ => ThemeMode.Dark
        };
    }

    /// <summary>
    /// Gets all theme mode display names for UI options.
    /// </summary>
    public static string[] GetAllDisplayNames()
    {
        return
        [
            ThemeMode.Light.GetDisplayName(),
            ThemeMode.Dark.GetDisplayName(),
            ThemeMode.System.GetDisplayName()
        ];
    }
}

/// <summary>
/// Service for managing application theme.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme mode.
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets whether the current effective theme is dark.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Sets the theme mode.
    /// </summary>
    /// <param name="theme">The theme mode to apply.</param>
    void SetTheme(ThemeMode theme);

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeMode>? ThemeChanged;
}
