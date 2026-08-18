using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the year end modal.
///
/// The reopen case is the one worth pinning. Refilling the year list makes the ComboBox drop its
/// selection and report null, and a non-nullable bound property turned that into a conversion
/// error printed under the control. It only showed on the SECOND open, because the first had no
/// selection to lose, which is exactly the kind of thing a single-open test would miss.
/// </summary>
public class YearEndModalViewModelTests : ModalViewModelTestBase
{
    private static Employee Person(string id = "EMP-001", string province = "AB") => new()
    {
        Id = id,
        Name = "Dana Smith",
        Sin = "046454286",
        Province = province,
        PayType = PayType.Salary,
        PayRate = 52000m,
        PayFrequency = PayFrequency.Biweekly,
    };

    private static PayRun Run(string id, DateTime payDate, string employeeId = "EMP-001") => new()
    {
        Id = id,
        PayDate = payDate,
        PeriodStart = payDate.AddDays(-14),
        PeriodEnd = payDate.AddDays(-1),
        Status = PayRunStatus.Approved,
        Lines =
        {
            new PayRunLine
            {
                EmployeeId = employeeId,
                EmployeeName = "Dana Smith",
                Province = "AB",
                GrossPay = 2000m,
                CppEmployee = 100m,
                EiEmployee = 30m,
                FederalTax = 200m,
                ProvincialTax = 90m,
                NetPay = 1580m,
            },
        },
    };

