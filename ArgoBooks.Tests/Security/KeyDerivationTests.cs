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
    public void Iterations_HasExpectedValue()
    {
        Assert.Equal(100_000, KeyDerivation.Iterations);
    }

    [Fact]
    public void SaltSize_HasExpectedValue()
    {
        Assert.Equal(32, KeyDerivation.SaltSize);
    }

    [Fact]
    public void KeySize_HasExpectedValue()
    {
        Assert.Equal(32, KeyDerivation.KeySize);
    }

    [Fact]
    public void HashSize_HasExpectedValue()
    {
        Assert.Equal(32, KeyDerivation.HashSize);
    }

    [Fact]
    public void IvSize_HasExpectedValue()
    {
        Assert.Equal(12, KeyDerivation.IvSize);
    }

    [Fact]
    public void TagSize_HasExpectedValue()
    {
        Assert.Equal(16, KeyDerivation.TagSize);
    }

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
    public void GenerateSalt_ReturnsCorrectLength()
    {
        var salt = KeyDerivation.GenerateSalt();
        Assert.Equal(KeyDerivation.SaltSize, salt.Length);
    }

    [Fact]
    public void GenerateSalt_ReturnsNonEmptyArray()
    {
        var salt = KeyDerivation.GenerateSalt();
        Assert.NotNull(salt);
        Assert.NotEmpty(salt);
    }

    [Fact]
    public void GenerateSalt_ReturnsUniqueValues()
    {
        var salt1 = KeyDerivation.GenerateSalt();
        var salt2 = KeyDerivation.GenerateSalt();

        Assert.False(salt1.SequenceEqual(salt2), "Generated salts should be unique");
    }

    [Fact]
    public void GenerateSalt_MultipleCalls_AllUnique()
    {
        var salts = new List<byte[]>();
        for (int i = 0; i < 10; i++)
        {
            salts.Add(KeyDerivation.GenerateSalt());
        }

        // Verify all salts are distinct from each other
        for (int i = 0; i < salts.Count; i++)
        {
            for (int j = i + 1; j < salts.Count; j++)
            {
                Assert.False(salts[i].SequenceEqual(salts[j]),
                    $"Salt at index {i} should differ from salt at index {j}");
            }
        }
    }

    [Fact]
    public void GenerateSaltBase64_ReturnsValidBase64()
    {
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        Assert.False(string.IsNullOrEmpty(saltBase64));

        // Should be valid Base64 that decodes to correct length
        var decoded = Convert.FromBase64String(saltBase64);
        Assert.Equal(KeyDerivation.SaltSize, decoded.Length);
    }

    [Fact]
    public void GenerateSaltBase64_ReturnsNonEmptyString()
    {
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        Assert.False(string.IsNullOrWhiteSpace(saltBase64));
    }

    [Fact]
    public void GenerateSaltBase64_ReturnsUniqueValues()
    {
        var salt1 = KeyDerivation.GenerateSaltBase64();
        var salt2 = KeyDerivation.GenerateSaltBase64();

        Assert.NotEqual(salt1, salt2);
    }

    [Fact]
    public void GenerateSaltBase64_RoundTripsCorrectly()
    {
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var decoded = Convert.FromBase64String(saltBase64);
        var reEncoded = Convert.ToBase64String(decoded);

        Assert.Equal(saltBase64, reEncoded);
    }

    #endregion

    #region IV Generation Tests

    [Fact]
    public void GenerateIv_ReturnsCorrectLength()
    {
        var iv = KeyDerivation.GenerateIv();
        Assert.Equal(KeyDerivation.IvSize, iv.Length);
    }

    [Fact]
    public void GenerateIv_ReturnsNonEmptyArray()
    {
        var iv = KeyDerivation.GenerateIv();
        Assert.NotNull(iv);
        Assert.NotEmpty(iv);
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

    [Fact]
    public void GenerateIvBase64_ReturnsUniqueValues()
    {
        var iv1 = KeyDerivation.GenerateIvBase64();
        var iv2 = KeyDerivation.GenerateIvBase64();

        Assert.NotEqual(iv1, iv2);
    }

    [Fact]
    public void GenerateIvBase64_RoundTripsCorrectly()
    {
        var ivBase64 = KeyDerivation.GenerateIvBase64();
        var decoded = Convert.FromBase64String(ivBase64);
        var reEncoded = Convert.ToBase64String(decoded);

        Assert.Equal(ivBase64, reEncoded);
    }

    #endregion

    #region DeriveKey (byte[] salt) Tests

    [Fact]
    public void DeriveKey_ReturnsCorrectLength()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var key = KeyDerivation.DeriveKey(password, salt);

        Assert.Equal(KeyDerivation.KeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_SameInputs_ProducesConsistentOutput()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var key1 = KeyDerivation.DeriveKey(password, salt);
        var key2 = KeyDerivation.DeriveKey(password, salt);

        Assert.True(key1.SequenceEqual(key2), "Same password and salt should produce same key");
    }

    [Fact]
    public void DeriveKey_DifferentPasswords_ProducesDifferentOutput()
    {
        var salt = KeyDerivation.GenerateSalt();

        var key1 = KeyDerivation.DeriveKey("Password1", salt);
        var key2 = KeyDerivation.DeriveKey("Password2", salt);

        Assert.False(key1.SequenceEqual(key2), "Different passwords should produce different keys");
    }

    [Fact]
    public void DeriveKey_DifferentSalts_ProducesDifferentOutput()
    {
        var password = "TestPassword123";
        var salt1 = KeyDerivation.GenerateSalt();
        var salt2 = KeyDerivation.GenerateSalt();

        var key1 = KeyDerivation.DeriveKey(password, salt1);
        var key2 = KeyDerivation.DeriveKey(password, salt2);

        Assert.False(key1.SequenceEqual(key2), "Different salts should produce different keys");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("a longer password with spaces")]
    [InlineData("P@$$w0rd!#%^&*()")]
    [InlineData("unicode-password-\u00e9\u00e8\u00ea")]
    public void DeriveKey_VariousPasswords_ReturnsCorrectLength(string password)
    {
        var salt = KeyDerivation.GenerateSalt();

        var key = KeyDerivation.DeriveKey(password, salt);

        Assert.Equal(KeyDerivation.KeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_NullPassword_ThrowsArgumentNullException()
    {
        var salt = KeyDerivation.GenerateSalt();

        Assert.Throws<ArgumentNullException>(() => KeyDerivation.DeriveKey(null!, salt));
    }

    [Fact]
    public void DeriveKey_EmptyPassword_ThrowsArgumentException()
    {
        var salt = KeyDerivation.GenerateSalt();

        Assert.Throws<ArgumentException>(() => KeyDerivation.DeriveKey("", salt));
    }

    [Fact]
    public void DeriveKey_NullSalt_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => KeyDerivation.DeriveKey("password", (byte[])null!));
    }

    [Fact]
    public void DeriveKey_ReturnsNonZeroBytes()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var key = KeyDerivation.DeriveKey(password, salt);

        // The key should not be all zeros (extremely unlikely for a valid derivation)
        Assert.False(key.All(b => b == 0), "Derived key should not be all zeros");
    }

    #endregion

    #region DeriveKey (string saltBase64) Tests

    [Fact]
    public void DeriveKey_Base64Salt_ReturnsCorrectLength()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        var key = KeyDerivation.DeriveKey(password, saltBase64);

        Assert.Equal(KeyDerivation.KeySize, key.Length);
    }

    [Fact]
    public void DeriveKey_Base64Salt_MatchesByteArrayOverload()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var saltBase64 = Convert.ToBase64String(salt);

        var keyFromBytes = KeyDerivation.DeriveKey(password, salt);
        var keyFromBase64 = KeyDerivation.DeriveKey(password, saltBase64);

        Assert.True(keyFromBytes.SequenceEqual(keyFromBase64),
            "Both DeriveKey overloads should produce identical output for the same salt");
    }

    [Fact]
    public void DeriveKey_Base64Salt_SameInputs_ProducesConsistentOutput()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        var key1 = KeyDerivation.DeriveKey(password, saltBase64);
        var key2 = KeyDerivation.DeriveKey(password, saltBase64);

        Assert.True(key1.SequenceEqual(key2),
            "Same password and base64 salt should produce same key");
    }

    #endregion

    #region ComputePasswordHash Tests

    [Fact]
    public void ComputePasswordHash_ReturnsCorrectLength()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        Assert.Equal(KeyDerivation.HashSize, hash.Length);
    }

    [Fact]
    public void ComputePasswordHash_SameInputs_ProducesConsistentOutput()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();

        var hash1 = KeyDerivation.ComputePasswordHash(password, salt);
        var hash2 = KeyDerivation.ComputePasswordHash(password, salt);

        Assert.True(hash1.SequenceEqual(hash2),
            "Same password and salt should produce same hash");
    }

    [Fact]
    public void ComputePasswordHash_DifferentPasswords_ProducesDifferentHash()
    {
        var salt = KeyDerivation.GenerateSalt();

        var hash1 = KeyDerivation.ComputePasswordHash("Password1", salt);
        var hash2 = KeyDerivation.ComputePasswordHash("Password2", salt);

        Assert.False(hash1.SequenceEqual(hash2),
            "Different passwords should produce different hashes");
    }

    [Fact]
    public void ComputePasswordHash_DifferentSalts_ProducesDifferentHash()
    {
        var password = "TestPassword123";
        var salt1 = KeyDerivation.GenerateSalt();
        var salt2 = KeyDerivation.GenerateSalt();

        var hash1 = KeyDerivation.ComputePasswordHash(password, salt1);
        var hash2 = KeyDerivation.ComputePasswordHash(password, salt2);

        Assert.False(hash1.SequenceEqual(hash2),
            "Different salts should produce different hashes");
    }

    [Fact]
    public void ComputePasswordHash_NullPassword_ThrowsArgumentNullException()
    {
        var salt = KeyDerivation.GenerateSalt();

        Assert.Throws<ArgumentNullException>(() => KeyDerivation.ComputePasswordHash(null!, salt));
    }

    [Fact]
    public void ComputePasswordHash_EmptyPassword_ThrowsArgumentException()
    {
        var salt = KeyDerivation.GenerateSalt();

        Assert.Throws<ArgumentException>(() => KeyDerivation.ComputePasswordHash("", salt));
    }

    [Fact]
    public void ComputePasswordHash_NullSalt_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            KeyDerivation.ComputePasswordHash("password", null!));
    }

    #endregion

    #region ComputePasswordHashBase64 Tests

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

    [Fact]
    public void ComputePasswordHashBase64_MatchesByteArrayOverload()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var saltBase64 = Convert.ToBase64String(salt);

        var hashBytes = KeyDerivation.ComputePasswordHash(password, salt);
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        var expectedBase64 = Convert.ToBase64String(hashBytes);
        Assert.Equal(expectedBase64, hashBase64);
    }

    [Fact]
    public void ComputePasswordHashBase64_SameInputs_ProducesConsistentOutput()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();

        var hash1 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);
        var hash2 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        Assert.Equal(hash1, hash2);
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        var result = KeyDerivation.VerifyPassword(password, hash, salt);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        var result = KeyDerivation.VerifyPassword("WrongPassword", hash, salt);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_CaseSensitive_ReturnsFalse()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        // Test with different case
        var result = KeyDerivation.VerifyPassword("testpassword123", hash, salt);

        Assert.False(result, "Password verification should be case-sensitive");
    }

    [Fact]
    public void VerifyPassword_TamperedHash_ReturnsFalse()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        // Tamper with the hash by flipping a single byte
        var tamperedHash = (byte[])hash.Clone();
        tamperedHash[0] = (byte)(tamperedHash[0] ^ 0xFF);

        var result = KeyDerivation.VerifyPassword(password, tamperedHash, salt);

        Assert.False(result, "Verification should fail with a tampered hash");
    }

    [Fact]
    public void VerifyPassword_WrongSalt_ReturnsFalse()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var wrongSalt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        var result = KeyDerivation.VerifyPassword(password, hash, wrongSalt);

        Assert.False(result, "Verification should fail with wrong salt");
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("P@$$w0rd!#%^&*()")]
    [InlineData("a very long password with many words in it for testing purposes")]
    [InlineData("12345678a")]
    public void VerifyPassword_VariousPasswords_VerifiesCorrectly(string password)
    {
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        Assert.True(KeyDerivation.VerifyPassword(password, hash, salt));
        Assert.False(KeyDerivation.VerifyPassword(password + "x", hash, salt));
    }

    #endregion

    #region VerifyPasswordBase64 Tests

    [Fact]
    public void VerifyPasswordBase64_CorrectPassword_ReturnsTrue()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        var result = KeyDerivation.VerifyPasswordBase64(password, hashBase64, saltBase64);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPasswordBase64_WrongPassword_ReturnsFalse()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        var result = KeyDerivation.VerifyPasswordBase64("WrongPassword", hashBase64, saltBase64);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPasswordBase64_MatchesByteArrayOverload()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var hash = KeyDerivation.ComputePasswordHash(password, salt);

        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        var byteResult = KeyDerivation.VerifyPassword(password, hash, salt);
        var base64Result = KeyDerivation.VerifyPasswordBase64(password, hashBase64, saltBase64);

        Assert.Equal(byteResult, base64Result);
    }

    [Fact]
    public void VerifyPasswordBase64_CaseSensitive_ReturnsFalse()
    {
        var password = "TestPassword123";
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        var result = KeyDerivation.VerifyPasswordBase64("testpassword123", hashBase64, saltBase64);

        Assert.False(result, "Base64 password verification should be case-sensitive");
    }

    #endregion

    #region End-to-End / Integration Tests

    [Fact]
    public void EndToEnd_HashAndVerify_RoundTripsSuccessfully()
    {
        // Simulate a full password storage and verification workflow
        var password = "MySecurePassword99!";

        // Step 1: Generate salt and hash for storage
        var saltBase64 = KeyDerivation.GenerateSaltBase64();
        var hashBase64 = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);

        // Step 2: Later, verify the password against the stored hash and salt
        Assert.True(KeyDerivation.VerifyPasswordBase64(password, hashBase64, saltBase64));
        Assert.False(KeyDerivation.VerifyPasswordBase64("DifferentPassword1", hashBase64, saltBase64));
    }

    [Fact]
    public void EndToEnd_DeriveKeyAndVerify_ConsistentAcrossOverloads()
    {
        var password = "TestPassword123";
        var salt = KeyDerivation.GenerateSalt();
        var saltBase64 = Convert.ToBase64String(salt);

        // Both overloads should produce the same derived key
        var keyFromBytes = KeyDerivation.DeriveKey(password, salt);
        var keyFromBase64 = KeyDerivation.DeriveKey(password, saltBase64);

        Assert.True(keyFromBytes.SequenceEqual(keyFromBase64));

        // Both overloads should produce the same hash
        var hashFromBytes = KeyDerivation.ComputePasswordHash(password, salt);
        var hashFromBase64String = KeyDerivation.ComputePasswordHashBase64(password, saltBase64);
        var hashFromBase64Decoded = Convert.FromBase64String(hashFromBase64String);

        Assert.True(hashFromBytes.SequenceEqual(hashFromBase64Decoded));
    }

    #endregion
}
