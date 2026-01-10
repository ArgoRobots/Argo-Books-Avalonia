using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ArgoBooks.Data;
using ArgoBooks.Services;

namespace ArgoBooks.Localization;

/// <summary>
/// Admin tool for generating translations.
/// Scans source files for translatable strings and translates them using Azure Translator API.
/// </summary>
public partial class TranslationGenerator
{
    // Azure Translator API configuration
    private readonly string _azureKey;
    private readonly string _azureRegion;
    private const string AzureEndpoint = "https://api.cognitive.microsofttranslator.com";

    private readonly HttpClient _httpClient = new();

    // Regex patterns for extracting translatable strings
    [GeneratedRegex(@"\{Loc\s+([^}]+)\}")]
    private static partial Regex LocExtensionRegex();

    [GeneratedRegex(@"\.Translate\(\s*\)")]
    private static partial Regex TranslateCallRegex();

    [GeneratedRegex(@"\.TranslateFormat\s*\(")]
    private static partial Regex TranslateFormatCallRegex();

    [GeneratedRegex(@"Loc\.Tr\s*\(\s*""([^""]+)""")]
    private static partial Regex LocTrCallRegex();

    [GeneratedRegex(@"""([^""]+)""\s*\.Translate")]
    private static partial Regex StringTranslateRegex();

    [GeneratedRegex(@"""([^""]+)""\s*\.TranslateFormat")]
    private static partial Regex StringTranslateFormatRegex();

    // Batch size for Azure API calls (max 100 texts per request, max 10000 chars)
    private const int BatchSize = 50;
    private const int MaxCharsPerBatch = 9000;

    /// <summary>
    /// Event raised to report progress.
    /// </summary>
    public event EventHandler<TranslationGeneratorProgressEventArgs>? Progress;

    /// <summary>
    /// Creates a new TranslationGenerator.
    /// </summary>
    /// <param name="azureKey">Azure Translator API key.</param>
    /// <param name="azureRegion">Azure region (e.g., "eastus").</param>
    public TranslationGenerator(string azureKey, string azureRegion = "eastus")
    {
        _azureKey = azureKey ?? throw new ArgumentNullException(nameof(azureKey));
        _azureRegion = azureRegion;
    }

    /// <summary>
    /// Collects all translatable strings from the source directory.
    /// </summary>
    /// <param name="sourceDirectory">The root source directory to scan.</param>
    /// <returns>Dictionary of translation keys to English text.</returns>
    public Dictionary<string, string> CollectStrings(string sourceDirectory)
    {
        var strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ReportProgress("Scanning source files...", 0, 0);

        // Scan AXAML files for {Loc ...} usages
        var axamlFiles = Directory.GetFiles(sourceDirectory, "*.axaml", SearchOption.AllDirectories);
        foreach (var file in axamlFiles)
        {
            CollectFromAxamlFile(file, strings);
        }

        // Scan CS files for .Translate() and Loc.Tr() calls
        var csFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            // Skip the TranslationGenerator itself
            if (file.EndsWith("TranslationGenerator.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            CollectFromCsFile(file, strings);
        }

        ReportProgress($"Found {strings.Count} translatable strings", strings.Count, strings.Count);

        return strings;
    }

    /// <summary>
    /// Collects translatable strings from an AXAML file.
    /// </summary>
    private void CollectFromAxamlFile(string filePath, Dictionary<string, string> strings)
    {
        try
        {
            var content = File.ReadAllText(filePath);

            // Find all {Loc ...} usages
            var matches = LocExtensionRegex().Matches(content);
            foreach (Match match in matches)
            {
                var text = match.Groups[1].Value.Trim();

                // Remove quotes if present
                if ((text.StartsWith('\'') && text.EndsWith('\'')) ||
                    (text.StartsWith('"') && text.EndsWith('"')))
                {
                    text = text[1..^1];
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    var key = LanguageService.GetStringKey(text);
                    if (!strings.ContainsKey(key))
                    {
                        strings[key] = text;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scanning {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Collects translatable strings from a C# file.
    /// </summary>
    private void CollectFromCsFile(string filePath, Dictionary<string, string> strings)
    {
        try
        {
            var content = File.ReadAllText(filePath);

            // Find "text".Translate() patterns
            var translateMatches = StringTranslateRegex().Matches(content);
            foreach (Match match in translateMatches)
            {
                AddString(strings, match.Groups[1].Value);
            }

            // Find "text".TranslateFormat(...) patterns
            var translateFormatMatches = StringTranslateFormatRegex().Matches(content);
            foreach (Match match in translateFormatMatches)
            {
                AddString(strings, match.Groups[1].Value);
            }

            // Find Loc.Tr("text") patterns
            var locTrMatches = LocTrCallRegex().Matches(content);
            foreach (Match match in locTrMatches)
            {
                AddString(strings, match.Groups[1].Value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scanning {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a string to the collection if valid.
    /// </summary>
    private static void AddString(Dictionary<string, string> strings, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Skip if it looks like a variable or placeholder
        if (text.StartsWith('{') || text.Contains("{{"))
            return;

        var key = LanguageService.GetStringKey(text);
        if (!strings.ContainsKey(key))
        {
            strings[key] = text;
        }
    }

    /// <summary>
    /// Translates all collected strings to the specified languages.
    /// </summary>
    /// <param name="englishStrings">Dictionary of key -> English text.</param>
    /// <param name="targetLanguages">List of target languages (names like "French" or ISO codes like "fr").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of ISO code -> (key -> translated text).</returns>
    public async Task<Dictionary<string, Dictionary<string, string>>> TranslateAllAsync(
        Dictionary<string, string> englishStrings,
        IEnumerable<string> targetLanguages,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, Dictionary<string, string>>();
        var languageList = targetLanguages.ToList();

        var totalLanguages = languageList.Count;
        var currentLanguage = 0;

        foreach (var language in languageList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            currentLanguage++;

            // Accept either language name ("French") or ISO code ("fr")
            string isoCode;
            string displayName;
            if (Languages.IsValidIsoCode(language))
            {
                isoCode = language;
                displayName = Languages.GetLanguageName(language) ?? language;
            }
            else
            {
                isoCode = Languages.GetIsoCode(language);
                displayName = language;
            }

            if (isoCode == "en")
            {
                // For English, just copy the strings
                results["en"] = new Dictionary<string, string>(englishStrings);
                continue;
            }

            ReportProgress($"Translating to {displayName} ({isoCode})...", currentLanguage, totalLanguages);

            var translations = await TranslateToLanguageAsync(englishStrings, isoCode, cancellationToken);
            results[isoCode] = translations;
        }

        return results;
    }

    /// <summary>
    /// Translates strings to a single language.
    /// </summary>
    private async Task<Dictionary<string, string>> TranslateToLanguageAsync(
        Dictionary<string, string> englishStrings,
        string targetIsoCode,
        CancellationToken cancellationToken)
    {
        var translations = new Dictionary<string, string>();
        var keyList = englishStrings.Keys.ToList();
        var textList = englishStrings.Values.ToList();

        // Batch the texts
        var batches = CreateBatches(textList);
        var processedCount = 0;

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var translatedBatch = await TranslateBatchAsync(batch, targetIsoCode, cancellationToken);

            // Map translations back to keys
            for (int i = 0; i < batch.Count && processedCount < keyList.Count; i++)
            {
                var key = keyList[processedCount];
                var translatedText = i < translatedBatch.Count ? translatedBatch[i] : englishStrings[key];
                translations[key] = translatedText;
                processedCount++;
            }
        }

        return translations;
    }

    /// <summary>
    /// Creates batches of texts for API calls.
    /// </summary>
    private static List<List<string>> CreateBatches(List<string> texts)
    {
        var batches = new List<List<string>>();
        var currentBatch = new List<string>();
        var currentCharCount = 0;

        foreach (var text in texts)
        {
            if (currentBatch.Count >= BatchSize || currentCharCount + text.Length > MaxCharsPerBatch)
            {
                if (currentBatch.Count > 0)
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<string>();
                    currentCharCount = 0;
                }
            }

            currentBatch.Add(text);
            currentCharCount += text.Length;
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// Translates a batch of texts using Azure Translator API.
    /// </summary>
    private async Task<List<string>> TranslateBatchAsync(
        List<string> texts,
        string targetIsoCode,
        CancellationToken cancellationToken)
    {
        var route = $"/translate?api-version=3.0&from=en&to={targetIsoCode}";
        var uri = new Uri(AzureEndpoint + route);

        // Build request body
        var requestBody = texts.Select(t => new { Text = t }).ToArray();
        var requestJson = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _azureKey);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _azureRegion);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Azure Translator API error ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseData = JsonSerializer.Deserialize<List<TranslationResponse>>(responseJson);

        if (responseData == null)
            throw new InvalidOperationException("Azure Translator API returned null response");

        return responseData
            .Select(r => r.translations?.FirstOrDefault()?.text ?? "")
            .ToList();
    }

    /// <summary>
    /// Saves translation results to JSON files.
    /// </summary>
    /// <param name="translations">Dictionary of ISO code -> (key -> translated text).</param>
    /// <param name="outputDirectory">Directory to save JSON files.</param>
    public async Task SaveTranslationsAsync(
        Dictionary<string, Dictionary<string, string>> translations,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        foreach (var (isoCode, languageTranslations) in translations)
        {
            var filePath = Path.Combine(outputDirectory, $"{isoCode}.json");
            var json = JsonSerializer.Serialize(languageTranslations, options);
            await File.WriteAllTextAsync(filePath, json);

            ReportProgress($"Saved {isoCode}.json", 0, 0);
        }
    }

    /// <summary>
    /// Generates translations for all supported languages.
    /// </summary>
    /// <param name="sourceDirectory">Source code directory to scan.</param>
    /// <param name="outputDirectory">Directory to save translation JSON files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task GenerateAllTranslationsAsync(
        string sourceDirectory,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        // Collect strings from source
        var englishStrings = CollectStrings(sourceDirectory);

        if (englishStrings.Count == 0)
        {
            ReportProgress("No translatable strings found!", 0, 0);
            return;
        }

        // Translate to all languages
        var translations = await TranslateAllAsync(
            englishStrings,
            Languages.All,
            cancellationToken);

        // Save to files
        await SaveTranslationsAsync(translations, outputDirectory);

        ReportProgress("Translation generation complete!", translations.Count, translations.Count);
    }

    /// <summary>
    /// Generates translations for specific languages only.
    /// </summary>
    /// <param name="sourceDirectory">Source code directory to scan.</param>
    /// <param name="outputDirectory">Directory to save translation JSON files.</param>
    /// <param name="languages">List of language names to translate to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task GenerateTranslationsAsync(
        string sourceDirectory,
        string outputDirectory,
        IEnumerable<string> languages,
        CancellationToken cancellationToken = default)
    {
        var englishStrings = CollectStrings(sourceDirectory);

        if (englishStrings.Count == 0)
        {
            ReportProgress("No translatable strings found!", 0, 0);
            return;
        }

        var translations = await TranslateAllAsync(
            englishStrings,
            languages,
            cancellationToken);

        await SaveTranslationsAsync(translations, outputDirectory);

        ReportProgress("Translation generation complete!", translations.Count, translations.Count);
    }

    /// <summary>
    /// Compares current strings with a reference file and returns only new/changed strings.
    /// </summary>
    /// <param name="currentStrings">Currently collected strings.</param>
    /// <param name="referenceFilePath">Path to reference English JSON file.</param>
    /// <returns>Dictionary of new/changed strings only.</returns>
    public Dictionary<string, string> GetChangedStrings(
        Dictionary<string, string> currentStrings,
        string referenceFilePath)
    {
        var changed = new Dictionary<string, string>();

        Dictionary<string, string>? reference = null;
        if (File.Exists(referenceFilePath))
        {
            try
            {
                var json = File.ReadAllText(referenceFilePath);
                reference = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch
            {
                // If we can't read reference, treat all as new
            }
        }

        foreach (var (key, value) in currentStrings)
        {
            if (reference == null || !reference.TryGetValue(key, out var refValue) || refValue != value)
            {
                changed[key] = value;
            }
        }

        return changed;
    }

    /// <summary>
    /// Reports progress.
    /// </summary>
    private void ReportProgress(string message, int current, int total)
    {
        Progress?.Invoke(this, new TranslationGeneratorProgressEventArgs(message, current, total));
        System.Diagnostics.Debug.WriteLine($"TranslationGenerator: {message}");
    }

    // Response models for Azure API
    private class TranslationResponse
    {
        public List<Translation>? translations { get; set; }
    }

    private class Translation
    {
        public string? text { get; set; }
        public string? to { get; set; }
    }
}

/// <summary>
/// Progress event arguments for TranslationGenerator.
/// </summary>
public class TranslationGeneratorProgressEventArgs : EventArgs
{
    public string Message { get; }
    public int Current { get; }
    public int Total { get; }

    public TranslationGeneratorProgressEventArgs(string message, int current, int total)
    {
        Message = message;
        Current = current;
        Total = total;
    }
}
