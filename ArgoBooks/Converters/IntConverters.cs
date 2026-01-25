using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Integer converters for various UI elements.
/// </summary>
public static class IntConverters
{
    /// <summary>
    /// Returns true if the integer is zero.
    /// </summary>
    public static readonly IValueConverter IsZero =
        new FuncValueConverter<int, bool>(value => value == 0);

    /// <summary>
    /// Returns true if the integer is positive (greater than zero).
    /// </summary>
    public static readonly IValueConverter IsPositive =
        new FuncValueConverter<int, bool>(value => value > 0);

    /// <summary>
    /// Returns true if the integer is greater than one.
    /// Useful for showing pagination controls only when there are multiple pages.
    /// </summary>
    public static readonly IValueConverter IsGreaterThanOne =
        new FuncValueConverter<int, bool>(value => value > 1);

    /// <summary>
    /// Returns true if the integer is not zero.
    /// </summary>
    public static readonly IValueConverter IsNotZero =
        new FuncValueConverter<int, bool>(value => value != 0);
}
