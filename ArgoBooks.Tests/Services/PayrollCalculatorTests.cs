using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the payroll deduction engine.
///
/// The federal formula test is the important one: it reproduces the constants from CRA's own
/// published worked example. The rest cover the annual maximums, which are where a payroll
/// calculator silently goes wrong: over-deducting past a ceiling produces money owed back to
/// an employee and a T4 that does not reconcile.
///
/// These do not prove the engine agrees with CRA on a whole pay stub. That needs fixtures
/// captured from CRA's Payroll Deductions Online Calculator, which is the oracle for this
/// kind of software.
/// </summary>
public class PayrollCalculatorTests
{
    private static PayrollRateTable Rates() => new PayrollRateService().GetForDate(new DateTime(2026, 8, 15))!;

    private static PayrollInput Input(decimal gross, int periods = 26) => new()
    {
        GrossPay = gross,
        Province = "AB",
        PayPeriodsPerYear = periods,
    };

    [Fact]
    public void RateTable_IsAvailableForAPayDateInTheSecondHalfOf2026()
    {
        Assert.NotNull(Rates());
    }

    #region Federal formula, against CRA's published constants

    [Fact]
    public void FederalCredits_MatchCraWorkedExample()
    {
        PayrollRateTable rates = Rates();
        decimal lowest = rates.Federal.LowestRateForCredits;

        // CRA's example states K1 = 2303.28 and K4 = 210.14. Both fall out of the table.
        Assert.Equal(2303.28m, Math.Round(lowest * rates.Federal.BasicPersonalAmount.Maximum, 2));
        Assert.Equal(210.14m, Math.Round(lowest * rates.Federal.CanadaEmploymentAmount, 2));
    }

    [Fact]
    public void FederalTax_ReproducesCraWorkedExampleForTheYear()
    {
        PayrollRateTable rates = Rates();
        decimal lowest = rates.Federal.LowestRateForCredits;

        // From CRA's bonus example: A = 55,450.76 with K2 = 491.63 gives T3 = 4,758.06.
        const decimal a = 55450.76m;
        const decimal k2 = 491.63m;

        decimal k1 = lowest * rates.Federal.BasicPersonalAmount.Maximum;
        decimal k4 = lowest * rates.Federal.CanadaEmploymentAmount;
        decimal t3 = rates.Federal.Brackets[0].Rate * a - rates.Federal.Brackets[0].ConstantK - k1 - k2 - k4;

        Assert.Equal(4758.06m, Math.Round(t3, 2));
    }

    #endregion

    #region Bracket continuity

    [Theory]
    [InlineData(58523)]
    [InlineData(117045)]
    [InlineData(181440)]
    [InlineData(258482)]
    public void FederalBrackets_AreContinuousAtEveryBoundary(decimal boundary)
    {
        List<TaxBracket> brackets = Rates().Federal.Brackets;
        int i = brackets.FindIndex(b => b.UpTo == boundary);
        Assert.True(i >= 0 && i + 1 < brackets.Count);

        decimal below = brackets[i].Rate * boundary - brackets[i].ConstantK;
        decimal above = brackets[i + 1].Rate * boundary - brackets[i + 1].ConstantK;

        // Constants are published rounded to whole dollars, so a boundary can differ by cents.
        Assert.True(Math.Abs(below - above) < 1m, $"discontinuity of {below - above:F2} at {boundary}");
    }

    #endregion

    #region Annual maximums

    [Fact]
    public void Cpp_StopsAtTheAnnualMaximum()
    {
        PayrollRateTable rates = Rates();
        var ytd = new PayrollYearToDate { CppEmployee = rates.Cpp.MaxContributionEmployee };

        PayrollDeductions result = PayrollCalculator.Calculate(Input(3000m), ytd, rates);

        Assert.Equal(0m, result.CppEmployee);
    }

    [Fact]
    public void Cpp_IsPartiallyDeductedInThePeriodThatReachesTheMaximum()
    {
        PayrollRateTable rates = Rates();
        var ytd = new PayrollYearToDate { CppEmployee = rates.Cpp.MaxContributionEmployee - 20m };

        PayrollDeductions result = PayrollCalculator.Calculate(Input(3000m), ytd, rates);

        Assert.Equal(20m, result.CppEmployee);
    }

