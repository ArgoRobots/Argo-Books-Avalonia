namespace ArgoBooks.Core.Services;

/// <summary>
/// Produces a single, consistent, user-friendly message explaining why an online
/// action failed: no internet, the Argo Books server being unreachable, or an
/// otherwise transient problem. Use this from any catch block that handles a
/// network failure (HttpRequestException / timeout) so the user is told the real
/// cause instead of a raw exception string.
/// </summary>
public static class ConnectivityMessage
{
    private static readonly IConnectivityService DefaultConnectivity = new ConnectivityService();

    /// <summary>Friendly text for the offline case, also used as a safe fallback.</summary>
    public const string NoInternet = "No internet connection. Please check your network and try again.";

    /// <summary>Friendly text when we have internet but can't reach our servers.</summary>
    public const string ServerUnreachable = "Unable to reach Argo Books servers. The service may be temporarily unavailable. Please try again later.";

    /// <summary>Friendly text when the server was reached but the call still failed.</summary>
    public const string GenericFailure = "Something went wrong while contacting the server. Please try again.";

    /// <summary>Shared dialog title for every connectivity error, so they all look the same.</summary>
    public const string Title = "Connection Problem";

    /// <summary>
    /// True if <paramref name="message"/> is one of the messages this class produces.
    /// Lets a shared error renderer recognise a connectivity failure and present it with
    /// the unified <see cref="Title"/> instead of a feature-specific title.
    /// </summary>
    public static bool IsConnectivityMessage(string? message)
        => message == NoInternet || message == ServerUnreachable || message == GenericFailure;

    /// <summary>
    /// Probes connectivity and returns the message that best explains the failure.
    /// The probe uses its own short timeout, so pass <see cref="System.Threading.CancellationToken.None"/>
    /// (the default) when the caller's token may already be cancelled or timed out.
    /// </summary>
    public static Task<string> ResolveAsync(CancellationToken cancellationToken = default)
        => ResolveAsync(DefaultConnectivity, cancellationToken);

    /// <inheritdoc cref="ResolveAsync(CancellationToken)" />
    public static async Task<string> ResolveAsync(IConnectivityService connectivity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await connectivity.IsInternetAvailableAsync(cancellationToken))
            {
                return NoInternet;
            }

            if (!await connectivity.IsHostReachableAsync(ApiConfig.BaseUrl, cancellationToken))
            {
                return ServerUnreachable;
            }

            return GenericFailure;
        }
        catch
        {
            // If the probe itself fails, the most likely explanation is no connection.
            return NoInternet;
        }
    }
}
