namespace ArgoBooks.Core.Models.Payroll;

/// <summary>
/// One Quebec employee's RL-1 for one tax year.
///
/// A Quebec employer files BOTH a T4 with CRA and an RL-1 with Revenu Quebec. They are not
/// translations of each other: the RL-1 carries the Quebec income tax and the QPIP figures,
/// which have no home on the T4 summary, and it is why the T4 Summary's CPP totals exclude
/// Quebec.
///
/// Derived, never stored, for the same reason the T4 is: it restates pay runs that were
/// already frozen when they were approved.
/// </summary>
public class Rl1Slip
{
    public string EmployeeId { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string GivenName { get; set; } = string.Empty;

    public string Sin { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public Common.Address Address { get; set; } = new();

    /// <summary>Box A.</summary>
    public decimal EmploymentIncome { get; set; }

    /// <summary>Box B.A.</summary>
    public decimal QppContribution { get; set; }

    /// <summary>Box B.B. Separate from B.A the way CPP2 is separate from CPP.</summary>
    public decimal AdditionalQppContribution { get; set; }

    /// <summary>Box C. The federal EI premium, at Quebec's reduced rate.</summary>
    public decimal EiPremium { get; set; }

    /// <summary>Box D. Not collected by this app, so always nil.</summary>
    public decimal RppContribution { get; set; }

    /// <summary>Box E. QUEBEC income tax only. The federal half belongs on the T4.</summary>
    public decimal QuebecIncomeTax { get; set; }

    /// <summary>Box F. Not collected by this app, so always nil.</summary>
    public decimal UnionDues { get; set; }

    /// <summary>Box G. Nil for an employee exempt from QPP.</summary>
    public decimal QppPensionableSalary { get; set; }

    /// <summary>Box H.</summary>
    public decimal QpipPremium { get; set; }

    /// <summary>
    /// Box I. Revenu Quebec is explicit that this takes "0" when there is none, rather than
    /// being left blank, so it is always written.
    /// </summary>
    public decimal QpipEligibleSalary { get; set; }

    /// <summary>The employer side, for the summary rather than the slip.</summary>
    public decimal EmployerQpp { get; set; }

    public decimal EmployerQpp2 { get; set; }

    public decimal EmployerQpip { get; set; }
}

/// <summary>A whole year's RL-1 filing: every Quebec slip plus what the summary needs.</summary>
public class Rl1Return
{
    public int TaxYear { get; set; }

    /// <summary>
    /// The Revenu Quebec identification number, which is NOT the CRA payroll account number.
    /// A Quebec employer holds both and they look nothing alike.
    /// </summary>
    public string QuebecIdentificationNumber { get; set; } = string.Empty;

    public string EmployerName { get; set; } = string.Empty;

    public Common.Address EmployerAddress { get; set; } = new();

    public List<Rl1Slip> Slips { get; set; } = [];

    /// <summary>R for an original slip, A to amend, D to cancel. Revenu Quebec's own codes.</summary>
    public Rl1SlipCode SlipCode { get; set; } = Rl1SlipCode.Original;

    public decimal TotalEmploymentIncome => Slips.Sum(s => s.EmploymentIncome);

    public decimal TotalQpp => Slips.Sum(s => s.QppContribution + s.AdditionalQppContribution);

    public decimal TotalEmployerQpp => Slips.Sum(s => s.EmployerQpp + s.EmployerQpp2);

    public decimal TotalQpip => Slips.Sum(s => s.QpipPremium);

    public decimal TotalEmployerQpip => Slips.Sum(s => s.EmployerQpip);

    public decimal TotalQuebecIncomeTax => Slips.Sum(s => s.QuebecIncomeTax);

    /// <summary>
    /// What the RL-1 Summary reconciles: everything the employer should already have remitted
    /// to Revenu Quebec over the year. The health services fund contribution is NOT included,
    /// because this app does not calculate it.
    /// </summary>
    public decimal TotalRemittable =>
        TotalQpp + TotalEmployerQpp + TotalQpip + TotalEmployerQpip + TotalQuebecIncomeTax;
}

public enum Rl1SlipCode
{
    Original,
    Amended,
    Cancelled,
}
