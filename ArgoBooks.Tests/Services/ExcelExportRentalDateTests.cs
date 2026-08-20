using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The rental-records exporter writes an un-returned rental's Return Date as
/// <c>ReturnDate ?? DateTime.MinValue</c>. The CSV/PDF path blanks that sentinel, but the Excel
/// writer used to write it as a real date (rendered ~1899-12-30), which then re-imported as a
/// bogus return date, marking a still-checked-out rental as returned.
/// </summary>
public class ExcelExportRentalDateTests
{
    [Fact]
    public async Task ExcelExport_ActiveRental_LeavesReturnDateBlank()
    {
        var data = new CompanyData();
        data.Rentals.Add(new RentalRecord
        {
            Id = "RNT-001",
            RentalItemId = "RNT-ITM-001",
            CustomerId = "CUS-001",
            Quantity = 1,
            RateType = RateType.Daily,
            RateAmount = 10m,
            StartDate = new DateTime(2026, 1, 1),
            DueDate = new DateTime(2026, 1, 8),
            ReturnDate = null, // still checked out
            Status = RentalStatus.Active
        });

        var path = Path.Combine(Path.GetTempPath(), $"exp_{Guid.NewGuid():N}.xlsx");
        try
        {
            await new SpreadsheetExportService().ExportToExcelAsync(path, data, ["Rental Records"], null, null);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            // Locate the "Return Date" column from the header row.
            var returnCol = ws.Row(1).CellsUsed().FirstOrDefault(c => c.GetString().Trim() == "Return Date");
            Assert.NotNull(returnCol);

            // The single data row's Return Date cell must be empty, not a bogus 0001/1899 date.
            var cell = ws.Cell(2, returnCol.Address.ColumnNumber);
            Assert.True(cell.IsEmpty(), $"Return Date cell should be blank but was '{cell.GetString()}'");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
