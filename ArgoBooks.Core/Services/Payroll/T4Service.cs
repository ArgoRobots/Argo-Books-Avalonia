using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;

namespace ArgoBooks.Core.Services.Payroll;

/// <summary>
/// Builds a year's T4 return from the pay runs already recorded.
///
/// Everything here is derived. A T4 is a restatement of pay runs that were frozen when they
/// were approved, so nothing is stored: the return is assembled on demand and comes out
/// identical every time. Storing it would create a second copy that could drift from the
/// first, which is exactly the reconciliation failure T4s are supposed to prevent.
/// </summary>
public class T4Service
{
    private readonly PayrollRateService _rates;

    public T4Service(PayrollRateService? rates = null) => _rates = rates ?? new PayrollRateService();

    /// <summary>
    /// Assembles the return for a tax year. Draft runs are excluded, but a draft is also a
    /// reason to refuse to file at all, so callers should check <see cref="Validate"/> first
    /// rather than relying on the omission.
    /// </summary>
    public T4Return Build(CompanyData data, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(data);

        var company = data.Settings.Company;

        var t4 = new T4Return
        {
            TaxYear = taxYear,
            PayrollAccountNumber = company.PayrollAccountNumber ?? string.Empty,
            EmployerName = company.Name,
            EmployerAddress = new Models.Common.Address
            {
                Street = company.Address ?? string.Empty,
                City = company.City ?? string.Empty,
                State = company.ProvinceState ?? string.Empty,
                Country = string.IsNullOrWhiteSpace(company.Country) ? "CAN" : company.Country,
                ZipCode = company.PostalCode ?? string.Empty,
            },
            ContactName = company.PayrollContactName ?? string.Empty,
            ContactPhone = company.PayrollContactPhone ?? company.Phone ?? string.Empty,

            // Both for the T619 transmittal that wraps the submission rather than for any slip.
            ContactEmail = company.PayrollContactEmail ?? string.Empty,
            LanguageCode = string.Equals(data.Settings.Localization.Language, "French",
                StringComparison.OrdinalIgnoreCase) ? "F" : "E",
        };

        EarningsCeilings ceilings = EarningsCeilings.For(_rates, taxYear);

        // Everything except drafts, so a voided run and its reversal both count and cancel to
        // zero. Matches how the year-to-date figures are built.
        var lines = data.PayRuns
            .Where(r => r.Status != PayRunStatus.Draft && r.PayDate.Year == taxYear)
            .SelectMany(r => r.Lines)
            .ToList();

        foreach (var group in lines.GroupBy(l => l.EmployeeId))
        {
            Employee? employee = data.Employees.FirstOrDefault(e => e.Id == group.Key);
            if (employee == null)
            {
                continue;
            }

            T4Slip slip = BuildSlip(employee, group.ToList(), ceilings);

            // A slip whose every figure nets to zero is one whose runs were all voided. There
            // is nothing to report and CRA has no element that means "nil year".
            if (slip.EmploymentIncome > 0 || slip.IncomeTaxDeducted > 0)
            {
                t4.Slips.Add(slip);
            }
        }

        t4.Slips.Sort((a, b) => string.Compare(a.Surname, b.Surname, StringComparison.CurrentCultureIgnoreCase));
        return t4;
    }

