namespace ArgoBooks.Core.Services;

/// <summary>
/// Small shared helper for writing CSV. Single source of truth for RFC-4180 field quoting so the
/// various export/preview CSV builders don't each carry their own (subtly different) copy.
/// </summary>
public static class CsvWriter
{
    /// <summary>
    /// Quotes a CSV field per RFC 4180 when it contains a comma, double-quote, or newline
    /// (CR or LF), doubling any embedded double-quotes. Returns the field unchanged otherwise.
    /// </summary>
    public static string QuoteField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}
