using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Validating a workbook's invoice references before importing it.
///
/// Exporting a company and importing the same workbook back is the closest thing to a
/// round trip this app has, and it flagged every payment and every line item as pointing at an
/// invoice that was not there. The Invoices sheet exports both an "ID" (INV-2026-00001) and an
/// "Invoice #" (#INV-2026-00001), and the child sheets reference the ID, so collecting the
/// display number matched nothing. The line item issues were not auto-fixable either, so there
/// was no way past the dialog.
/// </summary>
public class InvoiceReferenceValidationTests : IDisposable
{
    private readonly List<string> _files = [];

    private sealed record Sheet(string Name, string[] Headers, string[][] Rows);

    private string Workbook(params Sheet[] sheets)
    {
        string path = Path.Combine(Path.GetTempPath(), $"argo-refs-{Guid.NewGuid():N}.xlsx");
        _files.Add(path);

        using var workbook = new XLWorkbook();

        foreach (Sheet sheet in sheets)
        {
            IXLWorksheet worksheet = workbook.AddWorksheet(sheet.Name);

            for (int c = 0; c < sheet.Headers.Length; c++)
            {
                worksheet.Cell(1, c + 1).Value = sheet.Headers[c];
            }

            for (int r = 0; r < sheet.Rows.Length; r++)
            {
                for (int c = 0; c < sheet.Rows[r].Length; c++)
                {
                    worksheet.Cell(r + 2, c + 1).Value = sheet.Rows[r][c];
                }
            }
        }

        workbook.SaveAs(path);
        return path;
    }

    private static async Task<ImportValidationResult> Validate(string path) =>
        await new SpreadsheetImportService().ValidateImportAsync(path, new CompanyData());

    /// <summary>The shape this app exports: an ID column and a display number beside it.</summary>
    private static Sheet Invoices() => new(
        "Invoices",
        ["ID", "Invoice #", "Customer", "Total"],
        [["INV-2026-00001", "#INV-2026-00001", "Acme Ltd", "500"]]);

    private static Sheet Payments() => new(
        "Payments",
        ["ID", "Invoice ID", "Amount", "Date"],
        [["PAY-0001", "INV-2026-00001", "500", "2026-08-14"]]);

    private static Sheet LineItems() => new(
        "Invoice Line Items",
        ["ID", "Invoice ID", "Description", "Quantity", "Unit Price"],
        [["ILI-0001", "INV-2026-00001", "Consulting", "1", "500"]]);

    private static List<ValidationIssue> InvoiceIssues(ImportValidationResult result) =>
        [.. result.Issues.Where(i => i.ReferenceType.Contains("Invoice", StringComparison.OrdinalIgnoreCase)
                                     || i.Description.Contains("Invoice", StringComparison.OrdinalIgnoreCase))];

    [Fact]
    public async Task AnExportedWorkbookImportedBack_HasNoMissingInvoices()
    {
        ImportValidationResult result = await Validate(Workbook(Invoices(), Payments(), LineItems()));

        Assert.Empty(InvoiceIssues(result));
        Assert.False(result.MissingReferences.ContainsKey("Invoices"));
    }

    /// <summary>
    /// The schema documents that a sheet carrying only "Invoice #" still imports, so the
    /// reference collection has to mirror that fallback or every child row of such a sheet is
    /// reported as an orphan.
    /// </summary>
    [Fact]
    public async Task AnInvoicesSheetWithOnlyTheDisplayNumber_StillSatisfiesItsChildren()
    {
        var invoices = new Sheet(
            "Invoices",
            ["Invoice #", "Customer", "Total"],
            [["INV-2026-00001", "Acme Ltd", "500"]]);

        ImportValidationResult result = await Validate(Workbook(invoices, Payments(), LineItems()));

        Assert.Empty(InvoiceIssues(result));
    }

    /// <summary>
    /// The guard has to still catch a genuine orphan, or it has been turned off rather than
    /// fixed.
    /// </summary>
    [Fact]
    public async Task APaymentAgainstAnInvoiceThatIsNotThere_IsStillReported()
    {
        var payments = new Sheet(
            "Payments",
            ["ID", "Invoice ID", "Amount", "Date"],
            [["PAY-0001", "INV-2026-09999", "500", "2026-08-14"]]);

        ImportValidationResult result = await Validate(Workbook(Invoices(), payments));

        Assert.NotEmpty(InvoiceIssues(result));
    }

    public void Dispose()
    {
        foreach (string file in _files)
        {
            try { File.Delete(file); } catch { /* best effort */ }
        }

        GC.SuppressFinalize(this);
    }
}