    private static T4Slip BuildSlip(Employee employee, List<PayRunLine> lines, EarningsCeilings ceilings)
    {
        (string surname, string given, string initial) = SplitName(employee.Name);

        decimal gross = lines.Sum(l => l.GrossPay);
        bool quebec = string.Equals(employee.Province, "QC", StringComparison.OrdinalIgnoreCase);
        decimal qpip = lines.Sum(l => l.QpipEmployee);

        return new T4Slip
        {
            EmployeeId = employee.Id,
            Surname = surname,
            GivenName = given,
            Initial = initial,
            Sin = employee.Sin,
            EmployeeNumber = employee.EmployeeNumber,
            Address = employee.Address,
            ProvinceOfEmployment = employee.Province,
            IsQuebec = quebec,
            EmploymentIncome = gross,
            CppContributions = lines.Sum(l => l.CppEmployee),
            Cpp2Contributions = lines.Sum(l => l.Cpp2Employee),
            EiPremiums = lines.Sum(l => l.EiEmployee),

            // Box 22 is federal, provincial and territorial tax together, with one exception
            // stated in as many words by RC4120: "This includes the federal, provincial (except
            // Quebec), and territorial taxes that apply."
            //
            // Quebec income tax is withheld in the same pay run and stored in the same column,
            // but it is remitted to Revenu Quebec and reported on RL-1 box E. Adding it here
            // reports the same money on both slips, and the employee claims credit for it twice.
            IncomeTaxDeducted = lines.Sum(l => l.FederalTax)
                                + (quebec ? 0m : lines.Sum(l => l.ProvincialTax)),

            // Exempt employment reports nil earnings rather than omitting the box, so these
            // deliberately go to zero rather than to gross.
            //
            // Capped at the year's ceiling. Someone earning above it stopped contributing part
            // way through the year, and reporting their whole salary here would have CRA expect
            // contributions on money that was never pensionable or insurable. The figure looks
            // right either way, which is exactly why it needs pinning.
            InsurableEarnings = employee.IsEiExempt ? 0m : ceilings.CapEi(gross),
            PensionableEarnings = employee.IsCppExempt ? 0m : ceilings.CapPensionable(gross),

            QpipPremiums = qpip,

            // Tied to whether a premium was actually withheld rather than to the province alone.
            // RC4120 pairs the two: "If you report an amount in box 55, you have to report
            // insurable earnings using box 56", and box 28's PPIP tick means no premium was
            // withheld for the whole period. Earnings with no premium against them is the one
            // combination that cannot be true.
            //
            // RC4120 also carries a list under box 56 of when NOT to report it, which includes
            // "the insurable earnings are the same as the employment income in box 14" and "the
            // insurable earnings are over the maximum for the year". Taken literally that would
            // suppress box 56 in every case this app can produce, since QPIP eligible earnings
            // here are always gross capped at the ceiling. The two instructions contradict each
            // other and both are CRA's.
            //
            // Settled by the XML specification, which marks prov_insu_ern_amt optional rather
            // than required. So neither choice is rejected on submission, which leaves the box 55
            // pairing as the only one of the two phrased as an obligation, and reporting a
            // redundant figure as the cheaper way to be wrong. Reported.
            QpipInsurableEarnings = quebec && qpip > 0 ? ceilings.CapQpip(gross) : 0m,
            EmployerQpip = lines.Sum(l => l.QpipEmployer),

            CppExemptAllYear = employee.IsCppExempt,
            EiExemptAllYear = employee.IsEiExempt,
            DentalBenefit = employee.DentalBenefit,
            EmployerCpp = lines.Sum(l => l.CppEmployer),
            EmployerCpp2 = lines.Sum(l => l.Cpp2Employer),
            EmployerEi = lines.Sum(l => l.EiEmployer),
        };
    }

    /// <summary>
    /// Everything that would make CRA reject the filing, or make it wrong. Returned as
    /// messages rather than thrown, because the year end screen shows them all at once: an
    /// employer missing three SINs wants to see three, not to fix one and be told about the
    /// next.
    /// </summary>
    public static List<string> Validate(CompanyData data, T4Return t4)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(t4);

        var problems = new List<string>();

        // A draft run means the year is not finished being recorded. Filing now would produce
        // a T4 that disagrees with the books as soon as the draft is approved.
        int drafts = data.PayRuns.Count(r => r.Status == PayRunStatus.Draft && r.PayDate.Year == t4.TaxYear);
        if (drafts > 0)
        {
            problems.Add($"{drafts} pay run(s) in {t4.TaxYear} are still drafts. Approve or delete them before filing.");
        }

        if (!IsPayrollAccountNumber(t4.PayrollAccountNumber))
        {
            problems.Add("The payroll account number is missing or not in the form 000000000RP0000. "
                         + "It is on your CRA statement of account and is required on every slip.");
        }

        if (string.IsNullOrWhiteSpace(t4.EmployerName))
        {
            problems.Add("The company name is required on the T4 Summary.");
        }

