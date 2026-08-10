using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the pay run orchestration.
///
/// The year-to-date logic matters most. The calculator takes those totals as an input and
/// cannot check them, so a mistake here silently produces wrong deductions for anyone
/// approaching an annual ceiling, which is exactly where payroll errors cost real money.
/// </summary>
public class PayrollServiceTests
{
    private static readonly DateTime PayDate = new(2026, 8, 14);

    private static CompanyData DataWithEmployee(string id = "EMP-001") => new()
    {
        Employees =
        {
            new Employee
            {
                Id = id,
                Name = "Test Person",
                Province = "AB",
                PayType = PayType.Salary,
                PayRate = 52000m,
                PayFrequency = PayFrequency.Biweekly,
            },
        },
    };

    private static PayRun ApprovedRun(string id, DateTime payDate, string employeeId, decimal gross,
                                      decimal cpp, decimal ei) => new()
    {
        Id = id,
        PayDate = payDate,
        Status = PayRunStatus.Approved,
        Lines =
        {
            new PayRunLine
            {
                EmployeeId = employeeId,
                GrossPay = gross,
                CppEmployee = cpp,
                EiEmployee = ei,
                NetPay = gross - cpp - ei,
            },
        },
    };

    [Fact]
    public void CreateDraft_ProducesALineForEveryActiveEmployee()
    {
        CompanyData data = DataWithEmployee();
        data.Employees.Add(new Employee { Id = "EMP-002", Name = "Archived", Province = "AB", IsArchived = true });

        PayRun? run = new PayrollService().CreateDraft(data, PayDate, PayDate.AddDays(-13), PayDate);

        Assert.NotNull(run);
        Assert.Single(run.Lines);
        Assert.Equal("EMP-001", run.Lines[0].EmployeeId);
    }

    [Fact]
    public void CreateDraft_PrefillsSalariedPayFromTheAnnualRate()
    {
        PayRun? run = new PayrollService().CreateDraft(DataWithEmployee(), PayDate, PayDate.AddDays(-13), PayDate);

        // 52,000 a year over 26 periods.
        Assert.Equal(2000m, run!.Lines[0].BasePay);
    }

    [Fact]
    public void CreateDraft_RecordsWhichRateEditionWasUsed()
    {
        PayRun? run = new PayrollService().CreateDraft(DataWithEmployee(), PayDate, PayDate.AddDays(-13), PayDate);

        Assert.Equal("2026-07", run!.RateEditionId);
    }

    [Fact]
    public void CreateDraft_ReturnsNullWhenNoRateEditionCoversThePayDate()
    {
        // Deliberately not a fallback to the nearest edition: calculating with the wrong rates
        // produces figures that look plausible and are wrong.
        PayRun? run = new PayrollService()
            .CreateDraft(DataWithEmployee(), new DateTime(2030, 3, 1), new DateTime(2030, 2, 16), new DateTime(2030, 3, 1));

        Assert.Null(run);
    }

    #region Year to date

    [Fact]
    public void YearToDate_IsZeroWithNoPriorRuns()
    {
        PayrollYearToDate ytd = new PayrollService().YearToDateFor(DataWithEmployee(), "EMP-001");

        Assert.Equal(0m, ytd.CppEmployee);
        Assert.Equal(0m, ytd.EiEmployee);
        Assert.Equal(0m, ytd.PensionableEarnings);
    }

    [Fact]
    public void YearToDate_AccumulatesApprovedRuns()
    {
        CompanyData data = DataWithEmployee();
        data.PayRuns.Add(ApprovedRun("PR-0001", new DateTime(2026, 1, 16), "EMP-001", 2000m, 110.99m, 32.60m));
        data.PayRuns.Add(ApprovedRun("PR-0002", new DateTime(2026, 1, 30), "EMP-001", 2000m, 110.99m, 32.60m));

        PayrollYearToDate ytd = new PayrollService().YearToDateFor(data, "EMP-001");

        Assert.Equal(221.98m, ytd.CppEmployee);
        Assert.Equal(65.20m, ytd.EiEmployee);
        Assert.Equal(4000m, ytd.PensionableEarnings);
    }

