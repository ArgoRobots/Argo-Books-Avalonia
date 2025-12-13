using System.Security.Cryptography;
using System.Text;

namespace ArgoBooks.Core.Security;

/// <summary>
/// Provides key derivation functionality using PBKDF2.
/// </summary>
public static class KeyDerivation
{
    /// <summary>
    /// Number of PBKDF2 iterations.
    /// OWASP recommends at least 600,000 for SHA-256 as of 2023.
    /// Using 100,000 for balance between security and performance.
    /// </summary>
    public const int Iterations = 100_000;

    /// <summary>
    /// Salt size in bytes (256 bits).
    /// </summary>
    public const int SaltSize = 32;

    /// <summary>
    /// Derived key size in bytes (256 bits for AES-256).
    /// </summary>
    public const int KeySize = 32;

    /// <summary>
    /// Hash size for password verification in bytes.
    /// </summary>
    public const int HashSize = 32;

    /// <summary>
    /// IV/Nonce size for AES-GCM in bytes (96 bits recommended).
    /// </summary>
    public const int IvSize = 12;

    /// <summary>
    /// Authentication tag size for AES-GCM in bytes (128 bits).
    /// </summary>
    public const int TagSize = 16;

    /// <summary>
    /// Derives an encryption key from a password using PBKDF2-SHA256.
    /// </summary>
    /// <param name="password">The password to derive the key from.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <returns>The derived key bytes.</returns>
    public static byte[] DeriveKey(string password, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }

    /// <summary>
    /// Derives an encryption key from a password using PBKDF2-SHA256.
    /// </summary>
    /// <param name="password">The password to derive the key from.</param>
    /// <param name="saltBase64">Base64-encoded salt.</param>
    /// <returns>The derived key bytes.</returns>
    public static byte[] DeriveKey(string password, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        return DeriveKey(password, salt);
    }

    /// <summary>
    /// Generates a cryptographically secure random salt.
    /// </summary>
    /// <returns>Random salt bytes.</returns>
    public static byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(SaltSize);
    }

    /// <summary>
    /// Generates a cryptographically secure random salt as Base64.
    /// </summary>
    /// <returns>Base64-encoded random salt.</returns>
    public static string GenerateSaltBase64()
    {
        return Convert.ToBase64String(GenerateSalt());
    }

    /// <summary>
    /// Generates a cryptographically secure random IV/nonce for AES-GCM.
    /// </summary>
    /// <returns>Random IV bytes.</returns>
    public static byte[] GenerateIv()
    {
        return RandomNumberGenerator.GetBytes(IvSize);
    }

    /// <summary>
    /// Generates a cryptographically secure random IV/nonce as Base64.
    /// </summary>
    /// <returns>Base64-encoded random IV.</returns>
    public static string GenerateIvBase64()
    {
        return Convert.ToBase64String(GenerateIv());
    }

    /// <summary>
    /// Computes a password hash for storage/verification using PBKDF2.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <returns>The password hash bytes.</returns>
    public static byte[] ComputePasswordHash(string password, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(HashSize);
    }

    /// <summary>
    /// Computes a password hash for storage/verification using PBKDF2.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="saltBase64">Base64-encoded salt.</param>
    /// <returns>Base64-encoded password hash.</returns>
    public static string ComputePasswordHashBase64(string password, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var hash = ComputePasswordHash(password, salt);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies a password against a stored hash using constant-time comparison.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="storedHash">The stored hash bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <returns>True if the password matches.</returns>
    public static bool VerifyPassword(string password, byte[] storedHash, byte[] salt)
    {
        var computedHash = ComputePasswordHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }

    /// <summary>
    /// Verifies a password against a stored hash using constant-time comparison.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="storedHashBase64">Base64-encoded stored hash.</param>
    /// <param name="saltBase64">Base64-encoded salt.</param>
    /// <returns>True if the password matches.</returns>
    public static bool VerifyPasswordBase64(string password, string storedHashBase64, string saltBase64)
    {
        var storedHash = Convert.FromBase64String(storedHashBase64);
        var salt = Convert.FromBase64String(saltBase64);
        return VerifyPassword(password, storedHash, salt);
    }
}
