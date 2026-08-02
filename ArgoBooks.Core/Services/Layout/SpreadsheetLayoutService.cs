using System.Globalization;
using System.Text;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services.Layout;

/// <summary>
/// Asks an LLM to describe the layout of a messy worksheet, returning a
/// <see cref="LayoutDescriptor"/> that the deterministic <see cref="GridExtractor"/>
/// then turns into a clean <c>headers + rows</c> table.
/// <para>
/// The service ships the model a <b>compact</b> view of the sheet rather than every
/// cell: a per-row shape line for every row (so it can locate a header band far down a
/// long preamble), the actual cell content for a focused window (so it can read the
/// header labels), and the merged ranges (so it can detect multi-row / merged headers).
/// It returns <c>null</c> on any failure (null/empty response, malformed JSON), which
/// the caller treats as "fall back to the existing heuristic path".
/// </para>
/// <para>
/// This is a single-pass spike (Phase 3, Task 3). A future enhancement could add a
/// cost-gated second pass that reads specific coordinates the model requests when the
/// first answer is low-confidence; that is intentionally out of scope here.
/// </para>
/// </summary>
public sealed class SpreadsheetLayoutService
{
    private readonly IGeminiService _geminiService;
    private readonly IErrorLogger? _errorLogger;

    /// <summary>Max tokens for the descriptor response. Descriptors are small.</summary>
    private const int MaxResponseTokens = 3000;

    /// <summary>How many leading rows to show in full (cell content).</summary>
    private const int HeaderWindowRows = 15;

    /// <summary>How many additional sample data rows to show from lower in the sheet.</summary>
    private const int SampleDataRows = 5;

    /// <summary>Truncate any single cell's content to keep the prompt compact.</summary>
    private const int MaxCellChars = 40;

    public SpreadsheetLayoutService(IGeminiService geminiService, IErrorLogger? errorLogger = null)
    {
        _geminiService = geminiService;
        _errorLogger = errorLogger;
    }

    /// <summary>
    /// Asks the model to describe <paramref name="grid"/>'s layout and parses the
    /// answer into a <see cref="LayoutDescriptor"/>. Returns <c>null</c> if the grid
    /// is empty, the model returns nothing, or the response cannot be parsed.
    /// </summary>
    public async Task<LayoutDescriptor?> GetLayoutDescriptorAsync(SheetGrid grid, CancellationToken ct = default)
    {
        if (grid.RowCount == 0 || grid.ColCount == 0)
            return null;

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(grid);

        string? response;
        try
        {
            response = await _geminiService.SendChatAsync(
                systemPrompt, userPrompt, MaxResponseTokens, temperature: 0.0, ct);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Import, "Layout descriptor request failed");
            return null;
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            _errorLogger?.LogWarning("Layout descriptor response was null or empty", "SpreadsheetLayoutService");
            return null;
        }

