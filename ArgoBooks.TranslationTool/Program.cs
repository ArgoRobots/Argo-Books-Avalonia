using System.Text.Json;
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

Console.WriteLine("=== Argo Books Translation Generator ===\n");

// Parse command line arguments (args is provided by top-level statements)
var collectOnly = args.Contains("--collect");
var translateAll = args.Contains("--translate");
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

// Get Azure API key from environment
var azureKey = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
var azureRegion = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "eastus";

if (!collectOnly && string.IsNullOrEmpty(azureKey))
{
    Console.WriteLine("WARNING: AZURE_TRANSLATOR_KEY environment variable not set.");
    Console.WriteLine("Set it to translate, or use --collect to just collect strings.\n");
    Console.WriteLine("Example:");
    Console.WriteLine("  export AZURE_TRANSLATOR_KEY=your-api-key");
    Console.WriteLine("  export AZURE_TRANSLATOR_REGION=eastus  # optional, defaults to eastus\n");
    collectOnly = true;
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

Console.WriteLine($"\nStep 2: Translating to {languagesToTranslate.Count} languages...");
Console.WriteLine("Languages: " + string.Join(", ", languagesToTranslate.Take(10)) +
    (languagesToTranslate.Count > 10 ? $" ... and {languagesToTranslate.Count - 10} more" : ""));
Console.WriteLine();

try
{
    var translations = await generator.TranslateAllAsync(
        strings,
        languagesToTranslate,
        CancellationToken.None);

    await generator.SaveTranslationsAsync(translations, outputDir);

    Console.WriteLine($"\nDone! Translations saved to: {outputDir}");
    Console.WriteLine($"Generated {translations.Count} language files.");

    // Show file sizes
    Console.WriteLine("\nGenerated files:");
    foreach (var file in Directory.GetFiles(outputDir, "*.json"))
    {
        var info = new FileInfo(file);
        Console.WriteLine($"  {Path.GetFileName(file)}: {info.Length / 1024.0:F1} KB");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nERROR: Translation failed: {ex.Message}");
    return 1;
}

return 0;
