namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Seam for caching the last decrypted snapshot JSON per paired company, so the phone can keep
/// showing data while offline. The real implementation writes a file under the app's data
/// directory (see <see cref="FileSnapshotCache"/>); tests use an in-memory fake.
/// </summary>
public interface ISnapshotCache
{
    /// <summary>Persists the decrypted snapshot JSON for the given company (overwrites any previous cache entry).</summary>
    Task SaveAsync(string companyUid, string json);

    /// <summary>Returns the last cached snapshot JSON for the given company, or null if none is cached.</summary>
    Task<string?> LoadAsync(string companyUid);
}
