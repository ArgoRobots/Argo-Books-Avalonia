namespace ArgoBooks.Core.Models.Payroll;

/// <summary>
/// How often CRA expects this employer to send the deductions in.
///
/// CRA assigns it from the average monthly withholding amount two calendar years ago, so it is
/// not something the app can work out: a new employer has no history to read, and CRA can also
/// hold someone at quarterly only while their compliance record is perfect. It is told to the
/// employer, and shown in their CRA account, which is why this is a setting rather than a
/// calculation.
///
/// Everything here assumed <see cref="Regular"/> before, which is right for most small employers
/// and wrong in the direction that costs money for the two accelerated types: their deductions
/// are due up to five weeks earlier than the fifteenth of the following month, and CRA charges
/// between 3% and 10% on a late remittance.
/// </summary>
public enum RemitterType
{
    /// <summary>AMWA under $25,000. The fifteenth of the following month.</summary>
    Regular = 0,

    /// <summary>
    /// AMWA under $3,000 with a perfect compliance record, or under $1,000 for a new small
    /// employer. Four times a year rather than twelve.
    /// </summary>
    Quarterly = 1,

    /// <summary>AMWA from $25,000 to $99,999.99. Twice a month.</summary>
    AcceleratedThreshold1 = 2,

    /// <summary>AMWA of $100,000 or more. Four times a month, on working days.</summary>
    AcceleratedThreshold2 = 3,
}

public static class RemitterTypeExtensions
{
    public static string DisplayName(this RemitterType type) => type switch
    {
        RemitterType.Quarterly => "Quarterly",
        RemitterType.AcceleratedThreshold1 => "Accelerated, threshold 1",
        RemitterType.AcceleratedThreshold2 => "Accelerated, threshold 2",
        _ => "Regular (monthly)",
    };

    /// <summary>The plain description, so nobody has to know what an AMWA is to pick correctly.</summary>
    public static string Description(this RemitterType type) => type switch
    {
        RemitterType.Quarterly =>
            "Four times a year, due the 15th of April, July, October and January. CRA puts very "
            + "small employers here.",
        RemitterType.AcceleratedThreshold1 =>
            "Twice a month, due the 25th for the first half and the 10th of the next month for "
            + "the second half.",
        RemitterType.AcceleratedThreshold2 =>
            "Four times a month, due the 3rd working day after the 7th, 14th, 21st and the end "
            + "of the month.",
        _ => "Once a month, due the 15th of the following month. This is the most common.",
    };
}
