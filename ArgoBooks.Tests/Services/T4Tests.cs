using System.Xml.Linq;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services;
using ArgoBooks.Core.Services.Payroll;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the T4 return and the XML CRA accepts for it.
///
/// The rules worth guarding are CRA's own rejection rules, because breaking one does not fail
/// here or in the app: it fails months later at the filing deadline, against a submission
/// that cannot be corrected quickly. The two sharpest are that an optional element carrying
/// no value rejects the whole submission, and that the payroll account number on every slip
/// must equal the one on the summary.
/// </summary>
public class T4Tests
{
    private const string Bn = "123456789RP0001";

    private static CompanyData Data(params Employee[] employees)
    {
        var data = new CompanyData();
        data.Settings.Company.Name = "Test Company";
        data.Settings.Company.Address = "1 Main Street";
        data.Settings.Company.City = "Calgary";
        data.Settings.Company.ProvinceState = "AB";
        data.Settings.Company.Country = "CAN";
        data.Settings.Company.PostalCode = "T2P1A1";
        data.Settings.Company.PayrollAccountNumber = Bn;
        data.Settings.Company.PayrollContactName = "Pat Owner";
        data.Settings.Company.PayrollContactPhone = "4035551234";
        data.Employees.AddRange(employees);
        return data;
    }

    private static Employee Person(string id = "EMP-001", string name = "Dana Smith", string sin = "046454286") => new()
    {
        Id = id,
        Name = name,
        Sin = sin,
        Province = "AB",
        PayType = PayType.Salary,
        PayRate = 52000m,
        PayFrequency = PayFrequency.Biweekly,
        DentalBenefit = DentalBenefitCode.PayeeOnly,
    };

    private static PayRun Run(string id, DateTime payDate, string employeeId, decimal gross,
                              decimal cpp = 100m, decimal ei = 30m, decimal fed = 200m, decimal prov = 90m) => new()
    {
        Id = id,
        PayDate = payDate,
        Status = PayRunStatus.Approved,
        Lines =
        {
            new PayRunLine
            {
                EmployeeId = employeeId,
                EmployeeName = "Dana Smith",
                Province = "AB",
                GrossPay = gross,
                CppEmployee = cpp,
                CppEmployer = cpp,
                EiEmployee = ei,
                EiEmployer = Math.Round(ei * 1.4m, 2),
                FederalTax = fed,
                ProvincialTax = prov,
                NetPay = gross - cpp - ei - fed - prov,
            },
        },
    };

    private static T4Return BuiltReturn(CompanyData data, int year = 2026) => new T4Service().Build(data, year);

    #region Assembling the return

