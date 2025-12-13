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
