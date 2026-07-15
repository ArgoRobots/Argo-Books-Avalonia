namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Writes each company's decrypted snapshot JSON to "&lt;baseDirectory&gt;/snapshot-&lt;companyUid&gt;.json".
/// Plain file IO, so it is not unit-tested directly; <c>SnapshotStoreTests</c> use an in-memory
/// <see cref="ISnapshotCache"/> fake instead. On Android, construct with
/// <c>Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory</c>.
/// </summary>
public class FileSnapshotCache : ISnapshotCache
{
    private readonly string _baseDirectory;

    public FileSnapshotCache(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be empty.", nameof(baseDirectory));

        _baseDirectory = baseDirectory;
    }

    private string PathFor(string companyUid) => Path.Combine(_baseDirectory, $"snapshot-{companyUid}.json");

    public async Task SaveAsync(string companyUid, string json)
    {
        Directory.CreateDirectory(_baseDirectory);
        await File.WriteAllTextAsync(PathFor(companyUid), json);
    }

    public async Task<string?> LoadAsync(string companyUid)
    {
        var path = PathFor(companyUid);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
