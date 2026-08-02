namespace ArgoBooks.Shared.Mobile;

/// <summary>One cropped receipt waiting offline: its stable queue <see cref="Id"/> (reused as the
/// transaction's ScanUid so a later push is idempotent) and the cropped <see cref="Image"/> bytes.</summary>
public sealed record PendingScan(string Id, byte[] Image);

/// <summary>
/// Task 6's offline capture queue: when ML Kit crops a receipt but there's no network to run the
/// AI scan, the cropped image bytes are enqueued here instead of calling the scanner immediately.
/// Once connectivity returns, the Capture screen surfaces a "N receipts ready to review" prompt and
/// the user walks each queued image through the SAME scan -> review -> confirm flow as an online
/// capture (see ShellViewModel.StartOfflineReviewAsync): nothing is auto-posted to the books without
/// review. <see cref="PeekNextAsync"/> hands the next queued image to that flow; <see cref="RemoveAsync"/>
/// drops it once the user has confirmed and it has pushed. The queue <see cref="PendingScan.Id"/> is
/// reused as the transaction's ScanUid, so if a push response is lost the item stays queued and a
/// re-review re-sends the same idempotency key (the desktop de-duplicates). Pure logic behind the
/// <see cref="IPendingScanStorage"/> seam, so it's unit-tested with an in-memory fake (see
/// PendingScanOutboxTests) rather than real device file storage (<see cref="FilePendingScanStorage"/>
/// is the real implementation).
/// </summary>
public class PendingScanOutbox
{
    private readonly IPendingScanStorage _storage;

    public PendingScanOutbox(IPendingScanStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    /// <summary>Queues a cropped image for a later scan and returns its queue id. Throws if the
    /// image is empty - callers should only enqueue a real ML-Kit-cropped image.</summary>
    public async Task<string> EnqueueAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            throw new ArgumentException("Cannot queue an empty image.", nameof(imageBytes));
        }

        var id = Guid.NewGuid().ToString("N");
        await _storage.SaveAsync(id, imageBytes);
        return id;
    }

    /// <summary>Number of images still queued, waiting to be reviewed.</summary>
    public async Task<int> GetPendingCountAsync() => (await _storage.ListIdsAsync()).Count;

    /// <summary>
    /// Returns the next queued image (its stable id + cropped bytes) for the review flow to scan, or
    /// null when the queue is empty. Stale entries whose bytes have gone missing are dropped in
    /// passing rather than returned. The item is NOT removed here - the caller removes it via
    /// <see cref="RemoveAsync"/> only once the user has confirmed the review and it has pushed, so a
    /// user who backs out (or a failed push) leaves the receipt queued for another attempt.
    /// </summary>
    public async Task<PendingScan?> PeekNextAsync()
    {
        foreach (var id in await _storage.ListIdsAsync())
        {
            var imageBytes = await _storage.LoadAsync(id);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                // Nothing usable under this id - drop the stale entry and look at the next.
                await _storage.DeleteAsync(id);
                continue;
            }

            return new PendingScan(id, imageBytes);
        }

        return null;
    }

    /// <summary>Drops a queued image once the user has reviewed and pushed it. Idempotent - deleting
    /// an id that's already gone is a no-op.</summary>
    public Task RemoveAsync(string id) => _storage.DeleteAsync(id);
}
