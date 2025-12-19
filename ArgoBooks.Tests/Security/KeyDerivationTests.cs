using ArgoBooks.Core.Security;
using Xunit;

namespace ArgoBooks.Tests.Security;

/// <summary>
/// Tests for the KeyDerivation class.
/// </summary>
public class KeyDerivationTests
{
    #region Constants Tests

    [Fact]
    public void Constants_HaveSecureValues()
    {
        // PBKDF2 iterations should be at least 100,000 for security
        Assert.True(KeyDerivation.Iterations >= 100_000);

        // Salt size should be at least 128 bits (16 bytes), 256 bits (32 bytes) is ideal
        Assert.True(KeyDerivation.SaltSize >= 16);

        // Key size should be 256 bits (32 bytes) for AES-256
        Assert.Equal(32, KeyDerivation.KeySize);

        // IV size should be 96 bits (12 bytes) for AES-GCM
        Assert.Equal(12, KeyDerivation.IvSize);

        // Tag size should be 128 bits (16 bytes) for AES-GCM
        Assert.Equal(16, KeyDerivation.TagSize);
    }

    #endregion

    #region Salt Generation Tests

    [Fact]
    public void GenerateSalt_ReturnsCorrectSize()
    {
        var salt = KeyDerivation.GenerateSalt();
        Assert.Equal(KeyDerivation.SaltSize, salt.Length);
    }

    [Fact]
    public void GenerateSalt_ReturnsUniqueValues()
    {
        var salt1 = KeyDerivation.GenerateSalt();
        var salt2 = KeyDerivation.GenerateSalt();

        Assert.False(salt1.SequenceEqual(salt2), "Generated salts should be unique");
    }

    [Fact]
    public void GenerateSaltBase64_ReturnsValidBase64()
    {
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        Assert.False(string.IsNullOrEmpty(saltBase64));

        // Should be valid Base64
        var decoded = Convert.FromBase64String(saltBase64);
        Assert.Equal(KeyDerivation.SaltSize, decoded.Length);
    }

    [Fact]
    public void GenerateSaltBase64_ReturnsUniqueValues()
    {
        var salt1 = KeyDerivation.GenerateSaltBase64();
        var salt2 = KeyDerivation.GenerateSaltBase64();

        Assert.NotEqual(salt1, salt2);
    }

    #endregion

    #region IV Generation Tests

    [Fact]
    public void GenerateIv_ReturnsCorrectSize()
    {
        var iv = KeyDerivation.GenerateIv();
        Assert.Equal(KeyDerivation.IvSize, iv.Length);
    }

    [Fact]
    public void GenerateIv_ReturnsUniqueValues()
    {
        var iv1 = KeyDerivation.GenerateIv();
        var iv2 = KeyDerivation.GenerateIv();

        Assert.False(iv1.SequenceEqual(iv2), "Generated IVs should be unique");
    }

    [Fact]
    public void GenerateIvBase64_ReturnsValidBase64()
    {
        var ivBase64 = KeyDerivation.GenerateIvBase64();

        Assert.False(string.IsNullOrEmpty(ivBase64));

        var decoded = Convert.FromBase64String(ivBase64);
        Assert.Equal(KeyDerivation.IvSize, decoded.Length);
    }

    #endregion

    #region Key Derivation Tests

    [Fact]
    public void DeriveKey_ReturnsCorrectSize()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var key = KeyDerivation.DeriveKey(password, salt);

        Assert.Equal(KeyDerivation.KeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_SameInputProducesSameOutput()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var key1 = KeyDerivation.DeriveKey(password, salt);
        var key2 = KeyDerivation.DeriveKey(password, salt);

        Assert.True(key1.SequenceEqual(key2), "Same password and salt should produce same key");
    }

    [Fact]
    public void DeriveKey_DifferentPasswordProducesDifferentKey()
    {
        var salt = KeyDerivation.GenerateSalt();

        var key1 = KeyDerivation.DeriveKey("Password1", salt);
        var key2 = KeyDerivation.DeriveKey("Password2", salt);

        Assert.False(key1.SequenceEqual(key2), "Different passwords should produce different keys");
    }

    [Fact]
    public void DeriveKey_DifferentSaltProducesDifferentKey()
    {
        var password = "TestPassword123";
        var salt1 = KeyDerivation.GenerateSalt();
        var salt2 = KeyDerivation.GenerateSalt();

        var key1 = KeyDerivation.DeriveKey(password, salt1);
        var key2 = KeyDerivation.DeriveKey(password, salt2);

        Assert.False(key1.SequenceEqual(key2), "Different salts should produce different keys");
    }

    [Fact]
    public void DeriveKey_WithBase64Salt_ReturnsCorrectSize()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        var key = KeyDerivation.DeriveKey(password, saltBase64);

        Assert.Equal(KeyDerivation.KeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_ThrowsOnNullPassword()
    {
        var salt = KeyDerivation.GenerateSalt();

        Assert.Throws<ArgumentException>(() => KeyDerivation.DeriveKey(null!, salt));
    }

    [Fact]
    public void DeriveKey_ThrowsOnEmptyPassword()
    {
        var salt = KeyDerivation.GenerateSalt();

        Assert.Throws<ArgumentException>(() => KeyDerivation.DeriveKey("", salt));
    }

    [Fact]
    public void DeriveKey_ThrowsOnNullSalt()
    {
        Assert.Throws<ArgumentNullException>(() => KeyDerivation.DeriveKey("password", (byte[])null!));
    }

    #endregion

    #region Password Hashing Tests

    [Fact]
    public void ComputePasswordHash_ReturnsCorrectSize()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        Assert.Equal(KeyDerivation.HashSize, hash.Length);
    }

    [Fact]
    public void ComputePasswordHash_SameInputProducesSameOutput()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var hash1 = KeyDerivation.ComputePasswordHash(password, salt);
        var hash2 = KeyDerivation.ComputePasswordHash(password, salt);

        Assert.True(hash1.SequenceEqual(hash2));
    }

    [Fact]
    public void ComputePasswordHashBase64_ReturnsValidBase64()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        Assert.False(string.IsNullOrEmpty(hashBase64));

        var decoded = Convert.FromBase64String(hashBase64);
        Assert.Equal(KeyDerivation.HashSize, decoded.Length);
    }

    #endregion

    #region Password Verification Tests

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        var result = KeyDerivation.VerifyPassword(password, hash, salt);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        var result = KeyDerivation.VerifyPassword("WrongPassword", hash, salt);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordBase64_ReturnsTrueForCorrectPassword()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        var result = KeyDerivation.VerifyPasswordBase64(password, hashBase64, saltBase64);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPasswordBase64_ReturnsFalseForIncorrectPassword()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        var result = KeyDerivation.VerifyPasswordBase64("WrongPassword", hashBase64, saltBase64);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_CaseSensitive()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        // Test with different case
        var result = KeyDerivation.VerifyPassword("testpassword123", hash, salt);

        Assert.False(result, "Password verification should be case-sensitive");
    }

    #endregion
}
