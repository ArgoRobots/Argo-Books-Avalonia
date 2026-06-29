namespace ArgoBooks.Core.Services;

/// <summary>
/// Reads files with <see cref="FileShare.ReadWrite"/> so a user-selected file (receipt, logo,
/// bank statement, spreadsheet, etc.) can still be read while it is open in another program
/// (Excel, LibreOffice, a PDF viewer, an image editor) instead of failing with a sharing violation.
/// Use this for any file the user picks; app-managed files (settings, the company file) don't need it.
/// </summary>
public static class SharedFileReader
{
    public static byte[] ReadAllBytes(string path)
    {
        using var stream = OpenRead(path);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return bytes;
    }

    public static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
}
