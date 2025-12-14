using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Service for managing application theme with Avalonia support.
/// </summary>
public class ThemeService : IThemeService
{
    private static ThemeService? _instance;

    /// <summary>
    /// Gets the singleton instance of the ThemeService.
    /// </summary>
    public static ThemeService Instance => _instance ??= new ThemeService();

    private ThemeMode _currentTheme = ThemeMode.Dark;
    private string _currentAccentColor = "Blue";

    // Accent color definitions: (Primary, Hover, Light, Dark)
    private static readonly Dictionary<string, (Color Primary, Color Hover, Color Light, Color Dark)> AccentColors = new()
    {
        ["Blue"] = (Color.Parse("#3B82F6"), Color.Parse("#2563EB"), Color.Parse("#DBEAFE"), Color.Parse("#1D4ED8")),
        ["Green"] = (Color.Parse("#10B981"), Color.Parse("#059669"), Color.Parse("#D1FAE5"), Color.Parse("#047857")),
        ["Purple"] = (Color.Parse("#8B5CF6"), Color.Parse("#7C3AED"), Color.Parse("#EDE9FE"), Color.Parse("#6D28D9")),
        ["Pink"] = (Color.Parse("#EC4899"), Color.Parse("#DB2777"), Color.Parse("#FCE7F3"), Color.Parse("#BE185D")),
        ["Orange"] = (Color.Parse("#F97316"), Color.Parse("#EA580C"), Color.Parse("#FFEDD5"), Color.Parse("#C2410C")),
        ["Teal"] = (Color.Parse("#14B8A6"), Color.Parse("#0D9488"), Color.Parse("#CCFBF1"), Color.Parse("#0F766E"))
    };

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
    /// Sets the theme by name (Light, Dark, System).
    /// </summary>
    /// <param name="themeName">The theme name.</param>
    public void SetTheme(string themeName)
    {
        var theme = themeName switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            "System" => ThemeMode.System,
            _ => ThemeMode.System
        };
        SetTheme(theme);
    }

    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    public string CurrentThemeName => _currentTheme switch
    {
        ThemeMode.Light => "Light",
        ThemeMode.Dark => "Dark",
        ThemeMode.System => "System",
        _ => "System"
    };

    /// <summary>
    /// Gets or sets the current accent color name.
    /// </summary>
    public string CurrentAccentColor => _currentAccentColor;

    /// <summary>
    /// Sets the accent color by name.
    /// </summary>
    /// <param name="colorName">The color name (Blue, Green, Purple, Pink, Orange, Teal).</param>
    public void SetAccentColor(string colorName)
    {
        if (!AccentColors.ContainsKey(colorName))
            return;

        _currentAccentColor = colorName;
        ApplyAccentColor();
    }

    private void ApplyAccentColor()
    {
        var app = Application.Current;
        if (app == null || !AccentColors.TryGetValue(_currentAccentColor, out var colors))
            return;

        // Update the application resources
        app.Resources["PrimaryColor"] = colors.Primary;
        app.Resources["PrimaryHoverColor"] = colors.Hover;
        app.Resources["PrimaryLightColor"] = colors.Light;
        app.Resources["PrimaryDarkColor"] = colors.Dark;

        // Update the brushes
        app.Resources["PrimaryBrush"] = new SolidColorBrush(colors.Primary);
        app.Resources["PrimaryHoverBrush"] = new SolidColorBrush(colors.Hover);
        app.Resources["PrimaryLightBrush"] = new SolidColorBrush(colors.Light);
        app.Resources["PrimaryDarkBrush"] = new SolidColorBrush(colors.Dark);
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