    [Fact]
    public void AYearsSlips_AreTheSumOfThatYearsApprovedRuns()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));
        data.PayRuns.Add(Run("PR-0002", new DateTime(2026, 7, 17), "EMP-001", 2000m));

        T4Slip slip = BuiltReturn(data).Slips.Single();

        Assert.Equal(4000m, slip.EmploymentIncome);
        Assert.Equal(200m, slip.CppContributions);
        Assert.Equal(60m, slip.EiPremiums);
        Assert.Equal(580m, slip.IncomeTaxDeducted);
    }

    [Fact]
    public void IncomeTax_CombinesFederalAndProvincial()
    {
        // Box 22 is a single figure. Reporting only the federal half would understate every
        // employee's withholding on their own return.
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m, fed: 200m, prov: 90m));

        Assert.Equal(290m, BuiltReturn(data).Slips.Single().IncomeTaxDeducted);
    }

    [Fact]
    public void AnotherYearsRuns_AreNotIncluded()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2025, 12, 19), "EMP-001", 2000m));
        data.PayRuns.Add(Run("PR-0002", new DateTime(2026, 7, 3), "EMP-001", 3000m));

        Assert.Equal(3000m, BuiltReturn(data).Slips.Single().EmploymentIncome);
    }

    [Fact]
    public void AVoidedRunAndItsReversal_CancelOutOfTheSlip()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        PayRun second = Run("PR-0002", new DateTime(2026, 7, 17), "EMP-001", 2000m);
        data.PayRuns.Add(second);
        new PayrollService().Void(data, second);

        // Only the first run survives. If the reversal were dropped while the voided run was
        // kept, or the other way round, this employee's T4 would not match their pay stubs.
        Assert.Equal(2000m, BuiltReturn(data).Slips.Single().EmploymentIncome);
    }

    [Fact]
    public void AnEmployeeWhoseEntireYearWasVoided_GetsNoSlipAtAll()
    {
        CompanyData data = Data(Person());
        PayRun run = Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m);
        data.PayRuns.Add(run);
        new PayrollService().Void(data, run);

        Assert.Empty(BuiltReturn(data).Slips);
    }

    [Fact]
    public void ADraftRun_IsNotReported()
    {
        CompanyData data = Data(Person());
        PayRun draft = Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m);
        draft.Status = PayRunStatus.Draft;
        data.PayRuns.Add(draft);

        Assert.Empty(BuiltReturn(data).Slips);
    }

    [Fact]
    public void ExemptEmployment_ReportsNilEarningsRatherThanGross()
    {
        // CRA is explicit: exempt employment files 0.00 in boxes 24 and 26. Filing gross there
        // would imply contributions that were never made.
        Employee employee = Person();
        employee.IsCppExempt = true;
        employee.IsEiExempt = true;

        CompanyData data = Data(employee);
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m, cpp: 0m, ei: 0m));

        T4Slip slip = BuiltReturn(data).Slips.Single();

        Assert.Equal(2000m, slip.EmploymentIncome);
        Assert.Equal(0m, slip.InsurableEarnings);
        Assert.Equal(0m, slip.PensionableEarnings);
        Assert.True(slip.CppExemptAllYear);
        Assert.True(slip.EiExemptAllYear);
    }

    #region Amendments

    [Fact]
    public void AnAmendment_CarriesAOnBothTheSlipAndTheSummary()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        T4Return t4 = BuiltReturn(data);
        t4.ReportType = T4ReportType.Amendment;

        XDocument doc = T4XmlWriter.Build(t4);

        Assert.Equal("A", doc.Descendants("T4Slip").Single().Element("rpt_tcd")!.Value);
        Assert.Equal("A", doc.Descendants("T4Summary").Single().Element("rpt_tcd")!.Value);
    }

    [Fact]
    public void ACancellation_CarriesCOnTheSlipButAOnTheSummary()
    {
        // CRA lists O and A for the summary and O, A and C for the slip. Writing C on the
        // summary would be a value the specification does not define.
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        T4Return t4 = BuiltReturn(data);
        t4.ReportType = T4ReportType.Cancel;

        XDocument doc = T4XmlWriter.Build(t4);

        Assert.Equal("C", doc.Descendants("T4Slip").Single().Element("rpt_tcd")!.Value);
        Assert.Equal("A", doc.Descendants("T4Summary").Single().Element("rpt_tcd")!.Value);
    }

    [Fact]
    public void TheAmendmentNote_IsWrittenOnlyForAnAmendment()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        T4Return t4 = BuiltReturn(data);
        t4.AmendmentNote = "Box 14 corrected for a missed bonus.";

        // An original carrying the element would be a value CRA accepts for type A only.
        Assert.Null(T4XmlWriter.Build(t4).Descendants("fileramendmentnote").FirstOrDefault());

        t4.ReportType = T4ReportType.Amendment;

        Assert.Equal("Box 14 corrected for a missed bonus.",
            T4XmlWriter.Build(t4).Descendants("fileramendmentnote").Single().Value);
    }

    [Fact]
    public void AnEmptyAmendmentNote_IsOmittedRatherThanWrittenBlank()
    {
        // An optional element carrying no value rejects the whole submission.
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        T4Return t4 = BuiltReturn(data);
        t4.ReportType = T4ReportType.Amendment;
        t4.AmendmentNote = "   ";

        Assert.Null(T4XmlWriter.Build(t4).Descendants("fileramendmentnote").FirstOrDefault());
    }

    [Fact]
    public void TheSummaryTotals_FollowTheSlipsActuallyFiled_NotTheWholeYear()
    {
        // CRA: the totals are those reported from the slips filed with this summary. An
        // amendment for one employee must not total the other three.
        CompanyData data = Data(Person("EMP-001", "Dana Smith"), Person("EMP-002", "Alex Jones"));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));
        data.PayRuns.Add(Run("PR-0002", new DateTime(2026, 7, 3), "EMP-002", 5000m));

        T4Return whole = BuiltReturn(data);
        Assert.Equal(7000m, whole.TotalEmploymentIncome);

        var amendment = new T4Return
        {
            TaxYear = whole.TaxYear,
            PayrollAccountNumber = whole.PayrollAccountNumber,
            EmployerName = whole.EmployerName,
            ContactName = whole.ContactName,
            ContactPhone = whole.ContactPhone,
            ReportType = T4ReportType.Amendment,
            Slips = whole.Slips.Where(s => s.EmployeeId == "EMP-002").ToList(),
        };

        Assert.Equal(5000m, amendment.TotalEmploymentIncome);
        Assert.Equal("1", T4XmlWriter.Build(amendment).Descendants("slp_cnt").Single().Value);
    }

    #endregion

    [Fact]
    public void Boxes24And26_AreCappedAtTheYearsCeilings_ForAHighEarner()
    {
        // Boxes 24 and 26 report the portion of pay that was actually insurable and pensionable,
        // not gross. Someone on $200,000 stopped contributing part way through the year, and a
        // slip reporting the whole salary there looks entirely reasonable while leaving CRA to
        // find the premiums short against the earnings.
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 200_000m));

        T4Slip slip = BuiltReturn(data).Slips.Single();

        Assert.Equal(200_000m, slip.EmploymentIncome);
        Assert.True(slip.InsurableEarnings < 200_000m, "box 24 was not capped");
        Assert.True(slip.PensionableEarnings < 200_000m, "box 26 was not capped");
    }

    [Theory]
    [InlineData("Dana Smith", "Smith", "Dana", "")]
    [InlineData("Dana Marie Smith", "Smith", "Dana", "M")]
    [InlineData("Cher", "Cher", "", "")]
    [InlineData("", "", "", "")]
    public void TheNameIsSplitIntoTheThreeFieldsTheSlipWants(
        string full, string surname, string given, string initial)
    {
        CompanyData data = Data(Person(name: full));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        T4Slip slip = BuiltReturn(data).Slips.Single();

        Assert.Equal((surname, given, initial), (slip.Surname, slip.GivenName, slip.Initial));
    }

    #endregion

    #region The XML CRA will accept

    private static XElement Slip(T4Return t4) =>
        T4XmlWriter.Build(t4).Root!.Element("T4")!.Elements("T4Slip").First();

    private static XElement Summary(T4Return t4) =>
        T4XmlWriter.Build(t4).Root!.Element("T4")!.Element("T4Summary")!;

    private static T4Return WithOneSlip()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));
        return BuiltReturn(data);
    }

    [Fact]
    public void NoElementIsEverWrittenEmpty()
    {
        // This is CRA's rejection rule from October 2025, and it applies to the whole document.
        // An employee with almost nothing filled in is the case that would trip it.
        CompanyData data = Data(new Employee { Id = "EMP-001", Name = "Nomad", Province = "AB" });
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        XDocument doc = T4XmlWriter.Build(BuiltReturn(data));

        string[] mayBeEmpty = ["Return", "T4", "T4Slip", "T4Summary", "EMPE_NM", "EMPE_ADDR",
                               "EMPR_NM", "EMPR_ADDR", "CNTC", "T4_AMT", "T4_TAMT"];

        foreach (XElement element in doc.Descendants())
        {
            if (element.HasElements || mayBeEmpty.Contains(element.Name.LocalName))
            {
                continue;
            }

            Assert.False(string.IsNullOrEmpty(element.Value),
                $"<{element.Name.LocalName}> was written with no value, which rejects the whole submission.");
        }
    }

    [Fact]
    public void ThePayrollAccountNumberOnTheSlipMatchesTheSummary()
    {
        // CRA validates this pair explicitly. They are both taken from the return rather than
        // from anything per-employee, which is what makes it impossible for them to diverge.
        T4Return t4 = WithOneSlip();

        Assert.Equal(Bn, Slip(t4).Element("bn")!.Value);
        Assert.Equal(Bn, Summary(t4).Element("bn")!.Value);
    }

    [Fact]
    public void RequiredEarningsBoxes_ArePresentEvenWhenNil()
    {
        // Boxes 24 and 26 are required. For exempt employment they are 0.00, NOT omitted,
        // which is the opposite of the rule for every other amount.
        Employee employee = Person();
        employee.IsCppExempt = true;
        employee.IsEiExempt = true;

        CompanyData data = Data(employee);
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m, cpp: 0m, ei: 0m));

        XElement amounts = Slip(BuiltReturn(data)).Element("T4_AMT")!;

        Assert.Equal("0.00", amounts.Element("ei_insu_ern_amt")!.Value);
        Assert.Equal("0.00", amounts.Element("cpp_qpp_ern_amt")!.Value);

        // The zero-valued optional ones are gone entirely.
        Assert.Null(amounts.Element("cpp_cntrb_amt"));
        Assert.Null(amounts.Element("empe_eip_amt"));
    }

    [Fact]
    public void AMissingSin_IsWrittenAsNineZeroesRatherThanOmitted()
    {
        // CRA's defined value for "the employee did not give me one". The element is required,
        // so leaving it out rejects the submission.
        CompanyData data = Data(Person(sin: string.Empty));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        Assert.Equal("000000000", Slip(BuiltReturn(data)).Element("sin")!.Value);
    }

    [Fact]
    public void ExemptCodes_AreWrittenAsZeroOrOne()
    {
        T4Return t4 = WithOneSlip();
        XElement slip = Slip(t4);

        Assert.Equal("0", slip.Element("cpp_qpp_xmpt_cd")!.Value);
        Assert.Equal("0", slip.Element("ei_xmpt_cd")!.Value);
    }

    [Fact]
    public void TheDentalCode_IsWrittenAsCrasOwnNumber()
    {
        Assert.Equal("2", Slip(WithOneSlip()).Element("empr_dntl_ben_rpt_cd")!.Value);
    }

    [Fact]
    public void TheContactPhone_IsSplitIntoAreaCodeAndHyphenatedNumber()
    {
        XElement contact = Summary(WithOneSlip()).Element("CNTC")!;

        Assert.Equal("403", contact.Element("cntc_area_cd")!.Value);
        Assert.Equal("555-1234", contact.Element("cntc_phn_nbr")!.Value);
    }

    [Fact]
    public void TheSummaryCountsItsSlips()
    {
        CompanyData data = Data(Person(), Person("EMP-002", "Alex Jones", "046454286"));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        PayRun second = Run("PR-0002", new DateTime(2026, 7, 3), "EMP-002", 1500m);
        second.Lines[0].EmployeeId = "EMP-002";
        data.PayRuns.Add(second);

        T4Return t4 = BuiltReturn(data);

        Assert.Equal("2", Summary(t4).Element("slp_cnt")!.Value);
        Assert.Equal(2, t4.Slips.Count);
    }

    [Fact]
    public void AmountsAreWrittenWithoutSeparators()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 12345.67m));

        Assert.Equal("12345.67", Slip(BuiltReturn(data)).Element("T4_AMT")!.Element("empt_incamt")!.Value);
    }

    [Fact]
    public void TheReportTypeAppearsOnBothTheSlipAndTheSummary()
    {
        // CRA refuses a return that mixes originals and amendments, so the two must agree.
        T4Return t4 = WithOneSlip();
        t4.ReportType = T4ReportType.Amendment;

        Assert.Equal("A", Slip(t4).Element("rpt_tcd")!.Value);
        Assert.Equal("A", Summary(t4).Element("rpt_tcd")!.Value);
    }

    [Fact]
    public void AQuebecEmployeeFilesQppBoxesAndNeverCppOnes()
    {
        // CRA: "Under no circumstances should amounts for both CPP and QPP appear on the same
        // slip." So the CPP elements are absent entirely, not zeroed.
        CompanyData data = Data(Person());
        data.Employees[0].Province = "QC";
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));
        data.PayRuns[0].Lines[0].QpipEmployee = 8.60m;
        data.PayRuns[0].Lines[0].QpipEmployer = 12.04m;

        XElement amounts = Slip(BuiltReturn(data)).Element("T4_AMT")!;

        Assert.NotNull(amounts.Element("qpp_cntrb_amt"));
        Assert.Null(amounts.Element("cpp_cntrb_amt"));
        Assert.Null(amounts.Element("cppe_cntrb_amt"));
    }

    [Fact]
    public void AQuebecEmployeeFilesQpipInBoxes55And56()
    {
        CompanyData data = Data(Person());
        data.Employees[0].Province = "QC";
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));
        data.PayRuns[0].Lines[0].QpipEmployee = 8.60m;

        XElement amounts = Slip(BuiltReturn(data)).Element("T4_AMT")!;

        Assert.Equal("8.60", amounts.Element("prov_pip_amt")!.Value);
        Assert.Equal("2000.00", amounts.Element("prov_insu_ern_amt")!.Value);
    }

    [Fact]
    public void ANonQuebecEmployeeCarriesNoQpipElementsAtAll()
    {
        // They are optional, and an optional element with no value rejects the submission.
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        XElement slip = Slip(BuiltReturn(data));

        Assert.Null(slip.Element("T4_AMT")!.Element("prov_pip_amt"));
        Assert.Null(slip.Element("T4_AMT")!.Element("prov_insu_ern_amt"));
        Assert.Null(slip.Element("prov_pip_xmpt_cd"));
    }

    [Fact]
    public void TheSummaryCppTotalsExcludeQuebecsQpp()
    {
        // CRA states QPP must not be included in the CPP totals, and there is no QPP total on
        // the T4 Summary at all: it goes to Revenu Quebec on the RL-1 Summary instead. So a
        // mixed employer totals less here than the sum of their slips, correctly.
        CompanyData data = Data(Person(), Person("EMP-002", "Alex Jones", "046454286"));
        data.Employees[1].Province = "QC";

        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m, cpp: 100m));

        PayRun quebec = Run("PR-0002", new DateTime(2026, 7, 3), "EMP-002", 2000m, cpp: 120m);
        quebec.Lines[0].EmployeeId = "EMP-002";
        data.PayRuns.Add(quebec);

        T4Return t4 = BuiltReturn(data);

        Assert.Equal(2, t4.Slips.Count);
        Assert.Equal(100m, t4.TotalEmployeeCpp);
    }

    #endregion

    #region Refusing to file something wrong

    [Fact]
    public void AMissingSin_WarnsButDoesNotBlockFiling()
    {
        // CRA defines all zeroes for this case and the XML writer files it, so refusing to file
        // was this app enforcing a rule stricter than the one it implements. It also could not
        // be cleared from the year end screen, which left the export button permanently dead.
        CompanyData data = Data(Person(sin: string.Empty));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        T4Return t4 = BuiltReturn(data);

        Assert.Empty(T4Service.Validate(data, t4));
        Assert.Contains(T4Service.Warnings(t4),
            w => w.Contains("social insurance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnEmployeeWithASin_ProducesNoWarning()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        Assert.Empty(T4Service.Warnings(BuiltReturn(data)));
    }

    [Fact]
    public void TheThingsCraActuallyRejects_StillBlock()
    {
        // The filing details are required and are all editable on the year end screen, which is
        // what separates them from a missing SIN.
        CompanyData data = Data(Person());
        data.Settings.Company.PayrollAccountNumber = null;
        data.Settings.Company.PayrollContactName = null;
        data.Settings.Company.PayrollContactPhone = null;
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        List<string> problems = T4Service.Validate(data, BuiltReturn(data));

        Assert.Contains(problems, p => p.Contains("payroll account number", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(problems, p => p.Contains("contact name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(problems, p => p.Contains("phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ADraftRunInTheYear_BlocksFiling()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        PayRun draft = Run("PR-0002", new DateTime(2026, 8, 14), "EMP-001", 2000m);
        draft.Status = PayRunStatus.Draft;
        data.PayRuns.Add(draft);

        List<string> problems = T4Service.Validate(data, BuiltReturn(data));

        Assert.Contains(problems, p => p.Contains("draft", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AMissingSin_IsReportedWithTheConsequenceRatherThanJustFlagged()
    {
        // Still says what it costs the employee, but as a warning: CRA accepts the slip, so
        // this must not be the reason a return cannot be filed.
        CompanyData data = Data(Person(sin: string.Empty));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        List<string> warnings = T4Service.Warnings(BuiltReturn(data));

        Assert.Contains(warnings, w => w.Contains("contributions will not be credited"));
    }

    [Fact]
    public void EverythingWrongIsReportedAtOnce()
    {
        // An employer missing three things wants all three, not one per attempt.
        CompanyData data = Data(Person());
        data.Settings.Company.PayrollAccountNumber = null;
        data.Settings.Company.PayrollContactName = null;
        data.Settings.Company.PayrollContactPhone = null;
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        List<string> problems = T4Service.Validate(data, BuiltReturn(data));

        Assert.True(problems.Count >= 3, $"expected several problems, got {problems.Count}");
    }

    [Fact]
    public void AGoodReturn_HasNothingToReport()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        Assert.Empty(T4Service.Validate(data, BuiltReturn(data)));
    }

    [Theory]
    [InlineData("123456789RP0001", true)]
    [InlineData("123456789rp0001", true)]
    [InlineData("123456789 RP 0001", true)]
    [InlineData("123456789RP", false)]
    [InlineData("123456789RC0001", false)]
    [InlineData("12345678RP00012", false)]
    [InlineData("", false)]
    public void ThePayrollAccountNumberIsCheckedAgainstItsRealFormat(string value, bool valid)
    {
        // RC is the corporation tax account and RP is payroll. They differ by one letter and
        // an employer looking at a CRA statement can easily copy the wrong one.
        Assert.Equal(valid, T4Service.IsPayrollAccountNumber(value));
    }

    #endregion

    #region Writing the file

    [Fact]
    public void TheXmlIsAlsoAvailableAsText_WhichIsWhatGetsSavedAndPreviewed()
    {
        CompanyData data = Data(Person());
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        string xml = T4XmlWriter.BuildString(BuiltReturn(data));

        Assert.Contains("<Return>", xml, StringComparison.Ordinal);
        Assert.Contains("046454286", xml, StringComparison.Ordinal);

        // Must parse back. A string built by hand somewhere in this path would not.
        Assert.NotNull(XDocument.Parse(xml));
    }

    [Fact]
    public void AnEmployeesAddress_ReachesTheSlipWhenThereIsOne()
    {
        // The address element is left out entirely when empty, because CRA rejects an optional
        // element that carries no value. The other side of that branch needs its own test.
        CompanyData data = Data(Person());
        data.Employees[0].Address = new ArgoBooks.Core.Models.Common.Address
        {
            Street = "42 Employee Road",
            City = "Calgary",
            State = "AB",
            ZipCode = "T2P1A1",
        };
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 7, 3), "EMP-001", 2000m));

        string xml = T4XmlWriter.BuildString(BuiltReturn(data));

        Assert.Contains("42 Employee Road", xml, StringComparison.Ordinal);
    }

    #endregion
}