    [Fact]
    public void Ei_StopsAtTheAnnualMaximum()
    {
        PayrollRateTable rates = Rates();
        var ytd = new PayrollYearToDate
        {
            EiEmployee = rates.Ei.MaxPremiumEmployee,
            InsurableEarnings = rates.Ei.MaxInsurableEarnings,
        };

        PayrollDeductions result = PayrollCalculator.Calculate(Input(3000m), ytd, rates);

        Assert.Equal(0m, result.EiEmployee);
    }

    [Fact]
    public void Cpp2_DoesNotApplyBelowTheFirstCeiling()
    {
        PayrollRateTable rates = Rates();
        var ytd = new PayrollYearToDate { PensionableEarnings = 10000m };

        PayrollDeductions result = PayrollCalculator.Calculate(Input(3000m), ytd, rates);

        Assert.Equal(0m, result.Cpp2Employee);
    }

    [Fact]
    public void Cpp2_AppliesOnEarningsAboveTheFirstCeiling()
    {
        PayrollRateTable rates = Rates();
        var ytd = new PayrollYearToDate { PensionableEarnings = rates.Cpp.YmpeCeiling };

        PayrollDeductions result = PayrollCalculator.Calculate(Input(1000m), ytd, rates);

        Assert.Equal(Math.Round(1000m * rates.Cpp2.RateEmployee, 2), result.Cpp2Employee);
    }

    [Fact]
    public void Cpp2_StopsAtTheSecondCeiling()
    {
        PayrollRateTable rates = Rates();
        var ytd = new PayrollYearToDate { PensionableEarnings = rates.Cpp2.YampeCeiling };

        PayrollDeductions result = PayrollCalculator.Calculate(Input(3000m), ytd, rates);

        Assert.Equal(0m, result.Cpp2Employee);
    }

    #endregion

    #region Exemptions and edges

    [Fact]
    public void ExemptEmployee_HasNoCppOrEi()
    {
        PayrollInput input = Input(3000m);
        input.IsCppExempt = true;
        input.IsEiExempt = true;

        PayrollDeductions result = PayrollCalculator.Calculate(input, new PayrollYearToDate(), Rates());

        Assert.Equal(0m, result.CppEmployee);
        Assert.Equal(0m, result.EiEmployee);
    }

    [Fact]
    public void ZeroGross_ProducesNoDeductions()
    {
        PayrollDeductions result = PayrollCalculator.Calculate(Input(0m), new PayrollYearToDate(), Rates());

        Assert.Equal(0m, result.CppEmployee);
        Assert.Equal(0m, result.EiEmployee);
        Assert.Equal(0m, result.NetPay);
    }

    [Fact]
    public void LowIncome_ProducesNoTaxRatherThanNegativeTax()
    {
        // Well under the basic personal amount, so the credits exceed the tax.
        PayrollDeductions result = PayrollCalculator.Calculate(Input(200m), new PayrollYearToDate(), Rates());

        Assert.Equal(0m, result.FederalTax);
        Assert.Equal(0m, result.ProvincialTax);
    }

    [Fact]
    public void UnsupportedProvince_ThrowsRatherThanGuessing()
    {
        PayrollInput input = Input(2000m);
        input.Province = "ON";

        Assert.Throws<NotSupportedException>(
            () => PayrollCalculator.Calculate(input, new PayrollYearToDate(), Rates()));
    }

    [Fact]
    public void EmployerEiIsHigherThanTheEmployeePremium()
    {
        PayrollRateTable rates = Rates();

        PayrollDeductions result = PayrollCalculator.Calculate(Input(2000m), new PayrollYearToDate(), rates);

        Assert.Equal(Math.Round(result.EiEmployee * rates.Ei.EmployerMultiplier, 2), result.EiEmployer);
    }

