namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Seam interface for secure storage operations.
/// This allows unit testing with an in-memory fake without requiring a device.
/// The real implementation uses Microsoft.Maui.Storage.SecureStorage (Android Keystore).
/// </summary>
public interface ISecureStore
{
    /// <summary>
    /// Stores a key-value pair in secure storage.
    /// </summary>
    /// <param name="key">The key to store under.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Retrieves a value from secure storage.
    /// </summary>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>The stored value, or null if the key does not exist.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Removes a key-value pair from secure storage.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string key);
}
