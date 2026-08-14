using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The supplier and customer name columns on the expenses and revenue sheets.
///
/// They exist so a sheet of scanned receipts can be read on its own. Without them the export
/// says SUP-014 and the reader has to hold the Suppliers sheet next to it to find out who that
/// was, which is most of the reason to export in the first place.
///
/// Presentation only. The id column is still what identifies the party and is still what the
/// importer reads, so the cases worth pinning are that the name never replaces the id and that
/// an id pointing at nothing produces a blank rather than a crash or a repeat of the id.
/// </summary>
public class SpreadsheetExportNameColumnTests
{
    private static CompanyData Company()
    {
        var data = new CompanyData();

        data.Suppliers.Add(new Supplier { Id = "SUP-014", Name = "Acme Hardware" });
        data.Customers.Add(new Customer { Id = "CUS-003", Name = "Jane Doe" });

        return data;
    }

    private static async Task<(List<string> Headers, List<List<string>> Rows)> ExportAsync(
        CompanyData data, string sheet)
    {
        string path = Path.Combine(Path.GetTempPath(), $"names_{Guid.NewGuid():N}.csv");

        try
        {
            await new SpreadsheetExportService().ExportToCsvAsync(path, data, [sheet], null, null);
            List<List<string>> rows = CsvReader.ReadAllRows(path, out List<string> headers);
            return (headers, rows);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Cell(List<string> headers, List<string> row, string header)
    {
        int index = headers.IndexOf(header);
        Assert.True(index >= 0, $"no '{header}' column, headers were: {string.Join(", ", headers)}");
        return index < row.Count ? row[index] : string.Empty;
    }

    #region Expenses

    [Fact]
    public async Task AnExpense_CarriesTheSupplierNameBesideTheId()
    {
        CompanyData data = Company();
        data.Expenses.Add(new Expense
        {
            Id = "PUR-001",
            Date = new DateTime(2026, 5, 4),
            SupplierId = "SUP-014",
            Description = "Screws",
            Total = 24.50m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Expenses");
        List<string> row = Assert.Single(rows);

        Assert.Equal("SUP-014", Cell(headers, row, "Supplier ID"));
        Assert.Equal("Acme Hardware", Cell(headers, row, "Supplier Name"));
    }

    [Fact]
    public async Task AnExpenseWithNoSupplier_LeavesBothColumnsBlank()
    {
        // A scanned receipt that nobody has matched to a supplier yet. Very common, and the
        // most likely row in the sheet this column was added for.
        CompanyData data = Company();
        data.Expenses.Add(new Expense
        {
            Id = "PUR-002",
            Date = new DateTime(2026, 5, 4),
            SupplierId = null,
            Description = "Parking",
            Total = 6m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Expenses");
        List<string> row = Assert.Single(rows);

        Assert.Equal(string.Empty, Cell(headers, row, "Supplier ID"));
        Assert.Equal(string.Empty, Cell(headers, row, "Supplier Name"));
    }

    [Fact]
    public async Task AnExpensePointingAtASupplierThatIsGone_StillExports()
    {
        // Deleting a supplier does not rewrite the expenses that referenced it. The name is
        // blank and the id is kept, so the row still says what it knows.
        CompanyData data = Company();
        data.Expenses.Add(new Expense
        {
            Id = "PUR-003",
            Date = new DateTime(2026, 5, 4),
            SupplierId = "SUP-999",
            Description = "Timber",
            Total = 80m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Expenses");
        List<string> row = Assert.Single(rows);

        Assert.Equal("SUP-999", Cell(headers, row, "Supplier ID"));
        Assert.Equal(string.Empty, Cell(headers, row, "Supplier Name"));
    }

    #endregion

    #region Revenue

    [Fact]
    public async Task ARevenueRow_CarriesTheCustomerNameBesideTheId()
    {
        CompanyData data = Company();
        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = new DateTime(2026, 5, 4),
            CustomerId = "CUS-003",
            Description = "Consulting",
            Total = 500m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Revenue");
        List<string> row = Assert.Single(rows);

        Assert.Equal("CUS-003", Cell(headers, row, "Customer ID"));
        Assert.Equal("Jane Doe", Cell(headers, row, "Customer Name"));
    }

    [Fact]
    public async Task ARevenueRowWithNoCustomer_LeavesBothColumnsBlank()
    {
        CompanyData data = Company();
        data.Revenues.Add(new Revenue
        {
            Id = "REV-002",
            Date = new DateTime(2026, 5, 4),
            CustomerId = null,
            Description = "Counter sale",
            Total = 12m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Revenue");
        List<string> row = Assert.Single(rows);

        Assert.Equal(string.Empty, Cell(headers, row, "Customer Name"));
    }

    #endregion

    #region Invoices and payments

    [Fact]
    public async Task AnInvoice_CarriesTheCustomerNameBesideTheId()
    {
        CompanyData data = Company();
        data.Invoices.Add(new Invoice
        {
            Id = "INV-2026-00001",
            InvoiceNumber = "#INV-2026-00001",
            CustomerId = "CUS-003",
            IssueDate = new DateTime(2026, 5, 4),
            Total = 500m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Invoices");
        List<string> row = Assert.Single(rows);

        Assert.Equal("CUS-003", Cell(headers, row, "Customer ID"));
        Assert.Equal("Jane Doe", Cell(headers, row, "Customer Name"));
    }

    [Fact]
    public async Task APayment_CarriesTheCustomerNameBesideTheId()
    {
        CompanyData data = Company();
        data.Payments.Add(new Payment
        {
            Id = "PAY-001",
            InvoiceId = "INV-2026-00001",
            CustomerId = "CUS-003",
            Date = new DateTime(2026, 5, 11),
            Amount = 500m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Payments");
        List<string> row = Assert.Single(rows);

        Assert.Equal("CUS-003", Cell(headers, row, "Customer ID"));
        Assert.Equal("Jane Doe", Cell(headers, row, "Customer Name"));
    }

    [Fact]
    public async Task APaymentForACustomerThatIsGone_StillExports()
    {
        CompanyData data = Company();
        data.Payments.Add(new Payment
        {
            Id = "PAY-002",
            InvoiceId = "INV-2026-00002",
            CustomerId = "CUS-999",
            Date = new DateTime(2026, 5, 11),
            Amount = 20m,
        });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Payments");
        List<string> row = Assert.Single(rows);

        Assert.Equal("CUS-999", Cell(headers, row, "Customer ID"));
        Assert.Equal(string.Empty, Cell(headers, row, "Customer Name"));
    }

    #endregion

    #region What the columns must not disturb

    [Fact]
    public async Task TheIdColumns_AreStillThereAndStillFirst()
    {
        // The importer identifies the party by id. Replacing the id with the name, which is the
        // tempting simplification, would silently turn every export into an unimportable one.
        CompanyData data = Company();
        data.Expenses.Add(new Expense { Id = "PUR-001", Date = new DateTime(2026, 5, 4), SupplierId = "SUP-014", Total = 1m });

        (List<string> headers, _) = await ExportAsync(data, "Expenses");

        Assert.True(headers.IndexOf("Supplier ID") < headers.IndexOf("Supplier Name"));
    }

    [Fact]
    public async Task TwoSuppliersSharingAnId_DoNotBringTheWholeExportDown()
    {
        // Ids come out of imported files as well as out of this app, so a repeat is possible.
        // A convenience column must never be the reason nothing exports at all.
        CompanyData data = Company();
        data.Suppliers.Add(new Supplier { Id = "SUP-014", Name = "Acme Hardware Ltd" });
        data.Expenses.Add(new Expense { Id = "PUR-001", Date = new DateTime(2026, 5, 4), SupplierId = "SUP-014", Total = 1m });

        (List<string> headers, List<List<string>> rows) = await ExportAsync(data, "Expenses");

        Assert.Equal("Acme Hardware Ltd", Cell(headers, Assert.Single(rows), "Supplier Name"));
    }

    #endregion
}
