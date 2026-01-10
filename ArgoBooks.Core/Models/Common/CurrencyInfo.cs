namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Represents information about a currency including its code, symbol, and display name.
/// </summary>
public class CurrencyInfo
{
    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR", "CAD").
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Currency symbol (e.g., "$", "€", "£").
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Full display name (e.g., "US Dollar", "Euro", "Canadian Dollar").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Number of decimal places typically used (usually 2, but 0 for JPY, KRW, etc.).
    /// </summary>
    public int DecimalPlaces { get; }

    /// <summary>
    /// Creates a new CurrencyInfo instance.
    /// </summary>
    public CurrencyInfo(string code, string symbol, string name, int decimalPlaces = 2)
    {
        Code = code;
        Symbol = symbol;
        Name = name;
        DecimalPlaces = decimalPlaces;
    }

    /// <summary>
    /// Gets the display string for dropdown (e.g., "USD - US Dollar ($)").
    /// </summary>
    public string DisplayString => $"{Code} - {Name} ({Symbol})";

    /// <summary>
    /// Formats an amount with this currency's symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="includeCode">Whether to include the currency code after the amount.</param>
    /// <returns>Formatted string like "$1,234.56" or "$1,234.56 USD".</returns>
    public string Format(decimal amount, bool includeCode = false)
    {
        var formatted = DecimalPlaces == 0
            ? $"{Symbol}{amount:N0}"
            : $"{Symbol}{amount:N2}";

        return includeCode ? $"{formatted} {Code}" : formatted;
    }

    public override string ToString() => DisplayString;

    /// <summary>
    /// All supported currencies with their information.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, CurrencyInfo> All = new Dictionary<string, CurrencyInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["ALL"] = new("ALL", "L", "Albanian Lek"),
        ["AUD"] = new("AUD", "$", "Australian Dollar"),
        ["BAM"] = new("BAM", "KM", "Bosnia-Herzegovina Mark"),
        ["BGN"] = new("BGN", "лв", "Bulgarian Lev"),
        ["BRL"] = new("BRL", "R$", "Brazilian Real"),
        ["BYN"] = new("BYN", "Br", "Belarusian Ruble"),
        ["CAD"] = new("CAD", "$", "Canadian Dollar"),
        ["CHF"] = new("CHF", "CHF", "Swiss Franc"),
        ["CNY"] = new("CNY", "¥", "Chinese Yuan"),
        ["CZK"] = new("CZK", "Kč", "Czech Koruna"),
        ["DKK"] = new("DKK", "kr", "Danish Krone"),
        ["EUR"] = new("EUR", "€", "Euro"),
        ["GBP"] = new("GBP", "£", "British Pound"),
        ["HUF"] = new("HUF", "Ft", "Hungarian Forint", 0),
        ["ISK"] = new("ISK", "kr", "Icelandic Króna", 0),
        ["JPY"] = new("JPY", "¥", "Japanese Yen", 0),
        ["KRW"] = new("KRW", "₩", "South Korean Won", 0),
        ["MKD"] = new("MKD", "ден", "Macedonian Denar"),
        ["NOK"] = new("NOK", "kr", "Norwegian Krone"),
        ["PLN"] = new("PLN", "zł", "Polish Zloty"),
        ["RON"] = new("RON", "lei", "Romanian Leu"),
        ["RSD"] = new("RSD", "дин", "Serbian Dinar"),
        ["RUB"] = new("RUB", "₽", "Russian Ruble"),
        ["SEK"] = new("SEK", "kr", "Swedish Krona"),
        ["TRY"] = new("TRY", "₺", "Turkish Lira"),
        ["TWD"] = new("TWD", "NT$", "Taiwan Dollar"),
        ["UAH"] = new("UAH", "₴", "Ukrainian Hryvnia"),
        ["USD"] = new("USD", "$", "US Dollar")
    };

    /// <summary>
    /// Priority/common currencies shown at the top of dropdowns.
    /// </summary>
    public static readonly IReadOnlyList<string> PriorityCodes = ["USD", "EUR", "CAD", "AUD", "GBP"];

    /// <summary>
    /// Gets currency info by code, or USD as fallback.
    /// </summary>
    public static CurrencyInfo GetByCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return All["USD"];

        return All.TryGetValue(code, out var info) ? info : All["USD"];
    }

    /// <summary>
    /// Gets the currency code from a display string like "USD - US Dollar ($)".
    /// </summary>
    public static string ParseCodeFromDisplayString(string displayString)
    {
        if (string.IsNullOrEmpty(displayString))
            return "USD";

        // Extract the code (first 3 characters before the dash)
        var dashIndex = displayString.IndexOf('-');
        if (dashIndex > 0)
        {
            return displayString[..dashIndex].Trim().ToUpperInvariant();
        }

        // If it's just a code, return it uppercase
        if (displayString.Length == 3)
        {
            return displayString.ToUpperInvariant();
        }

        return "USD";
    }

    /// <summary>
    /// Gets the symbol for a currency code.
    /// </summary>
    public static string GetSymbol(string code)
    {
        return GetByCode(code).Symbol;
    }

    /// <summary>
    /// Formats an amount using the specified currency code.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currencyCode">The currency code (e.g., "USD", "EUR").</param>
    /// <returns>Formatted currency string.</returns>
    public static string FormatAmount(decimal amount, string currencyCode)
    {
        return GetByCode(currencyCode).Format(amount);
    }

    /// <summary>
    /// Formats an amount as a whole number using the specified currency code.
    /// </summary>
    public static string FormatWholeAmount(decimal amount, string currencyCode)
    {
        var info = GetByCode(currencyCode);
        return $"{info.Symbol}{amount:N0}";
    }
}
