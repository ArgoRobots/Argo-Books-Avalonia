using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Importing an employees sheet.
///
/// The destructive case is the one that matters. Every field was written unconditionally from
/// whatever the row happened to carry, so importing a two-column sheet to annotate staff turned
/// hourly employees into salaried ones at nil pay, blanked their social insurance numbers and
/// wiped their addresses. Nothing warned, the import reported success, and the damage only
/// surfaced on the next pay run.
/// </summary>
public class EmployeeImportTests : IDisposable
{
    private readonly List<string> _files = [];

    /// <summary>Writes a one-sheet workbook and returns its path.</summary>
    private string Workbook(string sheet, string[] headers, params string[][] rows)
    {
        string path = Path.Combine(Path.GetTempPath(), $"argo-import-{Guid.NewGuid():N}.xlsx");
        _files.Add(path);

        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.AddWorksheet(sheet);

        for (int c = 0; c < headers.Length; c++)
        {
            worksheet.Cell(1, c + 1).Value = headers[c];
        }

        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                worksheet.Cell(r + 2, c + 1).Value = rows[r][c];
            }
        }

        workbook.SaveAs(path);
        return path;
    }

    private static async Task Import(string path, CompanyData data) =>
        await new SpreadsheetImportService().ImportFromExcelAsync(path, data);

    private static Employee Hourly() => new()
    {
        Id = "EMP-001",
        Name = "Dana Smith",
        EmployeeNumber = "42",
        Sin = "046454286",
        Province = "AB",
        PayType = PayType.Hourly,
        PayRate = 28m,
        PayFrequency = PayFrequency.Weekly,
        StandardHoursPerWeek = 37.5m,
        Address = new Core.Models.Common.Address
        {
            Street = "42 Employee Road",
            City = "Calgary",
            State = "AB",
            ZipCode = "T2P1A1",
        },
        Notes = "original",
    };

    #region A sheet that only annotates

    /// <summary>
    /// The check that would have caught it: a sheet carrying an ID and one other column changes
    /// that column and nothing else.
    /// </summary>
    [Fact]
    public async Task ASheetWithOnlyAnIdAndANote_LeavesEveryOtherFieldAlone()
    {
        var data = new CompanyData();
        Employee employee = Hourly();
        data.Employees.Add(employee);

        await Import(
            Workbook("Employees", ["ID", "Notes"], ["EMP-001", "moved to nights"]),
            data);

        Assert.Single(data.Employees);
        Assert.Equal("moved to nights", employee.Notes);

        Assert.Equal(PayType.Hourly, employee.PayType);
        Assert.Equal(28m, employee.PayRate);
        Assert.Equal(PayFrequency.Weekly, employee.PayFrequency);
        Assert.Equal("046454286", employee.Sin);
        Assert.Equal("42", employee.EmployeeNumber);
        Assert.Equal(37.5m, employee.StandardHoursPerWeek);
        Assert.Equal("42 Employee Road", employee.Address.Street);
        Assert.Equal("T2P1A1", employee.Address.ZipCode);
    }

    /// <summary>A column that IS present still updates, or the guard has gone too far.</summary>
    [Fact]
    public async Task ASheetCarryingAColumn_StillUpdatesIt()
    {
        var data = new CompanyData();
        Employee employee = Hourly();
        data.Employees.Add(employee);

        await Import(
            Workbook("Employees", ["ID", "Pay Type", "Pay Rate"], ["EMP-001", "Salary", "62400"]),
            data);

        Assert.Equal(PayType.Salary, employee.PayType);
        Assert.Equal(62_400m, employee.PayRate);

        // Untouched by this sheet.
        Assert.Equal("046454286", employee.Sin);
    }

    #endregion

    #region Names split across two columns

    [Fact]
    public async Task ASheetWithFirstAndLastName_JoinsThemIntoOne()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "First Name", "Last Name"], ["EMP-001", "Dana", "Smith"]),
            data);

        Assert.Equal("Dana Smith", Assert.Single(data.Employees).Name);
    }

    /// <summary>
    /// The same sheet with no ID column at all. The emptiness test ran before the two name
    /// columns were joined, so both values it looked at were blank on every row and the import
    /// reported every row as empty and brought in nobody.
    /// </summary>
    [Fact]
    public async Task ASheetWithNoIdColumn_StillImportsEverybody()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["First Name", "Last Name"],
                ["Dana", "Smith"],
                ["Chris", "Okafor"],
                ["Alex", "Reyes"]),
            data);

        Assert.Equal(3, data.Employees.Count);
        Assert.Contains(data.Employees, e => e.Name == "Chris Okafor");

        // Minted rather than shared, or three people collapse into one record.
        Assert.Equal(3, data.Employees.Select(e => e.Id).Distinct().Count());
    }

    /// <summary>Genuinely blank rows are still skipped, which is what the guard is for.</summary>
    [Fact]
    public async Task TrailingBlankRows_AreStillSkipped()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "First Name", "Last Name"],
                ["EMP-001", "Dana", "Smith"],
                ["", "", ""],
                ["", "", ""]),
            data);

        Assert.Single(data.Employees);
    }

    #endregion

    #region Column names other systems use

    [Fact]
    public async Task SalaryTypeAndSalaryAmount_AreReadAsPayTypeAndPayRate()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "Name", "Salary Type", "Salary Amount"],
                ["EMP-001", "Dana Smith", "Annual", "62400"]),
            data);

        Employee employee = Assert.Single(data.Employees);
        Assert.Equal(PayType.Salary, employee.PayType);
        Assert.Equal(62_400m, employee.PayRate);
    }

    [Fact]
    public async Task SalaryTypeOfHourly_IsReadAsHourly()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "Name", "Salary Type", "Salary Amount"],
                ["EMP-001", "Dana Smith", "Hourly", "28"]),
            data);

        Assert.Equal(PayType.Hourly, Assert.Single(data.Employees).PayType);
    }

    /// <summary>
    /// Written with a hyphen or a space almost everywhere except here. Falling back to the
    /// default would pay a monthly employee every two weeks.
    /// </summary>
    [Theory]
    [InlineData("Bi-weekly", PayFrequency.Biweekly)]
    [InlineData("Semi Monthly", PayFrequency.SemiMonthly)]
    [InlineData("Semi-Monthly", PayFrequency.SemiMonthly)]
    [InlineData("Monthly", PayFrequency.Monthly)]
    [InlineData("Weekly", PayFrequency.Weekly)]
    public async Task APayFrequencyWrittenWithPunctuation_LandsOnTheRightOne(string text, PayFrequency expected)
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "Name", "Pay Frequency"], ["EMP-001", "Dana Smith", text]),
            data);

        Assert.Equal(expected, Assert.Single(data.Employees).PayFrequency);
    }

    [Fact]
    public async Task HireDate_IsReadAsTheStartDate()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "Name", "Hire Date"], ["EMP-001", "Dana Smith", "2026-01-05"]),
            data);

        Assert.Equal(new DateTime(2026, 1, 5), Assert.Single(data.Employees).StartDate);
    }

    /// <summary>
    /// Null rather than zero when the cell is blank. Zero reads as "worked no hours" on a record
    /// of employment, which costs the employee their claim.
    /// </summary>
    [Fact]
    public async Task ABlankStandardHoursCell_StaysUnknownRatherThanBecomingZero()
    {
        var data = new CompanyData();

        await Import(
            Workbook("Employees", ["ID", "Name", "Standard Hours Per Week"], ["EMP-001", "Dana Smith", ""]),
            data);

        Assert.Null(Assert.Single(data.Employees).StandardHoursPerWeek);
    }

    #endregion

    public void Dispose()
    {
        foreach (string file in _files)
        {
            try { File.Delete(file); } catch { /* best effort */ }
        }

        GC.SuppressFinalize(this);
    }
}
