using Avalonia;
using Avalonia.Styling;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Service for managing application theme with Avalonia support.
/// </summary>
public class ThemeService : IThemeService
{
    private ThemeMode _currentTheme = ThemeMode.System;

    /// <inheritdoc />
    public ThemeMode CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public bool IsDarkTheme
    {
        get
        {
            if (_currentTheme == ThemeMode.System)
            {
                // Check the actual theme variant from Avalonia
                var app = Application.Current;
                if (app != null)
                {
                    var actualTheme = app.ActualThemeVariant;
                    return actualTheme == ThemeVariant.Dark;
                }
                return false;
            }
            return _currentTheme == ThemeMode.Dark;
        }
    }

    /// <inheritdoc />
    public event EventHandler<ThemeMode>? ThemeChanged;

    /// <inheritdoc />
    public void SetTheme(ThemeMode theme)
    {
        if (_currentTheme == theme)
            return;

        _currentTheme = theme;
        ApplyTheme();
        ThemeChanged?.Invoke(this, theme);
    }

    /// <summary>
    /// Initializes the theme service and sets up system theme detection.
    /// </summary>
    public void Initialize()
    {
        var app = Application.Current;
        if (app != null)
        {
            // Subscribe to system theme changes
            app.ActualThemeVariantChanged += OnSystemThemeChanged;
        }
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var app = Application.Current;
        if (app == null)
            return;

        app.RequestedThemeVariant = _currentTheme switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.System => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
    }

    private void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        // Only notify if we're following system theme
        if (_currentTheme == ThemeMode.System)
        {
            ThemeChanged?.Invoke(this, _currentTheme);
        }
    }
}
