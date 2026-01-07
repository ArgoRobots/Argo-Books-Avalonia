namespace ArgoBooks.Data;

/// <summary>
/// Provides a shared list of supported currencies.
/// </summary>
public static class Currencies
{
    /// <summary>
    /// Priority/common currencies shown at the top of dropdowns.
    /// </summary>
    public static readonly IReadOnlyList<string> Priority =
    [
        "USD - US Dollar ($)",
        "EUR - Euro (€)",
        "CAD - Canadian Dollar ($)",
        "AUD - Australian Dollar ($)"
    ];

    /// <summary>
    /// Complete list of supported currencies.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        "ALL - Albanian Lek (L)",
        "AUD - Australian Dollar ($)",
        "BAM - Bosnia-Herzegovina Mark (KM)",
        "BGN - Bulgarian Lev (лв)",
        "BRL - Brazilian Real (R$)",
        "BYN - Belarusian Ruble (Br)",
        "CAD - Canadian Dollar ($)",
        "CHF - Swiss Franc (CHF)",
        "CNY - Chinese Yuan (¥)",
        "CZK - Czech Koruna (Kč)",
        "DKK - Danish Krone (kr)",
        "EUR - Euro (€)",
        "GBP - British Pound (£)",
        "HUF - Hungarian Forint (Ft)",
        "ISK - Icelandic Króna (kr)",
        "JPY - Japanese Yen (¥)",
        "KRW - South Korean Won (₩)",
        "MKD - Macedonian Denar (ден)",
        "NOK - Norwegian Krone (kr)",
        "PLN - Polish Złoty (zł)",
        "RON - Romanian Leu (lei)",
        "RSD - Serbian Dinar (дин)",
        "RUB - Russian Ruble (₽)",
        "SEK - Swedish Krona (kr)",
        "TRY - Turkish Lira (₺)",
        "TWD - Taiwan Dollar (NT$)",
        "UAH - Ukrainian Hryvnia (₴)",
        "USD - US Dollar ($)"
    ];
}
