using ArgoBooks.Services;

namespace ArgoBooks.Localization;

/// <summary>
/// Extension methods for string localization.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Translates the string using the current language.
    /// </summary>
    /// <param name="text">The English text to translate.</param>
    /// <returns>The translated text, or original if no translation found.</returns>
    /// <example>
    /// var message = "Save changes?".Translate();
    /// var error = "Customer {0} not found".Translate();
    /// </example>
    public static string Translate(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return LanguageService.Instance.Translate(text);
    }

    /// <summary>
    /// Translates the string using a specific language.
    /// </summary>
    /// <param name="text">The English text to translate.</param>
    /// <param name="isoCode">The target language ISO code (e.g., "fr", "de").</param>
    /// <returns>The translated text, or original if no translation found.</returns>
    /// <example>
    /// var frenchMessage = "Save".Translate("fr");
    /// </example>
    public static string Translate(this string text, string isoCode)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return LanguageService.Instance.Translate(text, isoCode);
    }

    /// <summary>
    /// Translates the string and formats it with arguments.
    /// </summary>
    /// <param name="text">The English text to translate (with format placeholders).</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The translated and formatted text.</returns>
    /// <example>
    /// var message = "Customer {0} created successfully".TranslateFormat("John Doe");
    /// var error = "Error on line {0}: {1}".TranslateFormat(42, "Invalid syntax");
    /// </example>
    public static string TranslateFormat(this string text, params object[] args)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var translated = LanguageService.Instance.Translate(text);
        return string.Format(translated, args);
    }

    /// <summary>
    /// Checks if a translation exists for the string in the current language.
    /// </summary>
    /// <param name="text">The English text to check.</param>
    /// <returns>True if a translation exists.</returns>
    public static bool HasTranslation(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return LanguageService.Instance.HasTranslation(text);
    }

    /// <summary>
    /// Gets the translation key for a string.
    /// Useful for debugging or for the TranslationGenerator.
    /// </summary>
    /// <param name="text">The English text.</param>
    /// <returns>The translation key that would be used for this string.</returns>
    public static string GetTranslationKey(this string text)
    {
        return LanguageService.GetStringKey(text);
    }
}
