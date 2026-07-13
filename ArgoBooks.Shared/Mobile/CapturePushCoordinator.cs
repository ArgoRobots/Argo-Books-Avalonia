using System.Text.Json;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Shared.Sync;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Task 5: encrypts a confirmed <see cref="CapturedTransaction"/> with the active paired company's
/// sync key and pushes it onto the desktop's capture queue (<see cref="MobileSyncClient.PushCaptureAsync"/>),
/// mirroring what <see cref="SnapshotStore"/> does in reverse for downloading a snapshot. Pure logic
/// with no UI/device dependency beyond the injected <see cref="MobileSyncClient"/>/
/// <see cref="PairedCompanyStore"/> seams, so it is fully unit-testable (see
/// ArgoBooks.Tests/Mobile/CapturePushCoordinatorTests.cs).
/// </summary>
public class CapturePushCoordinator
{
    private readonly MobileSyncClient _client;
    private readonly PairedCompanyStore _store;

    public CapturePushCoordinator(MobileSyncClient client, PairedCompanyStore store)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Encrypts <paramref name="tx"/> with the active paired company's sync key and pushes it to
    /// the server queue. Returns false (without throwing) if there's no active paired company, or
    /// if the push fails for any reason (network error, non-success response, etc.) - the caller
    /// still gets to keep the confirmed scan locally either way.
    /// </summary>
    public async Task<bool> PushAsync(CapturedTransaction tx, CancellationToken ct)
    {
        if (tx == null) throw new ArgumentNullException(nameof(tx));

        var record = await _store.GetActiveAsync();
        if (record == null)
        {
            return false;
        }

        try
        {
            var cipher = SyncCrypto.Encrypt(JsonSerializer.SerializeToUtf8Bytes(tx), record.SyncKeyBase64);
            await _client.PushCaptureAsync(record.DeviceToken, cipher, ct);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