        var cleaned = JsonResponseHelper.StripMarkdownCodeBlock(response);
        try
        {
            return ParseDescriptor(cleaned);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Parsing, "Failed to parse layout descriptor JSON");
            return null;
        }
    }

    // ─── Prompt construction ─────────────────────────────────────────────────

    private static string BuildSystemPrompt() =>
        """
        You are a spreadsheet layout analyzer. You are given a compact summary of one
        worksheet and you describe where the real data tables are, so a deterministic
        program can extract clean header+row tables.

        Sheets may be messy: long title/preamble rows before the real header, multi-row
        or merged headers, subtotal/note rows mixed into the data, multiple stacked
        tables, or cross-tab ("wide") layouts where values are spread across columns that
        are really one category.

        Respond with STRICT JSON only. No prose, no markdown, no code fences. Output a
        single JSON object with this exact shape:

        {
          "tables": [
            {
              "firstDataRow": <int>,   // 0-based row index of the first DATA row (not header)
              "lastDataRow": <int>,    // 0-based row index of the last data row (inclusive)
              "firstCol": <int>,       // 0-based first column of the table region
              "lastCol": <int>,        // 0-based last column (inclusive)
              "headerRows": [<int>...], // 0-based header row indices, top-to-bottom; may be empty
              "orientation": "long" | "wide",
              "ignoreRows": [<int>...], // 0-based data rows to skip (subtotals, notes, blanks)
              "keyColumns": [<int>...]  // for "wide" only: the row-key columns; null/omit for "long"
            }
          ]
        }

        Rules:
        - All indices are 0-based into the row/column grid described below.
        - "long" = one record per row (the normal case). "wide" = a cross-tab where data
          columns are really one spread-out category; set keyColumns to the identifying
          columns and the program will transpose the rest into long form.
        - Prefer one table unless the sheet clearly contains multiple separate tables.
        - If you cannot find any real data table, return {"tables": []}.
        """;

    private static string BuildUserPrompt(SheetGrid grid)
    {
        var sb = new StringBuilder();
        sb.Append("Worksheet summary. RowCount=").Append(grid.RowCount)
          .Append(", ColCount=").Append(grid.ColCount).Append(".\n\n");

        // 1) Per-row shape profile for EVERY row so the model can locate the header band.
        sb.Append("PER-ROW SHAPE (one line per row, all rows). ")
          .Append("Format: row=<0-based index> nonEmpty=<count> numericFrac=<0..1> textFrac=<0..1>\n");
        for (int r = 0; r < grid.RowCount; r++)
        {
            var shape = grid.Shapes[r];
            sb.Append("row=").Append(r)
              .Append(" nonEmpty=").Append(shape.NonEmpty)
              .Append(" numericFrac=").Append(shape.NumericFraction.ToString("0.00", CultureInfo.InvariantCulture))
              .Append(" textFrac=").Append(shape.TextFraction.ToString("0.00", CultureInfo.InvariantCulture))
              .Append('\n');
        }
        sb.Append('\n');

        // 2) Cell content for a focused window: the first N rows in full, plus a few
        //    sample data rows further down so the model can read header/label text.
        sb.Append("CELL CONTENT (focused window; cells shown as col=<0-based>:\"value\").\n");

        int headerWindowEnd = Math.Min(HeaderWindowRows, grid.RowCount);
        for (int r = 0; r < headerWindowEnd; r++)
            AppendRowContent(sb, grid, r);

        // Sample a few rows beyond the header window (evenly spaced) so the model sees data shape.
        if (grid.RowCount > headerWindowEnd)
        {
            sb.Append("... sample data rows further down ...\n");
            int remaining = grid.RowCount - headerWindowEnd;
            int step = Math.Max(1, remaining / SampleDataRows);
            for (int r = headerWindowEnd; r < grid.RowCount; r += step)
                AppendRowContent(sb, grid, r);
        }
        sb.Append('\n');

        // 3) Merged ranges (1-based ClosedXML coords) so the model can detect merged headers.
        sb.Append("MERGED RANGES (1-based row/col coordinates: firstRow,firstCol-lastRow,lastCol).\n");
        if (grid.MergedRanges.Count == 0)
        {
            sb.Append("(none)\n");
        }
        else
        {
            foreach (var m in grid.MergedRanges)
                sb.Append(m.FirstRow).Append(',').Append(m.FirstCol)
                  .Append('-').Append(m.LastRow).Append(',').Append(m.LastCol).Append('\n');
        }

        sb.Append("\nReturn the JSON layout descriptor now.");
        return sb.ToString();
    }

    private static void AppendRowContent(StringBuilder sb, SheetGrid grid, int row)
    {
        var cells = grid.Cells[row];
        sb.Append("row=").Append(row).Append(':');
        bool any = false;
        for (int c = 0; c < cells.Count; c++)
        {
            var val = cells[c];
            if (string.IsNullOrEmpty(val))
                continue;
            any = true;
            sb.Append(" col=").Append(c).Append(":\"").Append(Truncate(val)).Append('"');
        }
        if (!any)
            sb.Append(" (empty)");
        sb.Append('\n');
    }

    private static string Truncate(string value)
    {
        // Collapse newlines/tabs to spaces so each cell stays on one line, then cap length.
        var flat = value.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ').Replace("\"", "'");
        if (flat.Length > MaxCellChars)
            flat = flat[..MaxCellChars] + "...";
        return flat;
    }

    // ─── Tolerant JSON parsing ───────────────────────────────────────────────

    /// <summary>
    /// Parses a cleaned JSON string into a <see cref="LayoutDescriptor"/>. Tolerant of
    /// missing fields (sensible defaults) and case-insensitive property names. Throws on
    /// genuinely malformed JSON, which the caller catches and turns into a null result.
    /// </summary>
    private static LayoutDescriptor ParseDescriptor(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var descriptor = new LayoutDescriptor();

        if (!TryGetProperty(root, "tables", out var tablesEl) || tablesEl.ValueKind != JsonValueKind.Array)
            return descriptor; // no tables -> empty descriptor (valid: "found nothing")

        foreach (var tableEl in tablesEl.EnumerateArray())
        {
            if (tableEl.ValueKind != JsonValueKind.Object)
                continue;

            var region = new TableRegion
            {
                FirstDataRow = GetInt(tableEl, "firstDataRow"),
                LastDataRow = GetInt(tableEl, "lastDataRow"),
                FirstCol = GetInt(tableEl, "firstCol"),
                LastCol = GetInt(tableEl, "lastCol"),
                HeaderRows = GetIntList(tableEl, "headerRows"),
                Orientation = GetOrientation(tableEl),
                IgnoreRows = GetIntList(tableEl, "ignoreRows"),
                KeyColumns = GetNullableIntList(tableEl, "keyColumns"),
            };

            descriptor.Tables.Add(region);
        }

        return descriptor;
    }

    private static string GetOrientation(JsonElement el)
    {
        if (TryGetProperty(el, "orientation", out var val) && val.ValueKind == JsonValueKind.String)
        {
            var s = val.GetString();
            if (string.Equals(s, "wide", StringComparison.OrdinalIgnoreCase))
                return "wide";
        }
        return "long"; // default
    }

    private static int GetInt(JsonElement el, string prop)
    {
        if (!TryGetProperty(el, prop, out var val))
            return 0;

        return val.ValueKind switch
        {
            JsonValueKind.Number when val.TryGetInt32(out var i) => i,
            JsonValueKind.Number => (int)Math.Round(val.GetDouble()),
            JsonValueKind.String when int.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) => s,
            _ => 0,
        };
    }

    private static List<int> GetIntList(JsonElement el, string prop)
    {
        var list = new List<int>();
        if (!TryGetProperty(el, prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var i))
                list.Add(i);
            else if (item.ValueKind == JsonValueKind.Number)
                list.Add((int)Math.Round(item.GetDouble()));
            else if (item.ValueKind == JsonValueKind.String &&
                     int.TryParse(item.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
                list.Add(s);
        }
        return list;
    }

    private static List<int>? GetNullableIntList(JsonElement el, string prop)
    {
        if (!TryGetProperty(el, prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null; // missing or null -> not a wide table
        return GetIntList(el, prop);
    }

    /// <summary>Case-insensitive property lookup (models may vary header casing).</summary>
    private static bool TryGetProperty(JsonElement el, string name, out JsonElement value)
    {
        if (el.TryGetProperty(name, out value))
            return true;

        foreach (var prop in el.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
