namespace ArgoBooks.Core.Models.Payroll;

/// <summary>
/// Every rate, threshold and constant needed to calculate source deductions for one CRA
/// edition. CRA publishes these twice a year, effective January 1 and July 1, and a July
/// edition can carry prorated amounts that only apply to the second half of the year, so a
/// pay run must use the edition in force on its pay date rather than "this year's rates".
///
/// Deliberately not part of company data. It is identical for every company and changes on
/// CRA's schedule rather than the user's, so it is downloaded and cached the same way
/// language files are. Shipping no rates inside the app means a rate change is a file
/// upload rather than a release, which matters when the deadline is fixed and twice yearly.
/// </summary>
public class PayrollRateTable
{
    /// <summary>Identifies the CRA edition, for example "2026-07" for the 123rd edition.</summary>
    [JsonPropertyName("editionId")]
    public string EditionId { get; set; } = string.Empty;

    /// <summary>First pay date this edition applies to.</summary>
    [JsonPropertyName("effectiveFrom")]
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Last pay date this edition applies to.</summary>
    [JsonPropertyName("effectiveTo")]
    public DateTime EffectiveTo { get; set; }

    [JsonPropertyName("federal")]
    public FederalRates Federal { get; set; } = new();

    [JsonPropertyName("cpp")]
    public CppRates Cpp { get; set; } = new();

    [JsonPropertyName("cpp2")]
    public Cpp2Rates Cpp2 { get; set; } = new();

    [JsonPropertyName("ei")]
    public EiRates Ei { get; set; } = new();

    /// <summary>Keyed by two letter code: AB, BC, ON and so on.</summary>
    [JsonPropertyName("provinces")]
    public Dictionary<string, ProvincialRates> Provinces { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when this edition covers the given pay date.</summary>
    public bool Covers(DateTime payDate)
    {
        DateTime date = payDate.Date;
        return date >= EffectiveFrom.Date && date <= EffectiveTo.Date;
    }
}

/// <summary>
/// One row of a progressive tax table. T4127 expresses tax as (rate x annual income) minus a
/// constant, so each bracket carries the constant that makes it continuous with the one below.
/// </summary>
public class TaxBracket
{
    /// <summary>Upper bound of this bracket. Null means the top bracket, with no ceiling.</summary>
    [JsonPropertyName("upTo")]
    public decimal? UpTo { get; set; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    /// <summary>T4127's K (federal) or KP (provincial).</summary>
    [JsonPropertyName("constantK")]
    public decimal ConstantK { get; set; }
}

/// <summary>
/// A personal amount that is reduced as income rises. Where an amount is fixed, Maximum and
/// Minimum are equal and the phase-out bounds are ignored.
/// </summary>
public class PersonalAmount
{
    [JsonPropertyName("maximum")]
    public decimal Maximum { get; set; }

    [JsonPropertyName("minimum")]
    public decimal Minimum { get; set; }

    [JsonPropertyName("phaseoutStart")]
    public decimal PhaseoutStart { get; set; }

    [JsonPropertyName("phaseoutEnd")]
    public decimal PhaseoutEnd { get; set; }

    /// <summary>
    /// The amount for a given annual net income, reduced linearly between the phase-out
    /// bounds. A fixed amount is expressed as Maximum equal to Minimum, which this returns
    /// unchanged at every income.
    /// </summary>
    public decimal ForIncome(decimal netIncome)
    {
        if (Maximum == Minimum || netIncome <= PhaseoutStart)
        {
            return Maximum;
        }

        if (netIncome >= PhaseoutEnd)
        {
            return Minimum;
        }

        decimal range = PhaseoutEnd - PhaseoutStart;
        if (range <= 0)
        {
            return Maximum;
        }

        return Maximum - (netIncome - PhaseoutStart) * ((Maximum - Minimum) / range);
    }
}

public class FederalRates
{
    [JsonPropertyName("brackets")]
    public List<TaxBracket> Brackets { get; set; } = [];

    /// <summary>T4127's BPAF, which phases down above the third bracket threshold.</summary>
    [JsonPropertyName("basicPersonalAmount")]
    public PersonalAmount BasicPersonalAmount { get; set; } = new();

    /// <summary>Feeds T4127's K4.</summary>
    [JsonPropertyName("canadaEmploymentAmount")]
    public decimal CanadaEmploymentAmount { get; set; }

    /// <summary>The rate credits are converted at, which is the lowest bracket rate.</summary>
    [JsonPropertyName("lowestRateForCredits")]
    public decimal LowestRateForCredits { get; set; }
}

public class CppRates
{
    [JsonPropertyName("rateEmployee")]
    public decimal RateEmployee { get; set; }

