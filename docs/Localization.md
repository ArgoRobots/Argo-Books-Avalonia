# Localization

Argo Books supports multiple languages through a dynamic translation system that downloads and caches translations from the server. You can view the list of supported languages on the website [here](https://www.argorobots.com/documentation/pages/reference/supported_languages.php).

## Overview

![Localization Overview](diagrams/localization/localization-overview.svg)

## XAML Usage

Translations are applied in XAML using the `{loc:Loc}` markup extension:

```xml
<TextBlock Text="{loc:Loc 'Save Changes'}" />
<Button Content="{loc:Loc 'Cancel'}" />
```

For strings containing apostrophes, escape them by doubling:

```xml
<TextBlock Text="{loc:Loc 'Don''t save'}" />
```

### TranslateConverter

For data binding scenarios (e.g., ItemTemplates), use `TranslateConverter`:

```xml
<ComboBox ItemsSource="{Binding Options}">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Converter={StaticResource TranslateConverter}}" />
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

## Code Usage

### Loc.Tr() (Static Helper)

```csharp
using ArgoBooks.Localization;

var message = Loc.Tr("Operation completed successfully");
var formatted = Loc.Tr("Saved {0} items", count);

// Check current language
if (Loc.IsEnglish) { /* ... */ }
var isoCode = Loc.CurrentIsoCode;  // e.g., "fr"
var name = Loc.CurrentLanguage;     // e.g., "French"
```

## Translation Flow

![Translation Flow](diagrams/localization/translation-flow.svg)

1. **English text** is provided in XAML or code
2. **Key generation** converts text to a lookup key (`str_savechanges`)
3. **Cache lookup** finds the translation for the current language
4. **Result** is returned (or original text if no translation exists)

### Key Generation

Translation keys are generated from the English text:
- Convert to lowercase
- Remove special characters (except `{0}` placeholders)
- Prefix with `str_`
- Truncate to 50 characters max

Example: `"Save Changes"` → `str_savechanges`

**Note:** This means `"Save Changes"` and `"save changes"` produce the same key. Avoid duplicate strings that differ only in case or punctuation.

## Language Change Flow

![Language Change Flow](diagrams/localization/language-change-flow.svg)

1. **User** selects a new language in Settings
2. **Settings** calls `LanguageService.SetLanguageAsync()`
3. **LanguageService** downloads translations if not cached, then fires `LanguageChanged` event
4. **LocalizationManager** receives the event
5. **UI Update** refreshes all registered bindings with new translations

## Translation Download

![Download Flow](diagrams/localization/download-flow.svg)

Translations are downloaded from the server based on app version:

```
https://argorobots.com/resources/downloads/versions/{version}/languages/{isoCode}.json
```

### Caching

Downloaded translations are cached locally:

| Platform | Cache Location |
|----------|----------------|
| **Windows** | `%LOCALAPPDATA%\ArgoBooks\Languages\` |
| **macOS** | `~/Library/Caches/ArgoBooks/Languages/` |
| **Linux** | `~/.cache/ArgoBooks/Languages/` |

Cache files:
- `translations.json` - All non-English translations
- `en.json` - English translations
- `{isoCode}.json` - Individual language files (optional)

## Translation Generation (Admin)

Translations are generated using the `TranslationGenerator` class and the **Azure Translator API**.

### Running the Translation Tool

In **JetBrains Rider**, set the startup project to `ArgoBooks.TranslationTool`, then run it.

Or use the command line:

```powershell
# Set environment variables
$env:AZURE_TRANSLATOR_REGION = "canadacentral"
$env:AZURE_TRANSLATOR_KEY = "your-api-key"

cd ArgoBooks.TranslationTool
```

| Command | Description |
|---------|-------------|
| `dotnet run -- --translate` | Translate to all languages |
| `dotnet run -- --languages fr,de,es,ja` | Translate to specific languages |
| `dotnet run -- --output C:\MyTranslations` | Custom output directory |

Output files are saved to `./translations/` by default (e.g., `en.json`, `fr.json`).

**For incremental translation:** Copy existing translation files to the output folder first. The tool will use `GetChangedStrings()` to only translate new/changed strings, avoiding redundant API calls.

### How It Works

1. **Scan source files** - Collects all translatable strings from AXAML and C#
2. **Compare with existing** - Uses `GetChangedStrings()` to identify only new or modified strings
3. **Translate via Azure** - Sends only untranslated strings to Azure Translator API in batches
4. **Save JSON files** - Outputs `{isoCode}.json` files for each language

### Incremental Translation

The generator compares current strings against a reference file and only translates what's changed:

```csharp
// Only returns strings not already in reference file
var newStrings = generator.GetChangedStrings(currentStrings, "en.json");
```

This avoids re-translating existing content, saving API costs and preserving any manual translation fixes.

**Limitations:** Key collisions can cause issues:
- `"Save Changes"` and `"Save changes"` both produce key `str_savechanges`
- Keys are truncated to 50 characters, so only the first 50 chars affect the key

If two different English strings produce the same key, only one translation will exist. Manually delete the affected key from the reference file to force re-translation.

**Workaround for casing:** Instead of creating separate translations for different cases, translate once and transform in code:

```csharp
var upper = Loc.Tr("Save Changes").ToUpperInvariant();
```

Or use `UpperCaseConverter` in XAML for UI elements that need all caps.

## Translation File Format

Translation files are simple JSON key-value pairs:

```json
{
  "str_savechanges": "Enregistrer les modifications",
  "str_cancel": "Annuler",
  "str_saved0items": "Enregistré {0} éléments"
}
```

## Best Practices

| Practice | Description |
|----------|-------------|
| **Use markup extension** | Prefer `{loc:Loc 'text'}` over code translations for automatic refresh |
| **Use TranslateConverter for templates** | Required for translating bound data in ItemTemplates |
| **Keep text short** | Long keys are truncated; keep source text concise |
| **Use placeholders** | Use `{0}`, `{1}` for dynamic values: `Loc.Tr("Found {0} results", count)` |
| **Avoid concatenation** | Don't concatenate translated strings; use full sentences with placeholders |
| **Avoid case-only variants** | `"Save"` and `"SAVE"` produce the same key; use one consistently |