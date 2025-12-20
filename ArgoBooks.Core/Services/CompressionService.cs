using System.Formats.Tar;
using System.IO.Compression;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for TAR archive creation/extraction and GZip compression.
/// </summary>
public class CompressionService
{
    /// <summary>
    /// Creates a TAR archive from a directory.
    /// </summary>
    /// <param name="sourceDirectory">Directory to archive.</param>
    /// <param name="includeBaseDirectory">Whether to include the base directory name in the archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Memory stream containing the TAR archive.</returns>
    public async Task<MemoryStream> CreateTarArchiveAsync(
        string sourceDirectory,
        bool includeBaseDirectory = true,
        CancellationToken cancellationToken = default)
    {
        var tarStream = new MemoryStream();

        // Use explicit disposal to ensure position is set after TarWriter finalizes
        var tarWriter = new TarWriter(tarStream, TarEntryFormat.Pax, leaveOpen: true);
        try
        {
            var basePath = includeBaseDirectory
                ? Path.GetDirectoryName(sourceDirectory) ?? sourceDirectory
                : sourceDirectory;

            await AddDirectoryToTarAsync(tarWriter, sourceDirectory, basePath, cancellationToken);
        }
        finally
        {
            await tarWriter.DisposeAsync();
        }

        tarStream.Position = 0;
        return tarStream;
    }

    private async Task AddDirectoryToTarAsync(
        TarWriter tarWriter,
        string directory,
        string basePath,
        CancellationToken cancellationToken)
    {
        // Add all files
        foreach (var filePath in Directory.GetFiles(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entryName = Path.GetRelativePath(basePath, filePath).Replace('\\', '/');
            var entry = new PaxTarEntry(TarEntryType.RegularFile, entryName)
            {
                DataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
            };

            await tarWriter.WriteEntryAsync(entry, cancellationToken);
            await entry.DataStream.DisposeAsync();
        }

        // Recursively add subdirectories
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await AddDirectoryToTarAsync(tarWriter, subDirectory, basePath, cancellationToken);
        }
    }

    /// <summary>
    /// Extracts a TAR archive to a directory.
    /// </summary>
    /// <param name="tarStream">Stream containing the TAR archive.</param>
    /// <param name="destinationDirectory">Directory to extract to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExtractTarArchiveAsync(
        Stream tarStream,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        // Ensure destination exists
        Directory.CreateDirectory(destinationDirectory);

        await using var tarReader = new TarReader(tarStream, leaveOpen: true);

        TarEntry? entry;
        while ((entry = await tarReader.GetNextEntryAsync(true, cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Sanitize entry name to prevent path traversal
            var entryName = SanitizeEntryName(entry.Name);
            if (string.IsNullOrEmpty(entryName))
                continue;

            var destinationPath = Path.Combine(destinationDirectory, entryName);

            // Ensure the destination is within the target directory
            var fullDestination = Path.GetFullPath(destinationPath);
            var fullTarget = Path.GetFullPath(destinationDirectory);
            if (!fullDestination.StartsWith(fullTarget, StringComparison.OrdinalIgnoreCase))
                continue; // Skip entries that would escape the target directory

            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(destinationPath);
                    break;

                case TarEntryType.RegularFile:
                    // Ensure parent directory exists
                    var parentDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(parentDir))
                        Directory.CreateDirectory(parentDir);

                    // Extract file
                    await entry.ExtractToFileAsync(destinationPath, overwrite: true, cancellationToken);
                    break;
            }
        }
    }

    /// <summary>
    /// Compresses data using GZip.
    /// </summary>
    /// <param name="inputStream">Stream to compress.</param>
    /// <param name="compressionLevel">Compression level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Memory stream containing compressed data.</returns>
    public async Task<MemoryStream> CompressGZipAsync(
        Stream inputStream,
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        CancellationToken cancellationToken = default)
    {
        var compressedStream = new MemoryStream();

        await using (var gzipStream = new GZipStream(compressedStream, compressionLevel, leaveOpen: true))
        {
            await inputStream.CopyToAsync(gzipStream, cancellationToken);
        }

        compressedStream.Position = 0;
        return compressedStream;
    }

    /// <summary>
    /// Decompresses GZip data.
    /// </summary>
    /// <param name="compressedStream">Stream containing compressed data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Memory stream containing decompressed data.</returns>
    public async Task<MemoryStream> DecompressGZipAsync(
        Stream compressedStream,
        CancellationToken cancellationToken = default)
    {
        var decompressedStream = new MemoryStream();

        await using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true))
        {
            await gzipStream.CopyToAsync(decompressedStream, cancellationToken);
        }

        decompressedStream.Position = 0;
        return decompressedStream;
    }

    /// <summary>
    /// Sanitizes a TAR entry name to prevent path traversal attacks.
    /// </summary>
    private static string SanitizeEntryName(string entryName)
    {
        // Remove leading slashes and dots
        entryName = entryName.TrimStart('/', '\\', '.');

        // Replace backslashes with forward slashes
        entryName = entryName.Replace('\\', '/');

        // Remove any ".." components
        var parts = entryName.Split('/');
        var sanitizedParts = parts.Where(p => p != ".." && p != ".").ToArray();

        return string.Join("/", sanitizedParts);
    }
}
