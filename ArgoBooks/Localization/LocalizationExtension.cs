using System.ComponentModel;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using ArgoBooks.Services;

namespace ArgoBooks.Localization;

/// <summary>
/// XAML markup extension for localization.
/// Usage: {loc:Loc Key} or {loc:Loc 'Some English text'}
/// </summary>
public class LocExtension : MarkupExtension
{
    private string _key = string.Empty;

    public string Key
    {
        get => _key;
        set => _key = value ?? string.Empty;
    }

    public LocExtension() { }

    public LocExtension(string key)
    {
        _key = key ?? string.Empty;
    }

    public LocExtension(string part1, string part2)
    {
        _key = $"{part1}'{part2}";
    }

    public LocExtension(string part1, string part2, string part3)
    {
        _key = $"{part1}'{part2}'{part3}";
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_key))
            return string.Empty;

        // Get the target object and property
        var provideTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        if (provideTarget?.TargetObject is AvaloniaObject targetObject &&
            provideTarget.TargetProperty is AvaloniaProperty targetProperty)
        {
            // Register this binding for refresh on language change
            LocalizationManager.Register(targetObject, targetProperty, _key);

            // Return the current translation
            return LanguageService.Instance.Translate(_key);
        }

        // Fallback: return translated value directly
        return LanguageService.Instance.Translate(_key);
    }
}

/// <summary>
/// Manages localization bindings and refreshes them when language changes.
/// </summary>
public static class LocalizationManager
{
    private static readonly List<WeakReference<LocalizationBinding>> _bindings = new();
    private static bool _initialized;

    private class LocalizationBinding
    {
        public WeakReference<AvaloniaObject> Target { get; set; } = null!;
        public AvaloniaProperty Property { get; set; } = null!;
        public string Key { get; set; } = null!;
    }

    public static void Register(AvaloniaObject target, AvaloniaProperty property, string key)
    {
        EnsureInitialized();

        lock (_bindings)
        {
            _bindings.Add(new WeakReference<LocalizationBinding>(new LocalizationBinding
            {
                Target = new WeakReference<AvaloniaObject>(target),
                Property = property,
                Key = key
            }));
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
        _initialized = true;
    }

    private static void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[LOC-MGR] Language changed to {e.NewLanguage}. Refreshing bindings...");

        Dispatcher.UIThread.Post(() =>
        {
            RefreshAllBindings();
        }, DispatcherPriority.Normal);
    }

    private static void RefreshAllBindings()
    {
        List<LocalizationBinding> validBindings = new();

        lock (_bindings)
        {
            // Collect valid bindings and remove dead ones
            var toRemove = new List<WeakReference<LocalizationBinding>>();

            foreach (var weakRef in _bindings)
            {
                if (weakRef.TryGetTarget(out var binding) &&
                    binding.Target.TryGetTarget(out var target))
                {
                    validBindings.Add(binding);
                }
                else
                {
                    toRemove.Add(weakRef);
                }
            }

            foreach (var dead in toRemove)
            {
                _bindings.Remove(dead);
            }
        }

        System.Diagnostics.Debug.WriteLine($"[LOC-MGR] Refreshing {validBindings.Count} bindings");

        // Update all valid bindings
        foreach (var binding in validBindings)
        {
            if (binding.Target.TryGetTarget(out var target))
            {
                var translated = LanguageService.Instance.Translate(binding.Key);
                target.SetValue(binding.Property, translated);
            }
        }

        System.Diagnostics.Debug.WriteLine($"[LOC-MGR] Refresh complete");
    }
}

/// <summary>
/// Static helper class for quick translations in code.
/// </summary>
public static class Loc
{
    public static string Tr(string text) => LanguageService.Instance.Translate(text);

    public static string Tr(string text, params object[] args)
    {
        var translated = LanguageService.Instance.Translate(text);
        return string.Format(translated, args);
    }

    public static string CurrentLanguage => LanguageService.Instance.CurrentLanguage;
    public static string CurrentIsoCode => LanguageService.Instance.CurrentIsoCode;
    public static bool IsEnglish => LanguageService.Instance.IsEnglish;
}
