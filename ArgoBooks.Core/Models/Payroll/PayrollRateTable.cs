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
    /// <summary>
    /// Quebec, which is not a province entry because it is not a province-shaped problem.
    /// Revenu Quebec runs its own pension plan, its own parental insurance plan and its own
    /// income tax formula, and CRA reduces federal tax by an abatement to make room for it.
    /// Null until the edition carries Quebec data.
    /// </summary>
    [JsonPropertyName("quebec")]
    public QuebecRates? Quebec { get; set; }

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
/// A personal amount, with the income range over which it is reduced.
///
/// Only <see cref="Maximum"/> is read. The phase-out figures are recorded because T4127
/// publishes them and the rate files carry them, but payroll deliberately does NOT apply the
/// reduction: an employer withholds against the figure the employee wrote on their TD1, and
/// reflecting the high-income reduction is the employee's job when completing that form. CRA's
/// own calculator behaves the same way, which the PDOC fixtures confirm. See the comment in
/// PayrollCalculator where the claim amount is chosen.
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

    /// <summary>
    /// Annual taxable income at or below which a bonus is taxed at a flat rate instead of by
    /// the two-step calculation. T4127 states this as $5,000.
    /// </summary>
    [JsonPropertyName("flatBonusCeiling")]
    public decimal FlatBonusCeiling { get; set; } = 5000m;

    /// <summary>
    /// The flat rate applied under <see cref="FlatBonusCeiling"/>. T4127 states 15%, and states
    /// it as a single combined figure rather than a federal and a provincial part.
    ///
    /// Defaulted in code as well as carried in the rate file so that an edition published
    /// without it keeps working, and so that a change to it is a rate-file update rather than a
    /// release. It is deliberately NOT tied to the lowest bracket rate: those happen to have
    /// been equal historically and have since diverged.
    /// </summary>
    [JsonPropertyName("flatBonusRate")]
    public decimal FlatBonusRate { get; set; } = 0.15m;
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

    /// <summary>
    /// Yukon is the only jurisdiction that mirrors the federal Canada Employment Amount as a
    /// provincial credit. Zero everywhere else, which makes the term vanish rather than needing
    /// a null check.
    /// </summary>
    [JsonPropertyName("canadaEmploymentAmount")]
    public decimal CanadaEmploymentAmount { get; set; }

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

/// <summary>
/// A provincial low-income tax reduction. Ontario and British Columbia both have one and they
/// are different shapes, so this carries the fields for both and the calculator picks by
/// whether a phase-out is present.
///
/// Ontario doubles a personal amount and subtracts the tax already worked out, so the credit
/// disappears once tax exceeds twice the amount. British Columbia gives a flat credit that
/// tapers away over an income band instead.
/// </summary>
public class TaxReductionRates
{
    /// <summary>Ontario: the amount that gets doubled. BC: the flat credit before tapering.</summary>
    [JsonPropertyName("basic")]
    public decimal Basic { get; set; }

    /// <summary>Ontario only. Added to <see cref="Basic"/> for each dependant claimed.</summary>
    [JsonPropertyName("perDependant")]
    public decimal PerDependant { get; set; }

    /// <summary>
    /// BC only. Annual income at which the credit starts tapering. Zero selects Ontario's
    /// shape, which is what every other field here assumes.
    /// </summary>
    [JsonPropertyName("phaseOutStart")]
    public decimal PhaseOutStart { get; set; }

    /// <summary>BC only. Rate at which the credit tapers above <see cref="PhaseOutStart"/>.</summary>
    [JsonPropertyName("phaseOutRate")]
    public decimal PhaseOutRate { get; set; }

    /// <summary>
    /// BC only. Income above which no credit is given at all. CRA states this exists to stop
    /// the credit reaching anyone over the legislated maximum, so it is a hard cut-off rather
    /// than the point where the taper happens to reach zero.
    /// </summary>
    [JsonPropertyName("phaseOutEnd")]
    public decimal PhaseOutEnd { get; set; }
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


/// <summary>
/// Everything Quebec needs that the rest of Canada does not.
///
/// QPP and QPP2 reuse the CPP shapes because the arithmetic is identical and only the
/// constants differ: QPP is 6.30% against CPP's 5.95%, split as a 5.30% base plus a 1.00%
/// first additional contribution rather than CPP's 4.95 and 1.00.
/// </summary>
public class QuebecRates
{
    [JsonPropertyName("qpp")]
    public CppRates Qpp { get; set; } = new();