    [JsonPropertyName("rateEmployer")]
    public decimal RateEmployer { get; set; }

    /// <summary>
    /// The base portion of the rate, historically 4.95%. The remainder is the CPP
    /// enhancement, which is treated completely differently by the tax calculation: the
    /// enhanced part is deducted from income, while only the base part feeds the K2 credit.
    /// </summary>
    [JsonPropertyName("baseRateEmployee")]
    public decimal BaseRateEmployee { get; set; }

    /// <summary>Annual amount exempt from contributions, prorated across pay periods.</summary>
    [JsonPropertyName("basicExemptionAnnual")]
    public decimal BasicExemptionAnnual { get; set; }

    /// <summary>Year's maximum pensionable earnings.</summary>
    [JsonPropertyName("ympeCeiling")]
    public decimal YmpeCeiling { get; set; }

    [JsonPropertyName("maxContributionEmployee")]
    public decimal MaxContributionEmployee { get; set; }
}

/// <summary>Second additional CPP, on earnings between the YMPE and YAMPE ceilings.</summary>
public class Cpp2Rates
{
    [JsonPropertyName("rateEmployee")]
    public decimal RateEmployee { get; set; }

    [JsonPropertyName("rateEmployer")]
    public decimal RateEmployer { get; set; }

    /// <summary>Year's additional maximum pensionable earnings.</summary>
    [JsonPropertyName("yampeCeiling")]
    public decimal YampeCeiling { get; set; }

    [JsonPropertyName("maxContributionEmployee")]
    public decimal MaxContributionEmployee { get; set; }
}

public class EiRates
{
    [JsonPropertyName("rateEmployee")]
    public decimal RateEmployee { get; set; }

    /// <summary>Employer premium as a multiple of the employee's, normally 1.4.</summary>
    [JsonPropertyName("employerMultiplier")]
    public decimal EmployerMultiplier { get; set; }

    [JsonPropertyName("maxInsurableEarnings")]
    public decimal MaxInsurableEarnings { get; set; }

    [JsonPropertyName("maxPremiumEmployee")]
    public decimal MaxPremiumEmployee { get; set; }

    /// <summary>Quebec pays a lower EI rate because QPIP covers parental benefits.</summary>
    [JsonPropertyName("quebecRateEmployee")]
    public decimal QuebecRateEmployee { get; set; }

    [JsonPropertyName("quebecEmployerMultiplier")]
    public decimal QuebecEmployerMultiplier { get; set; }
}

public class ProvincialRates
{
    [JsonPropertyName("brackets")]
    public List<TaxBracket> Brackets { get; set; } = [];

    [JsonPropertyName("basicPersonalAmount")]
    public PersonalAmount BasicPersonalAmount { get; set; } = new();

    /// <summary>Ontario is the only province with a surtax. Null everywhere else.</summary>
    [JsonPropertyName("surtax")]
    public SurtaxRates? Surtax { get; set; }

    /// <summary>Ontario and British Columbia only. Null elsewhere.</summary>
    [JsonPropertyName("taxReduction")]
    public TaxReductionRates? TaxReduction { get; set; }

    /// <summary>Ontario only. Null elsewhere.</summary>
    [JsonPropertyName("healthPremium")]
    public List<HealthPremiumBand>? HealthPremium { get; set; }
}

/// <summary>Ontario's surtax, charged on provincial tax rather than on income.</summary>
public class SurtaxRates
{
    [JsonPropertyName("thresholds")]
    public List<decimal> Thresholds { get; set; } = [];

    [JsonPropertyName("rates")]
    public List<decimal> Rates { get; set; } = [];
}

public class TaxReductionRates
{
    [JsonPropertyName("basic")]
    public decimal Basic { get; set; }

    [JsonPropertyName("perDependant")]
    public decimal PerDependant { get; set; }
}

/// <summary>
/// One band of Ontario's health premium: a flat amount plus a rate on the excess over the
/// band's floor, capped.
/// </summary>
public class HealthPremiumBand
{
    [JsonPropertyName("incomeOver")]
    public decimal IncomeOver { get; set; }

    [JsonPropertyName("incomeUpTo")]
    public decimal? IncomeUpTo { get; set; }

    [JsonPropertyName("premium")]
    public decimal Premium { get; set; }

    [JsonPropertyName("rateOnExcess")]
    public decimal RateOnExcess { get; set; }

    [JsonPropertyName("maxPremium")]
    public decimal MaxPremium { get; set; }
}
