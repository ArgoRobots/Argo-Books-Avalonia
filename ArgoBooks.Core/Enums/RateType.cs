namespace ArgoBooks.Core.Enums;

/// <summary>
/// Rate type for rental pricing.
/// </summary>
public enum RateType
{
    /// <summary>Daily rental rate.</summary>
    Daily,

    /// <summary>Weekly rental rate.</summary>
    Weekly,

    /// <summary>Monthly rental rate.</summary>
    Monthly
}

/// <summary>
/// Extension methods for RateType.
/// </summary>
public static class RateTypeExtensions
{
    /// <summary>
    /// Gets all rate type names for UI options.
    /// </summary>
    public static string[] GetAllNames()
    {
        return
        [
            nameof(RateType.Daily),
            nameof(RateType.Weekly),
            nameof(RateType.Monthly)
        ];
    }
}
