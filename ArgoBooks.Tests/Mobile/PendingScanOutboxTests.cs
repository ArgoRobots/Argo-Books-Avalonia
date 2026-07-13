using ArgoBooks.Shared.Mobile;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>
/// Unit tests for PendingScanOutbox (Task 6's offline capture queue), using an in-memory
/// IPendingScanStorage fake rather than real device file storage.
/// </summary>
public class PendingScanOutboxTests
{
    private sealed class InMemoryPendingScanStorage : IPendingScanStorage
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public Task<IReadOnlyList<string>> ListIdsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(new List<string>(_files.Keys));

        public Task SaveAsync(string id, byte[] imageBytes)
        {
            _files[id] = imageBytes;
            return Task.CompletedTask;
        }

        public Task<byte[]?> LoadAsync(string id)
        {
            _files.TryGetValue(id, out var bytes);
            return Task.FromResult(bytes);
        }

        public Task DeleteAsync(string id)
        {
            _files.Remove(id);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task EnqueueAsync_IncreasesPendingCount()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());

        await outbox.EnqueueAsync([1, 2, 3]);
        await outbox.EnqueueAsync([4, 5, 6]);

        Assert.Equal(2, await outbox.GetPendingCountAsync());
    }

    [Fact]
    public async Task EnqueueAsync_EmptyImage_Throws()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());

        await Assert.ThrowsAsync<ArgumentException>(() => outbox.EnqueueAsync([]));
    }

    [Fact]
    public async Task DrainAsync_CallsScanForEveryQueuedImage_AndClearsQueueOnSuccess()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([1]);
        await outbox.EnqueueAsync([2]);

        var scannedPayloads = new List<byte[]>();
        await outbox.DrainAsync(bytes =>
        {
            scannedPayloads.Add(bytes);
            return Task.FromResult(true);
        });

        Assert.Equal(2, scannedPayloads.Count);
        Assert.Equal(0, await outbox.GetPendingCountAsync());
    }

    [Fact]
    public async Task DrainAsync_FailedScan_LeavesImageQueuedForNextAttempt()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([9]);

        await outbox.DrainAsync(_ => Task.FromResult(false));

        Assert.Equal(1, await outbox.GetPendingCountAsync());
    }

    [Fact]
    public async Task DrainAsync_ThrowingScan_LeavesImageQueuedRatherThanCrashing()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([9]);

        await outbox.DrainAsync(_ => throw new InvalidOperationException("network unavailable"));

        Assert.Equal(1, await outbox.GetPendingCountAsync());
    }

    [Fact]
    public async Task GetPendingCountAsync_Empty_ReturnsZero()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());

        Assert.Equal(0, await outbox.GetPendingCountAsync());
    }
}
