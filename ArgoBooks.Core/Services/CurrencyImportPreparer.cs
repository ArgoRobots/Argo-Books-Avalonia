using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Models.Common;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// One distinct ambiguous currency symbol found in a file's amount cells, awaiting user resolution.
/// </summary>
public sealed class AmbiguousSymbolPrompt
{
    public required string Symbol { get; init; }
    public required IReadOnlyList<string> Candidates { get; init; }
    /// <summary>How many amount rows across the file use this symbol (for "appears in N rows").</summary>
    public int RowCount { get; set; }
}

/// <summary>
/// The outcome of scanning a workbook's amount cells for in-cell currency.
/// </summary>
public sealed class CurrencyScanResult
{
    /// <summary>Sheet name -> (0-based data-row ordinal -> resolved ISO code).</summary>
    public Dictionary<string, Dictionary<int, string>> Resolved { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Distinct ambiguous symbols present in the file, for the resolution dialog.</summary>
    public List<AmbiguousSymbolPrompt> Ambiguities { get; } = [];

    /// <summary>Sheet -> (row ordinal -> raw ambiguous symbol), resolved later via the user's choice.</summary>
    internal Dictionary<string, Dictionary<int, string>> PendingAmbiguous { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Deterministic pre-pass that scans the amount columns of the financial sheets and resolves the
/// per-row currency from in-cell symbols/codes BEFORE import. Rows with an explicit code or an
/// unambiguous symbol are resolved immediately; rows with an ambiguous symbol (e.g. "$") are held
/// until the user resolves the symbol (see <see cref="ApplyResolution"/>). The resulting per-row
/// map is fed to the Tier 1 importer via <see cref="ImportOptions.RowCurrencyBySheet"/>.
/// </summary>
public static class CurrencyImportPreparer
{
    /// <summary>Target column names that hold a transaction amount worth inspecting for currency.</summary>
    private static readonly HashSet<string> MoneyTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Total", "Amount", "Unit Price", "Subtotal"
    };

    /// <summary>Sheet types that carry a currency + USD representation (the financial records).</summary>
    private static readonly HashSet<SpreadsheetSheetType> FinancialTypes =
    [
        SpreadsheetSheetType.Revenue,
        SpreadsheetSheetType.Expenses,
        SpreadsheetSheetType.Payments,
        SpreadsheetSheetType.Invoices,
        SpreadsheetSheetType.PurchaseOrders
    ];

    /// <summary>
    /// Scans the financial, Tier 1 sheets of <paramref name="filePath"/> for in-cell currency.
    /// CSV files are not scanned here (single sheet, handled by the caller's CSV path if needed).
    /// </summary>
    public static CurrencyScanResult ScanWorkbook(string filePath, SpreadsheetAnalysisResult analysis)
    {
        var result = new CurrencyScanResult();
        if (analysis is null || filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return result;

        // Aggregate ambiguous symbols across sheets (symbol -> count) so the user is asked once.
        var ambiguousCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        // Read-share so the scan works even if the file is open in Excel (mirrors the importer).
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new XLWorkbook(stream);

        foreach (var sheet in analysis.Sheets)
        {
            if (!sheet.IsIncluded
                || sheet.UnsupportedReason != null
                || sheet.Tier != ProcessingTier.Tier1_Mapping
                || !FinancialTypes.Contains(sheet.DetectedType))
                continue;

            if (!workbook.TryGetWorksheet(sheet.SourceSheetName, out var ws))
                continue;

            ScanSheet(ws, sheet, result, ambiguousCounts);
        }

        foreach (var (symbol, count) in ambiguousCounts)
        {
            result.Ambiguities.Add(new AmbiguousSymbolPrompt
            {
                Symbol = symbol,
                Candidates = CurrencyInfo.CandidatesForSymbol(symbol),
                RowCount = count
            });
        }

        return result;
    }

    private static void ScanSheet(
        IXLWorksheet ws, SheetAnalysis sheet, CurrencyScanResult result,
        Dictionary<string, int> ambiguousCounts)
    {
        var headerRow = SpreadsheetRowReader.FindHeaderRow(ws);
        var headers = SpreadsheetRowReader.GetHeaders(ws, headerRow);
        if (headers.Count == 0)
            return;

        // Rename headers to their mapped target so we can spot the amount columns by index.
        var mapped = new List<string>(headers);
        foreach (var m in sheet.ColumnMappings)
        {
            var idx = mapped.FindIndex(h => string.Equals(h, m.SourceColumn, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) mapped[idx] = m.TargetColumn;
        }

        var amountCols = new List<int>(); // 1-based worksheet column numbers
        for (int i = 0; i < mapped.Count; i++)
            if (MoneyTargets.Contains(mapped[i]))
                amountCols.Add(i + 1);

        if (amountCols.Count == 0)
            return;

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        int ordinal = 0; // matches SpreadsheetRowReader.GetDataRows (empty rows skipped, not counted)

        for (int rowNum = headerRow + 1; rowNum <= lastRow; rowNum++)
        {
            var xlRow = ws.Row(rowNum);

            // GetDataRows skips rows where all of the first headers.Count cells are empty.
            bool allEmpty = true;
            for (int col = 1; col <= headers.Count; col++)
            {
                if (!xlRow.Cell(col).IsEmpty()) { allEmpty = false; break; }
            }
            if (allEmpty)
                continue;

            // First resolved code on any amount cell wins; otherwise remember an ambiguous symbol.
            string? resolved = null;
            string? pendingSymbol = null;
            foreach (var col in amountCols)
            {
                var text = ReadFormatted(xlRow.Cell(col));
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var d = CurrencyCellDetector.Detect(text);
                if (d.Code != null) { resolved = d.Code; break; }
                if (d.AmbiguousSymbol != null)
                {
                    // A symbol shared by currencies with different decimal conventions (e.g. "¥" =
                    // JPY with 0 decimals or CNY with 2) is resolved from the cell's own formatting
                    // when the displayed decimal count picks exactly one candidate, so it doesn't
                    // need to prompt and won't default to the wrong currency.
                    var byFormat = DisambiguateByDecimals(d.AmbiguousSymbol, text);
                    if (byFormat != null) { resolved = byFormat; break; }
                    if (pendingSymbol == null)
                        pendingSymbol = d.AmbiguousSymbol;
                }
            }

            if (resolved != null)
            {
                AddResolved(result.Resolved, sheet.SourceSheetName, ordinal, resolved);
            }
            else if (pendingSymbol != null)
            {
                AddResolved(result.PendingAmbiguous, sheet.SourceSheetName, ordinal, pendingSymbol);
                ambiguousCounts[pendingSymbol] = ambiguousCounts.GetValueOrDefault(pendingSymbol) + 1;
            }

            ordinal++;
        }
    }

    /// <summary>
    /// After the user resolves each ambiguous symbol to a code, fills in the held rows so the
    /// per-row map is complete. <paramref name="symbolToCode"/> is "$" -> "CAD" etc.
    /// </summary>
    public static void ApplyResolution(CurrencyScanResult scan, IReadOnlyDictionary<string, string> symbolToCode)
    {
        if (scan is null || symbolToCode is null) return;

        foreach (var (sheetName, rows) in scan.PendingAmbiguous)
        {
            foreach (var (ordinal, symbol) in rows)
            {
                if (symbolToCode.TryGetValue(symbol, out var code) && !string.IsNullOrWhiteSpace(code))
                    AddResolved(scan.Resolved, sheetName, ordinal, code);
            }
        }
    }

    private static void AddResolved(Dictionary<string, Dictionary<int, string>> map, string sheet, int ordinal, string code)
    {
        if (!map.TryGetValue(sheet, out var rows))
        {
            rows = [];
            map[sheet] = rows;
        }
        rows[ordinal] = code;
    }

    /// <summary>
    /// Resolves an ambiguous currency symbol from the cell's displayed decimal count when that
    /// uniquely identifies one candidate (e.g. "¥95,000" -> JPY (0 dp), "¥1,200.00" -> CNY (2 dp)).
    /// Returns <c>null</c> when the symbol is unknown, has a single candidate, or more than one
    /// candidate shares the displayed decimal count (e.g. "$" -> USD/CAD/AUD are all 2 dp).
    /// </summary>
    private static string? DisambiguateByDecimals(string symbol, string formattedText)
    {
        var candidates = CurrencyInfo.CandidatesForSymbol(symbol);
        if (candidates.Count < 2)
            return null;

        var shown = DisplayedDecimalPlaces(formattedText);
        string? match = null;
        foreach (var code in candidates)
        {
            if (CurrencyInfo.GetByCode(code).DecimalPlaces != shown)
                continue;
            if (match != null)
                return null; // more than one candidate shares this decimal count
            match = code;
        }
        return match;
    }

    /// <summary>Counts the fractional digits shown in a formatted amount, e.g. "¥1,200.00" -> 2,
    /// "¥95,000" -> 0. Uses the last '.' as the decimal point (thousands use ',').</summary>
    private static int DisplayedDecimalPlaces(string text)
    {
        var dot = text.LastIndexOf('.');
        if (dot < 0)
            return 0;

        var count = 0;
        for (var i = dot + 1; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]))
                count++;
            else
                break;
        }
        return count;
    }

    /// <summary>
    /// Reads the cell's displayed text so currency carried in the NUMBER FORMAT (e.g. a numeric
    /// 100 shown as "£100.00") is visible, not just text like "£100". Falls back to the raw string.
    /// </summary>
    private static string ReadFormatted(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";
        try { return cell.GetFormattedString(); }
        catch { return cell.GetString(); }
    }
}
