using ArgoBooks.Core.Models.Payroll;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Calculates source deductions for one employee for one pay period, following the structure
/// of CRA's T4127 Payroll Deductions Formulas.
///
/// Deliberately a pure function: no file access, no network, no clock, no company data. This
/// is the one part of payroll that must be provably correct, and purity is what lets it be
/// tested against CRA's published figures without standing up an application.
///
/// Year-to-date figures are an input rather than something looked up, because CPP, CPP2 and
/// EI all stop at annual maximums. Passing them in keeps the engine free of dependencies and
/// makes the maximum-reached cases trivial to test.
///
/// Quebec is not handled here. Revenu Québec does not use CRA's rate-and-constant structure,
/// so it needs its own implementation behind the same interface rather than branches inside
/// this one.
/// </summary>
public static class PayrollCalculator
{
    /// <summary>
    /// Deductions for a single pay period.
    /// </summary>
    /// <param name="input">Gross pay and the employee's circumstances for this period.</param>
    /// <param name="ytd">What has already been deducted this calendar year.</param>
    /// <param name="rates">The CRA edition in force on the pay date.</param>
    public static PayrollDeductions Calculate(PayrollInput input, PayrollYearToDate ytd, PayrollRateTable rates)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(ytd);
        ArgumentNullException.ThrowIfNull(rates);

        if (!rates.Provinces.TryGetValue(input.Province, out ProvincialRates? province))
        {
            throw new NotSupportedException(
                $"No payroll rate table for province '{input.Province}' in edition {rates.EditionId}.");
        }

        int periods = input.PayPeriodsPerYear;
        decimal gross = input.GrossPay;

        decimal cpp = CppForPeriod(gross, periods, ytd, rates, input.IsCppExempt,
                                   out decimal cpp2, out decimal cppUnrounded);
        decimal ei = EiForPeriod(gross, ytd, rates, input.IsEiExempt);

        // The CPP enhancement, the part of the rate above the historical 4.95% base, is not a
        // tax credit. It is deducted from income before tax is worked out, and CPP2 is
        // deducted the same way. Only the base portion feeds the K2 credit below.
        //
        // Worth stating because the two effects cancel exactly inside the lowest tax bracket,
        // so getting this wrong looks correct until an employee earns enough to reach the
        // second bracket, and then the tax is out by a few dollars every period.
        decimal enhancedShare = rates.Cpp.RateEmployee > 0
            ? (rates.Cpp.RateEmployee - rates.Cpp.BaseRateEmployee) / rates.Cpp.RateEmployee
            : 0m;
        decimal enhancedCpp = Round(cpp * enhancedShare);

        // Annualised income. T4127 calls this A.
        decimal annual = (gross - enhancedCpp - cpp2) * periods;

        // Annual contributions, needed for the K2 credit. Capped at the annual maximum
        // because that is where deductions stop, and reduced to the base portion because the
        // enhancement was already relieved as a deduction above.
        // What the employee will have contributed across the whole year, not just this period
        // annualised. Ignoring the year-to-date figures understates the credit for anyone who
        // has already reached a ceiling, because their remaining periods deduct little or
        // nothing while the year's total is still the full maximum.
        // Annualised from the UNROUNDED contribution. Rounding each period to the cent first
        // and then multiplying by 26 compounds the rounding into several cents a year, which
        // is enough to move the final tax by a cent.
        decimal annualCpp = Math.Min(ytd.CppEmployee + cppUnrounded * periods, rates.Cpp.MaxContributionEmployee)
                            * (1 - enhancedShare);
        decimal annualEi = Math.Min(ytd.EiEmployee + ei * periods, rates.Ei.MaxPremiumEmployee);

        decimal federalAnnual = FederalTaxForYear(annual, annualCpp, annualEi, input, rates);
        decimal provincialAnnual = ProvincialTaxForYear(annual, annualCpp, annualEi, input, rates, province);

        decimal federal = Round(Math.Max(0, federalAnnual) / periods);
        decimal provincial = Round(Math.Max(0, provincialAnnual) / periods);

