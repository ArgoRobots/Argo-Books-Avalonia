using System.Diagnostics;
using System.Net.Http.Json;

using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for fetching and managing currency exchange rates.
/// Routes requests through the argorobots.com server proxy.
/// </summary>
public class ExchangeRateService
{
    private static string BaseUrl => $"{ApiConfig.BaseUrl}/api/exchange-rates.php";
    private static string BatchUrl => $"{ApiConfig.BaseUrl}/api/exchange-rates-batch.php";
    private const string BaseCurrency = "USD"; // All rates are relative to USD

    private readonly ExchangeRateCache _cache;
    private readonly HttpClient _httpClient;
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;
    private bool _isInitialized;

    /// <summary>
    /// Singleton instance for the exchange rate service.
    /// </summary>
    public static ExchangeRateService? Instance { get; private set; }

    /// <summary>
    /// Creates a new ExchangeRateService instance.
    /// </summary>
    /// <param name="errorLogger">Optional error logger for tracking errors.</param>
    /// <param name="telemetryManager">Optional telemetry manager for tracking API calls.</param>
    public ExchangeRateService(IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
        : this(PlatformServiceFactory.GetPlatformService(), new HttpClient(), errorLogger, telemetryManager)
    {
    }

    /// <summary>
    /// Creates a new ExchangeRateService instance with custom dependencies.
    /// </summary>
    public ExchangeRateService(IPlatformService platformService, HttpClient httpClient, IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _cache = new ExchangeRateCache(platformService);
        _errorLogger = errorLogger;
        _telemetryManager = telemetryManager;

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
    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date, bool fetchIfMissing = true, CancellationToken cancellationToken = default)
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
        if (fetchIfMissing)
        {
            var rates = await FetchRatesForDateAsync(date, cancellationToken: cancellationToken);
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

        return -1m; // Rate unavailable
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
    public async Task PreloadRatesAsync(IEnumerable<DateTime> dates, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var uniqueDates = dates.Select(d => d.Date).Distinct().ToList();
        var total = uniqueDates.Count;
        if (total == 0) return;

        // Filter out already-cached dates
        var datesToFetch = new List<DateTime>();
        var cached = 0;
        foreach (var date in uniqueDates)
        {
            if (_cache.TryGetRate(BaseCurrency, "EUR", date, out _))
                cached++;
            else
                datesToFetch.Add(date);
        }
        progress?.Report(cached * 100 / total);

        if (datesToFetch.Count == 0)
        {
            progress?.Report(100);
            return;
        }

        // Try batch endpoint first (one POST for all dates)
        var fetched = await FetchBatchRatesAsync(datesToFetch, cancellationToken);
        var failedDates = new List<DateTime>();

        foreach (var date in datesToFetch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dateKey = date.ToString("yyyy-MM-dd");
            if (fetched != null && fetched.TryGetValue(dateKey, out var rates))
            {
                _cache.SetRatesFromBase(rates, BaseCurrency, date);
                cached++;
                progress?.Report(cached * 100 / total);
            }
            else
            {
                failedDates.Add(date);
            }
        }

        // Fall back to single-date requests for any dates the batch missed
        foreach (var date in failedDates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rates = await FetchRatesForDateAsync(date, cancellationToken: cancellationToken);
            if (rates != null)
            {
                _cache.SetRatesFromBase(rates, BaseCurrency, date);
            }
            cached++;
            progress?.Report(cached * 100 / total);
        }

        await _cache.SaveAsync();
    }

    /// <summary>
    /// Fetches exchange rates for multiple dates in a single batch request.
    /// Returns a dictionary mapping date strings to their rate dictionaries, or null on failure.
    /// </summary>
    private async Task<Dictionary<string, Dictionary<string, decimal>>?> FetchBatchRatesAsync(
        List<DateTime> dates, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var dateStrings = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            var requestBody = new { dates = dateStrings };
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(BatchUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _errorLogger?.LogError($"Batch exchange rate API returned {response.StatusCode}", ErrorCategory.Api);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<BatchExchangeRatesResponse>(cancellationToken: cancellationToken);
            if (result?.Success == true && result.Results != null)
            {
                success = true;
                return result.Results;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "Failed to fetch batch exchange rates");
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _ = _telemetryManager?.TrackApiCallAsync(
                ApiName.OpenExchangeRates,
                stopwatch.ElapsedMilliseconds,
                success);
        }
    }

    /// <summary>
    /// Gets the number of cached exchange rates.
    /// </summary>
    public int CachedRatesCount => _cache.Count;

    /// <summary>
    /// Fetches exchange rates for a specific date from the API.
    /// </summary>
    private async Task<Dictionary<string, decimal>?> FetchRatesForDateAsync(DateTime date, int maxRetries = 2, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                if (attempt > 0)
                {
                    await Task.Delay(1000 * attempt, cancellationToken); // 1s, 2s backoff
                }

                var isToday = date.Date == DateTime.Today;
                var endpoint = isToday
                    ? BaseUrl
                    : $"{BaseUrl}?date={date:yyyy-MM-dd}";

                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _errorLogger?.LogError($"Exchange rate API returned {response.StatusCode} (attempt {attempt + 1}/{maxRetries + 1})", ErrorCategory.Api, $"Date: {date:yyyy-MM-dd}");
                    continue; // retry
                }

                var result = await response.Content.ReadFromJsonAsync<ProxyExchangeRatesResponse>();
                if (result?.Success == true && result.Rates != null)
                {
                    success = true;
                    return result.Rates;
                }

                _errorLogger?.LogError($"Exchange rate API returned invalid data (attempt {attempt + 1}/{maxRetries + 1})", ErrorCategory.Api, $"Date: {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _errorLogger?.LogError(ex, ErrorCategory.Api, $"Failed to fetch exchange rates for {date:yyyy-MM-dd} (attempt {attempt + 1}/{maxRetries + 1})");
                if (attempt == maxRetries) return null;
            }
            finally
            {
                stopwatch.Stop();
                _ = _telemetryManager?.TrackApiCallAsync(
                    ApiName.OpenExchangeRates,
                    stopwatch.ElapsedMilliseconds,
                    success);
            }
        }

        return null;
    }

    /// <summary>
    /// Saves the cache to disk.
    /// </summary>
    public Task SaveCacheAsync() => _cache.SaveAsync();

    /// <summary>
    /// Response from the exchange rates proxy endpoint.
    /// </summary>
    private class ProxyExchangeRatesResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("base")]
        public string? Base { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; init; }
    }

    /// <summary>
    /// Response from the batch exchange rates endpoint.
    /// </summary>
    private class BatchExchangeRatesResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("base")]
        public string? Base { get; init; }

        [JsonPropertyName("results")]
        public Dictionary<string, Dictionary<string, decimal>>? Results { get; init; }

        [JsonPropertyName("failed")]
        public List<string>? Failed { get; init; }
    }
}
