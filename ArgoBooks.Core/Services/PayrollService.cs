using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Puts a pay run together: works out year-to-date figures, calls the calculator for each
/// employee, and applies the results.
///
/// The calculator itself is pure and knows nothing about company data. This is the layer that
/// feeds it, which keeps the part that must be provably correct free of dependencies.
/// </summary>
public class PayrollService(PayrollRateService? rateService = null)
{
    private readonly PayrollRateService _rates = rateService ?? new PayrollRateService();

    /// <summary>
    /// Builds a draft run for the given pay date. Returns null when no CRA edition covers that
    /// date, which callers must surface rather than working around: calculating with the wrong
    /// edition produces figures that look plausible and are wrong.
    /// </summary>
    public PayRun? CreateDraft(CompanyData data, DateTime payDate, DateTime periodStart, DateTime periodEnd,
                               IEnumerable<string>? employeeIds = null)
    {
        PayrollRateTable? rates = _rates.GetForDate(payDate);
        if (rates == null)
        {
            return null;
        }

        HashSet<string>? only = employeeIds == null ? null : [.. employeeIds];

        var run = new PayRun
        {
            Id = NextRunId(data),
            PayDate = payDate,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RateEditionId = rates.EditionId,
            Status = PayRunStatus.Draft,
        };

        foreach (Employee employee in data.Employees.Where(e => !e.IsArchived))
        {
            if (only != null && !only.Contains(employee.Id))
            {
                continue;
            }

            run.Lines.Add(new PayRunLine
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Province = employee.Province,
                PayPeriodsPerYear = employee.PayFrequency.PeriodsPerYear(),
                BasePay = employee.GrossPerPeriod(),
            });
        }