    [JsonPropertyName("qpp2")]
    public Cpp2Rates Qpp2 { get; set; } = new();

    [JsonPropertyName("qpip")]
    public QpipRates Qpip { get; set; } = new();

    [JsonPropertyName("brackets")]
    public List<TaxBracket> Brackets { get; set; } = [];

    /// <summary>Personal credits from form TP-1015.3-V. The guide calls this E.</summary>
    [JsonPropertyName("basicPersonalAmount")]
    public decimal BasicPersonalAmount { get; set; }

    /// <summary>
    /// The rate personal credits are relieved at. Quebec uses 14%, which happens to equal the
    /// lowest bracket rate but is stated separately in the guide and could diverge.
    /// </summary>
    [JsonPropertyName("creditRate")]
    public decimal CreditRate { get; set; }

    /// <summary>
    /// The deduction for workers: a share of pay that comes off income before tax, capped for
    /// the year. Quebec only, with no federal equivalent at all.
    /// </summary>
    [JsonPropertyName("workerDeductionRate")]
    public decimal WorkerDeductionRate { get; set; }

    [JsonPropertyName("workerDeductionMaxAnnual")]
    public decimal WorkerDeductionMaxAnnual { get; set; }

    /// <summary>
    /// The share of federal tax CRA gives up for Quebec residents, because Quebec collects its
    /// own. Applied to federal tax after it is worked out.
    /// </summary>
    [JsonPropertyName("federalAbatement")]
    public decimal FederalAbatement { get; set; }

    /// <summary>
    /// Quebec's EI maximum. The RATE lives on EiRates as QuebecRateEmployee because it is a
    /// federal programme, but the maximum differs and has nowhere else to go.
    /// </summary>
    [JsonPropertyName("eiMaxPremiumEmployee")]
    public decimal EiMaxPremiumEmployee { get; set; }

    /// <summary>
    /// Annual remuneration, INCLUDING the bonus, at or below which Revenu Quebec has a bonus
    /// taxed at a flat rate rather than by the formula. $18,952 for 2026.
    ///
    /// Much higher than CRA's equivalent ceiling and not the same rule: this one is checked
    /// against remuneration, CRA's against annual taxable income.
    /// </summary>
    [JsonPropertyName("flatBonusCeiling")]
    public decimal FlatBonusCeiling { get; set; } = 18952m;

    /// <summary>The flat Quebec rate under <see cref="FlatBonusCeiling"/>.</summary>
    [JsonPropertyName("flatBonusRate")]
    public decimal FlatBonusRate { get; set; } = 0.07m;

    /// <summary>
    /// The FEDERAL flat rate on a bonus for a Quebec employee, under the federal ceiling on
    /// <see cref="FederalRates.FlatBonusCeiling"/>. T4127 publishes 10% here where it publishes
    /// 15% elsewhere, and 10% is a stated figure rather than 15% net of the abatement.
    /// </summary>
    [JsonPropertyName("federalFlatBonusRate")]
    public decimal FederalFlatBonusRate { get; set; } = 0.10m;
}

/// <summary>Quebec parental insurance plan. Has no equivalent anywhere else in Canada.</summary>
public class QpipRates
{
    [JsonPropertyName("rateEmployee")]
    public decimal RateEmployee { get; set; }

    [JsonPropertyName("rateEmployer")]
    public decimal RateEmployer { get; set; }

    [JsonPropertyName("maxInsurableEarnings")]
    public decimal MaxInsurableEarnings { get; set; }

    [JsonPropertyName("maxPremiumEmployee")]
    public decimal MaxPremiumEmployee { get; set; }

    [JsonPropertyName("maxPremiumEmployer")]
    public decimal MaxPremiumEmployer { get; set; }
}
