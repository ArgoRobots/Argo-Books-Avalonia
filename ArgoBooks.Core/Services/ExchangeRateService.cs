using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for fetching and managing currency exchange rates.
/// Uses OpenExchangeRates API with local caching.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private const string BaseUrl = "https://openexchangerates.org/api";
    private const string BaseCurrency = "USD"; // All rates are relative to USD

    private readonly ExchangeRateCache _cache;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly IPlatformService _platformService;
    private bool _isInitialized;

    /// <summary>
    /// Singleton instance for the exchange rate service.
    /// </summary>
    public static ExchangeRateService? Instance { get; private set; }

    /// <summary>
    /// Creates a new ExchangeRateService instance.
    /// </summary>
    /// <param name="apiKey">OpenExchangeRates API key. If null, will try to read from environment variable.</param>
    public ExchangeRateService(string? apiKey = null)
        : this(apiKey, PlatformServiceFactory.GetPlatformService(), new HttpClient())
    {
    }

    /// <summary>
    /// Creates a new ExchangeRateService instance with custom dependencies.
    /// </summary>
    public ExchangeRateService(string? apiKey, IPlatformService platformService, HttpClient httpClient)
    {
        _apiKey = apiKey ?? DotEnv.Get("OPENEXCHANGERATES_API_KEY");
        _platformService = platformService;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _cache = new ExchangeRateCache(platformService);

        Instance ??= this;
    }

    /// <summary>
    /// Initializes the service by loading the cache from disk.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _cache.LoadAsync();
        _isInitialized = true;
    }

    /// <summary>
    /// Gets the exchange rate between two currencies for a specific date.
    /// </summary>
    /// <param name="fromCurrency">Source currency code (e.g., "USD").</param>
    /// <param name="toCurrency">Target currency code (e.g., "EUR").</param>
    /// <param name="date">The date for the historical rate.</param>
    /// <param name="fetchIfMissing">Whether to fetch from API if not cached.</param>
    /// <returns>The exchange rate, or -1 if unavailable.</returns>
    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date, bool fetchIfMissing = true)
    {
        // Same currency - rate is always 1
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency = toCurrency.ToUpperInvariant();

        // Try cache first
        if (_cache.TryGetRate(fromCurrency, toCurrency, date, out var cachedRate))
        {
            return cachedRate;
        }

        // Fetch from API if allowed
        if (fetchIfMissing && !string.IsNullOrEmpty(_apiKey))
        {
            var rates = await FetchRatesForDateAsync(date);
            if (rates != null)
            {
                _cache.SetRatesFromBase(rates, BaseCurrency, date);
                await _cache.SaveAsync();

                // Calculate the requested rate
                if (_cache.TryGetRate(fromCurrency, toCurrency, date, out cachedRate))
                {
                    return cachedRate;
                }
            }
        }

        // Try to use latest cached rate for these currencies (any date)
        var fallbackRate = GetFallbackRate(fromCurrency, toCurrency);
        if (fallbackRate > 0)
        {
            return fallbackRate;
        }

        return -1m; // Rate unavailable
    }

    /// <summary>
    /// Gets the exchange rate synchronously, using only cached values.
    /// Will not fetch from API - returns -1 if not cached.
    /// </summary>
    public decimal GetExchangeRate(string fromCurrency, string toCurrency, DateTime date)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency = toCurrency.ToUpperInvariant();

        if (_cache.TryGetRate(fromCurrency, toCurrency, date, out var rate))
        {
            return rate;
        }

        // Try fallback
        return GetFallbackRate(fromCurrency, toCurrency);
    }

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrency">Source currency code.</param>
    /// <param name="toCurrency">Target currency code.</param>
    /// <param name="date">The date for the exchange rate.</param>
    /// <returns>The converted amount, or the original amount if conversion fails.</returns>
    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime date)
    {
        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency, date);
        if (rate <= 0)
        {
            return amount; // Return original if conversion fails
        }

        return Math.Round(amount * rate, 2);
    }

    /// <summary>
    /// Converts an amount to USD.
    /// </summary>
    /// <param name="amount">The amount in the source currency.</param>
    /// <param name="fromCurrency">Source currency code.</param>
    /// <param name="date">The date for the exchange rate.</param>
    /// <returns>The amount in USD.</returns>
    public async Task<decimal> ConvertToUSDAsync(decimal amount, string fromCurrency, DateTime date)
    {
        return await ConvertAsync(amount, fromCurrency, BaseCurrency, date);
    }

    /// <summary>
    /// Converts an amount from USD to another currency.
    /// </summary>
    /// <param name="amountUSD">The amount in USD.</param>
    /// <param name="toCurrency">Target currency code.</param>
    /// <param name="date">The date for the exchange rate.</param>
    /// <returns>The amount in the target currency.</returns>
    public async Task<decimal> ConvertFromUSDAsync(decimal amountUSD, string toCurrency, DateTime date)
    {
        return await ConvertAsync(amountUSD, BaseCurrency, toCurrency, date);
    }

    /// <summary>
    /// Preloads exchange rates for a range of dates.
    /// Useful for batch operations to minimize API calls.
    /// </summary>
    /// <param name="dates">The dates to preload rates for.</param>
    /// <param name="progress">Optional progress callback.</param>
    public async Task PreloadRatesAsync(IEnumerable<DateTime> dates, IProgress<int>? progress = null)
    {
        var uniqueDates = dates.Select(d => d.Date).Distinct().ToList();
        var loaded = 0;

        foreach (var date in uniqueDates)
        {
            // Skip if we already have rates for this date
            if (_cache.TryGetRate(BaseCurrency, "EUR", date, out _))
            {
                loaded++;
                progress?.Report(loaded * 100 / uniqueDates.Count);
                continue;
            }

            var rates = await FetchRatesForDateAsync(date);
            if (rates != null)
            {
                _cache.SetRatesFromBase(rates, BaseCurrency, date);
            }

            loaded++;
            progress?.Report(loaded * 100 / uniqueDates.Count);

            // Small delay to avoid rate limiting
            await Task.Delay(100);
        }

        await _cache.SaveAsync();
    }

    /// <summary>
    /// Checks if the service has a valid API key configured.
    /// </summary>
    public bool HasApiKey => !string.IsNullOrEmpty(_apiKey);

    /// <summary>
    /// Gets the number of cached exchange rates.
    /// </summary>
    public int CachedRatesCount => _cache.Count;

    /// <summary>
    /// Fetches exchange rates for a specific date from the API.
    /// </summary>
    private async Task<Dictionary<string, decimal>?> FetchRatesForDateAsync(DateTime date)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return null;
        }

        try
        {
            // Use historical endpoint for past dates, latest for today
            var isToday = date.Date == DateTime.Today;
            var endpoint = isToday
                ? $"{BaseUrl}/latest.json?app_id={_apiKey}&base={BaseCurrency}"
                : $"{BaseUrl}/historical/{date:yyyy-MM-dd}.json?app_id={_apiKey}&base={BaseCurrency}";

            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OpenExchangeRatesResponse>();
            return result?.Rates;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Tries to find a rate for any date as a fallback.
    /// </summary>
    private decimal GetFallbackRate(string fromCurrency, string toCurrency)
    {
        // This is a simple fallback - in a production system you might
        // want to track the most recent date for each currency pair
        return -1m;
    }

    /// <summary>
    /// Saves the cache to disk.
    /// </summary>
    public Task SaveCacheAsync() => _cache.SaveAsync();

    /// <summary>
    /// Response from OpenExchangeRates API.
    /// </summary>
    private class OpenExchangeRatesResponse
    {
        [JsonPropertyName("disclaimer")]
        public string? Disclaimer { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("base")]
        public string? Base { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}

/// <summary>
/// Interface for exchange rate service.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate between two currencies.
    /// </summary>
    Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date, bool fetchIfMissing = true);

    /// <summary>
    /// Gets the exchange rate synchronously from cache only.
    /// </summary>
    decimal GetExchangeRate(string fromCurrency, string toCurrency, DateTime date);

    /// <summary>
    /// Converts an amount between currencies.
    /// </summary>
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime date);

    /// <summary>
    /// Converts an amount to USD.
    /// </summary>
    Task<decimal> ConvertToUSDAsync(decimal amount, string fromCurrency, DateTime date);

    /// <summary>
    /// Converts an amount from USD.
    /// </summary>
    Task<decimal> ConvertFromUSDAsync(decimal amountUSD, string toCurrency, DateTime date);

    /// <summary>
    /// Whether the service has an API key configured.
    /// </summary>
    bool HasApiKey { get; }
}
