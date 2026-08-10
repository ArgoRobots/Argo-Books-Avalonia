using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Payroll;

/// <summary>
/// Someone on the payroll.
///
/// Archived rather than deleted when they leave, because their pay history has to survive:
/// a T4 must still be produceable in February for a person who left in March.
/// </summary>
public class Employee : BaseEntity
{
    /// <summary>Full name, as it should appear on the T4.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional payroll number, printed on the T4.</summary>
    [JsonPropertyName("employeeNumber")]
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Two letter code for the province of EMPLOYMENT, which is not necessarily where the
    /// employee lives. It decides which tax table applies.
    /// </summary>
    [JsonPropertyName("province")]
    public string Province { get; set; } = "AB";

    [JsonPropertyName("payType")]
    public PayType PayType { get; set; } = PayType.Salary;

    /// <summary>Annual salary, or the hourly rate, depending on <see cref="PayType"/>.</summary>
    [JsonPropertyName("payRate")]
    public decimal PayRate { get; set; }

    [JsonPropertyName("payFrequency")]
    public PayFrequency PayFrequency { get; set; } = PayFrequency.Biweekly;

    /// <summary>
    /// Total claim amount from the employee's federal TD1. Zero means they have not filed
    /// one, in which case the basic personal amount is used.
    /// </summary>
    [JsonPropertyName("federalClaimAmount")]
    public decimal FederalClaimAmount { get; set; }

    /// <summary>Total claim amount from the provincial or territorial TD1.</summary>
    [JsonPropertyName("provincialClaimAmount")]
    public decimal ProvincialClaimAmount { get; set; }

    /// <summary>Under 18, over 70, or already receiving a CPP retirement pension.</summary>
    [JsonPropertyName("isCppExempt")]
    public bool IsCppExempt { get; set; }

    /// <summary>Typically an owner controlling more than 40% of the voting shares.</summary>
    [JsonPropertyName("isEiExempt")]
    public bool IsEiExempt { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>Set when they leave. Needed for the Record of Employment figures.</summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>Archived employees stay in the file but are hidden from pay runs.</summary>
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>Gross pay for one period, for a salaried employee.</summary>
    public decimal GrossPerPeriod() =>
        PayType == PayType.Salary && PayFrequency.PeriodsPerYear() > 0
            ? Math.Round(PayRate / PayFrequency.PeriodsPerYear(), 2, MidpointRounding.AwayFromZero)
            : 0m;
}

/// <summary>How an employee is paid.</summary>
public enum PayType
{
    /// <summary>A fixed annual amount, divided across the pay periods.</summary>
    Salary,

    /// <summary>An hourly rate, multiplied by hours entered on each pay run.</summary>
    Hourly,
}

/// <summary>
/// How often an employee is paid. The number of periods a year is central to the deduction
/// maths, since annual thresholds are divided by it.
/// </summary>
public enum PayFrequency
{
    Weekly,
    Biweekly,
    SemiMonthly,
    Monthly,
}

public static class PayFrequencyExtensions
{
    /// <summary>Pay periods in a year, as CRA counts them.</summary>
    public static int PeriodsPerYear(this PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => 52,
        PayFrequency.Biweekly => 26,
        PayFrequency.SemiMonthly => 24,
        PayFrequency.Monthly => 12,
        _ => 26,
    };

    public static string DisplayName(this PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => "Weekly",
        PayFrequency.Biweekly => "Biweekly",
        PayFrequency.SemiMonthly => "Semi-monthly",
        PayFrequency.Monthly => "Monthly",
        _ => frequency.ToString(),
    };
}
