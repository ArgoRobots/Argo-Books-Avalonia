using System.Security.Cryptography;
using ArgoBooks.Core.Security;
using ArgoBooks.Core.Services;

// Offline support tool for recovering a company file whose password has been lost.
//
// Run this on a machine that holds the recovery private key, and nowhere else. The private
// key must never be committed, shipped, or copied onto a server.

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

try
{
    return args[0].ToLowerInvariant() switch
    {
        "keygen" => KeyGen(args),
        "inspect" => await Inspect(args),
        "unlock" => await Unlock(args),
        _ => Fail($"Unknown command '{args[0]}'.")
    };
}
catch (CryptographicException ex)
{
    return Fail($"Cryptographic failure: {ex.Message}\n" +
                "The private key probably does not match the one this file was wrapped under. " +
                "Check the key id shown by 'inspect'.");
}
catch (Exception ex)
{
    return Fail(ex.Message);
}

static void PrintUsage()
{
    Console.WriteLine("""
        Argo Books offline recovery tool

          keygen [private-key-path]
              Generate a new recovery key pair. Writes the private key to disk and prints
              the public key for pasting into RecoveryKeyProvider.

          inspect <file.argo>
              Show a file's format version and whether it has a recovery path.
              Reads metadata only. Does not decrypt anything.

          unlock <file.argo> <private-key.pem> [output.argo]
              Decrypt a file using the recovery key and write a copy with no password.
              The original file is never modified.
        """);
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static int KeyGen(string[] args)
{
    var outPath = args.Length > 1 ? args[1] : "argo-recovery-private.pem";

    // Never clobber an existing private key. Losing it means losing every recovery path
    // for every file already wrapped under it.
    if (File.Exists(outPath))
        return Fail($"Refusing to overwrite the existing key at {Path.GetFullPath(outPath)}.");

    using var rsa = RSA.Create(4096);
    File.WriteAllText(outPath, rsa.ExportPkcs8PrivateKeyPem());

    Console.WriteLine($"Private key written to {Path.GetFullPath(outPath)}");
    Console.WriteLine();
    Console.WriteLine("Move it to offline storage now, and do not keep a copy on this machine.");
    Console.WriteLine();
    Console.WriteLine("Paste the following into RecoveryKeyProvider.PublicKeyPem:");
    Console.WriteLine();
    Console.WriteLine("    private const string PublicKeyPem = \"\"\"");
    foreach (var line in rsa.ExportSubjectPublicKeyInfoPem().ReplaceLineEndings("\n").Split('\n'))
    {
        if (line.Length > 0)
            Console.WriteLine($"        {line}");
    }
    Console.WriteLine("        \"\"\";");
    Console.WriteLine();
    Console.WriteLine("Then bump RecoveryKeyProvider.CurrentKeyId if this replaces an earlier key.");
    return 0;
}

static async Task<int> Inspect(string[] args)
{
    if (args.Length < 2)
        return Fail("Usage: inspect <file.argo>");

    var footer = await new FooterService().ReadFooterAsync(args[1]);
    if (footer is null)
        return Fail("Not a valid .argo file, or the file is corrupt.");

    Console.WriteLine($"Company:        {footer.CompanyName}");
    Console.WriteLine($"Written by:     Argo Books {footer.Version}");
    Console.WriteLine($"Format version: {footer.FormatVersion}");
    Console.WriteLine($"Modified:       {footer.ModifiedAt:u}");
    Console.WriteLine($"Encrypted:      {(footer.IsEncrypted ? "yes" : "no")}");
    Console.WriteLine(footer.RecoveryBlob is null
        ? "Recovery:       NOT AVAILABLE"
        : $"Recovery:       available (key id: {footer.RecoveryKeyId})");

    if (footer.IsEncrypted && footer.RecoveryBlob is null)
    {
        Console.WriteLine();
        Console.WriteLine("This file was encrypted before recovery support existed, so its");
        Console.WriteLine("password is the only thing that can open it.");
    }

    return 0;
}

static async Task<int> Unlock(string[] args)
{
    if (args.Length < 3)
        return Fail("Usage: unlock <file.argo> <private-key.pem> [output.argo]");

    var inPath = args[1];
    var keyPath = args[2];

    if (!File.Exists(inPath))
        return Fail($"No such file: {inPath}");
    if (!File.Exists(keyPath))
        return Fail($"No such private key: {keyPath}");

    var outPath = args.Length > 3
        ? args[3]
        : Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(inPath))!,
            $"{Path.GetFileNameWithoutExtension(inPath)} (recovered).argo");

    if (File.Exists(outPath))
        return Fail($"Refusing to overwrite {Path.GetFullPath(outPath)}.");

    var footerService = new FooterService();
    var footer = await footerService.ReadFooterAsync(inPath);
    if (footer is null)
        return Fail("Not a valid .argo file, or the file is corrupt.");

    if (!footer.IsEncrypted)
        return Fail("This file has no password. It can already be opened as-is.");

    if (footer.FormatVersion < 2 || string.IsNullOrEmpty(footer.RecoveryBlob))
        return Fail("This file has no recovery path. It was encrypted before recovery " +
                    "support existed, so only its password can open it.");

    if (string.IsNullOrEmpty(footer.Iv))
        return Fail("This file is missing its encryption nonce and cannot be decrypted.");

    Console.WriteLine($"Company:  {footer.CompanyName}");
    Console.WriteLine($"Key id:   {footer.RecoveryKeyId}");
    Console.WriteLine("Unwrapping data key...");

    var dataKey = RecoveryKeyProvider.UnwrapDataKey(footer.RecoveryBlob, await File.ReadAllTextAsync(keyPath));
    byte[] plaintext;
    try
    {
        Console.WriteLine("Decrypting archive...");
        await using var content = await footerService.ReadContentAsync(inPath);
        plaintext = new EncryptionService()
            .DecryptWithKey(content.ToArray(), dataKey, Convert.FromBase64String(footer.Iv));
    }
    finally
    {
        CryptographicOperations.ZeroMemory(dataKey);
    }

    // Write the same archive back out with no password, so the customer can open it
    // immediately and set a new one themselves.
    footer.IsEncrypted = false;
    footer.Salt = null;
    footer.Iv = null;
    footer.PasswordHash = null;
    footer.WrappedKey = null;
    footer.KeyWrapNonce = null;
    footer.RecoveryBlob = null;
    footer.RecoveryKeyId = null;
    footer.BiometricEnabled = false;

    await using (var output = File.Create(outPath))
    {
        await output.WriteAsync(plaintext);
        await footerService.WriteFooterAsync(output, footer);
    }

    Console.WriteLine();
    Console.WriteLine($"Recovered file written to {Path.GetFullPath(outPath)}");
    Console.WriteLine("It has no password. Tell the customer to set a new one as soon as they open it.");
    return 0;
}
