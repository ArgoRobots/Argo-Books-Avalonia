using ArgoBooks.Core.Services.Sync;

namespace ArgoBooks.Shared.Mobile;

/// <summary>High-level status of a <see cref="SnapshotState"/> result.</summary>
public enum SnapshotStatus
{
    /// <summary>No company is paired on this device yet.</summary>
    NotPaired,

    /// <summary>Paired, but the desktop hasn't uploaded a snapshot yet (and nothing is cached).</summary>
    WaitingForFirstSync,

    /// <summary>A snapshot is available (either freshly fetched or served from the offline cache).</summary>
    Loaded,

    /// <summary>The server rejected this device's token (revoked desktop-side). The phone should
    /// drop the paired company and return to pairing.</summary>
    Revoked,
}

/// <summary>
/// Result of <see cref="SnapshotStore.RefreshAsync"/> / <see cref="SnapshotStore.LoadCachedAsync"/>.
/// </summary>
public sealed class SnapshotState
{
    /// <summary>What the phone should show.</summary>
    public SnapshotStatus Status { get; init; }

    /// <summary>The decrypted read model, set only when <see cref="Status"/> is <see cref="SnapshotStatus.Loaded"/>.</summary>
    public MobileSnapshot? Snapshot { get; init; }

    /// <summary>When this data was generated on the desktop (UTC), if known.</summary>
    public DateTime? LastSyncedAt { get; init; }

    /// <summary>True when this snapshot came from the local offline cache rather than a fresh server fetch.</summary>
    public bool IsStale { get; init; }

    /// <summary>A user-facing note about why a fresh fetch fell back to cache/waiting (network error, bad payload, etc.).</summary>
    public string? Error { get; init; }

    public static SnapshotState NotPaired() => new() { Status = SnapshotStatus.NotPaired };

    public static SnapshotState Revoked() => new() { Status = SnapshotStatus.Revoked };
}
