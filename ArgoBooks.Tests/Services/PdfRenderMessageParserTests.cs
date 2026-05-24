using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class PdfRenderMessageParserTests
{
    [Fact]
    public void TryParseDone_parses_count()
    {
        Assert.True(PdfThumbnailService.PdfRenderMessageParser.TryParseDone("render-done:3", out var c));
        Assert.Equal(3, c);
    }

    [Fact]
    public void TryParseDone_rejects_page_message()
    {
        Assert.False(PdfThumbnailService.PdfRenderMessageParser.TryParseDone(
            "render-page:0:data:image/jpeg;base64,QUJD", out _));
    }

    [Fact]
    public void TryParsePage_parses_index_and_bytes()
    {
        // "QUJD" is base64 for "ABC"
        Assert.True(PdfThumbnailService.PdfRenderMessageParser.TryParsePage(
            "render-page:2:data:image/jpeg;base64,QUJD", out var idx, out var bytes));
        Assert.Equal(2, idx);
        Assert.Equal(new byte[] { 0x41, 0x42, 0x43 }, bytes);
    }

    [Fact]
    public void TryParsePage_rejects_empty_payload()
    {
        Assert.False(PdfThumbnailService.PdfRenderMessageParser.TryParsePage(
            "render-page:0:data:image/jpeg;base64,", out _, out _));
    }

    [Fact]
    public void ToOrderedArray_orders_by_index_and_drops_gaps()
    {
        var map = new Dictionary<int, byte[]> { [2] = [0x02], [0] = [0x00] };
        var ordered = PdfThumbnailService.PdfRenderMessageParser.ToOrderedArray(map);
        Assert.Equal(2, ordered.Length);
        Assert.Equal(0x00, ordered[0][0]);
        Assert.Equal(0x02, ordered[1][0]);
    }
}
