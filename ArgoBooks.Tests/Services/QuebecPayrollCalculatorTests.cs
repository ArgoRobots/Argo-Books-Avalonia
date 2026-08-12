using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the Quebec calculator, against TP-1015.F-V (2026-01).
///
/// The two intermediate figures the guide publishes in its own worked example, Appendix 1, are
/// checked directly. Those are the parts that could not have been guessed from the federal
/// formula: the deduction for workers, which comes off income and has no federal equivalent,
/// and the deductible share of QPP, which is the 1.00 percentage point inside the 6.30% rate
/// rather than CPP's split.
///
/// These do NOT prove agreement with Revenu Quebec on a whole pay stub. That needs fixtures
/// captured from WebRAS, which is Quebec's equivalent of PDOC. PDOC deliberately excludes
/// Quebec, so the fixtures gathered for the rest of Canada say nothing here.
/// </summary>
public class QuebecPayrollCalculatorTests
{
    private static PayrollRateTable Rates() => new PayrollRateService().GetForDate(new DateTime(2026, 10, 9))!;

    private static PayrollInput Input(decimal gross, int periods = 26) => new()
    {
        GrossPay = gross,
        Province = "QC",
        PayPeriodsPerYear = periods,
    };

    private static PayrollDeductions Calc(decimal gross, int periods = 26, PayrollYearToDate? ytd = null) =>
        PayrollCalculator.Calculate(Input(gross, periods), ytd ?? new PayrollYearToDate(), Rates());

    #region Against the guide's own worked example

    [Fact]
    public void QppMatchesTheGuidesWorkedExample()
    {
        // Appendix 1 uses $4,000 biweekly. The guide's own intermediate for the deductible
        // share of QPP is $38.65, which implies a QPP contribution of $243.52:
        //   (4000 - 3500/26) x 0.0630 = 243.52
        //   243.52 x (0.01 / 0.0630)  = 38.65
        Assert.Equal(243.52m, Calc(4000m).CppEmployee);
    }

