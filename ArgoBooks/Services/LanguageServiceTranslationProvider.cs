using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Translation provider that uses the LanguageService for translations.
/// This bridges the Core project's ITranslationProvider with the UI project's LanguageService.
/// </summary>
public class LanguageServiceTranslationProvider : ITranslationProvider
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly LanguageServiceTranslationProvider Instance = new();

    /// <summary>
    /// Translates the given text using the LanguageService.
    /// </summary>
    public string Translate(string text) => LanguageService.Instance.Translate(text);
}
