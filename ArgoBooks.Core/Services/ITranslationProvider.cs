namespace ArgoBooks.Core.Services;

/// <summary>
/// Interface for providing translations in the Core project.
/// Implementations should be provided by the UI project which has access to translation resources.
/// </summary>
public interface ITranslationProvider
{
    /// <summary>
    /// Translates the given English text to the current language.
    /// </summary>
    /// <param name="text">The English text to translate.</param>
    /// <returns>The translated text, or the original if no translation is found.</returns>
    string Translate(string text);
}

/// <summary>
/// Default translation provider that returns the original text.
/// Used when no translation provider is configured.
/// </summary>
public class DefaultTranslationProvider : ITranslationProvider
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly DefaultTranslationProvider Instance = new();

    /// <summary>
    /// Returns the original text without translation.
    /// </summary>
    public string Translate(string text) => text;
}
