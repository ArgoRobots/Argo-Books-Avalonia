namespace ArgoBooks.Core.Models.Payroll;

/// <summary>
/// One employee's T4 for one tax year, assembled from their approved pay runs.
///
/// Not stored in the company file. A T4 is entirely derived from pay runs that are already
/// frozen, so storing it would create a second copy that could disagree with the first. It is
/// built on demand, filed, and rebuilt identically next time.
/// </summary>
public class T4Slip
{
    public string EmployeeId { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string GivenName { get; set; } = string.Empty;

    /// <summary>Second given name's initial, if there is one. One character.</summary>
    public string Initial { get; set; } = string.Empty;

    public string Sin { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public Common.Address Address { get; set; } = new();

    /// <summary>Box 10. Province of EMPLOYMENT, not of residence.</summary>
    public string ProvinceOfEmployment { get; set; } = string.Empty;

    /// <summary>Box 14.</summary>
    public decimal EmploymentIncome { get; set; }

    /// <summary>Box 16.</summary>
    public decimal CppContributions { get; set; }

    /// <summary>Box 16A. Separate from box 16 since 2024.</summary>
    public decimal Cpp2Contributions { get; set; }

    /// <summary>Box 18.</summary>
    public decimal EiPremiums { get; set; }

    /// <summary>Box 22.</summary>
    public decimal IncomeTaxDeducted { get; set; }

    /// <summary>Box 24. Required even when nil, in which case it is filed as 0.00.</summary>
    public decimal InsurableEarnings { get; set; }

    /// <summary>Box 26. Required even when nil.</summary>
    public decimal PensionableEarnings { get; set; }

    /// <summary>Box 28. True when exempt for the WHOLE year, not part of it.</summary>
    public bool CppExemptAllYear { get; set; }

    /// <summary>Box 28.</summary>
    public bool EiExemptAllYear { get; set; }

    /// <summary>Box 45.</summary>
    public DentalBenefitCode DentalBenefit { get; set; } = DentalBenefitCode.NotEligible;

    /// <summary>The employer share, needed for the summary rather than for the slip itself.</summary>
    public decimal EmployerCpp { get; set; }

    public decimal EmployerCpp2 { get; set; }

    public decimal EmployerEi { get; set; }
}

/// <summary>
/// A whole year's T4 filing: every slip plus the employer details the summary needs.
/// </summary>
public class T4Return
{
    public int TaxYear { get; set; }

    /// <summary>BN15, in the form 000000000RP0000. Required, and must match on every slip.</summary>
    public string PayrollAccountNumber { get; set; } = string.Empty;

    public string EmployerName { get; set; } = string.Empty;

    public Common.Address EmployerAddress { get; set; } = new();

    public string ContactName { get; set; } = string.Empty;

    /// <summary>Digits only. Split into area code and number when written out.</summary>
    public string ContactPhone { get; set; } = string.Empty;

    public List<T4Slip> Slips { get; set; } = [];

    /// <summary>
    /// O for an original filing, A for an amendment, C to cancel. CRA refuses a return that
    /// mixes originals and amendments, so this belongs to the return rather than the slip.
    /// </summary>
    public T4ReportType ReportType { get; set; } = T4ReportType.Original;

    public decimal TotalEmploymentIncome => Slips.Sum(s => s.EmploymentIncome);

    public decimal TotalEmployeeCpp => Slips.Sum(s => s.CppContributions);

    public decimal TotalEmployeeCpp2 => Slips.Sum(s => s.Cpp2Contributions);

    public decimal TotalEmployeeEi => Slips.Sum(s => s.EiPremiums);

    public decimal TotalIncomeTax => Slips.Sum(s => s.IncomeTaxDeducted);

    public decimal TotalEmployerCpp => Slips.Sum(s => s.EmployerCpp);

    public decimal TotalEmployerCpp2 => Slips.Sum(s => s.EmployerCpp2);

    public decimal TotalEmployerEi => Slips.Sum(s => s.EmployerEi);
}

public enum T4ReportType
{
    Original,
    Amendment,
    Cancel,
}
