using System.Globalization;
using System.Text;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Models.Telemetry;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service that uses an LLM to analyze spreadsheet/CSV files and produce
/// column mappings and entity type detection for import.
/// </summary>
public class SpreadsheetAnalysisService(
    IOpenAiService openAiService,
    IErrorLogger? errorLogger = null,
    string? country = null)
{
    private const int SampleFirstRows = 5;
    private const int SampleLastRows = 3;
    private const int SampleRandomRows = 5;
    private const int Tier2ChunkSize = 100;
    private const int MaxConcurrentChunks = 5;

    #region Analysis Phase

    /// <summary>
    /// Analyzes an Excel file and returns sheet type detection + column mappings.
    /// </summary>
    public async Task<SpreadsheetAnalysisResult?> AnalyzeAsync(
        string filePath,
        CancellationToken cancellationToken = default,
        IProgress<(string detail, double percent)>? progress = null)
    {
        try
        {
            // Report initial progress so the UI shows the loading overlay immediately
            progress?.Report(("Reading file...", 0));
            await Task.Yield();

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(fileStream);

            var sheetsData = new List<(string Name, List<string> Headers, List<List<string>> SampleRows, int TotalRows)>();

            foreach (var worksheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var headerRow = FindHeaderRow(worksheet);
                var headers = GetHeaders(worksheet, headerRow);
                if (headers.Count == 0) continue;

                var totalRows = (worksheet.LastRowUsed()?.RowNumber() ?? headerRow) - headerRow; // exclude header and rows above it
                var sampleRows = GetSampleRows(worksheet, headers.Count, totalRows);
                sheetsData.Add((worksheet.Name, headers, sampleRows, totalRows));
            }

            if (sheetsData.Count == 0)
                return null;

            return await AnalyzeWithLlmAsync(
                Path.GetFileName(filePath), sheetsData, cancellationToken, progress);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            errorLogger?.LogError(ex, ErrorCategory.Import, "Failed to analyze spreadsheet with AI");
            return null;
        }
    }

    /// <summary>
    /// Analyzes a CSV file and returns entity type detection + column mappings.
    /// </summary>
    public async Task<SpreadsheetAnalysisResult?> AnalyzeCsvAsync(
        string filePath,
        CancellationToken cancellationToken = default,
        IProgress<(string detail, double percent)>? progress = null)
    {
        try
        {
            // Report initial progress so the UI shows the loading overlay immediately
            progress?.Report(("Reading file...", 0));
            await Task.Yield();

            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            if (lines.Length < 2)
                return null;

            var delimiter = DetectCsvDelimiter(lines[0]);
            var headers = ParseCsvLine(lines[0], delimiter);
            if (headers.Count == 0)
                return null;

            var allDataRows = new List<List<string>>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                allDataRows.Add(ParseCsvLine(lines[i], delimiter));
            }

            var totalRows = allDataRows.Count;
            var sampleRows = GetSampleFromList(allDataRows, totalRows);
            var sheetName = Path.GetFileNameWithoutExtension(filePath);
            var sheetsData = new List<(string Name, List<string> Headers, List<List<string>> SampleRows, int TotalRows)>
            {
                (sheetName, headers, sampleRows, totalRows)
            };

            return await AnalyzeWithLlmAsync(
                Path.GetFileName(filePath), sheetsData, cancellationToken, progress);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            errorLogger?.LogError(ex, ErrorCategory.Import, "Failed to analyze CSV with AI");
            return null;
        }
    }

    private async Task<SpreadsheetAnalysisResult?> AnalyzeWithLlmAsync(
        string fileName,
        List<(string Name, List<string> Headers, List<List<string>> SampleRows, int TotalRows)> sheetsData,
        CancellationToken cancellationToken,
        IProgress<(string detail, double percent)>? progress = null)
    {
        var systemPrompt = BuildAnalysisSystemPrompt();
        var userPrompt = BuildAnalysisUserPrompt(sheetsData);

        // Scale max tokens based on number of sheets — each sheet needs ~300-500 tokens for mappings
        var maxTokens = Math.Max(4000, sheetsData.Count * 500);

        // Estimate LLM duration based on prompt size (more sheets → longer)
        var estimatedSeconds = Math.Max(6, sheetsData.Count * 3);
        var intervalMs = (int)(estimatedSeconds * 1000.0 / 95);

        var currentProgress = 0.0;
        progress?.Report(("Analyzing with AI...", currentProgress));

        using var progressTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
        var timerTask = Task.Run(async () =>
        {
            while (await progressTimer.WaitForNextTickAsync(cancellationToken))
            {
                currentProgress = Math.Min(currentProgress + 1, 95);
                progress?.Report(("Analyzing with AI...", currentProgress));

                // Past 95%, slow down significantly
                if (currentProgress >= 95)
                    await Task.Delay(2000, cancellationToken);
            }
        }, cancellationToken);

        string? response;
        try
        {
            response = await openAiService.SendChatAsync(
                systemPrompt, userPrompt, maxTokens: maxTokens, temperature: 0.1, cancellationToken);
        }
        finally
        {
            progressTimer.Dispose(); // stops the timer, timerTask will complete
        }

        progress?.Report(("Analyzing with AI...", 100));

        if (string.IsNullOrEmpty(response))
            return null;

        var result = ParseAnalysisResponse(response);
        if (result != null)
        {
            result.FileName = fileName;

            // Populate row counts from our data
            foreach (var sheet in result.Sheets)
            {
                var data = sheetsData.FirstOrDefault(s => s.Name == sheet.SourceSheetName);
                if (data != default)
                    sheet.RowCount = data.TotalRows;
            }
        }

        return result;
    }

    #endregion

    #region Tier 2 Processing

    /// <summary>
    /// Processes a chunk of rows through the LLM to normalize them into entity JSON.
    /// </summary>
    public async Task<LlmProcessedData?> ProcessChunkAsync(
        List<string> headers,
        List<List<string>> rows,
        SpreadsheetSheetType entityType,
        CancellationToken cancellationToken = default)
    {
        var schema = ImportSchemaDefinition.GetSchemaForType(entityType, country);
        if (schema == null)
            return null;

        var systemPrompt = BuildTier2SystemPrompt(entityType, schema);
        var userPrompt = BuildTier2UserPrompt(headers, rows);


        var response = await openAiService.SendChatAsync(
            systemPrompt, userPrompt, maxTokens: 8000, temperature: 0.0, cancellationToken);

        if (string.IsNullOrEmpty(response))
        {
            return null;
        }

        return ParseTier2Response(response, entityType, rows.Count);
    }

    /// <summary>
    /// Processes all rows of a sheet through LLM in chunks, reporting progress.
    /// </summary>
    public async Task<List<LlmProcessedData>> ProcessAllChunksAsync(
        string filePath,
        SheetAnalysis sheetAnalysis,
        IProgress<(int processed, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<LlmProcessedData>();

        List<string> headers;
        List<List<string>> allRows;

        if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            var delimiter = DetectCsvDelimiter(lines[0]);
            headers = ParseCsvLine(lines[0], delimiter);
            allRows = [];
            for (int i = 1; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    allRows.Add(ParseCsvLine(lines[i], delimiter));
            }
        }
        else
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sheetAnalysis.SourceSheetName);
            if (worksheet == null)
                return results;

            headers = GetHeaders(worksheet);
            allRows = GetAllRowsAsStrings(worksheet, headers.Count);
        }

        var total = allRows.Count;

        // Build all chunks upfront
        var chunks = new List<(int Index, List<List<string>> Rows)>();
        for (int i = 0; i < total; i += Tier2ChunkSize)
            chunks.Add((i, allRows.Skip(i).Take(Tier2ChunkSize).ToList()));

        // Process chunks in parallel with concurrency limit
        var semaphore = new SemaphoreSlim(MaxConcurrentChunks);
        var processedCount = 0;
        var chunkResults = new LlmProcessedData?[chunks.Count];

        var tasks = chunks.Select((chunk, idx) => Task.Run(async () =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                chunkResults[idx] = await ProcessChunkAsync(headers, chunk.Rows, sheetAnalysis.DetectedType, cancellationToken);
            }
            finally
            {
                semaphore.Release();
                var done = Interlocked.Add(ref processedCount, chunk.Rows.Count);
                progress?.Report((Math.Min(done, total), total));
            }
        }, cancellationToken)).ToArray();

        await Task.WhenAll(tasks);

        foreach (var result in chunkResults)
        {
            if (result != null)
                results.Add(result);
        }

        return results;
    }

    #endregion

    #region Prompt Building

    private static string BuildAnalysisSystemPrompt()
    {
        return @"You are an expert data analyst for a bookkeeping application called Argo Books. Your task is to analyze spreadsheet data and determine:
1. What type of business entity each sheet represents
2. How source columns map to the expected Argo Books schema
3. Whether simple column mapping (Tier 1) suffices, or if complex row transformation (Tier 2) is needed

Use Tier 2 ONLY when:
- Multiple entity types are mixed in one sheet
- Rows need grouping (e.g., line-item-per-row that must become one invoice)
- The structure is fundamentally different from a simple table (e.g., pivot tables, cross-tabs)
- Data requires splitting/combining columns in non-trivial ways

For everything else (renamed columns, different terminology, minor format differences), use Tier 1.

Respond with valid JSON only, no markdown code blocks.";
    }

    private string BuildAnalysisUserPrompt(
        List<(string Name, List<string> Headers, List<List<string>> SampleRows, int TotalRows)> sheetsData)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Target Schema");
        sb.AppendLine(ImportSchemaDefinition.FormatSchemaForPrompt(country));

        sb.AppendLine("## Source Data");
        sb.AppendLine();

        foreach (var (name, headers, sampleRows, totalRows) in sheetsData)
        {
            sb.AppendLine($"### Sheet: \"{name}\" ({totalRows} data rows)");
            sb.AppendLine();

            // Headers
            sb.Append("| ");
            sb.Append(string.Join(" | ", headers));
            sb.AppendLine(" |");

            sb.Append("| ");
            sb.Append(string.Join(" | ", headers.Select(_ => "---")));
            sb.AppendLine(" |");

            // Sample rows
            foreach (var row in sampleRows)
            {
                sb.Append("| ");
                // Pad row to match header count
                var cells = new List<string>(row);
                while (cells.Count < headers.Count)
                    cells.Add("");
                sb.Append(string.Join(" | ", cells.Select(c => c.Replace("|", "\\|"))));
                sb.AppendLine(" |");
            }
            sb.AppendLine();
        }

        sb.AppendLine(@"## Response Format
{
  ""sheets"": [
    {
      ""sourceSheetName"": ""<exact sheet name>"",
      ""detectedType"": ""<one of: Customers, Suppliers, Products, Categories, Locations, Departments, Invoices, Expenses, Inventory, Payments, Revenue, RentalInventory, RentalRecords, Employees, RecurringInvoices, StockAdjustments, PurchaseOrders, PurchaseOrderLineItems, Returns, LostDamaged, Unknown>"",
      ""confidence"": 0.95,
      ""tier"": ""Tier1_Mapping"",
      ""tierReason"": """",
      ""columnMappings"": [
        { ""sourceColumn"": ""<source col>"", ""targetColumn"": ""<target col from schema>"", ""confidence"": 0.98, ""transformHint"": null }
      ],
      ""unmappedSourceColumns"": [""<columns that don't map to any target>""],
      ""unmappedTargetColumns"": [""<target columns with no source match>""]
    }
  ],
  ""warnings"": [""<any general warnings>""]
}

IMPORTANT:
- sourceSheetName must EXACTLY match the original sheet name
- targetColumn must EXACTLY match a column name from the target schema above
- detectedType must be one of the listed entity types
- Only include mappings where you are reasonably confident (>0.5)
- Set tier to ""Tier2_LlmProcessing"" only when simple column mapping cannot work");

        return sb.ToString();
    }

    private static string BuildTier2SystemPrompt(SpreadsheetSheetType entityType, List<SchemaColumn> schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are converting raw spreadsheet data into normalized {entityType} records for Argo Books.");
        sb.AppendLine();
        sb.AppendLine("Target JSON schema (use these exact property names as JSON keys):");

        // Collect columns with JsonName, deduplicating by JsonName (some columns map to the same property)
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var col in schema)
        {
            var jsonName = col.JsonName ?? col.Name;
            if (!seen.Add(jsonName)) continue;

            var req = col.Required ? " (REQUIRED)" : "";
            sb.AppendLine($"- {jsonName} ({col.Type}): {col.Description}{req}");
        }

        // If any columns use dotted names (e.g., address.street), explain nesting
        if (schema.Any(c => c.JsonName?.Contains('.') == true))
        {
            sb.AppendLine();
            sb.AppendLine("For dotted property names like 'address.street', nest them as JSON objects:");
            sb.AppendLine("  { \"address\": { \"street\": \"value\", \"city\": \"value\" } }");
        }

        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Output a JSON array of objects using the exact JSON property names listed above");
        sb.AppendLine("- Generate reasonable IDs if none exist (e.g., CUS-001, INV-2024-001)");
        sb.AppendLine("- Parse dates to ISO 8601 format (yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss)");
        sb.AppendLine("- Parse decimal amounts (remove currency symbols, handle comma/dot separators)");
        sb.AppendLine("- Skip rows that are clearly subtotals, headers, or empty");
        sb.AppendLine("- If multiple source rows represent one entity, group them");
        sb.AppendLine("- Respond with JSON array only, no markdown");

        // Product-specific instructions for category generation
        if (entityType == SpreadsheetSheetType.Products)
        {
            sb.AppendLine();
            sb.AppendLine("Product-specific rules:");
            sb.AppendLine("- ALWAYS provide a categoryName for every product, even if the source data has no category column");
            sb.AppendLine("- If the source data has a category, use it as categoryName");
            sb.AppendLine("- If no category exists in source data, infer an appropriate category name from the product name and description (e.g., 'Industrial Drill Press' → 'Power Tools', 'Monthly Bookkeeping' → 'Bookkeeping Services', 'Copper Pipe' → 'Plumbing')");
            sb.AppendLine("- Set type to 'Expense' for products/services that are typically purchased or expensed (e.g., office supplies, bookkeeping, equipment rental), and 'Revenue' for items typically sold to customers");
        }

        var prompt = sb.ToString();
        return prompt;
    }

    private static string BuildTier2UserPrompt(List<string> headers, List<List<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Convert these rows:");
        sb.AppendLine();

        sb.Append("| ");
        sb.Append(string.Join(" | ", headers));
        sb.AppendLine(" |");

        sb.Append("| ");
        sb.Append(string.Join(" | ", headers.Select(_ => "---")));
        sb.AppendLine(" |");

        foreach (var row in rows)
        {
            sb.Append("| ");
            var cells = new List<string>(row);
            while (cells.Count < headers.Count)
                cells.Add("");
            sb.Append(string.Join(" | ", cells.Select(c => c.Replace("|", "\\|"))));
            sb.AppendLine(" |");
        }

        return sb.ToString();
    }

    #endregion

    #region Response Parsing

    internal static SpreadsheetAnalysisResult? ParseAnalysisResponse(string response)
    {
        try
        {
            var cleanResponse = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanResponse);
            var root = doc.RootElement;

            var result = new SpreadsheetAnalysisResult();

            if (root.TryGetProperty("sheets", out var sheetsArray))
            {
                foreach (var sheetEl in sheetsArray.EnumerateArray())
                {
                    var sheet = new SheetAnalysis
                    {
                        SourceSheetName = sheetEl.GetProperty("sourceSheetName").GetString() ?? "",
                        Confidence = GetDouble(sheetEl, "confidence"),
                    };

                    // Parse detected type
                    var typeStr = sheetEl.GetProperty("detectedType").GetString() ?? "Unknown";
                    sheet.DetectedType = Enum.TryParse<SpreadsheetSheetType>(typeStr, ignoreCase: true, out var parsed)
                        ? parsed
                        : SpreadsheetSheetType.Unknown;

                    // Parse tier
                    var tierStr = GetString(sheetEl, "tier");
                    sheet.Tier = tierStr.Contains("Tier2", StringComparison.OrdinalIgnoreCase)
                        ? ProcessingTier.Tier2_LlmProcessing
                        : ProcessingTier.Tier1_Mapping;
                    sheet.TierReason = GetString(sheetEl, "tierReason");

                    // Parse column mappings
                    if (sheetEl.TryGetProperty("columnMappings", out var mappingsArray))
                    {
                        foreach (var mapEl in mappingsArray.EnumerateArray())
                        {
                            sheet.ColumnMappings.Add(new ColumnMapping
                            {
                                SourceColumn = GetString(mapEl, "sourceColumn"),
                                TargetColumn = GetString(mapEl, "targetColumn"),
                                Confidence = GetDouble(mapEl, "confidence"),
                                TransformHint = mapEl.TryGetProperty("transformHint", out var hint) && hint.ValueKind != JsonValueKind.Null
                                    ? hint.GetString() : null,
                            });
                        }
                    }

                    // Parse unmapped columns
                    if (sheetEl.TryGetProperty("unmappedSourceColumns", out var unmappedSrc))
                    {
                        foreach (var col in unmappedSrc.EnumerateArray())
                            sheet.UnmappedSourceColumns.Add(col.GetString() ?? "");
                    }

                    if (sheetEl.TryGetProperty("unmappedTargetColumns", out var unmappedTgt))
                    {
                        foreach (var col in unmappedTgt.EnumerateArray())
                            sheet.UnmappedTargetColumns.Add(col.GetString() ?? "");
                    }

                    result.Sheets.Add(sheet);
                }
            }

            if (root.TryGetProperty("warnings", out var warningsArray))
            {
                foreach (var warning in warningsArray.EnumerateArray())
                    result.Warnings.Add(warning.GetString() ?? "");
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static LlmProcessedData? ParseTier2Response(string response, SpreadsheetSheetType entityType, int sourceRowCount)
    {
        try
        {
            var cleanResponse = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanResponse);

            var result = new LlmProcessedData
            {
                EntityType = entityType,
                SourceRowsProcessed = sourceRowCount,
            };

            // Response should be an array of entity objects
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entity in doc.RootElement.EnumerateArray())
                {
                    result.Entities.Add(entity.Clone());
                }
            }
            else if (doc.RootElement.TryGetProperty("entities", out var entitiesArray))
            {
                foreach (var entity in entitiesArray.EnumerateArray())
                {
                    result.Entities.Add(entity.Clone());
                }
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string CleanJsonResponse(string response)
    {
        var cleaned = response.Trim();
        if (cleaned.StartsWith("```"))
        {
            var startIndex = cleaned.IndexOf('\n') + 1;
            var endIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (endIndex > startIndex)
                cleaned = cleaned[startIndex..endIndex].Trim();
        }
        return cleaned;
    }

    private static string GetString(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() ?? "" : "";
    }

    private static double GetDouble(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return 0;
        return val.ValueKind == JsonValueKind.Number ? val.GetDouble() : 0;
    }

    #endregion

    #region Data Extraction Helpers

    /// <summary>
    /// Finds the header row by scanning for the first row with at least 2 non-empty cells.
    /// Falls back to row 1 if no such row is found within the first 10 rows.
    /// </summary>
    private static int FindHeaderRow(IXLWorksheet worksheet)
    {
        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 1, 10);
        var colCount = worksheet.ColumnsUsed().Count();

        for (int rowNum = 1; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            int nonEmpty = 0;
            for (int col = 1; col <= colCount; col++)
            {
                if (!row.Cell(col).IsEmpty()) nonEmpty++;
                if (nonEmpty >= 2) return rowNum;
            }
        }

        return 1;
    }

    private static List<string> GetHeaders(IXLWorksheet worksheet)
    {
        return GetHeaders(worksheet, FindHeaderRow(worksheet));
    }

    private static List<string> GetHeaders(IXLWorksheet worksheet, int headerRow)
    {
        var headers = new List<string>();
        var row = worksheet.Row(headerRow);
        for (int col = 1; col <= worksheet.ColumnsUsed().Count(); col++)
        {
            var cell = row.Cell(col);
            if (cell.IsEmpty()) break;
            headers.Add(cell.GetString().Trim());
        }
        return headers;
    }

    private static List<List<string>> GetSampleRows(IXLWorksheet worksheet, int columnCount, int totalRows)
    {
        if (totalRows <= 0) return [];

        var headerRow = FindHeaderRow(worksheet);
        var indices = GetSampleIndices(totalRows);
        var result = new List<List<string>>();

        foreach (var rowIdx in indices)
        {
            var xlRow = worksheet.Row(rowIdx + headerRow + 1); // +headerRow+1: skip header, data starts after
            var rowData = new List<string>();
            for (int col = 1; col <= columnCount; col++)
            {
                var cell = xlRow.Cell(col);
                rowData.Add(cell.IsEmpty() ? "" : CellToString(cell));
            }
            result.Add(rowData);
        }

        return result;
    }

    private static List<List<string>> GetAllRowsAsStrings(IXLWorksheet worksheet, int columnCount)
    {
        var headerRow = FindHeaderRow(worksheet);
        var rows = new List<List<string>>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = headerRow + 1; rowNum <= lastRow; rowNum++)
        {
            var xlRow = worksheet.Row(rowNum);
            var rowData = new List<string>();
            bool isEmpty = true;

            for (int col = 1; col <= columnCount; col++)
            {
                var cell = xlRow.Cell(col);
                var val = cell.IsEmpty() ? "" : CellToString(cell);
                if (!string.IsNullOrEmpty(val)) isEmpty = false;
                rowData.Add(val);
            }

            if (!isEmpty)
                rows.Add(rowData);
        }

        return rows;
    }

    private static string CellToString(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        return cell.DataType switch
        {
            XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean().ToString(),
            _ => cell.GetString()
        };
    }

    /// <summary>
    /// Gets sample row indices: first N, last M, and P random from the middle.
    /// </summary>
    internal static List<int> GetSampleIndices(int totalRows)
    {
        if (totalRows <= SampleFirstRows + SampleLastRows + SampleRandomRows)
        {
            // Return all rows if the total is small enough
            return Enumerable.Range(0, totalRows).ToList();
        }

        var indices = new HashSet<int>();

        // First rows
        for (int i = 0; i < SampleFirstRows; i++)
            indices.Add(i);

        // Last rows
        for (int i = totalRows - SampleLastRows; i < totalRows; i++)
            indices.Add(i);

        // Random from middle
        var rng = new Random(42); // deterministic seed for reproducibility
        var middleStart = SampleFirstRows;
        var middleEnd = totalRows - SampleLastRows;
        var attempts = 0;
        while (indices.Count < SampleFirstRows + SampleLastRows + SampleRandomRows && attempts < 50)
        {
            indices.Add(rng.Next(middleStart, middleEnd));
            attempts++;
        }

        return indices.OrderBy(i => i).ToList();
    }

    private static List<List<string>> GetSampleFromList(List<List<string>> allRows, int totalRows)
    {
        var indices = GetSampleIndices(totalRows);
        return indices.Where(i => i < allRows.Count).Select(i => allRows[i]).ToList();
    }

    #endregion

    #region CSV Helpers

    internal static char DetectCsvDelimiter(string headerLine)
    {
        char[] candidates = [',', '\t', ';', '|'];
        var maxCount = 0;
        var bestDelimiter = ',';

        foreach (var delimiter in candidates)
        {
            var count = headerLine.Count(c => c == delimiter);
            if (count > maxCount)
            {
                maxCount = count;
                bestDelimiter = delimiter;
            }
        }

        return bestDelimiter;
    }

    internal static List<string> ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }

    #endregion
}
