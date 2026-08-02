using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using NPOI.HSSF.UserModel;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class LegacyXlsConverterTests
{
    /// <summary>
    /// Builds a small legacy .xls with a "Customers" sheet: header row (ID/Name/Amount/JoinDate)
    /// and two data rows, exercising string, numeric and date-formatted cells. Returns the temp path.
    /// </summary>
    private static string CreateSampleXls(out DateTime expectedDate)
    {
        expectedDate = new DateTime(2024, 3, 15);

        var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Customers");

        var dateStyle = workbook.CreateCellStyle();
        var format = workbook.CreateDataFormat();
        dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

        // Header row
        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("ID");
        header.CreateCell(1).SetCellValue("Name");
        header.CreateCell(2).SetCellValue("Amount");
        header.CreateCell(3).SetCellValue("JoinDate");

        // Data row 1: numeric + date cells
        var row1 = sheet.CreateRow(1);
        row1.CreateCell(0).SetCellValue("C001");
        row1.CreateCell(1).SetCellValue("Acme Corp");
        row1.CreateCell(2).SetCellValue(1234.56);
        var dateCell = row1.CreateCell(3);
        dateCell.SetCellValue(expectedDate);
        dateCell.CellStyle = dateStyle;

        // Data row 2
        var row2 = sheet.CreateRow(2);
        row2.CreateCell(0).SetCellValue("C002");
        row2.CreateCell(1).SetCellValue("Globex");
        row2.CreateCell(2).SetCellValue(99.0);
        var dateCell2 = row2.CreateCell(3);
        dateCell2.SetCellValue(new DateTime(2024, 6, 1));
        dateCell2.CellStyle = dateStyle;

        var path = Path.Combine(Path.GetTempPath(), $"argo-test-{Guid.NewGuid():N}.xls");
        using (var fs = File.Create(path))
            workbook.Write(fs);

        return path;
    }

    [Fact]
    public void ConvertXlsToTempXlsx_PreservesSheetNameHeadersAndCellTypes()
    {
        var xlsPath = CreateSampleXls(out var expectedDate);
        string? xlsxPath = null;
        try
        {
            xlsxPath = LegacyXlsConverter.ConvertXlsToTempXlsx(xlsPath);

            Assert.True(File.Exists(xlsxPath));
            Assert.EndsWith(".xlsx", xlsxPath, StringComparison.OrdinalIgnoreCase);

            using var wb = new XLWorkbook(xlsxPath);
            var sheet = wb.Worksheet("Customers"); // sheet name preserved (throws if missing)

            // Header strings
            Assert.Equal("ID", sheet.Cell(1, 1).GetString());
            Assert.Equal("Name", sheet.Cell(1, 2).GetString());
            Assert.Equal("Amount", sheet.Cell(1, 3).GetString());
            Assert.Equal("JoinDate", sheet.Cell(1, 4).GetString());

            // Data row 1 string cells
            Assert.Equal("C001", sheet.Cell(2, 1).GetString());
            Assert.Equal("Acme Corp", sheet.Cell(2, 2).GetString());

            // Numeric cell stays numeric
            var amountCell = sheet.Cell(2, 3);
            Assert.Equal(XLDataType.Number, amountCell.DataType);
            Assert.Equal(1234.56, amountCell.GetDouble(), 5);

            // Date cell round-trips as a date
            var joinCell = sheet.Cell(2, 4);
            Assert.Equal(XLDataType.DateTime, joinCell.DataType);
            Assert.Equal(expectedDate, joinCell.GetDateTime());

            // Data row 2
            Assert.Equal("C002", sheet.Cell(3, 1).GetString());
            Assert.Equal("Globex", sheet.Cell(3, 2).GetString());
            Assert.Equal(99.0, sheet.Cell(3, 3).GetDouble(), 5);
            Assert.Equal(new DateTime(2024, 6, 1), sheet.Cell(3, 4).GetDateTime());
        }
        finally
        {
            TryDelete(xlsPath);
            TryDelete(xlsxPath);
        }
    }

    [Fact]
    public void ConvertXlsToTempXlsx_ToleratesRaggedRowsAndBlankCells()
    {
        // A sheet with a gap row and a short row should not throw and should preserve
        // the cells that are present.
        var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("Data");

        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("A");
        header.CreateCell(2).SetCellValue("C"); // gap at column B

        // Row 1 intentionally skipped (null row) -> ragged
        var row2 = sheet.CreateRow(2);
        row2.CreateCell(0).SetCellValue("only-first");

        var xlsPath = Path.Combine(Path.GetTempPath(), $"argo-test-{Guid.NewGuid():N}.xls");
        using (var fs = File.Create(xlsPath))
            workbook.Write(fs);

        string? xlsxPath = null;
        try
        {
            xlsxPath = LegacyXlsConverter.ConvertXlsToTempXlsx(xlsPath);
            using var wb = new XLWorkbook(xlsxPath);
            var s = wb.Worksheet("Data");

            Assert.Equal("A", s.Cell(1, 1).GetString());
            Assert.Equal("", s.Cell(1, 2).GetString()); // blank gap cell
            Assert.Equal("C", s.Cell(1, 3).GetString());
            Assert.Equal("only-first", s.Cell(3, 1).GetString());
        }
        finally
        {
            TryDelete(xlsPath);
            TryDelete(xlsxPath);
        }
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { File.Delete(path); } catch { /* best effort */ }
    }
}
