using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// A bonus must be taxed once, in the period it is paid.
///
/// T4127 chapter 4 step 1 gives the regular periodic base as
/// A = [P x (I - F - F2 - F5A - U1)] - HD - F1, with no B1 term. Year-to-date bonuses appear only
/// in the bonus calculation, where step 1 includes the payment being made now and step 2 does
/// not, so B1 sits on both sides of the subtraction.
///
/// Folding B1 into the periodic base annualised a bonus that had already been taxed in full when
/// it was paid, so every remaining period of the year withheld against it again.
/// </summary>
public class PayrollBonusYearTests
{
    private static readonly PayrollRateTable Rates =
        new PayrollRateService().GetForDate(new DateTime(2026, 8, 14))!;

    private const int Periods = 26;

    private static PayrollInput Input(decimal gross, decimal bonus) => new()
    {
        GrossPay = gross,
        NonPeriodicPay = bonus,
        Province = "AB",
        PayPeriodsPerYear = Periods,
    };

    /// <summary>
    /// Runs a full year: a bonus in period 1, then ordinary pay for the rest, accumulating
    /// year-to-date the way PayrollService does.
    /// </summary>
    private static decimal TaxWithheldOverAYear(decimal regular, decimal bonus)
    {
        var ytd = new PayrollYearToDate();
        decimal total = 0m;

        for (int period = 0; period < Periods; period++)
        {
            decimal thisBonus = period == 0 ? bonus : 0m;
            decimal gross = regular + thisBonus;

            PayrollDeductions d = PayrollCalculator.Calculate(Input(gross, thisBonus), ytd, Rates);

            total += d.FederalTax + d.ProvincialTax;

            ytd.PensionableEarnings += gross;
            ytd.CppEmployee += d.CppEmployee;
            ytd.EiEmployee += d.EiEmployee;
            ytd.NonPeriodicPay += thisBonus;
        }

        return total;
    }

    /// <summary>
    /// The year's withholding on salary plus a bonus must land near the tax on that same total
    /// paid evenly, not hundreds of dollars above it.
    ///
    /// A tolerance rather than an equality: annualising twenty-six periods and a one-off payment
    /// through separate roundings will never agree to the cent, and CRA's method is a withholding
    /// estimate rather than a return.
    /// </summary>
    [Fact]
    public void ABonusIsTaxedOnce_NotAgainEveryRemainingPeriod()
    {
        const decimal regular = 2400m;
        const decimal bonus = 5000m;

        decimal withBonus = TaxWithheldOverAYear(regular, bonus);

        // The same annual income with no bonus at all, plus the bonus spread evenly, is the
        // yardstick: both describe a year earning 62,400 + 5,000.
        decimal evenly = TaxWithheldOverAYear(regular + (bonus / Periods), 0m);

        decimal difference = Math.Abs(withBonus - evenly);

        Assert.True(difference < 200m,
            $"withheld {withBonus:N2} against {evenly:N2} for the same annual income, "
            + $"a difference of {difference:N2}. Before the fix this was over 1,100.");
    }

    /// <summary>Without a bonus anywhere, year-to-date must not drift the periodic tax at all.</summary>
    [Fact]
    public void OrdinaryPayWithholdsTheSameEveryPeriod()
    {
        var ytd = new PayrollYearToDate();
        decimal first = 0m;

        for (int period = 0; period < 6; period++)
        {
            PayrollDeductions d = PayrollCalculator.Calculate(Input(2400m, 0m), ytd, Rates);

            if (period == 0)
            {
                first = d.FederalTax + d.ProvincialTax;
            }
            else
            {
                Assert.Equal(first, d.FederalTax + d.ProvincialTax);
            }

            ytd.PensionableEarnings += 2400m;
            ytd.CppEmployee += d.CppEmployee;
            ytd.EiEmployee += d.EiEmployee;
        }
    }
}
