using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class PdfStatementExtractorTests
{
    [Fact]
    public void ParseRows_ValidJson_ReturnsSignedLines()
    {
        const string json = """
        { "success": true, "lines": [
          { "date": "2026-04-05", "description": "AMZN MKTP", "amount": -38.20 },
          { "date": "2026-04-06", "description": "STRIPE", "amount": 1200.00 }
        ]}
        """;

        var rows = PdfStatementExtractor.ParseRows(json);

        Assert.Equal(2, rows.Count);
        Assert.Equal(-38.20m, rows[0].Amount);
        Assert.Equal("AMZN MKTP", rows[0].Description);
        Assert.Equal(new DateTime(2026, 4, 6), rows[1].Date);
    }

    [Fact]
    public void ParseRows_Unsuccessful_ReturnsEmpty()
    {
        Assert.Empty(PdfStatementExtractor.ParseRows("""{ "success": false }"""));
    }

    [Fact]
    public void ParseRows_MalformedJson_ReturnsEmpty()
    {
        Assert.Empty(PdfStatementExtractor.ParseRows("not json <html>error</html>"));
        Assert.Empty(PdfStatementExtractor.ParseRows(""));
    }
}