        Recalculate(data, run);
        return run;
    }

    /// <summary>
    /// Whether a pay run for this date can be calculated for this province at all.
    ///
    /// The calculator throws for a province it has no table for, which is the right answer for a
    /// pure function and the wrong thing to discover halfway through building a draft. Callers
    /// ask first so they can say whose province is the problem.
    ///
    /// Quebec is supported when the edition carries Quebec data, and is deliberately not looked
    /// for in the provinces dictionary: it is not in there, and checking that dictionary alone
    /// reports a fully supported jurisdiction as missing.
    /// </summary>
    public bool Supports(DateTime payDate, string? province)
    {
        PayrollRateTable? rates = _rates.GetForDate(payDate);
        if (rates == null || string.IsNullOrWhiteSpace(province))
        {
            return false;
        }

        return string.Equals(province, "QC", StringComparison.OrdinalIgnoreCase)
            ? rates.Quebec != null
            : rates.Provinces.ContainsKey(province);
    }

    /// <summary>
    /// Recomputes every line from its inputs. Only ever called on a draft: an approved run's
    /// figures are frozen so they keep agreeing with the stub the employee was given.
    /// </summary>
    public void Recalculate(CompanyData data, PayRun run)
    {
        if (run.Status != PayRunStatus.Draft)
        {
            return;
        }

        PayrollRateTable? rates = _rates.GetForDate(run.PayDate);
        if (rates == null)
        {
            return;
        }

        foreach (PayRunLine line in run.Lines)
        {
            Employee? employee = data.Employees.FirstOrDefault(e => e.Id == line.EmployeeId);
            if (employee == null)
            {
                continue;
            }

            // An hourly employee's base pay is derived from their hours rather than typed, but
            // it still has to be stored: a pay stub shows the earnings lines adding up to
            // gross, and leaving this at zero makes the stub fail to reconcile in front of the
            // person being paid.
            if (employee.PayType == PayType.Hourly)
            {
                line.BasePay = Math.Round(line.HoursWorked * employee.PayRate, 2, MidpointRounding.AwayFromZero);
            }

            decimal gross = line.BasePay + line.Bonus + line.VacationPay;

            PayrollYearToDate ytd = YearToDateFor(data, employee.Id, run);

            PayrollDeductions d = PayrollCalculator.Calculate(
                new PayrollInput
                {
                    GrossPay = gross,

                    // Only the bonus is treated as non-periodic. Vacation pay is left as
                    // regular income: CRA's definition covers vacation pay taken as money
                    // INSTEAD of time off, but the common case here is the 4% added to every
                    // cheque, which does recur. Annualising a bonus is a large error in one
                    // direction; treating a recurring 4% as one-off would be an error in the
                    // other.
                    NonPeriodicPay = line.Bonus,
                    Province = employee.Province,
                    PayPeriodsPerYear = employee.PayFrequency.PeriodsPerYear(),
                    FederalClaimAmount = employee.FederalClaimAmount,
                    ProvincialClaimAmount = employee.ProvincialClaimAmount,

                    // Ontario's tax reduction is the only one that reads this. Everywhere else
                    // the term is absent from the formula, so it costs nothing to pass through.
                    Dependants = employee.OntarioDependants,
                    IsCppExempt = employee.IsCppExempt,
                    IsEiExempt = employee.IsEiExempt,
                },
                ytd,
                rates);

            line.GrossPay = d.GrossPay;
            line.CppEmployee = d.CppEmployee;
            line.CppEmployer = d.CppEmployer;
            line.Cpp2Employee = d.Cpp2Employee;
            line.Cpp2Employer = d.Cpp2Employer;
            line.EiEmployee = d.EiEmployee;
            line.EiEmployer = d.EiEmployer;
            line.QpipEmployee = d.QpipEmployee;
            line.QpipEmployer = d.QpipEmployer;
            line.FederalTax = d.FederalTax;
            line.ProvincialTax = d.ProvincialTax;
            line.NetPay = d.NetPay;
        }
    }

    /// <summary>
    /// What an employee has already had deducted this calendar year.
    ///
    /// Required rather than optional, because CPP, CPP2 and EI all stop at annual maximums and
    /// cannot be worked out from one period in isolation.
    ///
    /// Everything except drafts counts. A draft has not happened, so counting it would make
    /// the next run under-deduct. A VOIDED run does count, which reads oddly until you notice
    /// that its reversal is also counted: the two cancel to zero, which is the whole point of
    /// writing a reversal. Skipping the voided run as well would subtract it twice and push
    /// the year-to-date below where it was before the run ever existed, so every later run
    /// would over-deduct against a ceiling that had already moved.
    /// </summary>
    public PayrollYearToDate YearToDateFor(CompanyData data, string employeeId, PayRun? excluding = null)
    {
        var ytd = new PayrollYearToDate();
        int year = (excluding?.PayDate ?? DateTime.Today).Year;

        foreach (PayRun run in data.PayRuns)
        {
            if (run.Status == PayRunStatus.Draft
                || run.PayDate.Year != year
                || (excluding != null && run.Id == excluding.Id)
                || (excluding != null && run.PayDate > excluding.PayDate))
            {
                continue;
            }

            foreach (PayRunLine line in run.Lines.Where(l => l.EmployeeId == employeeId))
            {
                ytd.PensionableEarnings += line.GrossPay;
                ytd.InsurableEarnings += line.GrossPay;
                ytd.CppEmployee += line.CppEmployee;
                ytd.Cpp2Employee += line.Cpp2Employee;
                ytd.EiEmployee += line.EiEmployee;
                ytd.QpipEmployee += line.QpipEmployee;
                ytd.QpipEmployer += line.QpipEmployer;

                // T4127's B1. A second bonus has to be taxed on top of the first rather than as
                // though it were the only one this year.
                ytd.NonPeriodicPay += line.Bonus;
            }
        }

        return ytd;
    }

    /// <summary>
    /// What has to reach CRA next, and by when.
    ///
    /// A regular remitter sends what they withheld during a month by the 15th of the FOLLOWING
    /// month, so the two are always a month apart and the useful question is not "what have we
    /// withheld this month". In the middle of September the deadline that has not passed is 15
    /// September, and what it covers is AUGUST. Naming September's figure there would show an
    /// amount that is not yet due while hiding the one that is.
    ///
    /// The 15th itself counts as not yet passed. It is the day this matters most: the payment is
    /// due today rather than overdue, and rolling on to the next month would tell somebody they
    /// had nothing to pay on the morning they had to pay it.
    ///
    /// Everything except drafts counts, so a voided run and its reversal both appear and cancel,
    /// matching how the year-to-date figures are built.
    /// </summary>
    /// <param name="today">Injected so the boundary can be tested rather than waited for.</param>
    public static (decimal Amount, DateTime DueDate) NextRemittance(IEnumerable<PayRun> runs, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(runs);

        DateTime date = today.Date;

        // On or before the 15th the deadline is this month's, covering last month's payroll.
        // After it, the next deadline is next month's, covering this month's.
        DateTime dueDate = date.Day <= 15
            ? new DateTime(date.Year, date.Month, 15)
            : new DateTime(date.Year, date.Month, 15).AddMonths(1);

        DateTime covered = dueDate.AddMonths(-1);

        decimal amount = runs
            .Where(r => r.Status != PayRunStatus.Draft
                        && r.PayDate.Year == covered.Year
                        && r.PayDate.Month == covered.Month)
            .Sum(r => r.TotalRemittance);

        return (amount, dueDate);
    }

    /// <summary>
    /// Locks a run without touching the books. Use <see cref="ApproveAndRecord"/> for the
    /// normal path; this exists so the lock and the bookkeeping can be reasoned about apart.
    /// </summary>
    public void Approve(PayRun run)
    {
        if (run.Status != PayRunStatus.Draft)
        {
            return;
        }

        run.Status = PayRunStatus.Approved;
        run.ApprovedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Locks a run and records the wages in the books.
    ///
    /// One expense per employee at NET pay, not gross. The employer makes two separate
    /// withdrawals, the net pay now and the CRA remittance later, and both need somewhere to
    /// land. Recording gross here and the remittance again later would count the deductions
    /// twice. Separate rather than combined because a small employer e-transfers each person
    /// individually, so the bank statement shows separate lines and bank matching has to line
    /// up with them.
    /// </summary>
    /// <returns>The expenses that were created, in line order.</returns>
    public List<Expense> ApproveAndRecord(CompanyData data, PayRun run)
    {
        var created = new List<Expense>();

        if (run.Status != PayRunStatus.Draft)
        {
            return created;
        }

        Approve(run);

        string companyCurrency = string.IsNullOrWhiteSpace(data.Settings.Localization.Currency)
            ? "USD"
            : data.Settings.Localization.Currency;

        foreach (PayRunLine line in run.Lines)
        {
            if (line.NetPay == 0)
            {
                continue;
            }

            Expense expense = TransactionFactory.CreateExpense(data, new TransactionDraft(
                Date: run.PayDate,
                Description: $"Wages - {line.EmployeeName}",
                Total: line.NetPay,
                CounterpartyId: null,
                Notes: $"Net pay for {run.PeriodStart:yyyy-MM-dd} to {run.PeriodEnd:yyyy-MM-dd} ({run.Id}).",
                OriginalCurrency: companyCurrency));

            // Pass the amount straight through to the USD base, exactly as the bank import does.
            // Payroll is computed in the company's own currency by CRA rules, so there is no
            // exchange rate involved and none to look up. Left unset, the display path treats the
            // figure as USD needing conversion at the pay date, finds no rate, and shows Pending
            // instead of the amount. Worse, once a rate did arrive it would show a converted
            // number that was never what anyone was paid.
            expense.TotalUSD = expense.Total;
            expense.UnitPriceUSD = expense.UnitPrice;

            data.Expenses.Add(expense);
            line.ExpenseId = expense.Id;
            created.Add(expense);
        }

        return created;
    }

    /// <summary>
    /// Cancels an approved run by writing a reversing one, rather than deleting. A pay stub may
    /// already be in someone's hands, so the history has to survive the correction.
    /// </summary>
    public PayRun? Void(CompanyData data, PayRun run)
    {
        if (run.Status != PayRunStatus.Approved)
        {
            return null;
        }

        var reversal = new PayRun
        {
            Id = NextRunId(data),
            PayDate = run.PayDate,
            PeriodStart = run.PeriodStart,
            PeriodEnd = run.PeriodEnd,
            RateEditionId = run.RateEditionId,
            Status = PayRunStatus.Approved,
            VoidsPayRunId = run.Id,
            ApprovedAt = DateTime.UtcNow,
        };

        foreach (PayRunLine line in run.Lines)
        {
            reversal.Lines.Add(new PayRunLine
            {
                EmployeeId = line.EmployeeId,
                EmployeeName = line.EmployeeName,
                Province = line.Province,
                PayPeriodsPerYear = line.PayPeriodsPerYear,
                HoursWorked = -line.HoursWorked,
                BasePay = -line.BasePay,
                Bonus = -line.Bonus,
                VacationPay = -line.VacationPay,
                GrossPay = -line.GrossPay,
                CppEmployee = -line.CppEmployee,
                CppEmployer = -line.CppEmployer,
                Cpp2Employee = -line.Cpp2Employee,
                Cpp2Employer = -line.Cpp2Employer,
                EiEmployee = -line.EiEmployee,
                EiEmployer = -line.EiEmployer,
                QpipEmployee = -line.QpipEmployee,
                QpipEmployer = -line.QpipEmployer,
                FederalTax = -line.FederalTax,
                ProvincialTax = -line.ProvincialTax,
                NetPay = -line.NetPay,
            });
        }

        // The wage expenses are removed rather than reversed. They were written by this app,
        // not observed in the world, and a voided run is one whose money never left. Leaving
        // a matching pair of plus and minus expenses would double the transaction count on
        // every report for no gain.
        foreach (PayRunLine line in run.Lines)
        {
            if (line.ExpenseId is not { Length: > 0 } expenseId)
            {
                continue;
            }

            data.Expenses.RemoveAll(e => e.Id == expenseId);
            line.ExpenseId = null;
        }

        run.Status = PayRunStatus.Void;
        data.PayRuns.Add(reversal);
        return reversal;
    }

    private static string NextRunId(CompanyData data)
    {
        int highest = 0;
        foreach (PayRun run in data.PayRuns)
        {
            if (run.Id.StartsWith("PR-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(run.Id[3..], out int n) && n > highest)
            {
                highest = n;
            }
        }

        return $"PR-{highest + 1:D4}";
    }
}
