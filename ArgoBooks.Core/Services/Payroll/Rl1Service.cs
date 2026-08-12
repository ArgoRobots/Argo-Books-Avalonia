using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;

namespace ArgoBooks.Core.Services.Payroll;

/// <summary>
/// Builds a year's RL-1 return from the pay runs already recorded.
///
/// Mirrors <see cref="T4Service"/> deliberately, including its refusal to store anything: the
/// RL-1 restates frozen pay runs, so it is assembled on demand and comes out identical every
/// time.
///
/// The one structural difference is who it covers. A T4 is filed for every employee; an RL-1 is
/// filed only for Quebec employees, so a mixed employer's two returns list different people and
/// their totals are supposed to disagree.
/// </summary>
public class Rl1Service
{
    private readonly PayrollRateService _rates;

    public Rl1Service(PayrollRateService? rates = null) => _rates = rates ?? new PayrollRateService();

    /// <summary>
    /// Assembles the return. Quebec employees only. Drafts are excluded here and refused
    /// outright by <see cref="Validate"/>, for the same reason as the T4.
    /// </summary>
    public Rl1Return Build(CompanyData data, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(data);

        var company = data.Settings.Company;

        var rl1 = new Rl1Return
        {
            TaxYear = taxYear,
            QuebecIdentificationNumber = company.QuebecIdentificationNumber ?? string.Empty,
            EmployerName = company.Name,
            EmployerAddress = new Models.Common.Address
            {
                Street = company.Address ?? string.Empty,
                City = company.City ?? string.Empty,
                State = company.ProvinceState ?? string.Empty,
                Country = string.IsNullOrWhiteSpace(company.Country) ? "CAN" : company.Country,
                ZipCode = company.PostalCode ?? string.Empty,
            },
        };

        EarningsCeilings ceilings = EarningsCeilings.For(_rates, taxYear);

        // Everything except drafts, so a voided run and its reversal both count and cancel to
        // zero. Matches the T4 and the year-to-date figures.
        var lines = data.PayRuns
            .Where(r => r.Status != PayRunStatus.Draft && r.PayDate.Year == taxYear)
            .SelectMany(r => r.Lines)
            .ToList();

        foreach (var group in lines.GroupBy(l => l.EmployeeId))
        {
            Employee? employee = data.Employees.FirstOrDefault(e => e.Id == group.Key);
            if (employee == null || !IsQuebec(employee))
            {
                continue;
            }

            Rl1Slip slip = BuildSlip(employee, group.ToList(), ceilings);

            // A slip whose every figure nets to zero is one whose runs were all voided. There
            // is nothing to report.
            if (slip.EmploymentIncome > 0 || slip.QuebecIncomeTax > 0)
            {
                rl1.Slips.Add(slip);
            }
        }

        rl1.Slips.Sort((a, b) => string.Compare(a.Surname, b.Surname, StringComparison.CurrentCultureIgnoreCase));
        return rl1;
    }

    /// <summary>True when this employer has anyone to file an RL-1 for at all.</summary>
    public static bool HasQuebecEmployees(CompanyData data, int taxYear)
    {
        ArgumentNullException.ThrowIfNull(data);

        var quebec = data.Employees
            .Where(IsQuebec)
            .Select(e => e.Id)
            .ToHashSet(StringComparer.Ordinal);

        return quebec.Count > 0
               && data.PayRuns.Any(r => r.Status != PayRunStatus.Draft
                                        && r.PayDate.Year == taxYear
                                        && r.Lines.Any(l => quebec.Contains(l.EmployeeId)));
    }

    private static bool IsQuebec(Employee employee) =>
        string.Equals(employee.Province, "QC", StringComparison.OrdinalIgnoreCase);