    [Fact]
    public void NetPayIsGrossLessEveryEmployeeDeduction()
    {
        PayrollDeductions r = PayrollCalculator.Calculate(Input(2500m), new PayrollYearToDate(), Rates());

        decimal expected = r.GrossPay - r.CppEmployee - r.Cpp2Employee - r.EiEmployee - r.FederalTax - r.ProvincialTax;
        Assert.Equal(expected, r.NetPay);
    }

    [Fact]
    public void TotalCostIsGrossPlusEmployerContributions()
    {
        PayrollDeductions r = PayrollCalculator.Calculate(Input(2500m), new PayrollYearToDate(), Rates());

        Assert.Equal(r.GrossPay + r.CppEmployer + r.Cpp2Employer + r.EiEmployer, r.TotalCost);
    }

    #endregion

    #region PDOC fixtures, captured from CRA's own calculator

    /// <summary>
    /// Every figure CRA's Payroll Deductions Online Calculator produced for these inputs,
    /// Alberta, paid 2026-08-14, claim code 1 (16,452 federal and 22,769 provincial).
    ///
    /// This is the only test here that proves agreement with CRA rather than internal
    /// consistency. It found three real defects that every other test passed straight over:
    /// the CPP enhancement being deducted from income rather than credited, EI capping on the
    /// premium rather than on insurable earnings, and the K2 credit ignoring contributions
    /// already made earlier in the year.
    ///
    /// gross, periods, ytd(pensionable, cpp, insurable, ei), then expected
    /// federal, provincial, cpp, cpp2, ei, net.
    /// </summary>
    public static IEnumerable<object[]> PdocCases =>
    [
        ["biweekly 2000",        2000m, 26,     0m,    0m,     0m,    0m,  163.23m,  78.45m, 110.99m,   0m, 32.60m, 1614.73m],
        ["biweekly 3500",        3500m, 26,     0m,    0m,     0m,    0m,  442.63m, 215.21m, 200.24m,   0m, 57.05m, 2584.87m],
        ["weekly 1000",          1000m, 52,     0m,    0m,     0m,    0m,   81.61m,  39.23m,  55.50m,   0m, 16.30m,  807.36m],
        ["monthly 5000",         5000m, 12,     0m,    0m,     0m,    0m,  444.86m, 219.28m, 280.15m,   0m, 81.50m, 3974.21m],
        ["semi-monthly 2500",    2500m, 24,     0m,    0m,     0m,    0m,  222.43m, 109.64m, 140.07m,   0m, 40.75m, 1987.11m],
        ["biweekly 4000 at cap", 4000m, 26, 74000m, 4200m, 67500m, 1100m,  523.09m, 254.47m,  30.45m, 136m, 23.07m, 3032.92m],
        ["biweekly 500 at cap",   500m, 26, 74000m, 4200m, 67500m, 1100m,    0.00m,   0.00m,  21.74m,   0m,  8.15m,  470.11m],
        ["biweekly 8000 at cap", 8000m, 26, 74000m, 4200m, 67500m, 1100m, 1507.74m, 679.59m,  30.45m, 296m, 23.07m, 5463.15m],
    ];

    [Theory]
    [MemberData(nameof(PdocCases))]
    public void Deductions_MatchCraOnlineCalculator(
        string label, decimal gross, int periods,
        decimal ytdPensionable, decimal ytdCpp, decimal ytdInsurable, decimal ytdEi,
        decimal federal, decimal provincial, decimal cpp, decimal cpp2, decimal ei, decimal net)
    {
        PayrollDeductions r = PayrollCalculator.Calculate(
            new PayrollInput { GrossPay = gross, Province = "AB", PayPeriodsPerYear = periods },
            new PayrollYearToDate
            {
                PensionableEarnings = ytdPensionable,
                CppEmployee = ytdCpp,
                InsurableEarnings = ytdInsurable,
                EiEmployee = ytdEi,
            },
            Rates());

        Assert.Equal(cpp, r.CppEmployee);
        Assert.Equal(cpp2, r.Cpp2Employee);
        Assert.Equal(ei, r.EiEmployee);
        Assert.Equal(federal, r.FederalTax);
        Assert.Equal(provincial, r.ProvincialTax);
        Assert.Equal(net, r.NetPay);
    }

    #endregion
}
