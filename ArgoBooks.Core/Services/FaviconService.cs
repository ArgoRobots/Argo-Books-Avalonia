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
            // ResponseHeadersRead returns as soon as the headers arrive. We can then
            // inspect Content-Length / Content-Type before pulling the body — important
            // because a hostile server could otherwise force us to buffer a giant
            // response just to discover it's over the cap.
            using var response = await _client.Value.GetAsync(faviconUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
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

            // Reject upfront when the server is honest about an oversized payload.
            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength.HasValue && declaredLength.Value > MaxFaviconBytes)
                return null;

            // Stream into a bounded buffer; abort the moment we cross the cap so a
            // server lying about / omitting Content-Length can't force a huge alloc.
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream(capacity: declaredLength.HasValue ? (int)Math.Min(declaredLength.Value, MaxFaviconBytes) : 16 * 1024);
            var chunk = new byte[8192];
            while (true)
            {
                var read = await contentStream.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read <= 0) break;
                if (buffer.Length + read > MaxFaviconBytes)
                    return null; // Server overran the cap mid-stream.
                buffer.Write(chunk, 0, read);
            }

            if (buffer.Length == 0)
                return null;
            return buffer.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