        return new PayrollDeductions
        {
            GrossPay = gross,
            CppEmployee = cpp,
            CppEmployer = cpp,
            Cpp2Employee = cpp2,
            Cpp2Employer = cpp2,
            EiEmployee = ei,
            EiEmployer = Round(ei * rates.Ei.EmployerMultiplier),
            FederalTax = federal,
            ProvincialTax = provincial,
        };
    }

    /// <summary>
    /// Base CPP for the period, and second additional CPP as an out parameter.
    ///
    /// The basic exemption is annual and spread across pay periods. Contributions stop once
    /// the annual maximum is reached, which is why the year-to-date figure is required rather
    /// than optional.
    /// </summary>
    private static decimal CppForPeriod(
        decimal gross, int periods, PayrollYearToDate ytd, PayrollRateTable rates, bool exempt,
        out decimal cpp2, out decimal cppUnrounded)
    {
        cpp2 = 0m;
        cppUnrounded = 0m;
        if (exempt || gross <= 0)
        {
            return 0m;
        }

        decimal periodExemption = rates.Cpp.BasicExemptionAnnual / periods;
        decimal pensionable = Math.Max(0, gross - periodExemption);

        cppUnrounded = pensionable * rates.Cpp.RateEmployee;
        decimal cpp = Round(cppUnrounded);
        decimal remaining = Math.Max(0, rates.Cpp.MaxContributionEmployee - ytd.CppEmployee);
        cpp = Math.Min(cpp, remaining);
        cppUnrounded = Math.Min(cppUnrounded, remaining);

        // CPP2 applies to earnings between the two ceilings, so it only begins once the
        // employee's pensionable earnings for the year pass the first ceiling.
        decimal earnedBefore = ytd.PensionableEarnings;
        decimal earnedAfter = earnedBefore + gross;
        decimal above = Math.Max(0, Math.Min(earnedAfter, rates.Cpp2.YampeCeiling) - Math.Max(earnedBefore, rates.Cpp.YmpeCeiling));

        if (above > 0)
        {
            decimal remaining2 = Math.Max(0, rates.Cpp2.MaxContributionEmployee - ytd.Cpp2Employee);
            cpp2 = Math.Min(Round(above * rates.Cpp2.RateEmployee), remaining2);
        }

        return cpp;
    }

    private static decimal EiForPeriod(decimal gross, PayrollYearToDate ytd, PayrollRateTable rates, bool exempt)
    {
        if (exempt || gross <= 0)
        {
            return 0m;
        }

        // Capped on the premium rather than on remaining insurable earnings. Those two are
        // only equivalent when the year-to-date figures agree perfectly, and they rarely do
        // in real records. CRA's calculator caps the premium, so the last pay period of the
        // year lands exactly on the annual maximum.
        decimal premium = Round(gross * rates.Ei.RateEmployee);
        decimal remaining = Math.Max(0, rates.Ei.MaxPremiumEmployee - ytd.EiEmployee);
        return Math.Min(premium, remaining);
    }

    /// <summary>
    /// Annual federal tax. T4127 expresses this as T3 = (R x A) - K - K1 - K2 - K4.
    ///
    /// This shape is confirmed against CRA's published worked example, which it reproduces to
    /// the cent, including K1 as the lowest rate times the basic personal amount and K4 as the
    /// lowest rate times the Canada Employment Amount.
    /// </summary>
    private static decimal FederalTaxForYear(
        decimal annual, decimal annualCpp, decimal annualEi, PayrollInput input, PayrollRateTable rates)
    {
        FederalRates federal = rates.Federal;
        (decimal rate, decimal k) = BracketFor(federal.Brackets, annual);
        decimal lowest = federal.LowestRateForCredits;

        // The employee's TD1 claim, or the full basic personal amount when none is on file.
        // Deliberately not phased down for income: an employer applies the figure the employee
        // wrote on their TD1, and reflecting the high-income reduction is the employee's job
        // when completing that form. CRA's own calculator behaves this way.
        decimal claim = input.FederalClaimAmount > 0
            ? input.FederalClaimAmount
            : federal.BasicPersonalAmount.Maximum;

        decimal k1 = lowest * claim;
        decimal k2 = lowest * (annualCpp + annualEi);
        decimal k4 = lowest * Math.Min(annual, federal.CanadaEmploymentAmount);

        return rate * annual - k - k1 - k2 - k4;
    }

    /// <summary>
    /// Annual provincial tax, the same shape as federal with provincial constants, then
    /// Ontario's surtax and health premium added and any tax reduction subtracted.
    /// </summary>
    private static decimal ProvincialTaxForYear(
        decimal annual, decimal annualCpp, decimal annualEi,
        PayrollInput input, PayrollRateTable rates, ProvincialRates province)
    {
        (decimal rate, decimal k) = BracketFor(province.Brackets, annual);
        decimal lowest = province.Brackets.Count > 0 ? province.Brackets[0].Rate : 0m;

        decimal claim = input.ProvincialClaimAmount > 0
            ? input.ProvincialClaimAmount
            : province.BasicPersonalAmount.Maximum;

        decimal tax = rate * annual - k - lowest * claim - lowest * (annualCpp + annualEi);
        tax = Math.Max(0, tax);

        // Ontario charges its surtax on provincial tax, not on income, so it comes after.
        if (province.Surtax is { } surtax)
        {
            for (int i = 0; i < surtax.Thresholds.Count && i < surtax.Rates.Count; i++)
            {
                if (tax > surtax.Thresholds[i])
                {
                    tax += (tax - surtax.Thresholds[i]) * surtax.Rates[i];
                }
            }
        }

        if (province.TaxReduction is { } reduction)
        {
            // The reduction cannot create a refund; it only ever cancels tax owing.
            decimal available = (reduction.Basic + reduction.PerDependant * input.Dependants) * 2 - tax;
            tax = Math.Max(0, tax - Math.Max(0, Math.Min(tax, available)));
        }

        if (province.HealthPremium is { Count: > 0 } bands)
        {
            tax += HealthPremiumFor(annual, bands);
        }

        return tax;
    }

    private static decimal HealthPremiumFor(decimal annual, List<HealthPremiumBand> bands)
    {
        foreach (HealthPremiumBand band in bands)
        {
            bool inBand = annual > band.IncomeOver && (band.IncomeUpTo == null || annual <= band.IncomeUpTo);
            if (!inBand)
            {
                continue;
            }

            decimal premium = band.Premium + (annual - band.IncomeOver) * band.RateOnExcess;
            return Math.Min(premium, band.MaxPremium);
        }

        return 0m;
    }

    /// <summary>The rate and constant for the bracket containing this annual income.</summary>
    private static (decimal Rate, decimal ConstantK) BracketFor(List<TaxBracket> brackets, decimal annual)
    {
        foreach (TaxBracket bracket in brackets)
        {
            if (bracket.UpTo == null || annual <= bracket.UpTo)
            {
                return (bracket.Rate, bracket.ConstantK);
            }
        }

        return brackets.Count > 0
            ? (brackets[^1].Rate, brackets[^1].ConstantK)
            : (0m, 0m);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>What is known about one employee for one pay period.</summary>
public class PayrollInput
{
    public decimal GrossPay { get; set; }

    /// <summary>Two letter province of employment, which decides the tax table.</summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>52 weekly, 26 biweekly, 24 semi-monthly, 12 monthly.</summary>
    public int PayPeriodsPerYear { get; set; } = 26;

    /// <summary>TD1 total. Zero means use the basic personal amount.</summary>
    public decimal FederalClaimAmount { get; set; }

    /// <summary>TD1P total. Zero means use the basic personal amount.</summary>
    public decimal ProvincialClaimAmount { get; set; }

    /// <summary>Only used by provinces whose tax reduction has a dependant component.</summary>
    public int Dependants { get; set; }

    public bool IsCppExempt { get; set; }

    public bool IsEiExempt { get; set; }
}

/// <summary>
/// Totals for the calendar year before this pay period. Required rather than optional,
/// because CPP, CPP2 and EI all stop at annual maximums and cannot be computed correctly
/// from a single period in isolation.
/// </summary>
public class PayrollYearToDate
{
    public decimal PensionableEarnings { get; set; }

    public decimal InsurableEarnings { get; set; }

    public decimal CppEmployee { get; set; }

    public decimal Cpp2Employee { get; set; }

    public decimal EiEmployee { get; set; }
}

/// <summary>
/// The result for one employee for one pay period. These values are stored on the pay run
/// when it is approved and never recalculated, so that a historical run always agrees with
/// the stub the employee was given.
/// </summary>
public class PayrollDeductions
{
    public decimal GrossPay { get; set; }

    public decimal CppEmployee { get; set; }

    public decimal CppEmployer { get; set; }

    public decimal Cpp2Employee { get; set; }

    public decimal Cpp2Employer { get; set; }

    public decimal EiEmployee { get; set; }

    public decimal EiEmployer { get; set; }

    public decimal FederalTax { get; set; }

    public decimal ProvincialTax { get; set; }

    /// <summary>What the employee receives.</summary>
    public decimal NetPay =>
        GrossPay - CppEmployee - Cpp2Employee - EiEmployee - FederalTax - ProvincialTax;

    /// <summary>What must be remitted to CRA for this employee: withheld plus employer share.</summary>
    public decimal TotalRemittance =>
        CppEmployee + CppEmployer + Cpp2Employee + Cpp2Employer
        + EiEmployee + EiEmployer + FederalTax + ProvincialTax;

    /// <summary>What this employee actually costs, gross plus the employer contributions.</summary>
    public decimal TotalCost =>
        GrossPay + CppEmployer + Cpp2Employer + EiEmployer;
}
