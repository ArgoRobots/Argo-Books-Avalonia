using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The bonus sequence as a user runs it: three real pay runs saved into a company, each one
/// created and approved before the next is drafted.
///
/// The other bonus tests call the calculator directly with a year-to-date they assemble
/// themselves. This one goes through CreateDraft, Recalculate, Approve and YearToDateFor, so a
/// mistake in how approved runs are read back is caught as well as a mistake in the formula.
/// </summary>
public class PayrollBonusSequenceTests
{
    private const decimal AnnualSalary = 62_400m;
    private const decimal Bonus = 5_000m;

    private static CompanyData Company() => new()
    {
        Employees =
        {
            new Employee
            {
                Id = "EMP-001",
                Name = "Test Person",
                Province = "AB",
                PayType = PayType.Salary,
                PayRate = AnnualSalary,
                PayFrequency = PayFrequency.Biweekly,
            },
        },
    };

    /// <summary>Drafts a run, puts the bonus on it, approves it, and files it in the company.</summary>
    private static PayRunLine RunAndApprove(PayrollService payroll, CompanyData data,
                                            DateTime payDate, decimal bonus = 0m)
    {
        PayRun run = payroll.CreateDraft(data, payDate, payDate.AddDays(-13), payDate)!;

        if (bonus > 0)
        {
            run.Lines[0].Bonus = bonus;
            payroll.Recalculate(data, run);
        }

        payroll.Approve(run);
        data.PayRuns.Add(run);

        return run.Lines[0];
    }

    /// <summary>
    /// A bonus is taxed in its own period and leaves every later period alone. The period after
    /// the bonus has to withhold exactly what the period before it did.
    /// </summary>
    [Fact]
    public void ThePeriodAfterABonus_WithholdsWhatThePeriodBeforeItDid()
    {
        var payroll = new PayrollService();
        CompanyData data = Company();

        PayRunLine before = RunAndApprove(payroll, data, new DateTime(2026, 8, 14));
        PayRunLine bonus = RunAndApprove(payroll, data, new DateTime(2026, 8, 28), Bonus);
        PayRunLine after = RunAndApprove(payroll, data, new DateTime(2026, 9, 11));

        Assert.Equal(Bonus, bonus.Bonus);
        Assert.True(bonus.FederalTax > before.FederalTax, "the bonus itself must be taxed");

        Assert.Equal(before.FederalTax, after.FederalTax);
        Assert.Equal(before.ProvincialTax, after.ProvincialTax);
    }

    /// <summary>
    /// The rest of the year, not just the one period after. The old bug grew with each approved
    /// run, so a single following period understates it.
    /// </summary>
    [Fact]
    public void EveryPeriodAfterABonus_WithholdsWhatThePeriodBeforeItDid()
    {
        var payroll = new PayrollService();
        CompanyData data = Company();

        var payDate = new DateTime(2026, 1, 9);

        PayRunLine before = RunAndApprove(payroll, data, payDate);
        RunAndApprove(payroll, data, payDate.AddDays(14), Bonus);

        for (int period = 2; period < 26; period++)
        {
            PayRunLine line = RunAndApprove(payroll, data, payDate.AddDays(14 * period));

            Assert.Equal(before.FederalTax, line.FederalTax);
            Assert.Equal(before.ProvincialTax, line.ProvincialTax);
        }
    }
}
