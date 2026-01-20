namespace ArgoBooks.Core.Services;

/// <summary>
/// Service to check internet connectivity by pinging reliable servers.
/// </summary>
public class ConnectivityService : IConnectivityService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    // Reliable endpoints to check connectivity (in order of preference)
    private static readonly string[] ConnectivityCheckUrls =
    [
        "https://www.google.com/generate_204",      // Google's connectivity check (returns 204)
        "https://connectivitycheck.gstatic.com/generate_204",  // Google's alternate
        "https://www.cloudflare.com/cdn-cgi/trace"  // Cloudflare (returns 200 with trace info)
    ];

    /// <summary>
    /// Checks if the device has internet connectivity by pinging reliable servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if internet is available, false otherwise.</returns>
    public async Task<bool> IsInternetAvailableAsync(CancellationToken cancellationToken = default)
    {
        foreach (var url in ConnectivityCheckUrls)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Any successful response (2xx or 204) means we have internet
                if (response.IsSuccessStatusCode || (int)response.StatusCode == 204)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // Try next URL
                continue;
            }
            catch (TaskCanceledException)
            {
                // Timeout or cancellation, try next URL
                continue;
            }
            catch (Exception)
            {
                // Any other exception, try next URL
                continue;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a specific host is reachable.
    /// </summary>
    /// <param name="host">The host URL to check (e.g., "https://argorobots.com").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the host is reachable, false otherwise.</returns>
    public async Task<bool> IsHostReachableAsync(string host, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, host);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return true; // Any response means the host is reachable
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Interface for connectivity checking service.
/// </summary>
public interface IConnectivityService
{
    /// <summary>
    /// Checks if the device has internet connectivity.
    /// </summary>
    Task<bool> IsInternetAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific host is reachable.
    /// </summary>
    Task<bool> IsHostReachableAsync(string host, CancellationToken cancellationToken = default);
}
