using ArgoBooks.Services;

namespace ArgoBooks.Localization;

/// <summary>
/// Extension methods for string localization.
/// </summary>
public static class StringExtensions
{
    /// <param name="text">The English text to translate.</param>
    extension(string text)
    {
        /// <summary>
        /// Translates the string using the current language.
        /// </summary>
        /// <returns>The translated text, or original if no translation found.</returns>
        /// <example>
        /// var message = "Save changes?".Translate();
        /// var error = "Customer {0} not found".Translate();
        /// </example>
        public string Translate()
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return LanguageService.Instance.Translate(text);
        }

        /// <summary>
        /// Translates the string using a specific language.
        /// </summary>
        /// <param name="isoCode">The target language ISO code (e.g., "fr", "de").</param>
        /// <returns>The translated text, or original if no translation found.</returns>
        /// <example>
        /// var frenchMessage = "Save".Translate("fr");
        /// </example>
        public string Translate(string isoCode)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return LanguageService.Instance.Translate(text, isoCode);
        }

        /// <summary>
        /// Translates the string and formats it with arguments.
        /// </summary>
        /// <param name="args">Format arguments.</param>
        /// <returns>The translated and formatted text.</returns>
        /// <example>
        /// var message = "Customer {0} created successfully".TranslateFormat("John Doe");
        /// var error = "Error on line {0}: {1}".TranslateFormat(42, "Invalid syntax");
        /// </example>
        public string TranslateFormat(params object?[] args)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var translated = LanguageService.Instance.Translate(text);
            return string.Format(translated, args);
        }
        
        public string TranslateFormat(object? arg1)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return string.Format(LanguageService.Instance.Translate(text), arg1);
        }

        public string TranslateFormat(object? arg1, object? arg2)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return string.Format(LanguageService.Instance.Translate(text), arg1, arg2);
        }

        public string TranslateFormat(object? arg1, object? arg2, object? arg3)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return string.Format(LanguageService.Instance.Translate(text), arg1, arg2, arg3);
        }

        /// <summary>
        /// Checks if a translation exists for the string in the current language.
        /// </summary>
        /// <returns>True if a translation exists.</returns>
        public bool HasTranslation()
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return LanguageService.Instance.HasTranslation(text);
        }

        /// <summary>
        /// Gets the translation key for a string.
        /// Useful for debugging or for the TranslationGenerator.
        /// </summary>
        /// <returns>The translation key that would be used for this string.</returns>
        public string GetTranslationKey()
        {
            return LanguageService.GetStringKey(text);
        }
    }
}
