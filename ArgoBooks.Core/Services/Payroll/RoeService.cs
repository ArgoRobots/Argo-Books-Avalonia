using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;

namespace ArgoBooks.Core.Services.Payroll;

/// <summary>
/// Assembles the figures for a Record of Employment.
///
/// An ROE is due within five calendar days of the END OF THE PAY PERIOD in which the
/// interruption of earnings happens, not five days from the last day worked, and it is what
/// Service Canada calculates an EI claim from. Getting the hours or the earnings wrong does not
/// bounce; it quietly shortens someone's benefit.
///
/// Two counts drive everything and they are NOT the same number, which is the trap:
///
/// - Blocks 15A and 15C cover the last 53 weeks of payroll: 53 weekly periods, 27 biweekly,
///   25 semi-monthly, 13 monthly.
/// - Block 15B covers a shorter window: 27 weekly, 14 biweekly, 13 semi-monthly, 7 monthly.
///
/// Both are read from Service Canada's charts rather than derived, because they are not a
/// clean function of the frequency.
/// </summary>
public class RoeService
{
    /// <summary>Blocks 15A and 15C. The equivalent of 53 weeks.</summary>
    public static int HoursPeriodCount(PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => 53,
        PayFrequency.Biweekly => 27,
        PayFrequency.SemiMonthly => 25,
        PayFrequency.Monthly => 13,
        _ => 27,
    };

    /// <summary>Block 15B. Deliberately shorter than the hours window.</summary>
    public static int EarningsPeriodCount(PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => 27,
        PayFrequency.Biweekly => 14,
        PayFrequency.SemiMonthly => 13,
        PayFrequency.Monthly => 7,
        _ => 14,
    };

    /// <summary>
    /// Hours in one pay period for a salaried employee, from their contract week. Service
    /// Canada's answer for an employer who does not track hours.
    /// </summary>
    private static decimal HoursPerPeriod(decimal weeklyHours, PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => weeklyHours,
        PayFrequency.Biweekly => weeklyHours * 2m,
        PayFrequency.SemiMonthly => Math.Round(weeklyHours * 52m / 24m, 2, MidpointRounding.AwayFromZero),
        PayFrequency.Monthly => Math.Round(weeklyHours * 52m / 12m, 2, MidpointRounding.AwayFromZero),
        _ => weeklyHours * 2m,
    };

    public RoeWorksheet Build(CompanyData data, string employeeId)
    {
        ArgumentNullException.ThrowIfNull(data);

        Employee employee = data.Employees.FirstOrDefault(e => e.Id == employeeId)
                            ?? throw new ArgumentException($"No employee with id {employeeId}.", nameof(employeeId));

        var company = data.Settings.Company;

        var worksheet = new RoeWorksheet
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.Name,
            Sin = employee.Sin,
            Address = employee.Address,
            EmployerName = company.Name,
            PayrollAccountNumber = company.PayrollAccountNumber ?? string.Empty,
            PayFrequency = employee.PayFrequency,
            FirstDayWorked = employee.StartDate,
            HoursPeriodCount = HoursPeriodCount(employee.PayFrequency),
            EarningsPeriodCount = EarningsPeriodCount(employee.PayFrequency),
        };

        // Everything except drafts, so a voided run and its reversal cancel. Ordered most
        // recent first, which is the order block 15C wants and the opposite of storage order.
        var runs = data.PayRuns
            .Where(r => r.Status != PayRunStatus.Draft && r.Lines.Any(l => l.EmployeeId == employeeId))
            .OrderByDescending(r => r.PeriodEnd)
            .ToList();

        if (runs.Count == 0)
        {
            worksheet.HoursUnavailableReason = "This employee has no approved pay runs.";
            return worksheet;
        }

        // Runs are grouped by period rather than taken one to one, because a void and its
        // reversal share a period and must net out instead of consuming two of the 27 slots.
        var periods = runs
            .GroupBy(r => r.PeriodEnd)
            .OrderByDescending(g => g.Key)
            .Select(g => new
            {
                PeriodEnd = g.Key,
                Lines = g.SelectMany(r => r.Lines).Where(l => l.EmployeeId == employeeId).ToList(),
            })
            .ToList();