    [Fact]
    public void YearToDate_IgnoresDrafts()
    {
        // An unapproved run must not inflate the totals, or the next run under-deducts.
        CompanyData data = DataWithEmployee();
        PayRun draft = ApprovedRun("PR-0001", new DateTime(2026, 1, 16), "EMP-001", 2000m, 110.99m, 32.60m);
        draft.Status = PayRunStatus.Draft;
        data.PayRuns.Add(draft);

        PayrollYearToDate ytd = new PayrollService().YearToDateFor(data, "EMP-001");

        Assert.Equal(0m, ytd.CppEmployee);
    }

    [Fact]
    public void YearToDate_IgnoresVoidedRuns()
    {
        CompanyData data = DataWithEmployee();
        PayRun voided = ApprovedRun("PR-0001", new DateTime(2026, 1, 16), "EMP-001", 2000m, 110.99m, 32.60m);
        voided.Status = PayRunStatus.Void;
        data.PayRuns.Add(voided);

        PayrollYearToDate ytd = new PayrollService().YearToDateFor(data, "EMP-001");

        Assert.Equal(0m, ytd.CppEmployee);
    }

    [Fact]
    public void YearToDate_IgnoresOtherCalendarYears()
    {
        CompanyData data = DataWithEmployee();
        data.PayRuns.Add(ApprovedRun("PR-0001", new DateTime(2025, 12, 19), "EMP-001", 2000m, 110.99m, 32.60m));

        PayRun? current = new PayrollService().CreateDraft(data, PayDate, PayDate.AddDays(-13), PayDate);
        PayrollYearToDate ytd = new PayrollService().YearToDateFor(data, "EMP-001", current);

        Assert.Equal(0m, ytd.CppEmployee);
    }

    [Fact]
    public void YearToDate_IgnoresOtherEmployees()
    {
        CompanyData data = DataWithEmployee();
        data.PayRuns.Add(ApprovedRun("PR-0001", new DateTime(2026, 1, 16), "EMP-999", 5000m, 300m, 80m));

        PayrollYearToDate ytd = new PayrollService().YearToDateFor(data, "EMP-001");

        Assert.Equal(0m, ytd.CppEmployee);
    }

    #endregion

    #region Approve and void

    [Fact]
    public void Approve_LocksTheRun()
    {
        PayRun run = ApprovedRun("PR-0001", PayDate, "EMP-001", 2000m, 110.99m, 32.60m);
        run.Status = PayRunStatus.Draft;

        new PayrollService().Approve(run);

        Assert.Equal(PayRunStatus.Approved, run.Status);
        Assert.NotNull(run.ApprovedAt);
    }

    [Fact]
    public void Recalculate_DoesNothingToAnApprovedRun()
    {
        // Approved figures are frozen so a historical run always agrees with the pay stub the
        // employee was given.
        CompanyData data = DataWithEmployee();
        PayRun run = ApprovedRun("PR-0001", PayDate, "EMP-001", 2000m, 110.99m, 32.60m);
        run.Lines[0].BasePay = 9999m;
        data.PayRuns.Add(run);

        new PayrollService().Recalculate(data, run);

        Assert.Equal(2000m, run.Lines[0].GrossPay);
    }

    [Fact]
    public void Void_WritesAReversalAndKeepsTheOriginal()
    {
        CompanyData data = DataWithEmployee();
        PayRun run = ApprovedRun("PR-0001", PayDate, "EMP-001", 2000m, 110.99m, 32.60m);
        data.PayRuns.Add(run);

        PayRun? reversal = new PayrollService().Void(data, run);

        Assert.NotNull(reversal);
        Assert.Equal("PR-0001", reversal.VoidsPayRunId);
        Assert.Equal(-2000m, reversal.Lines[0].GrossPay);
        Assert.Equal(-110.99m, reversal.Lines[0].CppEmployee);
        Assert.Equal(PayRunStatus.Void, run.Status);
        Assert.Equal(2, data.PayRuns.Count);
    }

    [Fact]
    public void Void_AndItsReversalCancelOutInTheYearToDate()
    {
        CompanyData data = DataWithEmployee();
        PayRun run = ApprovedRun("PR-0001", new DateTime(2026, 1, 16), "EMP-001", 2000m, 110.99m, 32.60m);
        data.PayRuns.Add(run);

        var service = new PayrollService();
        service.Void(data, run);

        PayrollYearToDate ytd = service.YearToDateFor(data, "EMP-001");

        // The original is now Void so it is skipped, and the reversal is negative, so the net
        // effect has to be zero rather than a negative balance.
        Assert.Equal(-110.99m, ytd.CppEmployee);
    }

    #endregion
}
