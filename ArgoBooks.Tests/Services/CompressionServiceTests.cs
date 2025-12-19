using System.IO.Compression;
using System.Text;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the CompressionService class.
/// </summary>
public class CompressionServiceTests : IDisposable
{
    private readonly CompressionService _compressionService = new();
    private readonly string _testDirectory;

    public CompressionServiceTests()
    {
        // Create a temp directory for TAR tests
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CompressionTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region GZip Compression Tests

    [Fact]
    public async Task CompressGZipAsync_DecompressGZipAsync_RoundTrip_RestoresOriginalData()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello, World! This is test data for compression.");

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream);
        using var decompressedStream = await _compressionService.DecompressGZipAsync(compressedStream);

        var decompressedData = decompressedStream.ToArray();
        Assert.True(originalData.SequenceEqual(decompressedData));
    }

    [Fact]
    public async Task CompressGZipAsync_ReducesDataSize_ForCompressibleData()
    {
        // Create highly compressible data (repeated pattern)
        var repeatedData = new string('A', 10000);
        var originalData = Encoding.UTF8.GetBytes(repeatedData);

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream);

        Assert.True(compressedStream.Length < originalData.Length,
            "Compressed data should be smaller than original for compressible data");
    }

    [Fact]
    public async Task CompressGZipAsync_ReturnsStreamAtPositionZero()
    {
        var originalData = Encoding.UTF8.GetBytes("Test data");

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream);

        Assert.Equal(0, compressedStream.Position);
    }

    [Fact]
    public async Task DecompressGZipAsync_ReturnsStreamAtPositionZero()
    {
        var originalData = Encoding.UTF8.GetBytes("Test data");

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream);
        using var decompressedStream = await _compressionService.DecompressGZipAsync(compressedStream);

        Assert.Equal(0, decompressedStream.Position);
    }

    [Fact]
    public async Task CompressGZipAsync_WorksWithEmptyData()
    {
        var emptyData = Array.Empty<byte>();

        using var inputStream = new MemoryStream(emptyData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream);
        using var decompressedStream = await _compressionService.DecompressGZipAsync(compressedStream);

        Assert.Empty(decompressedStream.ToArray());
    }

    [Fact]
    public async Task CompressGZipAsync_WorksWithLargeData()
    {
        // 1 MB of random data
        var largeData = new byte[1024 * 1024];
        new Random(42).NextBytes(largeData);

        using var inputStream = new MemoryStream(largeData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream);
        using var decompressedStream = await _compressionService.DecompressGZipAsync(compressedStream);

        Assert.True(largeData.SequenceEqual(decompressedStream.ToArray()));
    }

    [Theory]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.Optimal)]
    [InlineData(CompressionLevel.SmallestSize)]
    public async Task CompressGZipAsync_WorksWithDifferentCompressionLevels(CompressionLevel level)
    {
        var originalData = Encoding.UTF8.GetBytes("Test data for different compression levels.");

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = await _compressionService.CompressGZipAsync(inputStream, level);
        using var decompressedStream = await _compressionService.DecompressGZipAsync(compressedStream);

        Assert.True(originalData.SequenceEqual(decompressedStream.ToArray()));
    }

    [Fact]
    public async Task CompressGZipAsync_SmallestSize_ProducesSmallerOutput()
    {
        // Create compressible data
        var repeatedData = new string('B', 5000);
        var originalData = Encoding.UTF8.GetBytes(repeatedData);

        using var inputFastest = new MemoryStream(originalData);
        using var compressedFastest = await _compressionService.CompressGZipAsync(inputFastest, CompressionLevel.Fastest);

        using var inputSmallest = new MemoryStream(originalData);
        using var compressedSmallest = await _compressionService.CompressGZipAsync(inputSmallest, CompressionLevel.SmallestSize);

        Assert.True(compressedSmallest.Length <= compressedFastest.Length,
            "SmallestSize compression should produce equal or smaller output than Fastest");
    }

    [Fact]
    public async Task CompressGZipAsync_SupportsCancellation()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var originalData = Encoding.UTF8.GetBytes("Test data");
        using var inputStream = new MemoryStream(originalData);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _compressionService.CompressGZipAsync(inputStream, CompressionLevel.Optimal, cts.Token));
    }

    #endregion

    #region TAR Archive Tests

    [Fact]
    public async Task CreateTarArchiveAsync_ExtractTarArchiveAsync_RoundTrip_RestoresFiles()
    {
        // Create test files
        var sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);

        var file1Path = Path.Combine(sourceDir, "file1.txt");
        var file2Path = Path.Combine(sourceDir, "file2.txt");
        await File.WriteAllTextAsync(file1Path, "Content of file 1");
        await File.WriteAllTextAsync(file2Path, "Content of file 2");

        // Create subdirectory with file
        var subDir = Path.Combine(sourceDir, "subdir");
        Directory.CreateDirectory(subDir);
        var file3Path = Path.Combine(subDir, "file3.txt");
        await File.WriteAllTextAsync(file3Path, "Content of file 3 in subdirectory");

        // Create TAR archive
        using var tarStream = await _compressionService.CreateTarArchiveAsync(sourceDir, includeBaseDirectory: true);

        // Extract to different location
        var extractDir = Path.Combine(_testDirectory, "extract");
        await _compressionService.ExtractTarArchiveAsync(tarStream, extractDir);

        // Verify files were extracted
        var extractedFile1 = Path.Combine(extractDir, "source", "file1.txt");
        var extractedFile2 = Path.Combine(extractDir, "source", "file2.txt");
        var extractedFile3 = Path.Combine(extractDir, "source", "subdir", "file3.txt");

        Assert.True(File.Exists(extractedFile1));
        Assert.True(File.Exists(extractedFile2));
        Assert.True(File.Exists(extractedFile3));

        Assert.Equal("Content of file 1", await File.ReadAllTextAsync(extractedFile1));
        Assert.Equal("Content of file 2", await File.ReadAllTextAsync(extractedFile2));
        Assert.Equal("Content of file 3 in subdirectory", await File.ReadAllTextAsync(extractedFile3));
    }

    [Fact]
    public async Task CreateTarArchiveAsync_WithoutBaseDirectory_DoesNotIncludeBasePath()
    {
        // Create test file
        var sourceDir = Path.Combine(_testDirectory, "source2");
        Directory.CreateDirectory(sourceDir);

        var filePath = Path.Combine(sourceDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "Test content");

        // Create TAR without base directory
        using var tarStream = await _compressionService.CreateTarArchiveAsync(sourceDir, includeBaseDirectory: false);

        // Extract
        var extractDir = Path.Combine(_testDirectory, "extract2");
        await _compressionService.ExtractTarArchiveAsync(tarStream, extractDir);

        // File should be directly in extract directory, not in source2 subdirectory
        var extractedFile = Path.Combine(extractDir, "test.txt");
        Assert.True(File.Exists(extractedFile));
    }

    [Fact]
    public async Task CreateTarArchiveAsync_ReturnsStreamAtPositionZero()
    {
        var sourceDir = Path.Combine(_testDirectory, "source3");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "Test");

        using var tarStream = await _compressionService.CreateTarArchiveAsync(sourceDir);

        Assert.Equal(0, tarStream.Position);
    }

    [Fact]
    public async Task ExtractTarArchiveAsync_CreatesDestinationDirectory()
    {
        // Create source
        var sourceDir = Path.Combine(_testDirectory, "source4");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "Test");

        using var tarStream = await _compressionService.CreateTarArchiveAsync(sourceDir, includeBaseDirectory: false);

        // Extract to non-existent directory
        var extractDir = Path.Combine(_testDirectory, "nonexistent", "nested", "extract");
        await _compressionService.ExtractTarArchiveAsync(tarStream, extractDir);

        Assert.True(Directory.Exists(extractDir));
    }

    [Fact]
    public async Task CreateTarArchiveAsync_SupportsCancellation()
    {
        var sourceDir = Path.Combine(_testDirectory, "source5");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "Test");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _compressionService.CreateTarArchiveAsync(sourceDir, cancellationToken: cts.Token));
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ExtractTarArchiveAsync_PreventsPathTraversal()
    {
        // Create a source directory with a normal file
        var sourceDir = Path.Combine(_testDirectory, "source6");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "safe.txt"), "Safe content");

        using var tarStream = await _compressionService.CreateTarArchiveAsync(sourceDir, includeBaseDirectory: false);

        // Extract to a target directory
        var extractDir = Path.Combine(_testDirectory, "extract6");
        await _compressionService.ExtractTarArchiveAsync(tarStream, extractDir);

        // Verify the file was extracted safely
        Assert.True(File.Exists(Path.Combine(extractDir, "safe.txt")));

        // Verify no files were created outside the target directory
        var parentDir = Path.GetDirectoryName(extractDir)!;
        var filesInParent = Directory.GetFiles(parentDir);
        Assert.DoesNotContain(filesInParent, f => Path.GetFileName(f) == "safe.txt");
    }

    #endregion

    #region Combined TAR + GZip Tests

    [Fact]
    public async Task CreateTarGzArchive_ExtractTarGzArchive_RoundTrip()
    {
        // Create test files
        var sourceDir = Path.Combine(_testDirectory, "source7");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "data.txt"), "Important data content");

        // Create TAR archive
        using var tarStream = await _compressionService.CreateTarArchiveAsync(sourceDir, includeBaseDirectory: false);

        // Compress with GZip
        using var gzipStream = await _compressionService.CompressGZipAsync(tarStream);

        // Decompress
        using var decompressedTar = await _compressionService.DecompressGZipAsync(gzipStream);

        // Extract TAR
        var extractDir = Path.Combine(_testDirectory, "extract7");
        await _compressionService.ExtractTarArchiveAsync(decompressedTar, extractDir);

        // Verify
        var extractedFile = Path.Combine(extractDir, "data.txt");
        Assert.True(File.Exists(extractedFile));
        Assert.Equal("Important data content", await File.ReadAllTextAsync(extractedFile));
    }

    #endregion
}