        worksheet.LastDayPaid = employee.EndDate ?? periods[0].PeriodEnd;
        worksheet.FinalPeriodEnd = periods[0].PeriodEnd;

        // Block 12 can never be earlier than block 11. An end date after the last pay period,
        // which is what a final unpaid week looks like, would otherwise produce exactly that.
        if (worksheet.LastDayPaid > worksheet.FinalPeriodEnd)
        {
            worksheet.FinalPeriodEnd = worksheet.LastDayPaid;
        }

        worksheet.FirstDayWorked ??= runs.Min(r => r.PeriodStart);

        bool hoursKnown = employee.PayType == PayType.Hourly || employee.StandardHoursPerWeek > 0;

        if (!hoursKnown)
        {
            worksheet.HoursUnavailableReason =
                "This is a salaried employee and no standard hours per week are recorded, so block 15A "
                + "cannot be worked out. Enter their contract hours on the employee, or fill block 15A "
                + "from the employment contract.";
        }

        foreach (var period in periods.Take(worksheet.HoursPeriodCount))
        {
            decimal gross = period.Lines.Sum(l => l.GrossPay);

            worksheet.Periods.Add(new RoePayPeriod
            {
                PeriodEnd = period.PeriodEnd,

                // An EI exempt employee has no insurable earnings at all, whatever they were
                // paid, so reporting gross would overstate a claim they cannot make.
                InsurableEarnings = employee.IsEiExempt ? 0m : gross,
                InsurableHours = !hoursKnown
                    ? null
                    : employee.PayType == PayType.Hourly
                        ? period.Lines.Sum(l => l.HoursWorked)

                        // A salaried nil period earned nothing and so worked nothing. Crediting
                        // contract hours to it would invent hours nobody worked.
                        : gross == 0m
                            ? 0m
                            : HoursPerPeriod(employee.StandardHoursPerWeek ?? 0m, employee.PayFrequency),
            });
        }

        worksheet.TotalInsurableHours = hoursKnown
            ? worksheet.Periods.Sum(p => p.InsurableHours ?? 0m)
            : null;

        // The shorter window, and the reason the two counts are separate constants.
        worksheet.TotalInsurableEarnings = worksheet.Periods
            .Take(worksheet.EarningsPeriodCount)
            .Sum(p => p.InsurableEarnings);

        worksheet.VacationPay = periods
            .SelectMany(p => p.Lines)
            .Sum(l => l.VacationPay);

        return worksheet;
    }

    /// <summary>
    /// What would make the worksheet wrong or unusable. Returned as messages so they can all be
    /// shown at once, matching the T4 and RL-1.
    /// </summary>
    public static List<string> Validate(RoeWorksheet worksheet)
    {
        ArgumentNullException.ThrowIfNull(worksheet);

        var problems = new List<string>();

        if (new string(worksheet.Sin.Where(char.IsAsciiDigit).ToArray()).Length != 9)
        {
            problems.Add("This employee has no social insurance number, which block 9 requires.");
        }

        if (worksheet.FirstDayWorked == null)
        {
            problems.Add("There is no start date for this employee, so block 10 cannot be filled.");
        }

        if (worksheet.Periods.Count == 0)
        {
            problems.Add("There are no approved pay runs for this employee, so there is nothing to report.");
        }

        if (worksheet.HoursUnavailableReason != null)
        {
            problems.Add(worksheet.HoursUnavailableReason);
        }

        // Not a rejection, but it is the number that decides how long someone is paid, and a
        // short history usually means pay runs were recorded elsewhere first.
        if (worksheet.Periods.Count > 0 && worksheet.Periods.Count < worksheet.EarningsPeriodCount)
        {
            problems.Add($"Only {worksheet.Periods.Count} pay period(s) are recorded, and block 15B normally covers "
                         + $"{worksheet.EarningsPeriodCount}. If this employee was paid before you started using Argo "
                         + "Books, add those periods by hand.");
        }

        return problems;
    }
}