        if (string.IsNullOrWhiteSpace(t4.ContactName))
        {
            problems.Add("A contact name is required on the T4 Summary, so CRA knows who to call.");
        }

        if (new string((t4.ContactPhone ?? string.Empty).Where(char.IsAsciiDigit).ToArray()).Length < 10)
        {
            problems.Add("A ten digit contact phone number is required on the T4 Summary.");
        }

        // Required by the T619 transmittal record wrapping the submission. Without it CRA rejects
        // the upload, so it is refused here rather than discovered at the deadline.
        if (!IsEmailAddress(t4.ContactEmail))
        {
            problems.Add("A contact email address is required. CRA uses it to tell you how the "
                         + "filing was processed, and the submission is rejected without one.");
        }

        foreach (T4Slip slip in t4.Slips)
        {
            string who = string.IsNullOrWhiteSpace(slip.Surname) ? slip.EmployeeId : slip.Surname;

            if (string.IsNullOrWhiteSpace(slip.ProvinceOfEmployment))
            {
                problems.Add($"{who} has no province of employment, which is required on the slip.");
            }
        }

        if (t4.Slips.Count == 0)
        {
            problems.Add($"There are no approved pay runs in {t4.TaxYear}, so there is nothing to file.");
        }

        return problems;
    }

    /// <summary>
    /// Things worth knowing before filing that do NOT stop it.
    ///
    /// Kept apart from <see cref="Validate"/> because conflating the two is what made the export
    /// button impossible to enable: a missing social insurance number was blocking a return that
    /// CRA actually accepts. CRA defines all zeroes for exactly this case, and
    /// <see cref="T4XmlWriter"/> writes it, so refusing to file was this app inventing a rule
    /// stricter than the one it was implementing.
    ///
    /// It still has to be said, because the employee loses the credit for their contributions.
    /// </summary>
    public static List<string> Warnings(T4Return t4)
    {
        ArgumentNullException.ThrowIfNull(t4);

        var warnings = new List<string>();

        foreach (T4Slip slip in t4.Slips)
        {
            string who = string.IsNullOrWhiteSpace(slip.Surname) ? slip.EmployeeId : slip.Surname;

            if (new string(slip.Sin.Where(char.IsAsciiDigit).ToArray()).Length != 9)
            {
                warnings.Add($"{who} has no social insurance number. The slip can still be filed, "
                             + "but their CPP contributions will not be credited to them.");
            }
        }

        return warnings;
    }

    /// <summary>
    /// Something plausible enough to be an address, capped at the 60 characters CRA allows.
    ///
    /// Deliberately loose. The point is to catch an empty box or an obvious slip, not to
    /// adjudicate the grammar of email addresses, and a filing blocked by a validator that is
    /// stricter than CRA's is worse than one CRA bounces.
    /// </summary>
    public static bool IsEmailAddress(string? value)
    {
        string v = (value ?? string.Empty).Trim();

        if (v.Length is 0 or > 60 || v.Any(char.IsWhiteSpace))
        {
            return false;
        }

        int at = v.IndexOf('@');

        return at > 0
               && at == v.LastIndexOf('@')
               && v.IndexOf('.', at) > at + 1
               && !v.EndsWith('.');
    }

    /// <summary>Nine digits, then RP, then four digits.</summary>
    public static bool IsPayrollAccountNumber(string? value)
    {
        string v = (value ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

        return v.Length == 15
               && v[..9].All(char.IsAsciiDigit)
               && v[9..11] == "RP"
               && v[11..].All(char.IsAsciiDigit);
    }

    /// <summary>
    /// Splits a display name into the surname, first given name and second initial the T4
    /// wants. The app stores one name field, so the last word is taken as the surname, which
    /// is right far more often than not and is visible on the slip for correcting when it is
    /// not.
    /// </summary>
    private static (string Surname, string Given, string Initial) SplitName(string name)
    {
        string[] parts = (name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => (string.Empty, string.Empty, string.Empty),
            1 => (parts[0], string.Empty, string.Empty),
            2 => (parts[1], parts[0], string.Empty),
            _ => (parts[^1], parts[0], parts[1][..1]),
        };
    }
}
