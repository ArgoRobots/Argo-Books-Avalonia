namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// How much is actually in a company file, recorded once per company per session at the point
/// it opens.
///
/// <para>
/// Nothing else distinguishes a file someone is poking at from a file someone runs their
/// business on, and the two need opposite responses: an empty file that stops being opened is
/// a first-run problem, a full one that stops being opened is a retention problem.
/// </para>
///
/// <para>
/// Counts only, unlike <see cref="CompanyProfileEvent"/>. No names, amounts, dates or any other
/// content leaves the machine, so this stays anonymous and carries no separate disclosure.
/// </para>
/// </summary>
public class CompanyScaleEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.CompanyScale;

    public int Expenses { get; set; }
    public int Revenues { get; set; }
    public int Invoices { get; set; }
    public int Payments { get; set; }
    public int Customers { get; set; }
    public int Suppliers { get; set; }
    public int Products { get; set; }
    public int Categories { get; set; }
    public int Receipts { get; set; }
    public int Employees { get; set; }

    /// <summary>
    /// Rows waiting on the Bank Matching page. High with everything else at zero is someone
    /// who imported a statement and never did anything with it.
    /// </summary>
    public int BankLines { get; set; }
}
