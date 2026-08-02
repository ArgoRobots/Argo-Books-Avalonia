using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class QrImageServiceTests
{
    [Fact]
    public void RenderPng_produces_png_bytes()
    {
        var bytes = new QrImageService().RenderPng("hello");
        Assert.True(bytes.Length > 0);
        // PNG signature
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
    }
}
