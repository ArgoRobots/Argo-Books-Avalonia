using System.ComponentModel;
using System.Text;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
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
    /// Constructor for XAML escaped single quotes (e.g., 'You''re' becomes two parts).
    /// </summary>
    /// <param name="part1">First part of the string.</param>
    /// <param name="part2">Second part of the string.</param>
    public LocExtension(string part1, string part2)
    {
        _key = $"{part1}'{part2}";
    }

    /// <summary>
    /// Constructor for XAML escaped single quotes with two escapes (e.g., 'It''s a ''test''').
    /// </summary>
    /// <param name="part1">First part of the string.</param>
    /// <param name="part2">Second part of the string.</param>
    /// <param name="part3">Third part of the string.</param>
    public LocExtension(string part1, string part2, string part3)
    {
        _key = $"{part1}'{part2}'{part3}";
    }

    /// <summary>
    /// Provides the translated value.
    /// </summary>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_key))
            return string.Empty;

        // Encode the key as hex to avoid binding path parsing issues with spaces/special chars
        var encodedKey = EncodeKeyAsHex(_key);

        // Create a binding to the localization source using ReflectionBindingExtension
        var binding = new ReflectionBindingExtension($"[{encodedKey}]")
        {
            Source = LocalizationSource.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }

    /// <summary>
    /// Encodes a key as hexadecimal to avoid binding path parsing issues.
    /// </summary>
    internal static string EncodeKeyAsHex(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Decodes a hex-encoded key back to the original string.
    /// </summary>
    internal static string DecodeKeyFromHex(string hexKey)
    {
        var bytes = Convert.FromHexString(hexKey);
        return Encoding.UTF8.GetString(bytes);
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
    private readonly HashSet<string> _usedKeys = new();

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
    /// Gets the translated string for a hex-encoded key.
    /// </summary>
    /// <param name="hexKey">The hex-encoded translation key.</param>
    /// <returns>The translated string.</returns>
    public string this[string hexKey]
    {
        get
        {
            if (string.IsNullOrEmpty(hexKey))
                return string.Empty;

            // Track used keys for targeted refresh
            _usedKeys.Add(hexKey);

            // Decode the hex key back to the original translation key
            var key = LocExtension.DecodeKeyFromHex(hexKey);
            var result = LanguageService.Instance.Translate(key);

            #if DEBUG
            // Only log first few to avoid spam
            if (_usedKeys.Count <= 20)
            {
                System.Diagnostics.Debug.WriteLine($"[LOC] '{key}' => '{result}' (lang: {LanguageService.Instance.CurrentIsoCode})");
            }
            #endif

            return result;
        }
    }

    /// <summary>
    /// Handles language change events.
    /// </summary>
    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[LOC] Language changed to {e.NewLanguageName} ({e.NewIsoCode}). Keys to refresh: {_usedKeys.Count}");

        // Ensure we're on the UI thread for binding updates
        if (Dispatcher.UIThread.CheckAccess())
        {
            NotifyAllBindings();
        }
        else
        {
            Dispatcher.UIThread.Post(NotifyAllBindings, DispatcherPriority.Normal);
        }
    }

    /// <summary>
    /// Notifies all bindings that translations have changed.
    /// </summary>
    private void NotifyAllBindings()
    {
        System.Diagnostics.Debug.WriteLine($"[LOC] NotifyAllBindings called with {_usedKeys.Count} keys");

        // Notify with null first (signals all properties changed)
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

        // Then notify each used key individually for more reliable updates
        foreach (var hexKey in _usedKeys)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{hexKey}]"));
        }

        System.Diagnostics.Debug.WriteLine($"[LOC] NotifyAllBindings completed");
    }

    /// <summary>
    /// Forces a refresh of all localized strings.
    /// </summary>
    public void Refresh()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            NotifyAllBindings();
        }
        else
        {
            Dispatcher.UIThread.Post(NotifyAllBindings, DispatcherPriority.Normal);
        }
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
