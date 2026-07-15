using Avalonia.Media.Imaging;
using QRCoder;

namespace ArgoBooks.Services;

public class QrImageService
{
    public byte[] RenderPng(string payload)
    {
        var qrCodeData = new QRCodeGenerator().CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }

    public Bitmap RenderBitmap(string payload)
    {
        return new Bitmap(new MemoryStream(RenderPng(payload)));
    }
}
