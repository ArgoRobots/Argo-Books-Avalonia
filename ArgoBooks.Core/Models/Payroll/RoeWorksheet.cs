namespace ArgoBooks.Core.Models.Payroll;

/// <summary>
/// The figures needed to complete a Record of Employment, in the order ROE Web asks for them.
///
/// Not an ROE. Service Canada issues ROEs through ROE Web or ROE SAT, and a printed sheet is
/// not a filing. This is what the employer keys in, which is how a two person business does it
/// anyway, and it exists because the alternative is re-adding 27 pay periods by hand on a five
/// day deadline.
/// </summary>
public class RoeWorksheet
{
    public string EmployeeId { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string Sin { get; set; } = string.Empty;

    public Common.Address Address { get; set; } = new();

    public string EmployerName { get; set; } = string.Empty;

    /// <summary>The CRA payroll account number, block 5.</summary>
    public string PayrollAccountNumber { get; set; } = string.Empty;

    public PayFrequency PayFrequency { get; set; }

    /// <summary>Block 10. The first day the employee worked and earned insurable earnings.</summary>
    public DateTime? FirstDayWorked { get; set; }

    /// <summary>
    /// Block 11. The last day for which insurable earnings were received, which is NOT always
    /// the last day worked: paid leave and vacation carry it later.
    /// </summary>
    public DateTime? LastDayPaid { get; set; }

    /// <summary>
    /// Block 12. The end of the pay period containing block 11. Can never be earlier than
    /// block 11, which is the one relationship Service Canada checks.
    /// </summary>
    public DateTime? FinalPeriodEnd { get; set; }

    /// <summary>Block 15A. Null when the hours are not known rather than zero.</summary>
    public decimal? TotalInsurableHours { get; set; }

    /// <summary>Block 15B.</summary>
    public decimal TotalInsurableEarnings { get; set; }

    /// <summary>
    /// Block 15C. Most recent pay period FIRST, which is the opposite of how the pay runs are
    /// stored and the single easiest thing to get backwards here.
    /// </summary>
    public List<RoePayPeriod> Periods { get; set; } = [];

    /// <summary>
    /// Block 17A. Vacation pay paid or payable BECAUSE OF the separation, which is the final
    /// period's only. Vacation pay included with every cheque is explicitly excluded by Service
    /// Canada's ROE guide, so this is not the employee's vacation pay for the year.
    /// </summary>
    public decimal VacationPay { get; set; }

    /// <summary>
    /// Why the hours could not be worked out, if they could not. Shown on the worksheet in
    /// place of a figure.
    /// </summary>
    public string? HoursUnavailableReason { get; set; }

    /// <summary>
    /// Five calendar days after the end of the pay period containing the interruption of
    /// earnings, which is the deadline for an electronic ROE.
    /// </summary>
    public DateTime? Deadline => FinalPeriodEnd?.AddDays(5);

    /// <summary>How many pay periods block 15B was totalled over. Printed so it can be checked.</summary>
    public int EarningsPeriodCount { get; set; }

    /// <summary>How many pay periods block 15A and 15C cover.</summary>
    public int HoursPeriodCount { get; set; }
}

/// <summary>One line of block 15C.</summary>
public class RoePayPeriod
{
    public DateTime PeriodEnd { get; set; }

    public decimal InsurableEarnings { get; set; }

    public decimal? InsurableHours { get; set; }
}
