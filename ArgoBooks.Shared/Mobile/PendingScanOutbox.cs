using System.Threading;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Task 6's offline capture queue: when ML Kit crops a receipt but there's no network to run the
/// AI scan, the cropped image bytes are enqueued here instead of calling the scanner immediately.
/// <see cref="DrainAsync"/> is called again once connectivity returns (or the app is foregrounded -
/// see ShellViewModel.DrainPendingScansAsync) to re-run the AI scan for each queued image. Pure
/// logic behind the <see cref="IPendingScanStorage"/> seam, so it's unit-tested with an in-memory
/// fake (see PendingScanOutboxTests) rather than real device file storage
/// (<see cref="FilePendingScanStorage"/> is the real implementation).
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

    /// <summary>Number of images still queued, waiting on a scan.</summary>
    public async Task<int> GetPendingCountAsync() => (await _storage.ListIdsAsync()).Count;

    /// <summary>
    /// Attempts <paramref name="scanAndPush"/> for every queued image (the actual AI-scan-and-push
    /// call is supplied by the caller, since only ShellViewModel has the scanner/push dependencies
    /// this needs). An image is removed from the queue only when <paramref name="scanAndPush"/>
    /// returns true; anything that fails (still offline, scan unreadable, push rejected) stays
    /// queued for the next drain attempt rather than being lost.
    /// </summary>
    public async Task DrainAsync(Func<byte[], Task<bool>> scanAndPush, CancellationToken cancellationToken = default)
    {
        if (scanAndPush == null)
        {
            throw new ArgumentNullException(nameof(scanAndPush));
        }

        foreach (var id in await _storage.ListIdsAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageBytes = await _storage.LoadAsync(id);
            if (imageBytes == null)
            {
                // Nothing left under this id (already gone) - drop the stale entry.
                await _storage.DeleteAsync(id);
                continue;
            }

            bool succeeded;
            try
            {
                succeeded = await scanAndPush(imageBytes);
            }
            catch (Exception)
            {
                succeeded = false;
            }

            if (succeeded)
            {
                await _storage.DeleteAsync(id);
            }
        }
    }
}