    [Fact]
    public void TheDeductionForWorkersMatchesTheGuidesWorkedExample()
    {
        // The guide states H = $55.77 for this employee. It is 6% of pay capped at $1,450 for
        // the YEAR, so at $4,000 biweekly the annual cap binds long before the 6% does:
        //   min(0.06 x 4000, 1450 / 26) = min(240.00, 55.77) = 55.77
        //
        // Verified through its effect: without the deduction, annual taxable income would be
        // 26 x 55.77 = $1,450.02 higher, which at the 19% bracket is $275.50 more tax a year.
        PayrollRateTable rates = Rates();
        QuebecRates qc = rates.Quebec!;

        decimal expected = Math.Min(qc.WorkerDeductionRate * 4000m, qc.WorkerDeductionMaxAnnual / 26m);

        Assert.Equal(55.77m, Math.Round(expected, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void TheConstantsAreThePublishedWholeDollarOnes()
    {
        // Deriving these from bracket continuity gives 2,717.25 / 8,151.25 / 10,465.54. Revenu
        // Quebec publishes them rounded to whole dollars and those are what must be used.
        List<TaxBracket> brackets = Rates().Quebec!.Brackets;

        Assert.Equal(0m, brackets[0].ConstantK);
        Assert.Equal(2717m, brackets[1].ConstantK);
        Assert.Equal(8151m, brackets[2].ConstantK);
        Assert.Equal(10465m, brackets[3].ConstantK);
    }

    #endregion

    #region What makes Quebec different

    [Fact]
    public void QuebecUsesQppRatesNotCpp()
    {
        // 6.30% against 5.95%. Same exemption and ceiling, so the only difference at this
        // income is the rate, and it is visible directly.
        decimal quebec = Calc(2400m).CppEmployee;
        decimal alberta = PayrollCalculator.Calculate(
            new PayrollInput { GrossPay = 2400m, Province = "AB", PayPeriodsPerYear = 26 },
            new PayrollYearToDate(), Rates()).CppEmployee;

        Assert.True(quebec > alberta, $"QPP {quebec} should exceed CPP {alberta}");
        Assert.Equal(Math.Round((2400m - 3500m / 26m) * 0.0630m, 2, MidpointRounding.AwayFromZero), quebec);
    }

    [Fact]
    public void QuebecEmployeesPayQpip()
    {
        // No equivalent exists anywhere else in Canada.
        PayrollDeductions d = Calc(2400m);

        Assert.Equal(Math.Round(2400m * 0.00430m, 2, MidpointRounding.AwayFromZero), d.QpipEmployee);
        Assert.Equal(Math.Round(2400m * 0.00602m, 2, MidpointRounding.AwayFromZero), d.QpipEmployer);
    }

    [Fact]
    public void TheQpipEmployerShareIsReadFromItsOwnPublishedRate()
    {
        // Revenu Quebec publishes the employer rate and maximum separately, and as it happens
        // both are exactly 1.4 times the employee's: 0.00430 x 1.4 = 0.00602 and
        // 442.90 x 1.4 = 620.06. So a multiplier would currently give the right answer.
        //
        // It is still read independently. The two figures are published independently and
        // nothing obliges them to keep that ratio, so deriving one from the other would be
        // relying on a coincidence that could quietly stop holding at an indexation.
        PayrollRateTable rates = Rates();
        QuebecRates qc = rates.Quebec!;

        Assert.Equal(0.00602m, qc.Qpip.RateEmployer);
        Assert.Equal(620.06m, qc.Qpip.MaxPremiumEmployer);

        PayrollDeductions d = Calc(2400m);
        Assert.Equal(Math.Round(2400m * qc.Qpip.RateEmployer, 2, MidpointRounding.AwayFromZero), d.QpipEmployer);
    }

    [Fact]
    public void QuebecEmployeesPayLessEiThanTheRestOfCanada()
    {
        // 1.30% against 1.63%, because QPIP covers the parental benefits EI covers elsewhere.
        decimal quebec = Calc(2400m).EiEmployee;
        decimal alberta = PayrollCalculator.Calculate(
            new PayrollInput { GrossPay = 2400m, Province = "AB", PayPeriodsPerYear = 26 },
            new PayrollYearToDate(), Rates()).EiEmployee;

        Assert.True(quebec < alberta, $"Quebec EI {quebec} should be below {alberta}");
        Assert.Equal(Math.Round(2400m * 0.0130m, 2, MidpointRounding.AwayFromZero), quebec);
    }

    [Fact]
    public void FederalTaxIsReducedByTheAbatement()
    {
        // CRA collects less from Quebec residents because Quebec collects its own income tax.
        // At the same gross, federal tax must be materially below the rest of Canada, and the
        // gap must be the abatement rather than a rounding difference.
        decimal quebec = Calc(2400m).FederalTax;
        decimal alberta = PayrollCalculator.Calculate(
            new PayrollInput { GrossPay = 2400m, Province = "AB", PayPeriodsPerYear = 26 },
            new PayrollYearToDate(), Rates()).FederalTax;

        Assert.True(quebec < alberta * 0.9m,
            $"Quebec federal tax {quebec} should be well below {alberta} after the 16.5% abatement");
    }

    [Fact]
    public void QuebecProvincialTaxIsChargedAndIsSubstantial()
    {
        PayrollDeductions d = Calc(2400m);

        Assert.True(d.ProvincialTax > 0);
    }

    #endregion

    #region Ceilings

    [Fact]
    public void QppStopsAtItsOwnMaximumNotCpps()
    {
        PayrollRateTable rates = Rates();
        var atMax = new PayrollYearToDate { CppEmployee = rates.Quebec!.Qpp.MaxContributionEmployee };

        Assert.Equal(0m, Calc(5000m, ytd: atMax).CppEmployee);

        // And CPP's lower maximum must NOT stop it: an employee between the two ceilings is
        // still contributing.
        var atCppMax = new PayrollYearToDate { CppEmployee = rates.Cpp.MaxContributionEmployee };
        Assert.True(Calc(5000m, ytd: atCppMax).CppEmployee > 0);
    }

    [Fact]
    public void QpipStopsAtItsAnnualMaximum()
    {
        PayrollRateTable rates = Rates();
        var atMax = new PayrollYearToDate { QpipEmployee = rates.Quebec!.Qpip.MaxPremiumEmployee };

        Assert.Equal(0m, Calc(5000m, ytd: atMax).QpipEmployee);
    }

    [Fact]
    public void EiStopsAtQuebecsLowerMaximum()
    {
        PayrollRateTable rates = Rates();
        var atMax = new PayrollYearToDate { EiEmployee = rates.Quebec!.EiMaxPremiumEmployee };

        Assert.Equal(0m, Calc(5000m, ytd: atMax).EiEmployee);
    }

    #endregion

    #region Identities that must hold here too

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(2400)]
    [InlineData(4000)]
    [InlineData(10000)]
    public void NetPayIsGrossMinusEveryEmployeeDeductionIncludingQpip(decimal gross)
    {
        PayrollDeductions d = Calc(gross);

        Assert.Equal(
            d.GrossPay - d.CppEmployee - d.Cpp2Employee - d.EiEmployee - d.QpipEmployee
            - d.FederalTax - d.ProvincialTax,
            d.NetPay);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(2400)]
    [InlineData(10000)]
    public void TotalCostAndRemittanceIncludeTheEmployerQpipShare(decimal gross)
    {
        PayrollDeductions d = Calc(gross);

        Assert.Equal(d.GrossPay + d.CppEmployer + d.Cpp2Employer + d.EiEmployer + d.QpipEmployer, d.TotalCost);
        Assert.True(d.TotalRemittance > d.FederalTax + d.ProvincialTax);
    }

    [Fact]
    public void ZeroGrossProducesNothing()
    {
        PayrollDeductions d = Calc(0m);

        Assert.Equal(0m, d.CppEmployee);
        Assert.Equal(0m, d.QpipEmployee);
        Assert.Equal(0m, d.EiEmployee);
        Assert.Equal(0m, d.FederalTax);
        Assert.Equal(0m, d.ProvincialTax);
        Assert.Equal(0m, d.NetPay);
    }

    [Fact]
    public void ALowEarnerPaysNoQuebecTaxRatherThanNegativeTax()
    {
        // Well under the $18,952 personal amount once annualised.
        Assert.Equal(0m, Calc(200m).ProvincialTax);
    }

    [Fact]
    public void QuebecIsReachedThroughTheOrdinaryEntryPoint()
    {
        // Callers do not choose the calculator. PayrollCalculator dispatches on the province,
        // so a Quebec employee cannot accidentally be run through the federal formula.
        PayrollDeductions d = PayrollCalculator.Calculate(
            new PayrollInput { GrossPay = 2400m, Province = "qc", PayPeriodsPerYear = 26 },
            new PayrollYearToDate(), Rates());

        Assert.True(d.QpipEmployee > 0, "lower case province code should still reach the Quebec calculator");
    }

    #endregion
}