    [Fact]
    public void ReopeningTheModal_KeepsAYearSelected()
    {
        // The ComboBox nulls its selection when the list it is bound to is cleared. Bound to a
        // plain int that null had nowhere to go, and the error surfaced under the control.
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));

        var vm = new YearEndModalViewModel();

        vm.Open();
        Assert.Equal(2026, vm.SelectedYear);

        vm.CloseCommand.Execute(null);
        vm.Open();

        Assert.Equal(2026, vm.SelectedYear);
        Assert.NotNull(vm.SelectedYear);
    }

    [Fact]
    public void ReopeningTheModal_StillShowsTheRows()
    {
        // The reason the null mattered beyond the message: a rebuild triggered while the year was
        // null would leave the screen wiped.
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));

        var vm = new YearEndModalViewModel();

        vm.Open();
        int first = vm.Rows.Count;

        vm.CloseCommand.Execute(null);
        vm.Open();

        Assert.Equal(first, vm.Rows.Count);
        Assert.NotEmpty(vm.Rows);
    }

    [Fact]
    public void ANewYearOfPayRuns_AppearsOnReopen()
    {
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2025, 7, 3)));

        var vm = new YearEndModalViewModel();
        vm.Open();

        Assert.Equal([2025], vm.AvailableYears);

        Company.PayRuns.Add(Run("PR-0002", new DateTime(2026, 7, 3)));
        vm.CloseCommand.Execute(null);
        vm.Open();

        // Newest first, and selected, so reopening after a pay run lands on the year just worked.
        Assert.Equal([2026, 2025], vm.AvailableYears);
        Assert.Equal(2026, vm.SelectedYear);
    }

    [Fact]
    public void WithNoPayRunsAtAll_ItStillOffersThisYear()
    {
        var vm = new YearEndModalViewModel();
        vm.Open();

        Assert.Single(vm.AvailableYears);
        Assert.Equal(DateTime.Today.Year, vm.SelectedYear);
        Assert.Empty(vm.Rows);
    }

    [Fact]
    public void SwitchingYear_RebuildsForThatYear()
    {
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));
        Company.PayRuns.Add(Run("PR-0002", new DateTime(2025, 7, 3)));

        var vm = new YearEndModalViewModel();
        vm.Open();

        Assert.Equal(2026, vm.SelectedYear);
        Assert.NotEmpty(vm.Rows);

        vm.SelectedYear = 2025;

        Assert.NotEmpty(vm.Rows);
        Assert.Single(vm.Rows);
    }

    #region The payroll account number

    /// <summary>
    /// The problem list at the top of the modal already says why filing is blocked, but it
    /// scrolls away, and the export button is at the bottom with nothing on it to explain why it
    /// is dead. The field itself has to say so.
    /// </summary>
    [Fact]
    public void AMalformedAccountNumber_IsReportedOnTheFieldItself()
    {
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));

        var vm = new YearEndModalViewModel();
        vm.Open();

        vm.AccountNumber = "67867";

        Assert.NotEmpty(vm.AccountNumberError);
        Assert.False(vm.CanFile);
    }

    [Fact]
    public void AWellFormedAccountNumber_ClearsTheError()
    {
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));

        var vm = new YearEndModalViewModel();
        vm.Open();

        vm.AccountNumber = "123456789RP0001";

        Assert.Empty(vm.AccountNumberError);
    }

    [Fact]
    public void AnEmptyAccountNumber_SaysNothingYet()
    {
        // Empty is not the same as wrong. The field is marked required and the problem list says
        // it is missing; colouring it red before anything has been typed is nagging.
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));

        var vm = new YearEndModalViewModel();
        vm.Open();

        vm.AccountNumber = string.Empty;

        Assert.Empty(vm.AccountNumberError);
        Assert.False(vm.CanFile);
    }

    [Fact]
    public void SpacingAndCaseInTheAccountNumber_AreAccepted()
    {
        // Stored on the CRA statement with spaces, and IsPayrollAccountNumber strips them, so the
        // field must not report an error on something the validator is going to accept.
        Company.Employees.Add(Person());
        Company.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3)));

        var vm = new YearEndModalViewModel();
        vm.Open();

        vm.AccountNumber = "123456789 rp 0001";

        Assert.Empty(vm.AccountNumberError);
    }

    #endregion

    #region What actually gets filed

    /// <summary>A company complete enough that filing is not blocked on something else.</summary>
    private YearEndModalViewModel Filable(int employees = 2)
    {
        Company.Settings.Company.Name = "Test Company";
        Company.Settings.Company.Address = "1 Main Street";
        Company.Settings.Company.City = "Calgary";
        Company.Settings.Company.ProvinceState = "AB";
        Company.Settings.Company.Country = "CAN";
        Company.Settings.Company.PostalCode = "T2P1A1";
        Company.Settings.Company.PayrollAccountNumber = "123456789RP0001";
        Company.Settings.Company.PayrollContactName = "Pat Owner";
        Company.Settings.Company.PayrollContactPhone = "4035551234";
        Company.Settings.Company.PayrollContactEmail = "pat@example.com";

        for (int i = 1; i <= employees; i++)
        {
            string id = $"EMP-{i:000}";
            Employee person = Person(id);
            person.Name = $"Employee {i}";
            person.DentalBenefit = DentalBenefitCode.PayeeOnly;

            Company.Employees.Add(person);
            Company.PayRuns.Add(Run($"PR-{i:0000}", new DateTime(2026, 7, 3), id));
        }

        var vm = new YearEndModalViewModel();
        vm.Open();
        vm.SelectedYear = 2026;
        return vm;
    }

    /// <summary>
    /// The filing detail boxes write straight through as they are typed, which rebuilds the slip
    /// table on every keystroke. The ticks have to survive that, or correcting a digit in the
    /// phone number silently clears the selection and greys out Export with nothing said.
    /// </summary>
    [Fact]
    public void TypingAFilingDetail_KeepsTheSlipsTicked()
    {
        YearEndModalViewModel vm = Filable();
        vm.FilingType = T4ReportType.Amendment;

        Assert.Equal(2, vm.Rows.Count);
        vm.Rows[0].IsSelected = true;
        vm.Rows[1].IsSelected = true;

        vm.ContactPhone = "4035551235";

        Assert.Equal(2, vm.Rows.Count);
        Assert.All(vm.Rows, r => Assert.True(r.IsSelected));
        Assert.True(vm.CanFile);
    }

    /// <summary>The other half: an unticked row must not come back ticked.</summary>
    [Fact]
    public void TypingAFilingDetail_DoesNotTickAnythingNew()
    {
        YearEndModalViewModel vm = Filable();

        vm.Rows[0].IsSelected = true;
        vm.Rows[1].IsSelected = false;

        vm.ContactName = "Pat O";

        Assert.True(vm.Rows[0].IsSelected);
        Assert.False(vm.Rows[1].IsSelected);
    }

    /// <summary>
    /// This view model is a shell singleton, so an amendment filed earlier in the session is
    /// still set when the screen is reopened. Left alone, the next export files another amended
    /// return instead of an original.
    /// </summary>
    [Fact]
    public void ReopeningYearEnd_IsBackToAnOriginalFiling()
    {
        YearEndModalViewModel vm = Filable();

        vm.FilingType = T4ReportType.Amendment;
        vm.AmendmentNote = "Corrected box 14";

        vm.Open();

        Assert.Equal(T4ReportType.Original, vm.FilingType);
        Assert.Empty(vm.AmendmentNote);
        Assert.False(vm.IsAmending);
    }

    /// <summary>
    /// The contact email reaches the file. It was dropped from the copy that gets written, so
    /// the screen showed the address, the XML carried an empty element, and CRA rejects a
    /// submission over exactly that.
    /// </summary>
    [Fact]
    public void TheExportedReturn_CarriesTheContactEmail()
    {
        YearEndModalViewModel vm = Filable();

        Core.Models.Payroll.T4Return filing = vm.BuildFilingReturn()!;

        Assert.Equal("pat@example.com", filing.ContactEmail);

        var xml = System.Xml.Linq.XDocument.Parse(
            Core.Services.Payroll.T4XmlWriter.BuildString(filing));

        System.Xml.Linq.XElement email =
            xml.Descendants().Single(e => e.Name.LocalName == "cntc_email_area");

        Assert.Equal("pat@example.com", email.Value);
    }

    /// <summary>
    /// Every other field on the copy, since the email was not a special case: any of them left
    /// off exports a file CRA rejects, and none of them fails visibly here.
    /// </summary>
    [Fact]
    public void TheExportedReturn_CarriesEveryEmployerField()
    {
        YearEndModalViewModel vm = Filable();
        vm.FilingType = T4ReportType.Amendment;
        vm.AmendmentNote = "Corrected box 14";
        vm.Rows[0].IsSelected = true;

        Core.Models.Payroll.T4Return filing = vm.BuildFilingReturn()!;

        Assert.Equal(2026, filing.TaxYear);
        Assert.Equal("123456789RP0001", filing.PayrollAccountNumber);
        Assert.Equal("Test Company", filing.EmployerName);
        Assert.Equal("T2P1A1", filing.EmployerAddress.ZipCode);
        Assert.Equal("Pat Owner", filing.ContactName);
        Assert.Equal("4035551234", filing.ContactPhone);
        Assert.Equal("pat@example.com", filing.ContactEmail);
        Assert.False(string.IsNullOrWhiteSpace(filing.LanguageCode));
        Assert.Equal(T4ReportType.Amendment, filing.ReportType);
        Assert.Equal("Corrected box 14", filing.AmendmentNote);
    }

    /// <summary>
    /// An amendment carries only the ticked employees. Filing the untouched ones again would
    /// amend slips that were already right.
    /// </summary>
    [Fact]
    public void AnAmendment_CarriesOnlyTheTickedSlips()
    {
        YearEndModalViewModel vm = Filable();

        vm.FilingType = T4ReportType.Amendment;
        vm.Rows[0].IsSelected = true;
        vm.Rows[1].IsSelected = false;

        T4Slip slip = Assert.Single(vm.BuildFilingReturn()!.Slips);
        Assert.Equal(vm.Rows[0].EmployeeId, slip.EmployeeId);
    }

    /// <summary>An original return covers every employee, ticked or not.</summary>
    [Fact]
    public void AnOriginalFiling_CarriesEverybody()
    {
        YearEndModalViewModel vm = Filable();

        Assert.Equal(T4ReportType.Original, vm.FilingType);
        Assert.Equal(2, vm.BuildFilingReturn()!.Slips.Count);
    }

    #endregion
}
