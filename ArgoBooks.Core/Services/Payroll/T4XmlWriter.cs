using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ArgoBooks.Core.Models.Payroll;

namespace ArgoBooks.Core.Services.Payroll;

/// <summary>
/// Writes a T4 return as the XML CRA accepts for electronic filing, following the 2026V4
/// specification.
///
/// Two of CRA's rules shape almost every decision here:
///
/// An optional element carrying no value causes the whole submission to be REJECTED, a
/// validation added in October 2025. So nothing is written speculatively: every optional
/// element is omitted unless it has content. Only the elements CRA marks required are always
/// present, and those are filled with a defined empty value rather than left blank.
///
/// The payroll account number on every slip must equal the one on the summary. It is
/// therefore taken from the return and written to both, never from anything per-employee.
/// </summary>
public static class T4XmlWriter
{
    /// <summary>
    /// Builds the return document. Does not include the T619 transmittal record, which wraps
    /// a submission and belongs to whoever transmits it rather than to the return.
    /// </summary>
    public static XDocument Build(T4Return t4)
    {
        ArgumentNullException.ThrowIfNull(t4);

        var slips = t4.Slips.Select(slip => BuildSlip(t4, slip));

        var root = new XElement("Return",
            new XElement("T4",
                slips,
                BuildSummary(t4)));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    public static string BuildString(T4Return t4)
    {
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder, CultureInfo.InvariantCulture);
        Build(t4).Save(writer, SaveOptions.None);
        return builder.ToString();
    }

    private static XElement BuildSlip(T4Return t4, T4Slip slip)
    {
        var name = new XElement("EMPE_NM", Required("snm", Text(slip.Surname, 20)));
        Add(name, "gvn_nm", Text(slip.GivenName, 12));
        Add(name, "init", Text(slip.Initial, 1));

        var address = new XElement("EMPE_ADDR");
        Add(address, "addr_l1_txt", Text(slip.Address.Street, 30));
        Add(address, "cty_nm", Text(slip.Address.City, 28));
        Add(address, "prov_cd", Upper(slip.Address.State, 2));
        Add(address, "cntry_cd", Upper(slip.Address.Country, 3));
        Add(address, "pstl_cd", Upper(slip.Address.ZipCode, 10));

        var amounts = new XElement("T4_AMT");

        // Boxes 24 and 26 are required even when nil, which is why they are added
        // unconditionally while the others are not. CRA states explicitly that exempt
        // employment files 0.00 rather than omitting the element.
        Add(amounts, "empt_incamt", Money(slip.EmploymentIncome));

        // CRA: "Under no circumstances should amounts for both CPP and QPP appear on the same
        // slip." Quebec employees contribute to QPP, so their money goes in boxes 17 and 17A
        // and the CPP elements are omitted entirely rather than written as zero.
        if (slip.IsQuebec)
        {
            Add(amounts, "qpp_cntrb_amt", Money(slip.CppContributions));
            Add(amounts, "qppe_cntrb_amt", Money(slip.Cpp2Contributions));
        }
        else
        {
            Add(amounts, "cpp_cntrb_amt", Money(slip.CppContributions));
            Add(amounts, "cppe_cntrb_amt", Money(slip.Cpp2Contributions));
        }

        Add(amounts, "empe_eip_amt", Money(slip.EiPremiums));
        Add(amounts, "itx_ddct_amt", Money(slip.IncomeTaxDeducted));
        amounts.Add(new XElement("ei_insu_ern_amt", Money(slip.InsurableEarnings) ?? "0.00"));
        amounts.Add(new XElement("cpp_qpp_ern_amt", Money(slip.PensionableEarnings) ?? "0.00"));

        // Boxes 55 and 56, Quebec only. Optional, so omitted rather than zeroed elsewhere.
        Add(amounts, "prov_pip_amt", Money(slip.QpipPremiums));
        Add(amounts, "prov_insu_ern_amt", Money(slip.QpipInsurableEarnings));

        var element = new XElement("T4Slip", name);

        if (address.HasElements)
        {
            element.Add(address);
        }

        // All zeroes is CRA's defined value for a missing SIN. It is accepted and it is also a
        // flag: the guide warns that it stops the employee's CPP being credited to them.
        element.Add(new XElement("sin", Digits(slip.Sin, 9) is { Length: 9 } sin ? sin : "000000000"));
        Add(element, "empe_nbr", Text(slip.EmployeeNumber, 20));
        element.Add(new XElement("bn", Upper(t4.PayrollAccountNumber, 15) ?? string.Empty));
        element.Add(new XElement("cpp_qpp_xmpt_cd", slip.CppExemptAllYear ? "1" : "0"));
        element.Add(new XElement("ei_xmpt_cd", slip.EiExemptAllYear ? "1" : "0"));

        // Box 28's QPIP exemption. Only meaningful in Quebec, and optional, so it is written
        // only where it says something.
        if (slip.IsQuebec)
        {
            element.Add(new XElement("prov_pip_xmpt_cd", slip.QpipPremiums > 0 ? "0" : "1"));
        }
        element.Add(new XElement("rpt_tcd", ReportCode(t4.ReportType)));
        element.Add(new XElement("empt_prov_cd", Upper(slip.ProvinceOfEmployment, 2) ?? string.Empty));
        element.Add(new XElement("empr_dntl_ben_rpt_cd", ((int)slip.DentalBenefit).ToString(CultureInfo.InvariantCulture)));
        element.Add(amounts);

        return element;
    }

