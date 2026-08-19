using System.Globalization;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Localization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Shows a remitter type by its name while the bound value stays the enum.
///
/// Same shape as <see cref="ProvinceNameConverter"/> and for the same reason: everything
/// downstream dispatches on the enum, and only the display goes through here.
/// </summary>
public class RemitterTypeNameConverter : IValueConverter
{
    public static readonly RemitterTypeNameConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RemitterType type ? type.DisplayName().Translate() : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("The combo box binds the enum directly; only display goes through here.");
}
