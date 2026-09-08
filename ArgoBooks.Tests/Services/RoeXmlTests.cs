using System.Xml.Linq;
using System.Xml.Schema;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services.Payroll;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ROE Web payroll extract.
///
/// ValidatesAgainstTheServiceCanadaSchema is the real check, against PayrollExtractXmlV2.xsd in
/// Schemas/ServiceCanada. It earned its place immediately: Appendix D and the schema disagree on
/// the middle name, surname and address line lengths, and on FN being mandatory, and the prose
/// was wrong on all four.
///
/// The rest assert the things a schema cannot: pay period ordering, that block 16 is never
/// defaulted, and that nothing is written empty.
/// </summary>
public class RoeXmlTests
{
    private const string Bn = "123456789RP0001";

    private const string Version = "2.0.15";

    private static CompanyData Data()
    {
        var data = new CompanyData();
        data.Settings.Company.Name = "Test Company";
        data.Settings.Company.PayrollAccountNumber = Bn;
        data.Settings.Company.PayrollContactName = "Pat Owner";
        data.Settings.Company.PayrollContactPhone = "3065551234";
        return data;
    }

    private static Employee Person(PayFrequency frequency = PayFrequency.Biweekly) => new()
    {
        Id = "EMP-001",
        Name = "Dana Marie Smith",
        Sin = "046454286",
        Province = "SK",
        PayType = PayType.Hourly,
        PayRate = 25m,
        PayFrequency = frequency,
        StartDate = new DateTime(2025, 1, 6),
        Address = { Street = "1 Main Street", City = "Saskatoon", State = "SK", ZipCode = "S7K 1A1", Country = "CAN" },
    };

    private static PayRun Run(string id, DateTime periodEnd, decimal gross, decimal hours) => new()
    {
        Id = id,
        PayDate = periodEnd.AddDays(3),
        PeriodStart = periodEnd.AddDays(-13),
        PeriodEnd = periodEnd,
        Status = PayRunStatus.Approved,
        Lines =
        {
            new PayRunLine
            {
                EmployeeId = "EMP-001",
                EmployeeName = "Dana Marie Smith",
                Province = "SK",
                GrossPay = gross,
                HoursWorked = hours,
                NetPay = gross,
            },
        },
    };

    /// <summary>A worksheet with everything the export needs, so each test can spoil one thing.</summary>
    private static RoeWorksheet Sheet(PayFrequency frequency = PayFrequency.Biweekly)
    {
        CompanyData data = Data();
        data.Employees.Add(Person(frequency));
        data.PayRuns.Add(Run("PR-0001", new DateTime(2026, 6, 12), 2000m, 80m));
        data.PayRuns.Add(Run("PR-0002", new DateTime(2026, 6, 26), 1800m, 72m));
        data.PayRuns.Add(Run("PR-0003", new DateTime(2026, 7, 10), 2200m, 88m));

        RoeWorksheet sheet = new RoeService().Build(data, "EMP-001");
        sheet.Reason = RoeReason.ShortageOfWork;
        return sheet;
    }

    private static XElement Roe(RoeWorksheet sheet, bool draft = false)
        => RoeXmlWriter.Build(sheet, Version, draft).Root!.Element("ROE")!;

    #region The header

    [Fact]
    public void TheHeader_CarriesTheOnlyFileVersionRoeWebAccepts()
    {
        XElement header = RoeXmlWriter.Build(Sheet(), Version).Root!;

        Assert.Equal("ROEHEADER", header.Name.LocalName);
        Assert.Equal("W-2.0", header.Attribute("FileVersion")!.Value);
        Assert.Equal("Argo Books", header.Attribute("ProductName")!.Value);
        Assert.Equal(Version, header.Attribute("ProductVersion")!.Value);
    }

    [Fact]
    public void AMissingProductVersion_IsOmittedRatherThanWrittenEmpty()
    {
        XElement header = RoeXmlWriter.Build(Sheet(), string.Empty).Root!;

        Assert.Null(header.Attribute("ProductVersion"));
    }

    [Fact]
    public void ADraft_IsMarkedD_AndASubmissionS()
    {
        Assert.Equal("D", Roe(Sheet(), draft: true).Attribute("Issue")!.Value);
        Assert.Equal("S", Roe(Sheet()).Attribute("Issue")!.Value);
    }

    [Fact]
    public void SeveralRoes_ShareOneHeader()
    {
        RoeWorksheet a = Sheet();
        RoeWorksheet b = Sheet();

        XElement header = RoeXmlWriter.Build([a, b], Version).Root!;

        Assert.Equal(2, header.Elements("ROE").Count());
    }

    #endregion

    #region Block order and content

