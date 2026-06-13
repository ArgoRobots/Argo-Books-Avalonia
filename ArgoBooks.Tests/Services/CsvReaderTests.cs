using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class CsvReaderTests
{
    [Fact]
    public void Read_QuotedFieldWithEmbeddedNewline_StaysOneField()
    {
        var csv = "Id,Notes\r\n1,\"line one\nline two\"\r\n2,plain\n";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, csv);

        var rows = CsvReader.ReadAllRows(path, out var headers);

        Assert.Equal(new[] { "Id", "Notes" }, headers);
        Assert.Equal(2, rows.Count);
        Assert.Equal("line one\nline two", rows[0][1]);
        Assert.Equal("plain", rows[1][1]);
    }

    [Fact]
    public void Read_AutoDetectsSemicolonDelimiter()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "a;b;c\n1;2;3\n");
        var rows = CsvReader.ReadAllRows(path, out var headers);
        Assert.Equal(new[] { "a", "b", "c" }, headers);
        Assert.Equal(new[] { "1", "2", "3" }, rows[0]);
    }
}
