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
    private IGlobalSettingsService? _globalSettingsService;

    /// <summary>
    /// Gets the singleton instance of the ThemeService.
    /// </summary>
    public static ThemeService Instance => _instance ??= new ThemeService();

    // Accent color definitions: (Primary, Hover, Light, Dark, Secondary/Gradient, IconBg)
    private static readonly Dictionary<string, AccentColorSet> AccentColors = new()
    {
        ["Blue"] = new AccentColorSet(
            Primary: Color.Parse("#3B82F6"),
            Hover: Color.Parse("#2563EB"),
            Light: Color.Parse("#DBEAFE"),
            Dark: Color.Parse("#1D4ED8"),
            Secondary: Color.Parse("#6366F1"),  // Indigo for gradient
            IconBgLight: Color.Parse("#EFF6FF"),
            IconBgDark: Color.Parse("#1E3A5F")
        ),
        ["Green"] = new AccentColorSet(
            Primary: Color.Parse("#10B981"),
            Hover: Color.Parse("#059669"),
            Light: Color.Parse("#D1FAE5"),
            Dark: Color.Parse("#047857"),
            Secondary: Color.Parse("#14B8A6"),  // Teal for gradient
            IconBgLight: Color.Parse("#ECFDF5"),
            IconBgDark: Color.Parse("#134E4A")
        ),
        ["Purple"] = new AccentColorSet(
            Primary: Color.Parse("#8B5CF6"),
            Hover: Color.Parse("#7C3AED"),
            Light: Color.Parse("#EDE9FE"),
            Dark: Color.Parse("#6D28D9"),
            Secondary: Color.Parse("#EC4899"),  // Pink for gradient
            IconBgLight: Color.Parse("#F5F3FF"),
            IconBgDark: Color.Parse("#3B2066")
        ),
        ["Pink"] = new AccentColorSet(
            Primary: Color.Parse("#EC4899"),
            Hover: Color.Parse("#DB2777"),
            Light: Color.Parse("#FCE7F3"),
            Dark: Color.Parse("#BE185D"),
            Secondary: Color.Parse("#F472B6"),  // Light pink for gradient
            IconBgLight: Color.Parse("#FDF2F8"),
            IconBgDark: Color.Parse("#5C1A3D")
        ),
        ["Orange"] = new AccentColorSet(
            Primary: Color.Parse("#F97316"),
            Hover: Color.Parse("#EA580C"),
            Light: Color.Parse("#FFEDD5"),
            Dark: Color.Parse("#C2410C"),
            Secondary: Color.Parse("#FBBF24"),  // Yellow for gradient
            IconBgLight: Color.Parse("#FFF7ED"),
            IconBgDark: Color.Parse("#5C2E0A")
        ),
        ["Teal"] = new AccentColorSet(
            Primary: Color.Parse("#14B8A6"),
            Hover: Color.Parse("#0D9488"),
            Light: Color.Parse("#CCFBF1"),
            Dark: Color.Parse("#0F766E"),
            Secondary: Color.Parse("#06B6D4"),  // Cyan for gradient
            IconBgLight: Color.Parse("#F0FDFA"),
            IconBgDark: Color.Parse("#134E4A")
        )
    };

    private record AccentColorSet(
        Color Primary,
        Color Hover,
        Color Light,
        Color Dark,
        Color Secondary,
        Color IconBgLight,
        Color IconBgDark
    );

    /// <inheritdoc />
    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Dark;

    /// <inheritdoc />
    public bool IsDarkTheme
    {
        get
        {
            if (CurrentTheme == ThemeMode.System)
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
            return CurrentTheme == ThemeMode.Dark;
        }
    }

    /// <inheritdoc />
    public event EventHandler<ThemeMode>? ThemeChanged;

    /// <inheritdoc />
    public void SetTheme(ThemeMode theme)
    {
        if (CurrentTheme == theme)
            return;

        CurrentTheme = theme;
        ApplyTheme();
        SaveToSettings();
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
    /// Sets the global settings service for theme persistence.
    /// </summary>
    /// <param name="settingsService">The global settings service.</param>
    public void SetGlobalSettingsService(IGlobalSettingsService? settingsService)
    {
        _globalSettingsService = settingsService;
    }

    /// <summary>
    /// Loads theme and accent color from global settings.
    /// </summary>
    public void LoadFromSettings()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Ui != null)
        {
            CurrentTheme = settings.Ui.Theme switch
            {
                "Light" => ThemeMode.Light,
                "Dark" => ThemeMode.Dark,
                "System" => ThemeMode.System,
                _ => ThemeMode.Dark
            };
            CurrentAccentColor = AccentColors.ContainsKey(settings.Ui.AccentColor)
                ? settings.Ui.AccentColor
                : "Blue";
        }
    }

    /// <summary>
    /// Saves current theme and accent color to global settings.
    /// </summary>
    private void SaveToSettings()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings != null)
        {
            settings.Ui.Theme = CurrentThemeName;
            settings.Ui.AccentColor = CurrentAccentColor;
            _globalSettingsService?.SaveSettings(settings);
        }
    }

    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    public string CurrentThemeName => CurrentTheme switch
    {
        ThemeMode.Light => "Light",
        ThemeMode.Dark => "Dark",
        ThemeMode.System => "System",
        _ => "System"
    };

    /// <summary>
    /// Gets or sets the current accent color name.
    /// </summary>
    public string CurrentAccentColor { get; private set; } = "Blue";

    /// <summary>
    /// Sets the accent color by name.
    /// </summary>
    /// <param name="colorName">The color name (Blue, Green, Purple, Pink, Orange, Teal).</param>
    public void SetAccentColor(string colorName)
    {
        if (!AccentColors.ContainsKey(colorName))
            return;

        CurrentAccentColor = colorName;
        ApplyAccentColor();
        SaveToSettings();
    }

    private void ApplyAccentColor()
    {
        var app = Application.Current;
        if (app == null || !AccentColors.TryGetValue(CurrentAccentColor, out var colors))
            return;

        // Update primary colors
        app.Resources["PrimaryColor"] = colors.Primary;
        app.Resources["PrimaryHoverColor"] = colors.Hover;
        app.Resources["PrimaryLightColor"] = colors.Light;
        app.Resources["PrimaryDarkColor"] = colors.Dark;

        // Update primary brushes
        app.Resources["PrimaryBrush"] = new SolidColorBrush(colors.Primary);
        app.Resources["PrimaryHoverBrush"] = new SolidColorBrush(colors.Hover);
        app.Resources["PrimaryLightBrush"] = new SolidColorBrush(colors.Light);
        app.Resources["PrimaryDarkBrush"] = new SolidColorBrush(colors.Dark);

        // Update accent/secondary colors (used in gradients)
        app.Resources["AccentColor"] = colors.Secondary;
        app.Resources["AccentBrush"] = new SolidColorBrush(colors.Secondary);

        // Update sidebar active color (used for navigation buttons)
        app.Resources["SidebarActiveColor"] = colors.Primary;
        app.Resources["SidebarActiveBrush"] = new SolidColorBrush(colors.Primary);

        // Update input focus border color
        app.Resources["InputFocusBorderColor"] = colors.Primary;
        app.Resources["InputFocusBorderBrush"] = new SolidColorBrush(colors.Primary);

        // Update icon background colors (depends on current theme)
        var iconBgColor = IsDarkTheme ? colors.IconBgDark : colors.IconBgLight;
        app.Resources["PrimaryIconBgColor"] = iconBgColor;
        app.Resources["PrimaryIconBgBrush"] = new SolidColorBrush(iconBgColor);

        // Update FluentTheme Slider resources
        app.Resources["SliderThumbBackground"] = new SolidColorBrush(colors.Primary);
        app.Resources["SliderThumbBackgroundPointerOver"] = new SolidColorBrush(colors.Hover);
        app.Resources["SliderThumbBackgroundPressed"] = new SolidColorBrush(colors.Dark);
        app.Resources["SliderTrackValueFill"] = new SolidColorBrush(colors.Primary);
        app.Resources["SliderTrackValueFillPointerOver"] = new SolidColorBrush(colors.Hover);
        app.Resources["SliderTrackValueFillPressed"] = new SolidColorBrush(colors.Dark);
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

        // Load saved settings if available
        LoadFromSettings();

        ApplyTheme();
        ApplyAccentColor();
    }

    private void ApplyTheme()
    {
        var app = Application.Current;
        if (app == null)
            return;

        app.RequestedThemeVariant = CurrentTheme switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.System => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };

        // Reapply accent colors since icon backgrounds depend on theme
        ApplyAccentColor();
    }

    private void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        // Only notify if we're following system theme
        if (CurrentTheme == ThemeMode.System)
        {
            ThemeChanged?.Invoke(this, CurrentTheme);
        }
    }
}
