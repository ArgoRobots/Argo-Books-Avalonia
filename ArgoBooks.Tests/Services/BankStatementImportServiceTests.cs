using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class BankStatementImportServiceTests
{
    // Headers already use the target column names, so an empty mapping leaves them untouched.
    private static SheetAnalysis EmptyMapping(string sheetName) => new()
    {
        SourceSheetName = sheetName,
        DetectedType = SpreadsheetSheetType.BankStatement,
        ColumnMappings = []
    };

    private static async Task<string> WriteTempCsvAsync(string content)
    {
        var path = Path.GetTempFileName() + ".csv";
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task ParseCsvAsync_SignedAmountColumn_KeepsSignAndParses()
    {
        var path = await WriteTempCsvAsync(
            "Date,Description,Amount,Balance\n" +
            "2025-01-05,Coffee shop,-12.50,100.00\n" +
            "2025-01-06,Client deposit,250.00,350.00\n");
        try
        {
            var lines = await new BankStatementImportService().ParseCsvWithAnalysisAsync(path, EmptyMapping(Path.GetFileNameWithoutExtension(path)));

            Assert.Equal(2, lines.Count);
            Assert.Equal(-12.50m, lines[0].Amount);
            Assert.Equal("Coffee shop", lines[0].Description);
            Assert.Equal(250.00m, lines[1].Amount);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ParseCsvAsync_DebitCreditColumns_NormalizesToSignedAmount()
    {
        var path = await WriteTempCsvAsync(
            "Date,Description,Debit,Credit,Balance\n" +
            "2025-02-03,Supplies,142.10,,8500.00\n" +
            "2025-02-06,Deposit,,980.00,9480.00\n");
        try
        {
            var lines = await new BankStatementImportService().ParseCsvWithAnalysisAsync(path, EmptyMapping(Path.GetFileNameWithoutExtension(path)));

            Assert.Equal(2, lines.Count);
            Assert.Equal(-142.10m, lines[0].Amount); // debit => money out (negative)
            Assert.Equal(142.10m, lines[0].Debit);
            Assert.Equal(980.00m, lines[1].Amount);  // credit => money in (positive)
            Assert.Equal(980.00m, lines[1].Credit);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ParseCsvAsync_ParenthesesNegativeAndCurrencySymbol_Parsed()
    {
        var path = await WriteTempCsvAsync(
            "Date,Description,Amount\n" +
            "2025-03-01,Refund,\"($45.00)\"\n");
        try
        {
            var lines = await new BankStatementImportService().ParseCsvWithAnalysisAsync(path, EmptyMapping(Path.GetFileNameWithoutExtension(path)));

            Assert.Single(lines);
            Assert.Equal(-45.00m, lines[0].Amount);
        }
        finally { File.Delete(path); }
    }
}
