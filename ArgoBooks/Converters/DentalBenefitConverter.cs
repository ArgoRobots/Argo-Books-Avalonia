using System.Globalization;
using ArgoBooks.Core.Models.Payroll;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Shows a T4 box 45 dental code as CRA's own wording, with the number kept in front.
///
/// The number matters as much as the description: an employer checking a filed slip against
/// CRA's guide is looking for "3", not for a sentence that means the same thing.
/// </summary>
public class DentalBenefitConverter : IValueConverter
{
    public static readonly DentalBenefitConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DentalBenefitCode code
            ? code switch
            {
                DentalBenefitCode.PayeeOnly => "2 - Them only",
                DentalBenefitCode.PayeeSpouseAndChildren => "3 - Them, their spouse and children",
                DentalBenefitCode.PayeeAndSpouse => "4 - Them and their spouse",
                DentalBenefitCode.PayeeAndChildren => "5 - Them and their children",
                _ => "1 - Not eligible for any coverage",
            }
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("The combo box binds the enum directly; only display goes through here.");
}
