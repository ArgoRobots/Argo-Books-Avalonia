using System;

namespace ArgoBooks.Shared.Sync;

/// <summary>
/// Thrown when the sync server rejects an authenticated request with 401/403, meaning this
/// device's token is no longer valid (revoked desktop-side). Deliberately NOT an
/// <see cref="System.Net.Http.HttpRequestException"/> so callers can tell a revocation apart from a
/// transient network error (which falls back to cached data) and disconnect instead.
/// </summary>
public sealed class SyncUnauthorizedException : Exception
{
    public SyncUnauthorizedException(string message) : base(message)
    {
    }
}
