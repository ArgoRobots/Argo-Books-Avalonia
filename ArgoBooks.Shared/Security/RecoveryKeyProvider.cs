using System.Security.Cryptography;

namespace ArgoBooks.Core.Security;

/// <summary>
/// Holds the Argo Books recovery public key and wraps a file's data encryption key under it.
///
/// From file format version 2, an encrypted company file stores its data encryption key twice:
/// once under the user's password and once under the public key below. Support can therefore
/// recover a file whose password has been lost, using the matching private key, which is held
/// offline and never ships with the app or touches a server.
///
/// Only the public half is embedded here. Someone who extracts it from the binary gains
/// nothing: it can encrypt a key but cannot decrypt one.
///
/// If <see cref="PublicKeyPem"/> is left empty the recovery wrap is simply omitted. Files stay
/// perfectly valid and open normally with their password, they just have no recovery path.
/// That is the deliberate default so an unconfigured build never fails to save.
///
/// To configure: run the ArgoBooks.Recovery tool's "keygen" command, keep the private key
/// offline, and paste the printed public key into <see cref="PublicKeyPem"/>.
/// </summary>
public static class RecoveryKeyProvider
{
    /// <summary>
    /// Identifier for the currently embedded recovery key.
    ///
    /// Stored alongside each wrapped key so that if the key pair is ever rotated, support can
    /// tell which private key opens a given file. Bump this whenever
    /// <see cref="PublicKeyPem"/> changes, and never reuse a previous identifier.
    /// </summary>
    public const string CurrentKeyId = "argo-recovery-1";

    /// <summary>
    /// PEM-encoded RSA public key used to wrap data encryption keys for support recovery.
    /// Empty means recovery is not configured for this build.
    /// </summary>
    private const string PublicKeyPem = """
                                        -----BEGIN PUBLIC KEY-----
                                        MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEApzhEN2v2nEx6a75CoxDu
                                        5rUdAjj05knRyjdn7+TJ7HneGqKxRAdTqufnjUsys7Q4QEJGccjjgq4DIYDMRWZB
                                        hZ6j/wwBMBE2bliXK4RP/MyF/ApCrp5WIOlu9CAI2D9ReHwTRfL4vesand57vo70
                                        INgTMviFDsaSQUTBzXjKPVb35VdIxFfKqaWc9JgZnZ4pugxBF7E0STtLUHkrvAwN
                                        tTWjsVh9lur0sTqrYqHbAjIUmfue/5CTELR5uwSMxbaEpHU116C5iYMah1KXWgae
                                        wg2vhmPrFo1RZDq5MI8fS3HeK/++kEWV+I37y+il5pL2KWjISo1lTo8oS8PUjjmw
                                        qzEDBa+xzDLg6bC3L6OPyTob2SXlSf1EJVhNYkwupS1CvwUDnXf6A9hj/lUQcHvs
                                        My8337mmRhMTHAcVlS+E8RDw83OvJyGZKib3dc/wbzy5g+pAdf3NCdgSJ+kJkGBl
                                        aJwIruWUibeUEMa4l1ZVaSR4qFtv8iarOn6Vil4gO8aUD3xJbce2+SDikUEEvQhy
                                        YDa9dq92APwCvPqhqdqQPz/cKdabggFmESBrGb6pMsSZgZflqEqBhdPItAUj9ghH
                                        t9C2gr4d7zl+GvsNFmUHnyeq5PrQi+dPThTPMRJDMw1PTETZ4mKK5hqumZ5+4XK9
                                        T2vFvTtXGnAKN1bAIgztUuECAwEAAQ==
                                        -----END PUBLIC KEY-----
                                        """;

    /// <summary>
    /// The public key compiled into this build, or an empty string when none is configured.
    /// Safe to expose: it already ships inside the binary and can only encrypt.
    /// </summary>
    public static string EmbeddedPublicKeyPem => PublicKeyPem;

    /// <summary>
    /// Whether this build has a recovery key configured.
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(PublicKeyPem);

    /// <summary>
    /// Wraps a data encryption key under a recovery public key.
    /// </summary>
    /// <param name="dataKey">The DEK to protect.</param>
    /// <param name="publicKeyPem">
    /// The public key to wrap under. Defaults to the key embedded in this build.
    /// </param>
    /// <returns>
    /// Base64-encoded wrapped key, or null when no recovery key is configured or the key
    /// cannot be parsed. A null result is not an error: the caller simply writes a file
    /// without a recovery path.
    /// </returns>
    public static string? TryWrapDataKey(byte[] dataKey, string? publicKeyPem = null)
    {
        ArgumentNullException.ThrowIfNull(dataKey);

        var pem = publicKeyPem ?? PublicKeyPem;
        if (string.IsNullOrWhiteSpace(pem))
            return null;

        try
        {
            return WrapDataKey(dataKey, pem);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            // A malformed key must never block the user from saving their work.
            return null;
        }
    }

    /// <summary>
    /// Wraps a data encryption key under an explicitly supplied public key.
    ///
    /// Exists so tests can exercise the exact wrap and unwrap pairing without depending on
    /// a configured build. That matters more than it looks: a padding mismatch between
    /// wrapping and unwrapping would be invisible in normal use and would only surface the
    /// first time a customer actually needed their file recovered.
    /// </summary>
    /// <param name="dataKey">The DEK to protect.</param>
    /// <param name="publicKeyPem">PEM-encoded RSA public key.</param>
    /// <returns>Base64-encoded wrapped key.</returns>
    public static string WrapDataKey(byte[] dataKey, string publicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(dataKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return Convert.ToBase64String(rsa.Encrypt(dataKey, RSAEncryptionPadding.OaepSHA256));
    }

    /// <summary>
    /// Unwraps a data encryption key using the recovery private key.
    ///
    /// Only ever called by the offline support recovery tool, never by the shipped app,
    /// which has no access to the private key.
    /// </summary>
    /// <param name="wrappedKeyBase64">Base64 wrapped key read from a file footer.</param>
    /// <param name="privateKeyPem">PEM-encoded RSA private key.</param>
    /// <returns>The original data encryption key.</returns>
    public static byte[] UnwrapDataKey(string wrappedKeyBase64, string privateKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wrappedKeyBase64);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var wrapped = Convert.FromBase64String(wrappedKeyBase64);
        return rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);
    }
}
