using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ArgoBooks.Core.Services;

/// <summary>
/// RFC-4180 CSV reader: handles quoted fields with embedded newlines/commas, BOM, and
/// auto-detects the delimiter from the first non-empty line.
/// </summary>
public static class CsvReader
{
    public static List<List<string>> ReadAllRows(string path, out List<string> headers)
    {
        var delimiter = DetectDelimiter(path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = false,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = false,
        };

        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvParser(reader, config);

        var all = new List<string[]>();
        while (csv.Read())
            all.Add(csv.Record ?? []);

        headers = all.Count > 0 ? all[0].ToList() : [];
        return all.Skip(1)
                  .Where(r => r.Any(f => !string.IsNullOrWhiteSpace(f)))
                  .Select(r => r.ToList())
                  .ToList();
    }

    public static char DetectDelimiter(string path)
    {
        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        string? first;
        do { first = reader.ReadLine(); }
        while (first != null && string.IsNullOrWhiteSpace(first));
        return SpreadsheetAnalysisService.DetectCsvDelimiter(first ?? "");
    }
}
