namespace ArgoBooks.Core.Data;

/// <summary>
/// Maps country names (and common abbreviations) to ISO 3166-1 alpha-3 codes
/// for GeoMap rendering, plus the reverse mapping for export.
/// Shared by ChartLoaderService (UI on-screen charts) and ReportRenderer
/// (PDF/image report output) so both surfaces recognize the same names.
/// </summary>
public static class CountryCodeMapping
{
    private static readonly Dictionary<string, string> NameToIsoCode = new(StringComparer.OrdinalIgnoreCase)
    {
        { "United States", "usa" }, { "USA", "usa" }, { "US", "usa" }, { "America", "usa" },
        { "United Kingdom", "gbr" }, { "UK", "gbr" }, { "Great Britain", "gbr" }, { "England", "gbr" },
        { "Canada", "can" }, { "CA", "can" },
        { "Germany", "deu" }, { "DE", "deu" },
        { "France", "fra" }, { "FR", "fra" },
        { "Italy", "ita" }, { "IT", "ita" },
        { "Spain", "esp" }, { "ES", "esp" },
        { "Australia", "aus" }, { "AU", "aus" },
        { "Japan", "jpn" }, { "JP", "jpn" },
        { "China", "chn" }, { "CN", "chn" },
        { "India", "ind" }, { "IN", "ind" },
        { "Brazil", "bra" }, { "BR", "bra" },
        { "Mexico", "mex" }, { "MX", "mex" },
        { "Russia", "rus" }, { "RU", "rus" },
        { "South Korea", "kor" }, { "Korea", "kor" }, { "KR", "kor" },
        { "Netherlands", "nld" }, { "NL", "nld" },
        { "Switzerland", "che" }, { "CH", "che" },
        { "Sweden", "swe" }, { "SE", "swe" },
        { "Norway", "nor" }, { "NO", "nor" },
        { "Denmark", "dnk" }, { "DK", "dnk" },
        { "Finland", "fin" }, { "FI", "fin" },
        { "Poland", "pol" }, { "PL", "pol" },
        { "Belgium", "bel" }, { "BE", "bel" },
        { "Austria", "aut" }, { "AT", "aut" },
        { "Ireland", "irl" }, { "IE", "irl" },
        { "Portugal", "prt" }, { "PT", "prt" },
        { "Greece", "grc" }, { "GR", "grc" },
        { "New Zealand", "nzl" }, { "NZ", "nzl" },
        { "Singapore", "sgp" }, { "SG", "sgp" },
        { "Hong Kong", "hkg" }, { "HK", "hkg" },
        { "Taiwan", "twn" }, { "TW", "twn" },
        { "South Africa", "zaf" }, { "ZA", "zaf" },
        { "Argentina", "arg" }, { "AR", "arg" },
        { "Chile", "chl" }, { "CL", "chl" },
        { "Colombia", "col" }, { "CO", "col" },
        { "Indonesia", "idn" }, { "ID", "idn" },
        { "Malaysia", "mys" }, { "MY", "mys" },
        { "Thailand", "tha" }, { "TH", "tha" },
        { "Vietnam", "vnm" }, { "VN", "vnm" },
        { "Philippines", "phl" }, { "PH", "phl" },
        { "Turkey", "tur" }, { "TR", "tur" },
        { "Saudi Arabia", "sau" }, { "SA", "sau" },
        { "UAE", "are" }, { "United Arab Emirates", "are" }, { "AE", "are" },
        { "Israel", "isr" }, { "IL", "isr" },
        { "Egypt", "egy" }, { "EG", "egy" },
        { "Nigeria", "nga" }, { "NG", "nga" },
        { "Kenya", "ken" }, { "KE", "ken" },
        { "Ukraine", "ukr" }, { "UA", "ukr" },
        { "Czech Republic", "cze" }, { "Czechia", "cze" }, { "CZ", "cze" },
        { "Romania", "rou" }, { "RO", "rou" },
        { "Hungary", "hun" }, { "HU", "hun" }
    };

    // Picks the longest name per ISO code so exports show the most descriptive label.
    private static readonly Dictionary<string, string> IsoCodeToDisplayName = BuildIsoCodeToDisplayName();

    private static Dictionary<string, string> BuildIsoCodeToDisplayName()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, code) in NameToIsoCode)
        {
            if (!result.TryGetValue(code, out var existing) || name.Length > existing.Length)
                result[code] = name;
        }
        return result;
    }

    /// <summary>
    /// Returns the ISO 3166-1 alpha-3 code for a country name, or the lowercased
    /// input as a best-effort fallback. Returns empty string for null/empty input.
    /// </summary>
    public static string GetIsoCode(string? countryName)
    {
        if (string.IsNullOrEmpty(countryName))
            return string.Empty;

        return NameToIsoCode.TryGetValue(countryName, out var code) ? code : countryName.ToLowerInvariant();
    }

    /// <summary>
    /// Converts a GeoMap data dictionary keyed by ISO codes into one keyed by
    /// human-readable country names. Used when exporting GeoMap data to a
    /// spreadsheet where readers expect names, not codes. Drops zero values.
    /// </summary>
    public static Dictionary<string, double> ConvertGeoMapDataForExport(Dictionary<string, double> isoCodeData)
    {
        return isoCodeData
            .Where(kvp => kvp.Value > 0)
            .ToDictionary(
                kvp => IsoCodeToDisplayName.TryGetValue(kvp.Key, out var name) ? name : kvp.Key.ToUpperInvariant(),
                kvp => kvp.Value);
    }
}
