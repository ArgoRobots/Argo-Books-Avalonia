using System;
using System.IO;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// <see cref="SpreadsheetImportService.CollectTransactionDates"/> reads each financial sheet's date
/// column (using the importer's own parser and the correct per-type mapped column name) so the
/// import rate gate can pre-fetch the exact-date rate for every transaction.
/// </summary>
public class CollectTransactionDatesTests
{
    [Fact]
    public void CollectTransactionDates_ReadsDateColumnsPerSheetType()
    {
        var path = Path.Combine(Path.GetTempPath(), $"argo-dates-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var wb = new XLWorkbook())
            {
                var rev = wb.AddWorksheet("Revenue");
                rev.Cell(1, 1).Value = "ID"; rev.Cell(1, 2).Value = "Date"; rev.Cell(1, 3).Value = "Total";
                rev.Cell(2, 1).Value = "R1"; rev.Cell(2, 2).Value = "2026-01-05"; rev.Cell(2, 3).Value = 10;

                var inv = wb.AddWorksheet("Invoices");
                inv.Cell(1, 1).Value = "ID"; inv.Cell(1, 2).Value = "Issue Date"; inv.Cell(1, 3).Value = "Total";
                inv.Cell(2, 1).Value = "I1"; inv.Cell(2, 2).Value = "2026-02-09"; inv.Cell(2, 3).Value = 20;
                wb.SaveAs(path);
            }

            var analysis = new SpreadsheetAnalysisResult
            {
                Sheets =
                [
                    new SheetAnalysis { SourceSheetName = "Revenue", DetectedType = SpreadsheetSheetType.Revenue, Tier = ProcessingTier.Tier1_Mapping, IsIncluded = true },
                    new SheetAnalysis { SourceSheetName = "Invoices", DetectedType = SpreadsheetSheetType.Invoices, Tier = ProcessingTier.Tier1_Mapping, IsIncluded = true },
                ]
            };

            var dates = new SpreadsheetImportService().CollectTransactionDates(path, analysis);

            Assert.Contains(new DateTime(2026, 1, 5), dates);
            Assert.Contains(new DateTime(2026, 2, 9), dates);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
