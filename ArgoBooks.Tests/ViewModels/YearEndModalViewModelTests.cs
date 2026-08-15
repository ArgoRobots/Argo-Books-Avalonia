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
}
