using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Telemetry;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Parses a bank statement spreadsheet/CSV into a list of <see cref="BankStatementLine"/> using
/// the column mappings produced by <see cref="SpreadsheetAnalysisService"/>. This is the "smart
/// importer" reused for bank matching: it never commits anything to CompanyData; it only produces
/// reference data for the matching engine.
/// </summary>
public class BankStatementImportService(IErrorLogger? errorLogger = null)
{
    // Target column names, matching the BankStatement schema in ImportSchemaDefinition.
    // ApplyColumnMapping renames the source headers to these before we read them.
    private const string ColDate = "Date";
    private const string ColDescription = "Description";
    private const string ColAmount = "Amount";
    private const string ColDebit = "Debit";
    private const string ColCredit = "Credit";
    private const string ColBalance = "Balance";
    private const string ColReference = "Reference";

    /// <summary>
    /// Parses an Excel bank statement. The sheet to read is selected from <paramref name="analysis"/>.
    /// </summary>
    public Task<List<BankStatementLine>> ParseExcelAsync(string filePath, SheetAnalysis analysis, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var workbook = new XLWorkbook(fileStream);

                var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == analysis.SourceSheetName)
                                ?? workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return new List<BankStatementLine>();

                var headers = SpreadsheetRowReader.GetHeaders(worksheet);
                if (headers.Count == 0) return new List<BankStatementLine>();

                var rows = SpreadsheetRowReader.GetDataRows(worksheet, headers.Count);
                SpreadsheetImportService.ApplyColumnMapping(headers, analysis);

                return BuildLines(headers, rows, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                errorLogger?.LogError(ex, ErrorCategory.Import, $"Failed to parse bank statement: {Path.GetFileName(filePath)}");
                return new List<BankStatementLine>();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Parses a CSV bank statement.
    /// </summary>
    public async Task<List<BankStatementLine>> ParseCsvAsync(string filePath, SheetAnalysis analysis, CancellationToken cancellationToken = default)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            if (lines.Length < 2) return [];

            var delimiter = SpreadsheetAnalysisService.DetectCsvDelimiter(lines[0]);
            var headers = SpreadsheetAnalysisService.ParseCsvLine(lines[0], delimiter);
            if (headers.Count == 0) return [];

            var rows = new List<List<object?>>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cells = SpreadsheetAnalysisService.ParseCsvLine(lines[i], delimiter);
                rows.Add(cells.Cast<object?>().ToList());
            }

            SpreadsheetImportService.ApplyColumnMapping(headers, analysis);
            return BuildLines(headers, rows, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            errorLogger?.LogError(ex, ErrorCategory.Import, $"Failed to parse bank statement CSV: {Path.GetFileName(filePath)}");
            return [];
        }
    }

    private static List<BankStatementLine> BuildLines(List<string> headers, List<List<object?>> rows, CancellationToken cancellationToken)
    {
        var result = new List<BankStatementLine>(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows[i];

            var date = SpreadsheetRowReader.GetDateTime(row, headers, ColDate);
            var description = SpreadsheetRowReader.GetString(row, headers, ColDescription);
            var debit = SpreadsheetRowReader.GetNullableDecimal(row, headers, ColDebit);
            var credit = SpreadsheetRowReader.GetNullableDecimal(row, headers, ColCredit);
            var signedAmount = SpreadsheetRowReader.GetNullableDecimal(row, headers, ColAmount);
            var balance = SpreadsheetRowReader.GetNullableDecimal(row, headers, ColBalance);
            var reference = SpreadsheetRowReader.GetString(row, headers, ColReference);

            // Skip rows with neither a date nor any monetary value (e.g. blank/summary rows).
            if (date == DateTime.MinValue && signedAmount is null && debit is null && credit is null)
                continue;

            // Canonical signed amount: negative = money out, positive = money in.
            // Prefer an explicit signed Amount column; otherwise derive from Credit - Debit.
            var amount = signedAmount ?? ((credit ?? 0m) - (debit ?? 0m));

            result.Add(new BankStatementLine
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = date,
                Description = description,
                Amount = amount,
                Debit = debit,
                Credit = credit,
                Balance = balance,
                RawReference = reference,
                MatchStatus = BankLineMatchStatus.Unmatched,
                SourceRowIndex = i
            });
        }

        return result;
    }
}