    private static Rl1Slip BuildSlip(Employee employee, List<PayRunLine> lines, EarningsCeilings ceilings)
    {
        (string surname, string given) = SplitName(employee.Name);

        decimal gross = lines.Sum(l => l.GrossPay);

        return new Rl1Slip
        {
            EmployeeId = employee.Id,
            Surname = surname,
            GivenName = given,
            Sin = employee.Sin,
            EmployeeNumber = employee.EmployeeNumber,
            Address = employee.Address,
            EmploymentIncome = gross,

            // The pay run stores Quebec's pension contributions in the CPP fields, because they
            // are the same column in the same run and only their destination differs. Boxes B.A
            // and B.B are where they land.
            QppContribution = lines.Sum(l => l.CppEmployee),
            AdditionalQppContribution = lines.Sum(l => l.Cpp2Employee),

            EiPremium = lines.Sum(l => l.EiEmployee),

            // Box E is the QUEBEC tax alone. The federal half was withheld in the same run but
            // it is remitted to CRA and reported on the T4, so putting the combined figure here
            // would have the employee claim a Quebec credit for money Quebec never received.
            QuebecIncomeTax = lines.Sum(l => l.ProvincialTax),

            // Capped at the year's ceilings, for the same reason as the T4's boxes 24 and 26:
            // someone earning above one stopped contributing part way through the year, and
            // reporting their whole salary would have Revenu Quebec expect contributions on
            // money that was never pensionable or eligible.
            QppPensionableSalary = employee.IsCppExempt ? 0m : ceilings.CapPensionable(gross),
            QpipPremium = lines.Sum(l => l.QpipEmployee),

            // Revenu Quebec is explicit that box I takes "0" when there is none rather than
            // being left blank, so the figure is always set even when it is nil.
            QpipEligibleSalary = employee.IsEiExempt ? 0m : ceilings.CapQpip(gross),

            EmployerQpp = lines.Sum(l => l.CppEmployer),
            EmployerQpp2 = lines.Sum(l => l.Cpp2Employer),
            EmployerQpip = lines.Sum(l => l.QpipEmployer),
        };
    }

    /// <summary>
    /// Everything that would make Revenu Quebec reject the filing, or make it wrong. Returned
    /// as messages rather than thrown, so the year end screen can show them all at once.
    /// </summary>
    public static List<string> Validate(CompanyData data, Rl1Return rl1)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(rl1);

        var problems = new List<string>();

        int drafts = data.PayRuns.Count(r => r.Status == PayRunStatus.Draft && r.PayDate.Year == rl1.TaxYear);
        if (drafts > 0)
        {
            problems.Add($"{drafts} pay run(s) in {rl1.TaxYear} are still drafts. Approve or delete them before filing.");
        }

        if (!IsQuebecIdentificationNumber(rl1.QuebecIdentificationNumber))
        {
            problems.Add("The Revenu Quebec identification number is missing or not in the form 1234567890RS0001. "
                         + "It is on your Revenu Quebec statement and is not the same as your CRA payroll account number.");
        }

        if (string.IsNullOrWhiteSpace(rl1.EmployerName))
        {
            problems.Add("The company name is required on the RL-1 Summary.");
        }

        foreach (Rl1Slip slip in rl1.Slips)
        {
            string who = string.IsNullOrWhiteSpace(slip.Surname) ? slip.EmployeeId : slip.Surname;

            if (new string(slip.Sin.Where(char.IsAsciiDigit).ToArray()).Length != 9)
            {
                problems.Add($"{who} has no social insurance number, which is required on the RL-1.");
            }

            if (string.IsNullOrWhiteSpace(slip.Address.City))
            {
                problems.Add($"{who} has no address. Revenu Quebec requires one on every RL-1 slip.");
            }
        }

        // Paper filing is only allowed under six slips. Above that this app cannot produce what
        // is needed, and saying so is better than handing over PDFs that will be sent back.
        if (rl1.Slips.Count > 5)
        {
            problems.Add($"There are {rl1.Slips.Count} RL-1 slips. Revenu Quebec requires six or more to be filed "
                         + "online in an XML file, which Argo Books does not produce. File through your accountant "
                         + "or Revenu Quebec's own service instead of mailing these.");
        }

        if (rl1.Slips.Count == 0)
        {
            problems.Add($"There are no approved pay runs for Quebec employees in {rl1.TaxYear}, so there is nothing to file.");
        }

        return problems;
    }

    /// <summary>Ten digits, then two letters, then four digits. Usually RS, sometimes RR.</summary>
    public static bool IsQuebecIdentificationNumber(string? value)
    {
        string v = (value ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

        return v.Length == 16
               && v[..10].All(char.IsAsciiDigit)
               && v[10..12].All(char.IsAsciiLetter)
               && v[12..].All(char.IsAsciiDigit);
    }

    /// <summary>
    /// The RL-1 wants a surname and given name, with no initial field, so this differs from the
    /// T4's split: everything before the last word is the given name rather than being trimmed
    /// to a single initial.
    /// </summary>
    private static (string Surname, string Given) SplitName(string name)
    {
        string[] parts = (name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[^1], string.Join(' ', parts[..^1])),
        };
    }
}
