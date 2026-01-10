using System.ComponentModel;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using ArgoBooks.Services;

namespace ArgoBooks.Localization;

/// <summary>
/// XAML markup extension for localization.
/// Usage: {Loc Key} or {Loc 'Some English text'}
/// </summary>
/// <example>
/// <![CDATA[
/// <TextBlock Text="{Loc Save}"/>
/// <Button Content="{Loc 'Add New Customer'}"/>
/// <TextBlock Text="{Loc SettingsTitle}"/>
/// ]]>
/// </example>
public class LocExtension : MarkupExtension
{
    private string _key = string.Empty;

    /// <summary>
    /// The translation key or English text to translate.
    /// </summary>
    public string Key
    {
        get => _key;
        set => _key = value ?? string.Empty;
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public LocExtension()
    {
    }

    /// <summary>
    /// Constructor with key parameter.
    /// </summary>
    /// <param name="key">The translation key or English text.</param>
    public LocExtension(string key)
    {
        _key = key ?? string.Empty;
    }

    /// <summary>
    /// Provides the translated value.
    /// </summary>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_key))
            return string.Empty;

        // Create a binding to the localization source
        var binding = new Binding
        {
            Source = LocalizationSource.Instance,
            Path = $"[{_key}]",
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}

/// <summary>
/// Observable source for localized strings.
/// Provides property indexer for binding in XAML.
/// </summary>
public class LocalizationSource : INotifyPropertyChanged
{
    private static LocalizationSource? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static LocalizationSource Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LocalizationSource();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Event raised when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Private constructor for singleton pattern.
    /// </summary>
    private LocalizationSource()
    {
        // Subscribe to language changes
        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Gets the translated string for a key.
    /// </summary>
    /// <param name="key">The translation key or English text.</param>
    /// <returns>The translated string.</returns>
    public string this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return LanguageService.Instance.Translate(key);
        }
    }

    /// <summary>
    /// Handles language change events.
    /// </summary>
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Notify all bindings that translations have changed
        // Using Indexer property to force re-evaluation of all Loc bindings
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
    }

    /// <summary>
    /// Forces a refresh of all localized strings.
    /// </summary>
    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
    }
}

/// <summary>
/// Static helper class for quick translations in code.
/// </summary>
public static class Loc
{
    /// <summary>
    /// Translates an English string using the current language.
    /// </summary>
    /// <param name="text">The English text to translate.</param>
    /// <returns>The translated text.</returns>
    public static string Tr(string text) => LanguageService.Instance.Translate(text);

    /// <summary>
    /// Translates an English string with format arguments.
    /// </summary>
    /// <param name="text">The English text to translate (with format placeholders).</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The translated and formatted text.</returns>
    public static string Tr(string text, params object[] args)
    {
        var translated = LanguageService.Instance.Translate(text);
        return string.Format(translated, args);
    }

    /// <summary>
    /// Gets the current language name.
    /// </summary>
    public static string CurrentLanguage => LanguageService.Instance.CurrentLanguage;

    /// <summary>
    /// Gets the current language ISO code.
    /// </summary>
    public static string CurrentIsoCode => LanguageService.Instance.CurrentIsoCode;

    /// <summary>
    /// Gets whether the current language is English.
    /// </summary>
    public static bool IsEnglish => LanguageService.Instance.IsEnglish;
}
