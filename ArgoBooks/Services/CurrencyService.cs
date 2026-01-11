using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Service for formatting currency values based on user settings.
/// Handles conversion between currencies using USD as the base.
/// </summary>
public static class CurrencyService
{
    /// <summary>
    /// Event raised when the currency setting changes.
    /// </summary>
    public static event EventHandler? CurrencyChanged;

    /// <summary>
    /// Raises the CurrencyChanged event to notify subscribers that the currency has changed.
    /// </summary>
    public static void NotifyCurrencyChanged()
    {
        CurrencyChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the current currency code from company settings (e.g., "USD", "EUR").
    /// </summary>
    public static string CurrentCurrencyCode =>
        App.CompanyManager?.CompanyData?.Settings.Localization.Currency ?? "USD";

    /// <summary>
    /// Gets the current currency info.
    /// </summary>
    public static CurrencyInfo CurrentCurrency => CurrencyInfo.GetByCode(CurrentCurrencyCode);

    /// <summary>
    /// Gets the current currency symbol (e.g., "$", "€").
    /// </summary>
    public static string CurrentSymbol => CurrentCurrency.Symbol;

    /// <summary>
    /// Gets the current currency display string for dropdowns (e.g., "USD - US Dollar ($)").
    /// </summary>
    public static string CurrentDisplayString => CurrentCurrency.DisplayString;

    /// <summary>
    /// Formats an amount using the current currency symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="includeCode">Whether to include the currency code (e.g., "$100.00 USD").</param>
    /// <returns>The formatted currency string.</returns>
    public static string Format(decimal amount, bool includeCode = false)
    {
        return CurrentCurrency.Format(amount, includeCode);
    }

    /// <summary>
    /// Formats an amount with no decimal places (for large numbers).
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <returns>The formatted currency string.</returns>
    public static string FormatWholeNumber(decimal amount)
    {
        return $"{CurrentSymbol}{amount:N0}";
    }

    /// <summary>
    /// Formats an amount from a MonetaryValue, converting to the current display currency.
    /// </summary>
    /// <param name="value">The monetary value to format.</param>
    /// <returns>The formatted currency string in the current display currency.</returns>
    public static string Format(MonetaryValue? value)
    {
        if (value == null)
            return Format(0m);

        var displayAmount = GetDisplayAmount(value);
        return Format(displayAmount);
    }

    /// <summary>
    /// Formats an amount from a MonetaryValue as a whole number.
    /// </summary>
    public static string FormatWholeNumber(MonetaryValue? value)
    {
        if (value == null)
            return FormatWholeNumber(0m);

        var displayAmount = GetDisplayAmount(value);
        return FormatWholeNumber(displayAmount);
    }

    /// <summary>
    /// Gets the display amount for a MonetaryValue in the current display currency.
    /// </summary>
    /// <param name="value">The monetary value.</param>
    /// <returns>The amount converted to the current display currency.</returns>
    public static decimal GetDisplayAmount(MonetaryValue value)
    {
        var targetCurrency = CurrentCurrencyCode;

        // If target is the original currency, return exact original
        if (string.Equals(targetCurrency, value.OriginalCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return value.OriginalAmount;
        }

        // If target is USD, return stored USD amount
        if (string.Equals(targetCurrency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            return value.AmountUSD;
        }

        // Convert from USD to target currency
        var exchangeService = ExchangeRateService.Instance;
        if (exchangeService != null)
        {
            var rate = exchangeService.GetExchangeRate("USD", targetCurrency, value.RateDate);
            if (rate > 0)
            {
                return Math.Round(value.AmountUSD * rate, 2);
            }
        }

        // Fallback to USD amount
        return value.AmountUSD;
    }

    /// <summary>
    /// Gets the display amount for a legacy decimal value (assumes USD).
    /// Converts to the current display currency.
    /// </summary>
    /// <param name="amountUSD">The amount in USD.</param>
    /// <param name="date">The date for exchange rate lookup.</param>
    /// <returns>The amount in the current display currency.</returns>
    public static decimal GetDisplayAmount(decimal amountUSD, DateTime date)
    {
        var targetCurrency = CurrentCurrencyCode;

        if (string.Equals(targetCurrency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            return amountUSD;
        }

        var exchangeService = ExchangeRateService.Instance;
        if (exchangeService != null)
        {
            var rate = exchangeService.GetExchangeRate("USD", targetCurrency, date);
            if (rate > 0)
            {
                return Math.Round(amountUSD * rate, 2);
            }
        }

        return amountUSD;
    }

    /// <summary>
    /// Formats a legacy decimal value (assumes USD) in the current display currency.
    /// </summary>
    /// <param name="amountUSD">The amount in USD.</param>
    /// <param name="date">The date for exchange rate lookup.</param>
    /// <returns>The formatted currency string.</returns>
    public static string FormatFromUSD(decimal amountUSD, DateTime date)
    {
        var displayAmount = GetDisplayAmount(amountUSD, date);
        return Format(displayAmount);
    }

    /// <summary>
    /// Formats a legacy decimal value (assumes USD) as a whole number in the current display currency.
    /// </summary>
    public static string FormatWholeNumberFromUSD(decimal amountUSD, DateTime date)
    {
        var displayAmount = GetDisplayAmount(amountUSD, date);
        return FormatWholeNumber(displayAmount);
    }

    /// <summary>
    /// Creates a MonetaryValue from a user-entered amount in the current currency.
    /// </summary>
    /// <param name="amount">The amount entered by the user.</param>
    /// <param name="date">The transaction date for exchange rate lookup.</param>
    /// <returns>A MonetaryValue with both original and USD amounts.</returns>
    public static async Task<MonetaryValue> CreateMonetaryValueAsync(decimal amount, DateTime date)
    {
        var currentCurrency = CurrentCurrencyCode;

        if (string.Equals(currentCurrency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            return new MonetaryValue(amount, "USD", amount, date);
        }

        // Convert to USD
        var exchangeService = ExchangeRateService.Instance;
        decimal amountUSD = amount;

        if (exchangeService != null)
        {
            amountUSD = await exchangeService.ConvertToUSDAsync(amount, currentCurrency, date);
        }

        return new MonetaryValue(amount, currentCurrency, amountUSD, date);
    }

    /// <summary>
    /// Creates a MonetaryValue synchronously (uses cached rates only).
    /// </summary>
    public static MonetaryValue CreateMonetaryValue(decimal amount, DateTime date)
    {
        var currentCurrency = CurrentCurrencyCode;

        if (string.Equals(currentCurrency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            return new MonetaryValue(amount, "USD", amount, date);
        }

        var exchangeService = ExchangeRateService.Instance;
        decimal amountUSD = amount;

        if (exchangeService != null)
        {
            var rate = exchangeService.GetExchangeRate(currentCurrency, "USD", date);
            if (rate > 0)
            {
                amountUSD = Math.Round(amount * rate, 2);
            }
        }

        return new MonetaryValue(amount, currentCurrency, amountUSD, date);
    }

    /// <summary>
    /// Gets the currency code from a display string like "USD - US Dollar ($)".
    /// </summary>
    public static string ParseCurrencyCode(string displayString)
    {
        return CurrencyInfo.ParseCodeFromDisplayString(displayString);
    }

    /// <summary>
    /// Gets the display string for a currency code.
    /// </summary>
    public static string GetDisplayString(string currencyCode)
    {
        return CurrencyInfo.GetByCode(currencyCode).DisplayString;
    }

    /// <summary>
    /// Gets the symbol for a currency code.
    /// </summary>
    public static string GetSymbol(string currencyCode)
    {
        return CurrencyInfo.GetSymbol(currencyCode);
    }
}
