namespace ArgoBooks.Core.Services;

/// <summary>
/// Best-effort favicon downloader. Used to give a supplier a default avatar derived
/// from its website when the user has not picked one. Direct GET against
/// <c>{scheme}://{host}/favicon.ico</c> on the supplier's own domain — no third-party
/// service is involved, which keeps the supplier list private and matches what the
/// user's browser already does for that site.
/// </summary>
public static class FaviconService
{
    private const int MaxFaviconBytes = 2 * 1024 * 1024; // 2 MB hard cap
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(5);

    private static readonly Lazy<HttpClient> _client = new(() =>
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };
        var client = new HttpClient(handler) { Timeout = FetchTimeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ArgoBooks/1.0 (favicon fetcher)");
        return client;
    });

    /// <summary>
    /// Tries to download <c>/favicon.ico</c> for the host implied by <paramref name="websiteUrl"/>.
    /// Returns the raw bytes (which may be ICO/PNG/etc) or null on any failure.
    /// Always swallows exceptions — the caller treats null as "no favicon available".
    /// </summary>
    public static async Task<byte[]?> TryFetchFaviconAsync(string? websiteUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
            return null;

        var input = websiteUrl.Trim();
        if (!input.Contains("://", StringComparison.Ordinal))
            input = "https://" + input;

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return null;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;
        if (string.IsNullOrEmpty(uri.Host))
            return null;

        var faviconUrl = $"{uri.Scheme}://{uri.Host}/favicon.ico";

        try
        {
            using var response = await _client.Value.GetAsync(faviconUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            // Reject obviously-non-image responses (e.g. an HTML 200 page when the
            // server doesn't have a real favicon and serves the index instead).
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null
                && !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            if (bytes.Length == 0 || bytes.Length > MaxFaviconBytes)
                return null;

            return bytes;
        }
        catch
        {
            return null;
        }
    }
}
