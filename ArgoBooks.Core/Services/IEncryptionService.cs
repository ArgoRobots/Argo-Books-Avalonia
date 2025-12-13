namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for AES-256 encryption and decryption of company files.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using AES-256-GCM.
    /// </summary>
    /// <param name="data">Data to encrypt</param>
    /// <param name="password">Password for encryption</param>
    /// <returns>Encrypted data with salt, nonce, and tag prepended</returns>
    byte[] Encrypt(byte[] data, string password);

    /// <summary>
    /// Decrypts data that was encrypted with AES-256-GCM.
    /// </summary>
    /// <param name="encryptedData">Encrypted data with salt, nonce, and tag</param>
    /// <param name="password">Password for decryption</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if password is incorrect</exception>
    byte[] Decrypt(byte[] encryptedData, string password);

    /// <summary>
    /// Encrypts data asynchronously using AES-256-GCM.
    /// </summary>
    /// <param name="data">Data to encrypt</param>
    /// <param name="password">Password for encryption</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted data with salt, nonce, and tag prepended</returns>
    Task<byte[]> EncryptAsync(byte[] data, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data asynchronously that was encrypted with AES-256-GCM.
    /// </summary>
    /// <param name="encryptedData">Encrypted data with salt, nonce, and tag</param>
    /// <param name="password">Password for decryption</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if password is incorrect</exception>
    Task<byte[]> DecryptAsync(byte[] encryptedData, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a password meets the minimum requirements.
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password meets requirements</returns>
    bool ValidatePassword(string password);

    /// <summary>
    /// Gets the password validation error message if the password is invalid.
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Error message or null if valid</returns>
    string? GetPasswordValidationError(string password);
}
