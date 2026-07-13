using ArgoBooks.Shared.Mobile;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>
/// Unit tests for PairedCompanyStore using an in-memory ISecureStore fake.
/// </summary>
public class PairedCompanyStoreTests
{
    /// <summary>
    /// In-memory implementation of ISecureStore for testing.
    /// Simulates secure storage without requiring a device.
    /// </summary>
    private class InMemorySecureStore : ISecureStore
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task SetAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            _storage.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task RemoveAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SaveAsync_And_GetAllAsync_ReturnsAllSavedRecords()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        var record1 = new PairedCompanyRecord
        {
            CompanyUid = "company-1",
            CompanyLabel = "Acme Corp",
            DeviceToken = "token-1",
            SyncKeyBase64 = "key1-base64=="
        };

        var record2 = new PairedCompanyRecord
        {
            CompanyUid = "company-2",
            CompanyLabel = "Widget Inc",
            DeviceToken = "token-2",
            SyncKeyBase64 = "key2-base64=="
        };

        // Act
        await pairedStore.SaveAsync(record1);
        await pairedStore.SaveAsync(record2);
        var all = await pairedStore.GetAllAsync();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Contains(all, c => c.CompanyUid == "company-1" && c.CompanyLabel == "Acme Corp");
        Assert.Contains(all, c => c.CompanyUid == "company-2" && c.CompanyLabel == "Widget Inc");
    }

    [Fact]
    public async Task SetActiveAsync_And_GetActiveAsync_ReturnsActiveCompany()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        var record = new PairedCompanyRecord
        {
            CompanyUid = "company-1",
            CompanyLabel = "Acme Corp",
            DeviceToken = "token-1",
            SyncKeyBase64 = "key1-base64=="
        };

        // Act
        await pairedStore.SaveAsync(record);
        await pairedStore.SetActiveAsync("company-1");
        var active = await pairedStore.GetActiveAsync();

        // Assert
        Assert.NotNull(active);
        Assert.Equal("company-1", active.CompanyUid);
        Assert.Equal("Acme Corp", active.CompanyLabel);
    }

    [Fact]
    public async Task SaveAsync_Upserts_DoesNotDuplicate()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        var record1 = new PairedCompanyRecord
        {
            CompanyUid = "company-1",
            CompanyLabel = "Acme Corp",
            DeviceToken = "token-1",
            SyncKeyBase64 = "key1-base64=="
        };

        var record1Updated = new PairedCompanyRecord
        {
            CompanyUid = "company-1",
            CompanyLabel = "Acme Corp Updated",
            DeviceToken = "token-1-updated",
            SyncKeyBase64 = "key1-updated-base64=="
        };

        // Act
        await pairedStore.SaveAsync(record1);
        await pairedStore.SaveAsync(record1Updated);
        var all = await pairedStore.GetAllAsync();

        // Assert
        Assert.Single(all);
        Assert.Equal("Acme Corp Updated", all[0].CompanyLabel);
        Assert.Equal("token-1-updated", all[0].DeviceToken);
    }

    [Fact]
    public async Task RemoveAsync_ClearsActiveIfItWasActive()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        var record1 = new PairedCompanyRecord
        {
            CompanyUid = "company-1",
            CompanyLabel = "Acme Corp",
            DeviceToken = "token-1",
            SyncKeyBase64 = "key1-base64=="
        };

        var record2 = new PairedCompanyRecord
        {
            CompanyUid = "company-2",
            CompanyLabel = "Widget Inc",
            DeviceToken = "token-2",
            SyncKeyBase64 = "key2-base64=="
        };

        // Act
        await pairedStore.SaveAsync(record1);
        await pairedStore.SaveAsync(record2);
        await pairedStore.SetActiveAsync("company-1");

        var activeBefore = await pairedStore.GetActiveAsync();
        Assert.NotNull(activeBefore);
        Assert.Equal("company-1", activeBefore.CompanyUid);

        await pairedStore.RemoveAsync("company-1");

        var activeAfter = await pairedStore.GetActiveAsync();
        var all = await pairedStore.GetAllAsync();

        // Assert
        Assert.Null(activeAfter);
        Assert.Single(all);
        Assert.Equal("company-2", all[0].CompanyUid);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsNull_WhenNoActiveSet()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        var record = new PairedCompanyRecord
        {
            CompanyUid = "company-1",
            CompanyLabel = "Acme Corp",
            DeviceToken = "token-1",
            SyncKeyBase64 = "key1-base64=="
        };

        // Act
        await pairedStore.SaveAsync(record);
        var active = await pairedStore.GetActiveAsync();

        // Assert
        Assert.Null(active);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoRecords()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        // Act
        var all = await pairedStore.GetAllAsync();

        // Assert
        Assert.Empty(all);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsArgumentException_WhenCompanyNotFound()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await pairedStore.SetActiveAsync("nonexistent-company")
        );
    }

    [Fact]
    public async Task SaveAsync_ThrowsArgumentException_WhenCompanyUidEmpty()
    {
        // Arrange
        var store = new InMemorySecureStore();
        var pairedStore = new PairedCompanyStore(store);

        var record = new PairedCompanyRecord
        {
            CompanyUid = string.Empty,
            CompanyLabel = "Acme Corp",
            DeviceToken = "token-1",
            SyncKeyBase64 = "key1-base64=="
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await pairedStore.SaveAsync(record)
        );
    }
}
