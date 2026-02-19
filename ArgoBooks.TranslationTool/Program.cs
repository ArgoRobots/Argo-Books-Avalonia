using System.Net.Http.Json;
using System.Text.Json;
using ArgoBooks.Core.Services;
using ArgoBooks.Data;
using ArgoBooks.Localization;

// Translation Generator Tool
// Usage: dotnet run -- [options]
//
// Options:
//   --collect           Collect all translatable strings (no API call)
//   --translate         Translate to all languages (requires Azure API key)
//   --languages fr,de   Translate to specific languages only
//   --output <path>     Output directory for JSON files
//   --source <path>     Source directory to scan (default: ../ArgoBooks)
//   --yes               Skip confirmation prompt

Console.WriteLine("=== Argo Books Translation Generator ===\n");

// Parse command line arguments (args is provided by top-level statements)
var collectOnly = args.Contains("--collect");
var translateAll = args.Contains("--translate");
var skipConfirmation = args.Contains("--yes") || args.Contains("-y");
var specificLanguages = new List<string>();
var outputDir = "./translations";
var sourceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "ArgoBooks"));

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--languages" && i + 1 < args.Length)
    {
        specificLanguages.AddRange(args[i + 1].Split(',').Select(l => l.Trim()));
    }
    else if (args[i] == "--output" && i + 1 < args.Length)
    {
        outputDir = args[i + 1];
    }
    else if (args[i] == "--source" && i + 1 < args.Length)
    {
        sourceDir = args[i + 1];
    }
}

// Load .env file for API keys
DotEnv.Load();

// Get Azure API key from environment
var azureKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
var azureRegion = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "eastus";

// Fetch USD to CAD exchange rate for cost display
var usdToCad = await GetUsdToCadRateAsync();


// If translation was requested but no API key, exit with error
var translationRequested = translateAll || specificLanguages.Count > 0;
if (translationRequested && string.IsNullOrEmpty(azureKey))
{
    Console.WriteLine("ERROR: AZURE_TRANSLATOR_KEY environment variable not set.\n");
    Console.WriteLine("Set the environment variable and try again:");
    Console.WriteLine("  PowerShell:  $env:AZURE_TRANSLATOR_KEY = \"your-api-key\"");
    Console.WriteLine("  Cmd:         set AZURE_TRANSLATOR_KEY=your-api-key");
    Console.WriteLine("  Bash:        export AZURE_TRANSLATOR_KEY=your-api-key\n");
    Console.WriteLine("Or use --collect to just collect strings without translating.");
    return 1;
}

Console.WriteLine($"Source directory: {sourceDir}");
Console.WriteLine($"Output directory: {outputDir}");
Console.WriteLine();

if (!Directory.Exists(sourceDir))
{
    Console.WriteLine($"ERROR: Source directory not found: {sourceDir}");
    Console.WriteLine("Use --source <path> to specify the ArgoBooks source directory.");
    return 1;
}

// Create output directory
Directory.CreateDirectory(outputDir);

// Create generator
var generator = new TranslationGenerator(azureKey ?? "", azureRegion);
generator.Progress += (_, e) => Console.WriteLine($"  {e.Message}");

// Step 1: Collect strings
Console.WriteLine("Step 1: Collecting translatable strings...");
var strings = generator.CollectStrings(sourceDir);
Console.WriteLine($"\nFound {strings.Count} translatable strings.\n");

// Save English strings
var englishPath = Path.Combine(outputDir, "en.json");
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
await File.WriteAllTextAsync(englishPath, JsonSerializer.Serialize(strings, options));
Console.WriteLine($"Saved English strings to: {englishPath}\n");

// Show sample strings
Console.WriteLine("Sample strings found:");
foreach (var kvp in strings.Take(10))
{
    Console.WriteLine($"  {kvp.Key}: \"{kvp.Value}\"");
}
if (strings.Count > 10)
{
    Console.WriteLine($"  ... and {strings.Count - 10} more\n");
}

if (collectOnly)
{
    Console.WriteLine("\nDone! Use --translate to translate to all languages.");
    Console.WriteLine("Or use --languages fr,de,es to translate to specific languages.");
    return 0;
}

// Step 2: Translate
var languagesToTranslate = specificLanguages.Count > 0
    ? specificLanguages
    : Languages.All.Where(l => l != "English").ToList();

// Resolve ISO codes for each language
var resolvedLanguages = new List<(string isoCode, string displayName)>();
foreach (var lang in languagesToTranslate)
{
    if (Languages.IsValidIsoCode(lang))
        resolvedLanguages.Add((lang, Languages.GetLanguageName(lang)));
    else
        resolvedLanguages.Add((Languages.GetIsoCode(lang), lang));
}

// Check existing translations and count new vs existing per language
Console.WriteLine($"\nStep 2: Checking existing translations...");
var totalNewStrings = 0;
var totalExistingStrings = 0;
var totalCharsToTranslate = 0L;

