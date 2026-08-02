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
    public async Task PeekNextAsync_ReturnsAQueuedImageWithItsBytes()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([1, 2, 3]);

        var next = await outbox.PeekNextAsync();

        Assert.NotNull(next);
        Assert.Equal([1, 2, 3], next!.Image);
        Assert.False(string.IsNullOrEmpty(next.Id));
    }

    [Fact]
    public async Task PeekNextAsync_EmptyQueue_ReturnsNull()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());

        Assert.Null(await outbox.PeekNextAsync());
    }

    [Fact]
    public async Task PeekNextAsync_DoesNotRemove_SoRepeatedPeeksSeeTheSameItem()
    {
        // Peek must not consume: a user who backs out of review (or a failed push) has to leave the
        // receipt queued. Removal is explicit, via RemoveAsync only after a confirmed+pushed review.
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([7]);

        var first = await outbox.PeekNextAsync();
        var second = await outbox.PeekNextAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(1, await outbox.GetPendingCountAsync());
    }

    [Fact]
    public async Task RemoveAsync_DropsTheReviewedImage()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([7]);
        var item = await outbox.PeekNextAsync();

        await outbox.RemoveAsync(item!.Id);

        Assert.Equal(0, await outbox.GetPendingCountAsync());
        Assert.Null(await outbox.PeekNextAsync());
    }

    [Fact]
    public async Task RemoveAsync_UnknownId_IsANoOp()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());
        await outbox.EnqueueAsync([7]);

        await outbox.RemoveAsync("does-not-exist");

        Assert.Equal(1, await outbox.GetPendingCountAsync());
    }

    [Fact]
    public async Task GetPendingCountAsync_Empty_ReturnsZero()
    {
        var outbox = new PendingScanOutbox(new InMemoryPendingScanStorage());

        Assert.Equal(0, await outbox.GetPendingCountAsync());
    }
}
