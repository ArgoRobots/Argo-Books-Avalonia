using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The app's own spreadsheet exports must survive a round trip through its own reader.
/// </summary>
public class SpreadsheetExportRoundTripTests
{
    [Fact]
    public async Task CsvExport_IsReadableByCsvReader_WithRealHeaders()
    {
        var data = new CompanyData();
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "Jane Doe", Email = "jane@x.com" });

        var path = Path.Combine(Path.GetTempPath(), $"exp_{Guid.NewGuid():N}.csv");
        try
        {
            await new SpreadsheetExportService().ExportToCsvAsync(path, data, ["Customers"], null, null);

            // The importer reads CSVs via CsvReader; the exported file must present the real column
            // headers on the first row, not a "# Customers" section-comment line.
            var rows = CsvReader.ReadAllRows(path, out var headers);

            Assert.DoesNotContain(headers, h => h.StartsWith("#", StringComparison.Ordinal));
            Assert.Contains("Name", headers);
            Assert.Contains("ID", headers);

            // And the exported customer must be a real data row (not swallowed into the header).
            var nameIdx = headers.IndexOf("Name");
            Assert.Contains(rows, r => nameIdx < r.Count && r[nameIdx] == "Jane Doe");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
