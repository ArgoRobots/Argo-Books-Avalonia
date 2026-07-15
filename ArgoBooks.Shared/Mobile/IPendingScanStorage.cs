namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Storage seam for <see cref="PendingScanOutbox"/>'s queued (offline-captured, not-yet-AI-scanned)
/// receipt images, so the outbox's enqueue/drain logic can be unit-tested with an in-memory fake
/// instead of real device storage. <see cref="FilePendingScanStorage"/> is the real implementation,
/// mirroring the ISnapshotCache/FileSnapshotCache split already used for the snapshot cache.
/// </summary>
public interface IPendingScanStorage
{
    /// <summary>All queued image ids, in no particular guaranteed order.</summary>
    Task<IReadOnlyList<string>> ListIdsAsync();

    Task SaveAsync(string id, byte[] imageBytes);

    /// <summary>The queued image's bytes, or null if it doesn't exist (already drained, or never
    /// written).</summary>
    Task<byte[]?> LoadAsync(string id);

    Task DeleteAsync(string id);
}