foreach (var (isoCode, displayName) in resolvedLanguages)
{
    if (isoCode == "en") continue;

    var existingFile = Path.Combine(outputDir, $"{isoCode}.json");
    var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (File.Exists(existingFile))
    {
        try
        {
            var json = await File.ReadAllTextAsync(existingFile);
            var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (existing != null)
            {
                foreach (var kvp in existing)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                        existingKeys.Add(kvp.Key);
                }
            }
        }
        catch { /* ignore parse errors */ }
    }

    var newCount = strings.Keys.Count(k => !existingKeys.Contains(k));
    var existingCount = strings.Keys.Count(k => existingKeys.Contains(k));
    var newChars = strings.Where(kvp => !existingKeys.Contains(kvp.Key)).Sum(kvp => (long)kvp.Value.Length);

    totalNewStrings += newCount;
    totalExistingStrings += existingCount;
    totalCharsToTranslate += newChars;

    if (existingKeys.Count > 0)
        Console.WriteLine($"  {displayName} ({isoCode}): {newCount} new, {existingCount} existing");
    else
        Console.WriteLine($"  {displayName} ({isoCode}): {strings.Count} new (no existing file)");
}

var nonEnglishCount = resolvedLanguages.Count(l => l.isoCode != "en");

// If nothing new to translate, skip entirely
if (totalNewStrings == 0)
{
    Console.WriteLine($"\nAll {totalExistingStrings} translations are up to date. Nothing to do.");
    return 0;
}

// Cost estimate based only on new characters that need translating
var estimatedCostUsd = totalCharsToTranslate / 1_000_000m * 10m;

Console.WriteLine();
Console.WriteLine($"New strings to translate: {totalNewStrings} across {nonEnglishCount} language(s)");
Console.WriteLine($"Characters to translate:  {totalCharsToTranslate:N0}");
Console.WriteLine($"Estimated cost:           {FormatCost(estimatedCostUsd, usdToCad)}");
Console.WriteLine();

// Confirmation prompt
if (!skipConfirmation)
{
    Console.Write("Proceed with translation? [y/N] ");
    var response = Console.ReadLine()?.Trim().ToLowerInvariant();
    if (response != "y" && response != "yes")
    {
        Console.WriteLine("Translation cancelled.");
        return 0;
    }
    Console.WriteLine();
}

Console.WriteLine($"Translating to {nonEnglishCount} languages...");
Console.WriteLine("Languages: " + string.Join(", ", resolvedLanguages.Where(l => l.isoCode != "en").Select(l => l.displayName).Take(10)) +
    (nonEnglishCount > 10 ? $" ... and {nonEnglishCount - 10} more" : ""));
Console.WriteLine();

try
{
    await generator.TranslateAllAsync(
        strings,
        languagesToTranslate,
        outputDir,
        CancellationToken.None);

    Console.WriteLine($"\nDone! Translations saved to: {outputDir}");
    Console.WriteLine($"Translated {generator.LanguagesTranslated} languages.");

    // Print Azure API usage summary
    Console.WriteLine();
    Console.WriteLine("--- Azure Translator API Usage ---");
    Console.WriteLine($"  Languages translated:  {generator.LanguagesTranslated}");
    Console.WriteLine($"  API calls made:        {generator.ApiCallCount}");
    Console.WriteLine($"  Characters translated: {generator.TotalCharactersTranslated:N0}");
    Console.WriteLine($"  Estimated cost:        {FormatCost(generator.EstimatedCost, usdToCad)}");
    Console.WriteLine("----------------------------------");
}
catch (Exception ex)
{
    Console.WriteLine($"\nERROR: Translation failed: {ex.Message}");
    if (generator.LanguagesTranslated > 0)
        Console.WriteLine($"  {generator.LanguagesTranslated} language(s) were saved before the error. Re-run to continue.");
    return 1;
}

return 0;

// --- Helper functions ---

static string FormatCost(decimal costUsd, decimal? usdToCad)
{
    if (usdToCad.HasValue)
    {
        var costCad = costUsd * usdToCad.Value;
        return $"${costCad:F4} CAD (${costUsd:F4} USD @ {usdToCad.Value:F4})";
    }
    return $"${costUsd:F4} USD (S1 @ $10 USD/1M chars)";
}

static async Task<decimal?> GetUsdToCadRateAsync()
{
    var oxrKey = DotEnv.Get("OPENEXCHANGERATES_API_KEY");
    if (string.IsNullOrEmpty(oxrKey))
        return null;

    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var url = $"https://openexchangerates.org/api/latest.json?app_id={oxrKey}&symbols=CAD";
        var response = await http.GetFromJsonAsync<JsonElement>(url);
        if (response.TryGetProperty("rates", out var rates) && rates.TryGetProperty("CAD", out var cad))
            return cad.GetDecimal();
    }
    catch
    {
        // Silently fall back to USD-only display
    }
    return null;
}