    [Fact]
    public void TheBlocks_AreInTheOrderAppendixDLists()
    {
        RoeWorksheet sheet = Sheet();
        sheet.Occupation = "Machinist";
        sheet.PayrollReferenceNumber = "REF-77";
        sheet.Comments = "Plant closed for the season.";

        string[] order = Roe(sheet).Elements().Select(e => e.Name.LocalName).ToArray();

        Assert.Equal(
            ["B3", "B5", "B6", "B8", "B9", "B10", "B11", "B12", "B13", "B14", "B15A", "B15C", "B16", "B18", "B20"],
            order);
    }

    [Fact]
    public void Block15C_NumbersTheFinalPayPeriodFirst()
    {
        // The numbering is the meaning. PP 1 is the final period, so emitting storage order
        // reverses the earnings history and changes the benefit rather than failing.
        XElement periods = Roe(Sheet()).Element("B15C")!;

        string[] numbers = periods.Elements("PP").Select(p => p.Attribute("nbr")!.Value).ToArray();
        string[] amounts = periods.Elements("PP").Select(p => p.Element("AMT")!.Value).ToArray();

        Assert.Equal(["1", "2", "3"], numbers);
        Assert.Equal("2200.00", amounts[0]);
        Assert.Equal("1800.00", amounts[1]);
        Assert.Equal("2000.00", amounts[2]);
    }

    [Fact]
    public void Block9_SplitsTheStoredNameIntoThreeParts()
    {
        XElement employee = Roe(Sheet()).Element("B9")!;

        Assert.Equal("Dana", employee.Element("FN")!.Value);
        // Cut to four: MiddleNameType caps there, whatever Appendix D calls the field.
        Assert.Equal("Mari", employee.Element("MN")!.Value);
        Assert.Equal("Smith", employee.Element("LN")!.Value);
    }

