# Translation Tool

## Setup
```powershell
$env:AZURE_TRANSLATOR_KEY = "your-api-key"
```

## Commands

**Collect strings only (no API call):**
```
dotnet run -- --collect
```

**Translate to all languages:**
```
dotnet run -- --translate
```

**Translate to specific languages:**
```
dotnet run -- --languages fr,de,es,ja
```

**Custom output directory:**
```
dotnet run -- --translate --output C:\MyTranslations
```

## Output
Files are saved to `./translations/` (e.g., `en.json`, `fr.json`, `de.json`).
