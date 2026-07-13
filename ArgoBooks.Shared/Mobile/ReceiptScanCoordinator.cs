using System;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Pure orchestration around <see cref="IReceiptScannerService"/> for the mobile capture flow:
/// hands the ML-Kit-cropped image bytes to the injected scanner and returns its
/// <see cref="ReceiptScanResult"/> unchanged. No Android/UI dependency, so it's unit-tested with a
/// fake <see cref="IReceiptScannerService"/> rather than a live call to the AI proxy (see
/// ReceiptScanCoordinatorTests). ScanningViewModel is the real caller, constructed with a
/// GeminiReceiptScannerService wired to the phone's DeviceApiAuth (X-Device-Id, Option A of the
/// capture plan's auth decision).
/// </summary>
public class ReceiptScanCoordinator
{
    private readonly IReceiptScannerService _scanner;

    public ReceiptScanCoordinator(IReceiptScannerService scanner)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    /// <summary>
    /// Scans the given image bytes and returns the result. Always returns a
    /// <see cref="ReceiptScanResult"/> (check <c>IsSuccess</c>/<c>ErrorMessage</c>) rather than
    /// throwing, mirroring the scanner service's own contract; the only failure this adds on top
    /// is guarding against an empty image so a cancelled/failed capture never reaches the network.
    /// </summary>
    public Task<ReceiptScanResult> ScanAsync(byte[] imageBytes, string fileName = "scan.jpg", CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return Task.FromResult(ReceiptScanResult.Failed("No image to scan."));
        }

        return _scanner.ScanReceiptAsync(imageBytes, fileName, cancellationToken);
    }
}