    private static XElement BuildSummary(T4Return t4)
    {
        var name = new XElement("EMPR_NM", Required("l1_nm", Text(t4.EmployerName, 30)));

        var address = new XElement("EMPR_ADDR");
        Add(address, "addr_l1_txt", Text(t4.EmployerAddress.Street, 30));
        Add(address, "cty_nm", Text(t4.EmployerAddress.City, 28));
        Add(address, "prov_cd", Upper(t4.EmployerAddress.State, 2));
        Add(address, "cntry_cd", Upper(t4.EmployerAddress.Country, 3));
        Add(address, "pstl_cd", Upper(t4.EmployerAddress.ZipCode, 10));

        // CRA wants the area code separately from the rest, and the rest hyphenated as 3-4.
        string phone = Digits(t4.ContactPhone, 10) ?? string.Empty;
        var contact = new XElement("CNTC",
            new XElement("cntc_nm", Text(t4.ContactName, 22) ?? string.Empty),
            new XElement("cntc_area_cd", phone.Length >= 10 ? phone[..3] : string.Empty),
            new XElement("cntc_phn_nbr", phone.Length >= 10 ? $"{phone[3..6]}-{phone[6..10]}" : string.Empty));

        var totals = new XElement("T4_TAMT");
        Add(totals, "tot_empt_incamt", Money(t4.TotalEmploymentIncome));
        Add(totals, "tot_empe_cpp_amt", Money(t4.TotalEmployeeCpp));
        Add(totals, "tot_empe_cppe_amt", Money(t4.TotalEmployeeCpp2));
        Add(totals, "tot_empe_eip_amt", Money(t4.TotalEmployeeEi));
        Add(totals, "tot_itx_ddct_amt", Money(t4.TotalIncomeTax));
        Add(totals, "tot_empr_cpp_amt", Money(t4.TotalEmployerCpp));
        Add(totals, "tot_empr_cppe_amt", Money(t4.TotalEmployerCpp2));
        Add(totals, "tot_empr_eip_amt", Money(t4.TotalEmployerEi));

        var summary = new XElement("T4Summary",
            new XElement("bn", Upper(t4.PayrollAccountNumber, 15) ?? string.Empty),
            name);

        if (address.HasElements)
        {
            summary.Add(address);
        }

        summary.Add(contact);
        summary.Add(new XElement("tx_yr", t4.TaxYear.ToString(CultureInfo.InvariantCulture)));
        summary.Add(new XElement("slp_cnt", t4.Slips.Count.ToString(CultureInfo.InvariantCulture)));
        summary.Add(new XElement("rpt_tcd", ReportCode(t4.ReportType)));
        summary.Add(totals);

        return summary;
    }

    /// <summary>Adds the element only if there is something to put in it.</summary>
    private static void Add(XElement parent, string element, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parent.Add(new XElement(element, value));
        }
    }

    private static XElement Required(string element, string? value) =>
        new(element, value ?? string.Empty);

    private static string ReportCode(T4ReportType type) => type switch
    {
        T4ReportType.Amendment => "A",
        T4ReportType.Cancel => "C",
        _ => "O",
    };

    /// <summary>
    /// Dollars and cents, no separators. Negatives are not permitted by the specification, so
    /// anything below zero is written as nil rather than silently sign-flipped: a negative
    /// total means a voided run was mishandled upstream and should be found, not papered over.
    /// </summary>
    private static string? Money(decimal value) =>
        value <= 0 ? null : value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string? Text(string? value, int max)
    {
        string trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed[..Math.Min(trimmed.Length, max)];
    }

    private static string? Upper(string? value, int max) => Text(value, max)?.ToUpperInvariant();

    private static string? Digits(string? value, int max)
    {
        string digits = new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return digits.Length == 0 ? null : digits[..Math.Min(digits.Length, max)];
    }
}
