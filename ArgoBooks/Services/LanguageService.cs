using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using ArgoBooks.Core.Services;
using ArgoBooks.Data;

namespace ArgoBooks.Services;

/// <summary>
/// Service for managing application language and translations.
/// Downloads pre-translated JSON files from server and applies them to UI.
/// </summary>
public partial class LanguageService
{
    private static LanguageService? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of the LanguageService.
    /// </summary>
    public static LanguageService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LanguageService();
                }
            }
            return _instance;
        }
    }

    // HTTP client for downloading translations
    private readonly HttpClient _httpClient = new();

    // Translation caches
    private Dictionary<string, string> _englishCache = new();
    private Dictionary<string, Dictionary<string, string>> _translationCache = new();

    // Current language
    private string _currentLanguage = "English";
    private string _currentIsoCode = "en";

    // File paths
    private readonly string _cacheDirectory;
    private readonly string _translationsFilePath;
    private readonly string _englishFilePath;

    // Download URL template (version will be inserted)
    private const string DownloadUrlTemplate = "https://argorobots.com/resources/downloads/versions/{0}/languages/{1}.json";

    /// <summary>
    /// Gets the current language name (e.g., "English", "French").
    /// </summary>
    public string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Gets the current ISO language code (e.g., "en", "fr").
    /// </summary>
    public string CurrentIsoCode => _currentIsoCode;

    /// <summary>
    /// Gets whether the current language is English.
    /// </summary>
    public bool IsEnglish => _currentIsoCode == "en";

    /// <summary>
    /// Event raised when the language changes.
    /// </summary>
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    /// <summary>
    /// Event raised when translations are being downloaded.
    /// </summary>
    public event EventHandler<TranslationProgressEventArgs>? TranslationProgress;

    /// <summary>
    /// Regex for extracting string key from text.
    /// </summary>
    [GeneratedRegex(@"[^\w{}]")]
    private static partial Regex NonWordCharactersRegex();

    /// <summary>
    /// Private constructor for singleton pattern.
    /// </summary>
    private LanguageService()
    {
        // Set up cache directory in AppData
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDirectory = Path.Combine(appData, "ArgoBooks", "Languages");
        _translationsFilePath = Path.Combine(_cacheDirectory, "translations.json");
        _englishFilePath = Path.Combine(_cacheDirectory, "en.json");

        // Ensure directory exists
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Initializes the language service by loading cached translations.
    /// </summary>
    public void Initialize()
    {
        LoadCachedTranslations();
    }

    /// <summary>
    /// Loads cached translations from disk.
    /// </summary>
    private void LoadCachedTranslations()
    {
        try
        {
            // Load non-English translation cache from translations.json
            if (File.Exists(_translationsFilePath))
            {
                var content = File.ReadAllText(_translationsFilePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(content);
                    if (translations != null)
                    {
                        _translationCache = translations;
                    }
                }
            }

            // Also load individual language JSON files (e.g., fr.json, de.json) for easier testing
            LoadIndividualLanguageFiles();

            // Load English translations
            if (File.Exists(_englishFilePath))
            {
                var content = File.ReadAllText(_englishFilePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                    if (translations != null)
                    {
                        _englishCache = translations;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"LanguageService: Loaded {_englishCache.Count} English translations, {_translationCache.Count} other language caches");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Error loading cached translations: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads individual language JSON files from the cache directory.
    /// This allows placing files like fr.json, de.json directly in the Languages folder.
    /// </summary>
    private void LoadIndividualLanguageFiles()
    {
        try
        {
            var jsonFiles = Directory.GetFiles(_cacheDirectory, "*.json");
            foreach (var file in jsonFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                // Skip translations.json and en.json (handled separately)
                if (fileName == "translations" || fileName == "en")
                    continue;

                // Check if it's a valid ISO code
                if (!Languages.IsValidIsoCode(fileName))
                    continue;

                try
                {
                    var content = File.ReadAllText(file);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                        if (translations != null && translations.Count > 0)
                        {
                            // Merge with existing translations (file takes priority)
                            if (!_translationCache.TryGetValue(fileName, out var existing))
                            {
                                existing = new Dictionary<string, string>();
                                _translationCache[fileName] = existing;
                            }

                            foreach (var kvp in translations)
                            {
                                existing[kvp.Key] = kvp.Value;
                            }

                            System.Diagnostics.Debug.WriteLine($"LanguageService: Loaded {translations.Count} translations from {fileName}.json");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LanguageService: Error loading {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Error scanning language files: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the translation cache to disk.
    /// </summary>
    private void SaveCacheToFile()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonContent = JsonSerializer.Serialize(_translationCache, options);
            File.WriteAllText(_translationsFilePath, jsonContent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Error saving translation cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the current language and downloads translations if needed.
    /// </summary>
    /// <param name="languageName">The language name (e.g., "French").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if language was successfully changed.</returns>
    public async Task<bool> SetLanguageAsync(string languageName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(languageName))
            return false;

        if (!Languages.IsValidLanguage(languageName))
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Invalid language '{languageName}'");
            return false;
        }

        var isoCode = Languages.GetIsoCode(languageName);
        var previousLanguage = _currentLanguage;
        var previousIsoCode = _currentIsoCode;

        // Download translations if not already cached
        var downloadSuccess = await DownloadAndCacheLanguageAsync(languageName, false, cancellationToken);

        if (!downloadSuccess && isoCode != "en")
        {
            // If download failed and it's not English, check if we have cached translations
            if (!_translationCache.ContainsKey(isoCode))
            {
                System.Diagnostics.Debug.WriteLine($"LanguageService: Failed to download or find cached translations for {languageName}");
                return false;
            }
        }

        // Update current language
        _currentLanguage = languageName;
        _currentIsoCode = isoCode;

        // Fire language changed event
        if (previousLanguage != languageName)
        {
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(previousLanguage, languageName, previousIsoCode, isoCode));
        }

        System.Diagnostics.Debug.WriteLine($"LanguageService: Language changed to {languageName} ({isoCode})");
        return true;
    }

    /// <summary>
    /// Downloads and caches translations for a language.
    /// </summary>
    /// <param name="languageName">The language name.</param>
    /// <param name="overwrite">Whether to overwrite existing cached translations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful or already cached.</returns>
    public async Task<bool> DownloadAndCacheLanguageAsync(string languageName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var isoCode = Languages.GetIsoCode(languageName);

        // Check if already cached
        if (!overwrite)
        {
            if (isoCode == "en")
            {
                if (_englishCache.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("LanguageService: English already cached");
                    return true;
                }
            }
            else if (_translationCache.ContainsKey(isoCode))
            {
                System.Diagnostics.Debug.WriteLine($"LanguageService: {languageName} already cached");
                return true;
            }
        }

        try
        {
            TranslationProgress?.Invoke(this, new TranslationProgressEventArgs(languageName, true, "Downloading translations..."));

            // Get app version for download URL
            var version = GetAppVersion();
            var downloadUrl = string.Format(DownloadUrlTemplate, version, isoCode);

            System.Diagnostics.Debug.WriteLine($"LanguageService: Downloading from {downloadUrl}");

            var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"LanguageService: Download failed with status {response.StatusCode}");
                TranslationProgress?.Invoke(this, new TranslationProgressEventArgs(languageName, false, "Download failed"));
                return false;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var downloadedTranslations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

            if (downloadedTranslations == null || downloadedTranslations.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("LanguageService: Downloaded translations are empty");
                TranslationProgress?.Invoke(this, new TranslationProgressEventArgs(languageName, false, "No translations found"));
                return false;
            }

            // Handle English separately
            if (isoCode == "en")
            {
                _englishCache = downloadedTranslations;

                // Save to dedicated English file
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_englishFilePath, JsonSerializer.Serialize(_englishCache, options), cancellationToken);
            }
            else
            {
                // Merge with existing translations
                if (!_translationCache.TryGetValue(isoCode, out var existingTranslations))
                {
                    existingTranslations = new Dictionary<string, string>();
                    _translationCache[isoCode] = existingTranslations;
                }

                foreach (var kvp in downloadedTranslations)
                {
                    existingTranslations[kvp.Key] = kvp.Value;
                }

                SaveCacheToFile();
            }

            TranslationProgress?.Invoke(this, new TranslationProgressEventArgs(languageName, false, "Translations loaded"));
            System.Diagnostics.Debug.WriteLine($"LanguageService: Successfully downloaded {downloadedTranslations.Count} translations for {languageName}");
            return true;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("LanguageService: Download cancelled");
            TranslationProgress?.Invoke(this, new TranslationProgressEventArgs(languageName, false, "Download cancelled"));
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Error downloading translations: {ex.Message}");
            TranslationProgress?.Invoke(this, new TranslationProgressEventArgs(languageName, false, $"Error: {ex.Message}"));
            return false;
        }
    }

    /// <summary>
    /// Updates all cached translations after an app upgrade.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all updates were successful.</returns>
    public async Task<bool> UpdateAllCachedTranslationsAsync(CancellationToken cancellationToken = default)
    {
        var languagesToUpdate = new HashSet<string>();

        // Add languages from translation cache
        foreach (var isoCode in _translationCache.Keys)
        {
            var languageName = Languages.GetLanguageName(isoCode);
            if (!string.IsNullOrEmpty(languageName))
            {
                languagesToUpdate.Add(languageName);
            }
        }

        // Add English if cached
        if (_englishCache.Count > 0)
        {
            languagesToUpdate.Add("English");
        }

        if (languagesToUpdate.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("LanguageService: No cached translations to update");
            return true;
        }

        var successCount = 0;
        foreach (var languageName in languagesToUpdate)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (await DownloadAndCacheLanguageAsync(languageName, true, cancellationToken))
            {
                successCount++;
            }
        }

        return successCount == languagesToUpdate.Count;
    }

    /// <summary>
    /// Translates a string using the current language.
    /// </summary>
    /// <param name="text">The English text to translate.</param>
    /// <returns>The translated text, or the original if no translation is found.</returns>
    public string Translate(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // If English, return original text (or lookup from English cache if needed)
        if (_currentIsoCode == "en")
        {
            var englishKey = GetStringKey(text);
            if (_englishCache.TryGetValue(englishKey, out var englishValue))
            {
                return englishValue;
            }
            return DecodeHtmlEntities(text);
        }

        // Look up in translation cache
        var key = GetStringKey(text);
        var result = GetCachedTranslationByKey(_currentIsoCode, key);

        // Log missing translations to console
        if (result == null && text.Length < 100)
        {
            Console.WriteLine($"[TRANSLATE] Missing: '{DecodeHtmlEntities(text)}' (key: {key}) for {_currentIsoCode}");
            return DecodeHtmlEntities(text);
        }

        return result ?? DecodeHtmlEntities(text);
    }

    /// <summary>
    /// Gets a cached translation by key directly.
    /// </summary>
    private string? GetCachedTranslationByKey(string isoCode, string key)
    {
        if (isoCode == "en")
        {
            if (_englishCache.TryGetValue(key, out var cachedTranslation))
            {
                // Decode HTML entities in case the translation file contains encoded characters
                return DecodeHtmlEntities(cachedTranslation);
            }
        }
        else
        {
            if (_translationCache.TryGetValue(isoCode, out var languageTranslations))
            {
                if (languageTranslations.TryGetValue(key, out var cachedTranslation))
                {
                    // Decode HTML entities in case the translation file contains encoded characters
                    return DecodeHtmlEntities(cachedTranslation);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Translates a string using a specific language.
    /// </summary>
    /// <param name="text">The English text to translate.</param>
    /// <param name="isoCode">The target language ISO code.</param>
    /// <returns>The translated text, or the original if no translation is found.</returns>
    public string Translate(string text, string isoCode)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (isoCode == "en")
            return DecodeHtmlEntities(text);

        var key = GetStringKey(text);
        var result = GetCachedTranslationByKey(isoCode, key);

        // Log missing translations to console
        if (result == null && text.Length < 100)
        {
            Console.WriteLine($"[TRANSLATE] Missing: '{DecodeHtmlEntities(text)}' (key: {key}) for {isoCode}");
            return DecodeHtmlEntities(text);
        }

        return result ?? DecodeHtmlEntities(text);
    }

    /// <summary>
    /// Decodes common HTML/XML entities that may come from XAML markup extensions.
    /// </summary>
    private static string DecodeHtmlEntities(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('&'))
            return text;

        return text
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'");
    }

    /// <summary>
    /// Gets a unique key for a string using text content.
    /// </summary>
    /// <param name="text">The text to generate a key for.</param>
    /// <returns>A unique key for the string.</returns>
    public static string GetStringKey(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // Decode HTML/XML entities that may not be decoded by XAML parser in markup extensions
        var decodedText = text
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'");

        // Replace & with "amp" to match server-side key generation
        var processedText = decodedText.ToLowerInvariant().Replace("&", "amp");

        // Remove non-word characters (except placeholders like {0})
        var cleanText = NonWordCharactersRegex().Replace(processedText, "");

        // Limit length to avoid extremely long keys
        if (cleanText.Length > 50)
        {
            cleanText = cleanText[..50];
        }

        return $"str_{cleanText}";
    }

    /// <summary>
    /// Gets the current app version for download URL.
    /// </summary>
    private static string GetAppVersion()
    {
        try
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }
        catch
        {
            // Ignore errors getting version
        }

        return "1.0.0";
    }

    /// <summary>
    /// Checks if a translation exists for the given text in the current language.
    /// </summary>
    /// <param name="text">The English text to check.</param>
    /// <returns>True if a translation exists.</returns>
    public bool HasTranslation(string text)
    {
        if (string.IsNullOrEmpty(text) || _currentIsoCode == "en")
            return true;

        var textKey = GetStringKey(text);

        if (_translationCache.TryGetValue(_currentIsoCode, out var languageTranslations))
        {
            return languageTranslations.ContainsKey(textKey);
        }

        return false;
    }

    /// <summary>
    /// Gets the number of cached translations for the current language.
    /// </summary>
    public int CachedTranslationCount
    {
        get
        {
            if (_currentIsoCode == "en")
                return _englishCache.Count;

            if (_translationCache.TryGetValue(_currentIsoCode, out var translations))
                return translations.Count;

            return 0;
        }
    }

    /// <summary>
    /// Gets a list of all cached language ISO codes.
    /// </summary>
    public IReadOnlyList<string> CachedLanguages
    {
        get
        {
            var languages = new List<string>();
            if (_englishCache.Count > 0)
                languages.Add("en");
            languages.AddRange(_translationCache.Keys);
            return languages;
        }
    }

    /// <summary>
    /// Clears all cached translations.
    /// </summary>
    public void ClearCache()
    {
        _englishCache.Clear();
        _translationCache.Clear();

        try
        {
            if (File.Exists(_translationsFilePath))
                File.Delete(_translationsFilePath);
            if (File.Exists(_englishFilePath))
                File.Delete(_englishFilePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Error clearing cache files: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds or updates a translation in the cache (for admin/generator use).
    /// </summary>
    /// <param name="isoCode">The language ISO code.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="value">The translated value.</param>
    public void SetTranslation(string isoCode, string key, string value)
    {
        if (isoCode == "en")
        {
            _englishCache[key] = value;
        }
        else
        {
            if (!_translationCache.TryGetValue(isoCode, out var translations))
            {
                translations = new Dictionary<string, string>();
                _translationCache[isoCode] = translations;
            }
            translations[key] = value;
        }
    }

    /// <summary>
    /// Saves all translations to disk (for admin/generator use).
    /// </summary>
    public async Task SaveAllTranslationsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Save English cache
            if (_englishCache.Count > 0)
            {
                await File.WriteAllTextAsync(_englishFilePath, JsonSerializer.Serialize(_englishCache, options));
            }

            // Save other translations
            if (_translationCache.Count > 0)
            {
                await File.WriteAllTextAsync(_translationsFilePath, JsonSerializer.Serialize(_translationCache, options));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LanguageService: Error saving translations: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all translations for a language (for admin/generator use).
    /// </summary>
    /// <param name="isoCode">The language ISO code.</param>
    /// <returns>Dictionary of key-value translation pairs.</returns>
    public IReadOnlyDictionary<string, string> GetTranslations(string isoCode)
    {
        if (isoCode == "en")
            return _englishCache;

        if (_translationCache.TryGetValue(isoCode, out var translations))
            return translations;

        return new Dictionary<string, string>();
    }
}

/// <summary>
/// Event arguments for language change events.
/// </summary>
public class LanguageChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous language name.
    /// </summary>
    public string PreviousLanguage { get; }

    /// <summary>
    /// The new language name.
    /// </summary>
    public string NewLanguage { get; }

    /// <summary>
    /// The previous ISO code.
    /// </summary>
    public string PreviousIsoCode { get; }

    /// <summary>
    /// The new ISO code.
    /// </summary>
    public string NewIsoCode { get; }

    public LanguageChangedEventArgs(string previousLanguage, string newLanguage, string previousIsoCode, string newIsoCode)
    {
        PreviousLanguage = previousLanguage;
        NewLanguage = newLanguage;
        PreviousIsoCode = previousIsoCode;
        NewIsoCode = newIsoCode;
    }
}

/// <summary>
/// Event arguments for translation progress events.
/// </summary>
public class TranslationProgressEventArgs : EventArgs
{
    /// <summary>
    /// The language being downloaded.
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// Whether the download is in progress.
    /// </summary>
    public bool IsDownloading { get; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string Message { get; }

    public TranslationProgressEventArgs(string language, bool isDownloading, string message)
    {
        Language = language;
        IsDownloading = isDownloading;
        Message = message;
    }
}
