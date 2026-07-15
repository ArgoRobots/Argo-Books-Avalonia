namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Writes each queued (offline-captured) receipt image to
/// "&lt;baseDirectory&gt;/pending-scans/&lt;id&gt;.jpg". Plain file IO, so it is not unit-tested
/// directly - PendingScanOutboxTests use an in-memory <see cref="IPendingScanStorage"/> fake
/// instead. On Android, construct with
/// <c>Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory</c>, the same base directory
/// FileSnapshotCache uses.
/// </summary>
public class FilePendingScanStorage : IPendingScanStorage
{
    private readonly string _directory;

    public FilePendingScanStorage(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be empty.", nameof(baseDirectory));

        _directory = Path.Combine(baseDirectory, "pending-scans");
    }

    private string PathFor(string id) => Path.Combine(_directory, $"{id}.jpg");

    public Task<IReadOnlyList<string>> ListIdsAsync()
    {
        if (!Directory.Exists(_directory))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        IReadOnlyList<string> ids = Directory.GetFiles(_directory, "*.jpg")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();

        return Task.FromResult(ids);
    }

    public async Task SaveAsync(string id, byte[] imageBytes)
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllBytesAsync(PathFor(id), imageBytes);
    }

    public async Task<byte[]?> LoadAsync(string id)
    {
        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await File.ReadAllBytesAsync(path);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public Task DeleteAsync(string id)
    {
        var path = PathFor(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
