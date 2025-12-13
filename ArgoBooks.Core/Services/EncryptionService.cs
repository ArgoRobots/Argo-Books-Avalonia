using System.Security.Cryptography;
using ArgoBooks.Core.Security;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Implementation of AES-256-GCM encryption service.
/// </summary>
public class EncryptionService : IEncryptionService
{
    #region Key Generation

    /// <inheritdoc />
    public string GenerateSalt()
    {
        return KeyDerivation.GenerateSaltBase64();
    }

    /// <inheritdoc />
    public string GenerateIv()
    {
        return KeyDerivation.GenerateIvBase64();
    }

    #endregion

    #region Password Hashing

    /// <inheritdoc />
    public string HashPassword(string password, string salt)
    {
        return KeyDerivation.ComputePasswordHashBase64(password, salt);
    }

    /// <inheritdoc />
    public bool ValidatePassword(string password, string storedHash, string salt)
    {
        return KeyDerivation.VerifyPasswordBase64(password, storedHash, salt);
    }

    #endregion

    #region Encryption (Byte Arrays)

    /// <inheritdoc />
    public byte[] Encrypt(byte[] data, string password, string salt, string iv)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(salt);
        ArgumentException.ThrowIfNullOrEmpty(iv);

        // Derive encryption key from password
        var key = KeyDerivation.DeriveKey(password, salt);
        var nonce = Convert.FromBase64String(iv);

        // Allocate space for ciphertext + tag
        var ciphertext = new byte[data.Length];
        var tag = new byte[KeyDerivation.TagSize];

        // Encrypt using AES-GCM
        using var aesGcm = new AesGcm(key, KeyDerivation.TagSize);
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        // Combine ciphertext and tag
        var result = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);

        return result;
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] encryptedData, string password, string salt, string iv)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(salt);
        ArgumentException.ThrowIfNullOrEmpty(iv);

        if (encryptedData.Length < KeyDerivation.TagSize)
            throw new CryptographicException("Invalid encrypted data.");

        // Derive encryption key from password
        var key = KeyDerivation.DeriveKey(password, salt);
        var nonce = Convert.FromBase64String(iv);

        // Split ciphertext and tag
        var ciphertextLength = encryptedData.Length - KeyDerivation.TagSize;
        var ciphertext = new byte[ciphertextLength];
        var tag = new byte[KeyDerivation.TagSize];

        Buffer.BlockCopy(encryptedData, 0, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(encryptedData, ciphertextLength, tag, 0, KeyDerivation.TagSize);

        // Decrypt using AES-GCM
        var plaintext = new byte[ciphertextLength];
        using var aesGcm = new AesGcm(key, KeyDerivation.TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    #endregion

    #region Encryption (Streams)

    /// <inheritdoc />
    public async Task<MemoryStream> EncryptAsync(Stream inputStream, string password, string salt, string iv)
    {
        ArgumentNullException.ThrowIfNull(inputStream);

        // Read all data from input stream
        byte[] data;
        if (inputStream is MemoryStream ms)
        {
            data = ms.ToArray();
        }
        else
        {
            using var memStream = new MemoryStream();
            await inputStream.CopyToAsync(memStream);
            data = memStream.ToArray();
        }

        // Encrypt
        var encryptedData = Encrypt(data, password, salt, iv);

        // Return as memory stream
        var result = new MemoryStream(encryptedData);
        result.Position = 0;
        return result;
    }

    /// <inheritdoc />
    public async Task<MemoryStream> DecryptAsync(Stream encryptedStream, string password, string salt, string iv)
    {
        ArgumentNullException.ThrowIfNull(encryptedStream);

        // Read all data from encrypted stream
        byte[] encryptedData;
        if (encryptedStream is MemoryStream ms)
        {
            encryptedData = ms.ToArray();
        }
        else
        {
            using var memStream = new MemoryStream();
            await encryptedStream.CopyToAsync(memStream);
            encryptedData = memStream.ToArray();
        }

        // Decrypt
        var decryptedData = Decrypt(encryptedData, password, salt, iv);

        // Return as memory stream
        var result = new MemoryStream(decryptedData);
        result.Position = 0;
        return result;
    }

    #endregion

    #region Password Validation

    /// <inheritdoc />
    public bool IsPasswordValid(string password)
    {
        return PasswordValidator.IsValid(password);
    }

    /// <inheritdoc />
    public string? GetPasswordValidationError(string password)
    {
        return PasswordValidator.GetValidationError(password);
    }

    #endregion
}
