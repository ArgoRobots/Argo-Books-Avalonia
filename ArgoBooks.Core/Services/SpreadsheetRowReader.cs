using System.Globalization;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Pure, reusable helpers for reading and parsing spreadsheet rows. Extracted from
/// <see cref="SpreadsheetImportService"/> so that other features (e.g. bank statement
/// matching) can parse arbitrary spreadsheet/CSV data into typed values without
/// pulling in the entity-import machinery. Has no dependency on CompanyData.
/// </summary>
internal static class SpreadsheetRowReader
{
    /// <summary>
    /// Finds the header row by scanning for the first row with at least 2 non-empty cells.
    /// Falls back to row 1 if no such row is found within the first 10 rows.
    /// </summary>
    public static int FindHeaderRow(IXLWorksheet worksheet)
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

    public static List<string> GetHeaders(IXLWorksheet worksheet)
    {
        return GetHeaders(worksheet, FindHeaderRow(worksheet));
    }

    public static List<string> GetHeaders(IXLWorksheet worksheet, int headerRow)
    {
        var headers = new List<string>();
        var row = worksheet.Row(headerRow);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = row.Cell(col);
            headers.Add(cell.IsEmpty() ? "" : cell.GetString().Trim());
        }

        return headers;
    }

    public static List<List<object?>> GetDataRows(IXLWorksheet worksheet, int columnCount)
    {
        var headerRow = FindHeaderRow(worksheet);
        var rows = new List<List<object?>>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = headerRow + 1; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            var rowData = new List<object?>();
            var isEmpty = true;

            for (int col = 1; col <= columnCount; col++)
            {
                var cell = row.Cell(col);
                if (!cell.IsEmpty()) isEmpty = false;
                rowData.Add(GetCellValue(cell));
            }

            if (!isEmpty)
            {
                rows.Add(rowData);
            }
        }

        return rows;
    }

    public static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble(),
            XLDataType.DateTime => cell.GetDateTime(),
            XLDataType.Boolean => cell.GetBoolean(),
            _ => cell.GetString()
        };
    }

    public static int GetColumnIndex(List<string> headers, string columnName)
    {
        // Case-insensitive column lookup
        for (int i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    public static string GetString(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return string.Empty;
        return row[index]?.ToString() ?? string.Empty;
    }

    public static string? GetNullableString(List<object?> row, List<string> headers, string columnName)
    {
        var value = GetString(row, headers, columnName);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static decimal GetDecimal(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return 0m;

        var value = row[index];
        return value switch
        {
            double d => (decimal)d,
            decimal dec => dec,
            int i => i,
            long l => l,
            string s => ParseDecimalString(s),
            _ => 0m
        };
    }

    /// <summary>
    /// Returns the decimal value for a column, or null when the column is absent or empty.
    /// Distinguishes "no value" from a genuine 0.
    /// </summary>
    public static decimal? GetNullableDecimal(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return null;

        var value = row[index];
        return value switch
        {
            null => null,
            double d => (decimal)d,
            decimal dec => dec,
            int i => i,
            long l => l,
            string s when string.IsNullOrWhiteSpace(s) => null,
            string s => ParseDecimalString(s),
            _ => null
        };
    }

    public static decimal ParseDecimalString(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;

        // Strip common currency symbols and whitespace before parsing
        var cleaned = s.Trim();
        foreach (var symbol in new[] { "$", "€", "£", "¥", "₹", "CHF", "CAD", "AUD", "USD", "EUR", "GBP" })
            cleaned = cleaned.Replace(symbol, "", StringComparison.OrdinalIgnoreCase);
        cleaned = cleaned.Trim();

        // Handle parentheses as negative: (123.45) → -123.45
        if (cleaned.StartsWith('(') && cleaned.EndsWith(')'))
            cleaned = "-" + cleaned[1..^1];

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0m;
    }

    public static int GetInt(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return 0;

        var value = row[index];
        return value switch
        {
            double d => (int)d,
            decimal dec => (int)dec,
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var result) => result,
            _ => 0
        };
    }

    public static DateTime GetDateTime(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return DateTime.MinValue;

        var value = row[index];
        return value switch
        {
            DateTime dt => dt,
            double d => DateTime.FromOADate(d),
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) => result,
            _ => DateTime.MinValue
        };
    }

    public static DateTime? GetNullableDateTime(List<object?> row, List<string> headers, string columnName)
    {
        var dt = GetDateTime(row, headers, columnName);
        return dt == DateTime.MinValue ? null : dt;
    }
}
