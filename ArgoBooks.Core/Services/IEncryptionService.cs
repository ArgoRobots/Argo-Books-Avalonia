namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for AES-256-GCM encryption and decryption of company files.
/// </summary>
public interface IEncryptionService
{
    #region Key Generation

    /// <summary>
    /// Generates a random salt for key derivation.
    /// </summary>
    /// <returns>Base64-encoded salt (32 bytes).</returns>
    string GenerateSalt();

    /// <summary>
    /// Generates a random IV/nonce for encryption.
    /// </summary>
    /// <returns>Base64-encoded IV (12 bytes for GCM).</returns>
    string GenerateIv();

    #endregion

    #region Password Hashing

    /// <summary>
    /// Hashes a password using PBKDF2 for storage.
    /// </summary>
    /// <param name="password">Password to hash.</param>
    /// <param name="salt">Base64-encoded salt.</param>
    /// <returns>Base64-encoded password hash.</returns>
    string HashPassword(string password, string salt);

    /// <summary>
    /// Validates a password against a stored hash.
    /// </summary>
    /// <param name="password">Password to validate.</param>
    /// <param name="storedHash">Base64-encoded stored hash.</param>
    /// <param name="salt">Base64-encoded salt.</param>
    /// <returns>True if password matches.</returns>
    bool ValidatePassword(string password, string storedHash, string salt);

    #endregion

    #region Encryption (Byte Arrays)

    /// <summary>
    /// Encrypts data using AES-256-GCM.
    /// </summary>
    /// <param name="data">Data to encrypt.</param>
    /// <param name="password">Password for encryption.</param>
    /// <param name="salt">Base64-encoded salt for key derivation.</param>
    /// <param name="iv">Base64-encoded IV/nonce.</param>
    /// <returns>Encrypted data with authentication tag appended.</returns>
    byte[] Encrypt(byte[] data, string password, string salt, string iv);

    /// <summary>
    /// Decrypts data that was encrypted with AES-256-GCM.
    /// </summary>
    /// <param name="encryptedData">Encrypted data with authentication tag.</param>
    /// <param name="password">Password for decryption.</param>
    /// <param name="salt">Base64-encoded salt for key derivation.</param>
    /// <param name="iv">Base64-encoded IV/nonce.</param>
    /// <returns>Decrypted data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if password is incorrect or data is tampered.</exception>
    byte[] Decrypt(byte[] encryptedData, string password, string salt, string iv);

    #endregion

    #region Encryption (Streams)

    /// <summary>
    /// Encrypts a stream using AES-256-GCM.
    /// </summary>
    /// <param name="inputStream">Stream to encrypt.</param>
    /// <param name="password">Password for encryption.</param>
    /// <param name="salt">Base64-encoded salt for key derivation.</param>
    /// <param name="iv">Base64-encoded IV/nonce.</param>
    /// <returns>Memory stream containing encrypted data.</returns>
    Task<MemoryStream> EncryptAsync(Stream inputStream, string password, string salt, string iv);

    /// <summary>
    /// Decrypts a stream that was encrypted with AES-256-GCM.
    /// </summary>
    /// <param name="encryptedStream">Stream containing encrypted data.</param>
    /// <param name="password">Password for decryption.</param>
    /// <param name="salt">Base64-encoded salt for key derivation.</param>
    /// <param name="iv">Base64-encoded IV/nonce.</param>
    /// <returns>Memory stream containing decrypted data.</returns>
    Task<MemoryStream> DecryptAsync(Stream encryptedStream, string password, string salt, string iv);

    #endregion

    #region Password Validation

    /// <summary>
    /// Validates that a password meets the minimum requirements.
    /// </summary>
    /// <param name="password">Password to validate.</param>
    /// <returns>True if password meets requirements.</returns>
    bool IsPasswordValid(string password);

    /// <summary>
    /// Gets the password validation error message if the password is invalid.
    /// </summary>
    /// <param name="password">Password to validate.</param>
    /// <returns>Error message or null if valid.</returns>
    string? GetPasswordValidationError(string password);

    #endregion
}
