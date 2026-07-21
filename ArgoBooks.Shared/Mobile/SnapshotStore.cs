using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Shared.Sync;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Fetches the encrypted snapshot from the sync server, decrypts it with the paired company's
/// sync key, and caches the decrypted JSON locally so the phone keeps showing data offline.
/// Pure logic with no UI/device dependency beyond the injected <see cref="MobileSyncClient"/>/
/// <see cref="PairedCompanyStore"/>/<see cref="ISnapshotCache"/> seams, so it is fully
/// unit-testable (see ArgoBooks.Tests/Mobile/SnapshotStoreTests.cs).
/// </summary>
public class SnapshotStore
{
    // Case-insensitive to tolerate any future casing drift; the DTOs pin their wire names with
    // [JsonPropertyName] so this matches the desktop's SnapshotBuilder.Serialize output exactly.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MobileSyncClient _client;
    private readonly PairedCompanyStore _store;
    private readonly ISnapshotCache _cache;

    /// <summary>The result of the most recent <see cref="RefreshAsync"/>/<see cref="LoadCachedAsync"/> call.</summary>
    public SnapshotState? Current { get; private set; }

    public SnapshotStore(MobileSyncClient client, PairedCompanyStore store, ISnapshotCache cache)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Fetches and decrypts the latest snapshot from the server for the active paired company.
    /// Falls back to the local cache (marked stale) on a network error, a missing/undecryptable
    /// payload, or when the server hasn't received a snapshot yet (404).
    /// </summary>
    public async Task<SnapshotState> RefreshAsync(CancellationToken ct)
    {
        var record = await _store.GetActiveAsync();
        if (record == null)
        {
            Current = SnapshotState.NotPaired();
            return Current;
        }

        SnapshotResult? result;
        try
        {
            result = await _client.GetSnapshotAsync(record.DeviceToken, ct);
        }
        catch (SyncUnauthorizedException)
        {
            // This device was revoked desktop-side; the token is dead. Signal the shell to drop the
            // paired company rather than quietly serving stale cached data.
            Current = SnapshotState.Revoked();
            return Current;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Current = await BuildFromCacheOrWaitingAsync(record, "Couldn't reach the server. Showing your last synced data.");
            return Current;
        }

        if (result == null || string.IsNullOrEmpty(result.Ciphertext))
        {
            // 404: the desktop hasn't uploaded a snapshot yet.
            Current = await BuildFromCacheOrWaitingAsync(record, null);
            return Current;
        }

        byte[] plaintext;
        MobileSnapshot? snapshot;
        try
        {
            plaintext = SyncCrypto.Decrypt(result.Ciphertext, record.SyncKeyBase64);
            snapshot = JsonSerializer.Deserialize<MobileSnapshot>(plaintext, JsonOptions);
        }
        catch (Exception)
        {
            // Corrupt payload, wrong key, etc. Don't crash the refresh; fall back to cache.
            Current = await BuildFromCacheOrWaitingAsync(record, "Couldn't read the latest data from your desktop. Showing your last synced data.");
            return Current;
        }

        if (snapshot == null)
        {
            Current = await BuildFromCacheOrWaitingAsync(record, null);
            return Current;
        }

        // Cache the raw decrypted JSON (not a re-serialization) so offline viewing matches exactly.
        var json = Encoding.UTF8.GetString(plaintext);
        await _cache.SaveAsync(record.CompanyUid, json);

        Current = new SnapshotState
        {
            Status = SnapshotStatus.Loaded,
            Snapshot = snapshot,
            LastSyncedAt = ParseUpdatedAt(result.UpdatedAt) ?? DateTime.UtcNow,
            IsStale = false,
        };
        return Current;
    }

    /// <summary>Loads the last cached snapshot for offline viewing, without hitting the network.</summary>
    public async Task<SnapshotState> LoadCachedAsync()
    {
        var record = await _store.GetActiveAsync();
        if (record == null)
        {
            Current = SnapshotState.NotPaired();
            return Current;
        }

        Current = await BuildFromCacheOrWaitingAsync(record, null);
        return Current;
    }

    private async Task<SnapshotState> BuildFromCacheOrWaitingAsync(PairedCompanyRecord record, string? error)
    {
        var cachedJson = await _cache.LoadAsync(record.CompanyUid);
        if (string.IsNullOrEmpty(cachedJson))
        {
            return new SnapshotState { Status = SnapshotStatus.WaitingForFirstSync, Error = error };
        }

        MobileSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<MobileSnapshot>(cachedJson, JsonOptions);
        }
        catch (JsonException)
        {
            return new SnapshotState { Status = SnapshotStatus.WaitingForFirstSync, Error = error };
        }

        if (snapshot == null)
        {
            return new SnapshotState { Status = SnapshotStatus.WaitingForFirstSync, Error = error };
        }

        return new SnapshotState
        {
            Status = SnapshotStatus.Loaded,
            Snapshot = snapshot,
            LastSyncedAt = snapshot.GeneratedAt == default ? null : snapshot.GeneratedAt,
            IsStale = true,
            Error = error,
        };
    }

    private static DateTime? ParseUpdatedAt(string? updatedAt)
    {
        if (string.IsNullOrWhiteSpace(updatedAt))
        {
            return null;
        }

        return DateTime.TryParse(
            updatedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var dt)
            ? dt
            : null;
    }
}
