namespace ArgoBooks.Core.Enums;

/// <summary>
/// Frequency for recurring items (invoices, payments, etc.).
/// </summary>
public enum Frequency
{
    /// <summary>Once per week.</summary>
    Weekly,

    /// <summary>Every two weeks.</summary>
    BiWeekly,

    /// <summary>Once per month.</summary>
    Monthly,

    /// <summary>Once per quarter (3 months).</summary>
    Quarterly,

    /// <summary>Once per year.</summary>
    Annually
}