    [Fact]
    public void ASingleWordName_IsRefused()
    {
        // FN and LN are both minOccurs="1". A blank FN would pass the schema and then be an ROE
        // with no first name on it, so this is reported instead.
        RoeWorksheet sheet = Sheet();
        sheet.EmployeeName = "Cher";

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("first and last name"));
    }

    [Fact]
    public void AMiddleNameLongerThanTheSchemaAllows_IsCutToAnInitialLength()
    {
        // Appendix D calls MN a middle name; the schema caps it at four characters. Cut rather
        // than refused, because MN is optional and losing it costs nothing.
        RoeWorksheet sheet = Sheet();
        sheet.EmployeeName = "Dana Alexandra Smith";

        Assert.Equal("Alex", Roe(sheet).Element("B9")!.Element("MN")!.Value);
    }

    [Fact]
    public void AMalformedPayrollAccountNumber_IsRefused()
    {
        RoeWorksheet sheet = Sheet();
        sheet.PayrollAccountNumber = "123456789";

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("Block 5"));
    }

    [Fact]
    public void Block16_CarriesTheSeparationCodeAndTheContact()
    {
        RoeWorksheet sheet = Sheet();
        sheet.Reason = RoeReason.DismissalOrSuspension;

        XElement reason = Roe(sheet).Element("B16")!;

        Assert.Equal("M00", reason.Element("CD")!.Value);
        Assert.Equal("Pat", reason.Element("FN")!.Value);
        Assert.Equal("Owner", reason.Element("LN")!.Value);
        Assert.Equal("306", reason.Element("AC")!.Value);
        Assert.Equal("5551234", reason.Element("TEL")!.Value);
    }

    [Theory]
    [InlineData("3065551234", "306", "5551234")]
    [InlineData("13065551234", "306", "5551234")]
    [InlineData("+1 306 555 1234", "306", "5551234")]
    [InlineData("(306) 555-1234", "306", "5551234")]
    public void Block16_TakesTheNationalNumber_HoweverItWasEntered(string entered, string area, string number)
    {
        // The phone control hands back a dial-code prefixed value, and people paste all four of
        // these. Taking the first ten digits of an eleven digit number shifts every digit one
        // place left and produces a valid looking wrong number.
        RoeWorksheet sheet = Sheet();
        sheet.ContactPhone = entered;

        XElement reason = Roe(sheet).Element("B16")!;

        Assert.Equal(area, reason.Element("AC")!.Value);
        Assert.Equal(number, reason.Element("TEL")!.Value);
    }

    [Fact]
    public void APhoneNumberThatIsNotTenDigits_IsRefused()
    {
        RoeWorksheet sheet = Sheet();
        sheet.ContactPhone = "30655512";

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("telephone"));
    }

    [Theory]
    [InlineData(PayFrequency.Weekly, "W")]
    [InlineData(PayFrequency.Biweekly, "B")]
    [InlineData(PayFrequency.SemiMonthly, "S")]
    [InlineData(PayFrequency.Monthly, "M")]
    public void Block6_IsThePayPeriodTypeCode(PayFrequency frequency, string expected)
    {
        Assert.Equal(expected, Roe(Sheet(frequency)).Element("B6")!.Value);
    }

    [Fact]
    public void Block14_WritesTheDateOnlyWhenSomeoneIsExpectedBack()
    {
        RoeWorksheet unknown = Sheet();
        Assert.Equal("U", Roe(unknown).Element("B14")!.Element("CD")!.Value);
        Assert.Null(Roe(unknown).Element("B14")!.Element("DT"));

        RoeWorksheet returning = Sheet();
        returning.Recall = RoeRecall.ExpectedDate;
        returning.RecallDate = new DateTime(2026, 9, 1);

        XElement recall = Roe(returning).Element("B14")!;
        Assert.Equal("Y", recall.Element("CD")!.Value);
        Assert.Equal("2026-09-01", recall.Element("DT")!.Value);
    }

    [Fact]
    public void DatesUseTheOnlyFormatTheLayoutAccepts()
    {
        XElement roe = Roe(Sheet());

        Assert.Equal("2025-01-06", roe.Element("B10")!.Value);
        Assert.Equal("2026-07-10", roe.Element("B11")!.Value);
        Assert.Equal("2026-07-10", roe.Element("B12")!.Value);
    }

    [Fact]
    public void EveryElement_CarriesAValue()
    {
        // Same discipline as the T4 writer. An element present but empty is the kind of thing a
        // validator rejects without saying why.
        XDocument xml = RoeXmlWriter.Build(Sheet(), Version);

        Assert.Empty(xml.Descendants().Where(e => !e.HasElements && string.IsNullOrEmpty(e.Value)));
    }

    [Fact]
    public void TheDeclarationSaysTheEncodingTheFileIsActuallyIn()
    {
        // Save(TextWriter) takes the declaration from the writer, and a plain StringWriter says
        // UTF-16 however the characters are written out afterwards. The file then announces an
        // encoding it is not in, which nothing here notices and the parser at the far end does.
        string xml = RoeXmlWriter.BuildString(Sheet(), Version);

        Assert.StartsWith("<?xml", xml, StringComparison.Ordinal);
        Assert.Contains("encoding=\"utf-8\"", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("utf-16", xml, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Refusing to produce a bad file

    [Fact]
    public void WithoutAReason_ItRefusesRatherThanGuessing()
    {
        // Quit, dismissal and leave are different legal statements with different consequences
        // for the claim, and the app cannot tell them apart. Defaulting would be a lie.
        RoeWorksheet sheet = Sheet();
        sheet.Reason = null;

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("Block 16"));
        Assert.Throws<InvalidOperationException>(() => RoeXmlWriter.Build(sheet, Version));
    }

    [Fact]
    public void ARecallDatePromised_ButMissing_IsRefused()
    {
        RoeWorksheet sheet = Sheet();
        sheet.Recall = RoeRecall.ExpectedDate;
        sheet.RecallDate = null;

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("Block 14"));
    }

    [Fact]
    public void AFinalPeriodEndingBeforeTheLastDayPaid_IsRefused()
    {
        // The one relationship Service Canada checks between two dates it is given.
        RoeWorksheet sheet = Sheet();
        sheet.FinalPeriodEnd = sheet.LastDayPaid!.Value.AddDays(-1);

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("Block 12"));
    }

    [Fact]
    public void UnknownHours_AreRefusedRatherThanReportedAsZero()
    {
        // Zero insurable hours is a claim, not an absence of one, and it is the difference
        // between a benefit and no benefit.
        RoeWorksheet sheet = Sheet();
        sheet.TotalInsurableHours = null;

        Assert.Contains(RoeXmlWriter.Validate(sheet), p => p.Contains("Block 15A"));
    }

    [Fact]
    public void AValidWorksheet_HasNothingToReport()
    {
        Assert.Empty(RoeXmlWriter.Validate(Sheet()));
    }

    #endregion

    #region The real check, once the schema is available

    private static string SchemaFile =>
        Path.Combine(AppContext.BaseDirectory, "Schemas", "ServiceCanada", "PayrollExtractXmlV2.xsd");

    [Fact]
    public void ValidatesAgainstTheServiceCanadaSchema()
    {
        // Service Canada distributes this through ROE Web rather than publishing it, so it
        // cannot be vendored the way CRA's package is. Drop PayrollExtractXmlV2.xsd into
        // ArgoBooks.Tests/Schemas/ServiceCanada and this starts checking on the next run.
        //
        // Passing while the file is absent is deliberate but is not a result. Until it is
        // there, the layout is only as right as Appendix D was read, which is what every other
        // test in this file is guarding.
        if (!File.Exists(SchemaFile))
        {
            return;
        }

        var schemas = new XmlSchemaSet();
        schemas.Add(null, SchemaFile);

        var errors = new List<string>();
        RoeXmlWriter.Build(Sheet(), Version).Validate(schemas, (_, e) => errors.Add(e.Message));

        Assert.Empty(errors);
    }

    #endregion
}
