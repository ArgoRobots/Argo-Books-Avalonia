using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for obtaining anonymous geographic location data.
/// </summary>
public class GeoLocationService : IGeoLocationService
{
    private const string IpApiUrl = "http://ip-api.com/json/?fields=status,country,countryCode,region,city,timezone,proxy";
    private const string IpApiCoUrl = "https://ipapi.co/json/";
    private const string IpInfoUrl = "https://ipinfo.io/json";
    private const string IpifyUrl = "https://api.ipify.org";
    private const string HashSalt = "ArgoBooks2024GeoHash"; // Salt for IP hashing
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly HttpClient _httpClient;
    private readonly IErrorLogger? _errorLogger;

    private GeoLocationData? _cachedData;
    private DateTime _cacheTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the GeoLocationService.
    /// </summary>
    public GeoLocationService(HttpClient? httpClient = null, IErrorLogger? errorLogger = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _errorLogger = errorLogger;
    }

    /// <inheritdoc />
    public async Task<GeoLocationData> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        // Return cached data if still valid
        if (_cachedData != null && DateTime.UtcNow - _cacheTime < CacheDuration)
        {
            return _cachedData;
        }

        var locationData = new GeoLocationData();

        // Try each geolocation API in order
        var success = await TryIpApiAsync(locationData, cancellationToken) ||
                      await TryIpApiCoAsync(locationData, cancellationToken) ||
                      await TryIpInfoAsync(locationData, cancellationToken);

        // Get and hash IP address
        var ip = await GetIpAddressAsync(cancellationToken);
        if (!string.IsNullOrEmpty(ip))
        {
            locationData.HashedIp = HashIpAddress(ip);
        }

        // Cache the result
        _cachedData = locationData;
        _cacheTime = DateTime.UtcNow;

        return locationData;
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _cachedData = null;
        _cacheTime = DateTime.MinValue;
    }

    private async Task<bool> TryIpApiAsync(GeoLocationData data, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(IpApiUrl, cancellationToken);
            if (response?.Status == "success")
            {
                data.Country = response.Country ?? "Unknown";
                data.CountryCode = response.CountryCode ?? "Unknown";
                data.Region = response.Region ?? "Unknown";
                data.City = response.City ?? "Unknown";
                data.Timezone = response.Timezone ?? "Unknown";
                return true;
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogDebug($"ip-api.com failed: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> TryIpApiCoAsync(GeoLocationData data, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiCoResponse>(IpApiCoUrl, cancellationToken);
            if (response != null && !string.IsNullOrEmpty(response.CountryName))
            {
                data.Country = response.CountryName ?? "Unknown";
                data.CountryCode = response.CountryCode ?? "Unknown";
                data.Region = response.Region ?? "Unknown";
                data.City = response.City ?? "Unknown";
                data.Timezone = response.Timezone ?? "Unknown";
                return true;
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogDebug($"ipapi.co failed: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> TryIpInfoAsync(GeoLocationData data, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpInfoResponse>(IpInfoUrl, cancellationToken);
            if (response != null && !string.IsNullOrEmpty(response.Country))
            {
                data.Country = response.Country ?? "Unknown";
                data.CountryCode = response.Country ?? "Unknown"; // ipinfo uses country code in country field
                data.Region = response.Region ?? "Unknown";
                data.City = response.City ?? "Unknown";
                data.Timezone = response.Timezone ?? "Unknown";
                return true;
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogDebug($"ipinfo.io failed: {ex.Message}");
        }
        return false;
    }

    private async Task<string?> GetIpAddressAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(IpifyUrl, cancellationToken);
            return response?.Trim();
        }
        catch (Exception ex)
        {
            _errorLogger?.LogDebug($"Failed to get IP address: {ex.Message}");
            return null;
        }
    }

    private static string HashIpAddress(string ipAddress)
    {
        var input = $"{HashSalt}{ipAddress}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return hash[..16]; // Truncate to 16 characters
    }

    #region API Response Models

    private class IpApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("proxy")]
        public bool Proxy { get; set; }
    }

    private class IpApiCoResponse
    {
        [JsonPropertyName("country_name")]
        public string? CountryName { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }

    private class IpInfoResponse
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }

    #endregion
}

/// <summary>
/// Interface for geolocation services.
/// </summary>
public interface IGeoLocationService
{
    /// <summary>
    /// Gets anonymous geographic location data based on IP address.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Geographic location data.</returns>
    Task<GeoLocationData> GetLocationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached location data.
    /// </summary>
    void ClearCache();
}
