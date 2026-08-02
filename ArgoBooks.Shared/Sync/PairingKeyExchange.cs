using System.Security.Cryptography;

namespace ArgoBooks.Shared.Sync;

/// <summary>
/// A throwaway RSA keypair used once during short-code pairing. The phone generates one of these,
/// sends the public key to the desktop, and decrypts the company sync key the desktop encrypts back.
/// </summary>
public sealed class PairingKeyPair : IDisposable
{
    private readonly RSA _rsa;

    internal PairingKeyPair(RSA rsa)
    {
        _rsa = rsa;
        PublicKeyBase64 = Convert.ToBase64String(_rsa.ExportSubjectPublicKeyInfo());
    }

    /// <summary>The public key, DER-encoded as SubjectPublicKeyInfo then base64.</summary>
    public string PublicKeyBase64 { get; }

    /// <summary>Decrypts a base64 RSA-OAEP-SHA256 ciphertext (produced by <see cref="PairingKeyExchange.EncryptSyncKey"/>) with the private key.</summary>
    public byte[] DecryptSyncKey(string ciphertextBase64)
        => _rsa.Decrypt(Convert.FromBase64String(ciphertextBase64), RSAEncryptionPadding.OaepSHA256);

    public void Dispose() => _rsa.Dispose();
}

/// <summary>
/// RSA-OAEP key exchange used to hand the 32-byte company sync key from the desktop to a phone
/// during short-code pairing, without either device having seen the sync key over the wire in
/// plaintext. Lives in Shared so both the desktop and the Android app can use it.
/// </summary>
public static class PairingKeyExchange
{
    /// <summary>Generates a fresh 2048-bit RSA keypair for one pairing attempt.</summary>
    public static PairingKeyPair GenerateKeyPair()
    {
        var rsa = RSA.Create(2048);
        return new PairingKeyPair(rsa);
    }

    /// <summary>Encrypts <paramref name="key"/> (the sync key) to the given base64 SubjectPublicKeyInfo public key, using RSA-OAEP-SHA256.</summary>
    public static string EncryptSyncKey(string publicKeyBase64, byte[] key)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
        return Convert.ToBase64String(rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256));
    }
}
