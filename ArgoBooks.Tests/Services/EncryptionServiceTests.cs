using System.Security.Cryptography;
using System.Text;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the EncryptionService class.
/// </summary>
public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService = new();

    #region Key Generation Tests

    [Fact]
    public void GenerateSalt_ReturnsValidBase64()
    {
        var salt = _encryptionService.GenerateSalt();

        Assert.False(string.IsNullOrEmpty(salt));

        // Should be valid Base64
        var decoded = Convert.FromBase64String(salt);
        Assert.True(decoded.Length > 0);
    }

    [Fact]
    public void GenerateSalt_ReturnsUniqueValues()
    {
        var salt1 = _encryptionService.GenerateSalt();
        var salt2 = _encryptionService.GenerateSalt();

        Assert.NotEqual(salt1, salt2);
    }

    [Fact]
    public void GenerateIv_ReturnsValidBase64()
    {
        var iv = _encryptionService.GenerateIv();

        Assert.False(string.IsNullOrEmpty(iv));

        // Should be valid Base64
        var decoded = Convert.FromBase64String(iv);
        Assert.True(decoded.Length > 0);
    }

    [Fact]
    public void GenerateIv_ReturnsUniqueValues()
    {
        var iv1 = _encryptionService.GenerateIv();
        var iv2 = _encryptionService.GenerateIv();

        Assert.NotEqual(iv1, iv2);
    }

    #endregion

    #region Password Hashing Tests

    [Fact]
    public void HashPassword_ReturnsDeterministicHash()
    {
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();

        var hash1 = _encryptionService.HashPassword(password, salt);
        var hash2 = _encryptionService.HashPassword(password, salt);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_DifferentSaltProducesDifferentHash()
    {
        var password = "TestPassword123";
        var salt1 = _encryptionService.GenerateSalt();
        var salt2 = _encryptionService.GenerateSalt();

        var hash1 = _encryptionService.HashPassword(password, salt1);
        var hash2 = _encryptionService.HashPassword(password, salt2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ValidatePassword_ReturnsTrueForCorrectPassword()
    {
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var hash = _encryptionService.HashPassword(password, salt);

        Assert.True(_encryptionService.ValidatePassword(password, hash, salt));
    }

    [Fact]
    public void ValidatePassword_ReturnsFalseForIncorrectPassword()
    {
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var hash = _encryptionService.HashPassword(password, salt);

        Assert.False(_encryptionService.ValidatePassword("WrongPassword", hash, salt));
    }

    #endregion

    #region Encryption/Decryption Tests (Byte Arrays)

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_RestoresOriginalData()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, World! This is a test message.");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(originalData, password, salt, iv);
        var decrypted = _encryptionService.Decrypt(encrypted, password, salt, iv);

        Assert.True(originalData.SequenceEqual(decrypted));
    }

    [Fact]
    public void Encrypt_ProducesDifferentOutputFromInput()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, World!");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(originalData, password, salt, iv);

        Assert.False(originalData.SequenceEqual(encrypted));
    }

    [Fact]
    public void Encrypt_SameDataDifferentIv_ProducesDifferentCiphertext()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, World!");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv1 = _encryptionService.GenerateIv();
        var iv2 = _encryptionService.GenerateIv();

        var encrypted1 = _encryptionService.Encrypt(originalData, password, salt, iv1);
        var encrypted2 = _encryptionService.Encrypt(originalData, password, salt, iv2);

        Assert.False(encrypted1.SequenceEqual(encrypted2));
    }

    [Fact]
    public void Decrypt_WithWrongPassword_ThrowsCryptographicException()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, World!");
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(originalData, "CorrectPassword1", salt, iv);

        Assert.Throws<CryptographicException>(() =>
            _encryptionService.Decrypt(encrypted, "WrongPassword1", salt, iv));
    }

    [Fact]
    public void Decrypt_WithWrongIv_ThrowsCryptographicException()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, World!");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv1 = _encryptionService.GenerateIv();
        var iv2 = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(originalData, password, salt, iv1);

        Assert.Throws<CryptographicException>(() =>
            _encryptionService.Decrypt(encrypted, password, salt, iv2));
    }

    [Fact]
    public void Encrypt_ThrowsOnNullData()
    {
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        Assert.Throws<ArgumentNullException>(() =>
            _encryptionService.Encrypt(null!, "password", salt, iv));
    }

    [Fact]
    public void Encrypt_ThrowsOnNullPassword()
    {
        var data = Encoding.UTF8.GetBytes("test");
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        Assert.Throws<ArgumentException>(() =>
            _encryptionService.Encrypt(data, null!, salt, iv));
    }

    [Fact]
    public void Encrypt_ThrowsOnEmptyPassword()
    {
        var data = Encoding.UTF8.GetBytes("test");
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        Assert.Throws<ArgumentException>(() =>
            _encryptionService.Encrypt(data, "", salt, iv));
    }

    [Fact]
    public void Decrypt_ThrowsOnInvalidEncryptedData()
    {
        var invalidData = new byte[5]; // Too small to contain auth tag
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        Assert.Throws<CryptographicException>(() =>
            _encryptionService.Decrypt(invalidData, "password", salt, iv));
    }

    [Fact]
    public void Encrypt_WorksWithEmptyData()
    {
        var emptyData = Array.Empty<byte>();
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(emptyData, password, salt, iv);
        var decrypted = _encryptionService.Decrypt(encrypted, password, salt, iv);

        Assert.Empty(decrypted);
    }

    [Fact]
    public void Encrypt_WorksWithLargeData()
    {
        var largeData = new byte[1024 * 1024]; // 1 MB
        new Random(42).NextBytes(largeData);
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(largeData, password, salt, iv);
        var decrypted = _encryptionService.Decrypt(encrypted, password, salt, iv);

        Assert.True(largeData.SequenceEqual(decrypted));
    }

    #endregion

    #region Async Encryption/Decryption Tests

    [Fact]
    public async Task EncryptAsync_DecryptAsync_RoundTrip_RestoresOriginalData()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, Async World!");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = await _encryptionService.EncryptAsync(inputStream, password, salt, iv);
        using var decryptedStream = await _encryptionService.DecryptAsync(encryptedStream, password, salt, iv);

        var decryptedData = decryptedStream.ToArray();
        Assert.True(originalData.SequenceEqual(decryptedData));
    }

    [Fact]
    public async Task EncryptAsync_ThrowsOnNullStream()
    {
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _encryptionService.EncryptAsync(null!, "password", salt, iv));
    }

    [Fact]
    public async Task EncryptAsync_ReturnsStreamAtPositionZero()
    {
        var originalData = Encoding.UTF8.GetBytes("Test data");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = await _encryptionService.EncryptAsync(inputStream, password, salt, iv);

        Assert.Equal(0, encryptedStream.Position);
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public void IsPasswordValid_ReturnsTrueForValidPassword()
    {
        Assert.True(_encryptionService.IsPasswordValid("Password123"));
    }

    [Fact]
    public void IsPasswordValid_ReturnsFalseForInvalidPassword()
    {
        Assert.False(_encryptionService.IsPasswordValid("weak"));
    }

    [Fact]
    public void GetPasswordValidationError_ReturnsNullForValidPassword()
    {
        Assert.Null(_encryptionService.GetPasswordValidationError("Password123"));
    }

    [Fact]
    public void GetPasswordValidationError_ReturnsErrorForInvalidPassword()
    {
        var error = _encryptionService.GetPasswordValidationError("weak");
        Assert.NotNull(error);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_CreateHashValidateEncryptDecrypt()
    {
        // Simulate a full file encryption workflow
        var password = "SecurePassword123!";
        var fileContent = Encoding.UTF8.GetBytes("This is confidential file content.");

        // Step 1: Generate salt and hash password for storage
        var salt = _encryptionService.GenerateSalt();
        var passwordHash = _encryptionService.HashPassword(password, salt);

        // Step 2: Validate password (simulating login)
        Assert.True(_encryptionService.ValidatePassword(password, passwordHash, salt));

        // Step 3: Generate IV and encrypt file
        var iv = _encryptionService.GenerateIv();
        var encryptedContent = _encryptionService.Encrypt(fileContent, password, salt, iv);

        // Step 4: Later, decrypt file with same password
        var decryptedContent = _encryptionService.Decrypt(encryptedContent, password, salt, iv);

        Assert.True(fileContent.SequenceEqual(decryptedContent));
    }

    [Fact]
    public void TamperedData_FailsAuthentication()
    {
        var originalData = Encoding.UTF8.GetBytes("Secret message");
        var password = "TestPassword123";
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        var encrypted = _encryptionService.Encrypt(originalData, password, salt, iv);

        // Tamper with the ciphertext
        encrypted[0] ^= 0xFF;

        // Should fail authentication
        Assert.Throws<CryptographicException>(() =>
            _encryptionService.Decrypt(encrypted, password, salt, iv));
    }

    #endregion
}
