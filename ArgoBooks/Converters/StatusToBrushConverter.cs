using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// A configurable converter that maps status strings to brush colors.
/// Use this instead of creating multiple individual status-to-brush converters.
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    private readonly Dictionary<string, string> _statusColors;
    private readonly string _defaultColor;

    /// <summary>
    /// Creates a new StatusToBrushConverter with the specified status-to-color mappings.
    /// </summary>
    /// <param name="statusColors">Dictionary mapping status strings to hex color codes.</param>
    /// <param name="defaultColor">Default hex color if status is not found.</param>
    public StatusToBrushConverter(Dictionary<string, string> statusColors, string defaultColor = "#F3F4F6")
    {
        _statusColors = statusColors;
        _defaultColor = defaultColor;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string status)
            return new SolidColorBrush(Color.Parse(_defaultColor));

        var color = _statusColors.TryGetValue(status, out var colorHex) ? colorHex : _defaultColor;
        return new SolidColorBrush(Color.Parse(color));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Pre-configured status converters for common status types.
/// </summary>
public static class StatusConverters
{
    #region Color Palettes

    // Background colors
    private const string GreenBg = "#DCFCE7";
    private const string YellowBg = "#FEF3C7";
    private const string RedBg = "#FEE2E2";
    private const string BlueBg = "#DBEAFE";
    private const string PurpleBg = "#F3E8FF";
    private const string IndigoBg = "#E0E7FF";
    private const string OrangeBg = "#FFEDD5";
    private const string GrayBg = "#F3F4F6";

    // Foreground colors
    private const string GreenFg = "#166534";
    private const string YellowFg = "#92400E";
    private const string RedFg = "#DC2626";
    private const string BlueFg = "#1E40AF";
    private const string PurpleFg = "#7C3AED";
    private const string IndigoFg = "#4F46E5";
    private const string OrangeFg = "#C2410C";
    private const string GrayFg = "#4B5563";

    #endregion

    #region Payment Status

    public static readonly IValueConverter PaymentStatusBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Current"] = GreenBg,
            ["Overdue"] = YellowBg,
            ["Delinquent"] = RedBg
        });

    public static readonly IValueConverter PaymentStatusForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Current"] = GreenFg,
            ["Overdue"] = YellowFg,
            ["Delinquent"] = RedFg
        }, GrayFg);

    #endregion

    #region Transaction Status (Expense/Revenue)

    public static readonly IValueConverter TransactionStatusBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Completed"] = GreenBg,
            ["Pending"] = YellowBg,
            ["Returned"] = BlueBg,
            ["Partial Return"] = PurpleBg,
            ["Cancelled"] = GrayBg
        });

    public static readonly IValueConverter TransactionStatusForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Completed"] = GreenFg,
            ["Pending"] = YellowFg,
            ["Returned"] = BlueFg,
            ["Partial Return"] = PurpleFg,
            ["Cancelled"] = GrayFg
        }, GrayFg);

    #endregion

    #region Invoice Status

    public static readonly IValueConverter InvoiceStatusBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Paid"] = GreenBg,
            ["Pending"] = YellowBg,
            ["Overdue"] = RedBg,
            ["Draft"] = GrayBg,
            ["Sent"] = BlueBg,
            ["Viewed"] = BlueBg,
            ["Partial"] = PurpleBg,
            ["Cancelled"] = GrayBg
        });

    public static readonly IValueConverter InvoiceStatusForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Paid"] = GreenFg,
            ["Pending"] = YellowFg,
            ["Overdue"] = RedFg,
            ["Draft"] = GrayFg,
            ["Sent"] = BlueFg,
            ["Viewed"] = BlueFg,
            ["Partial"] = PurpleFg,
            ["Cancelled"] = GrayFg
        }, GrayFg);

    #endregion

    #region Rental Status

    public static readonly IValueConverter RentalStatusBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Active"] = GreenBg,
            ["Returned"] = BlueBg,
            ["Overdue"] = RedBg,
            ["Cancelled"] = GrayBg
        });

    public static readonly IValueConverter RentalStatusForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Active"] = GreenFg,
            ["Returned"] = BlueFg,
            ["Overdue"] = RedFg,
            ["Cancelled"] = GrayFg
        }, GrayFg);

    #endregion

    #region Rental Item Status

    public static readonly IValueConverter RentalItemStatusBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Available"] = GreenBg,
            ["In Maintenance"] = YellowBg,
            ["All Rented"] = PurpleBg
        });

    public static readonly IValueConverter RentalItemStatusForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Available"] = GreenFg,
            ["In Maintenance"] = YellowFg,
            ["All Rented"] = PurpleFg
        }, GrayFg);

    #endregion

    #region Payment Transaction Status

    public static readonly IValueConverter PaymentTransactionStatusBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Completed"] = GreenBg,
            ["Pending"] = YellowBg,
            ["Partial"] = BlueBg,
            ["Refunded"] = PurpleBg
        });

    public static readonly IValueConverter PaymentTransactionStatusForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Completed"] = GreenFg,
            ["Pending"] = YellowFg,
            ["Partial"] = BlueFg,
            ["Refunded"] = PurpleFg
        }, GrayFg);

    #endregion

    #region History Type

    public static readonly IValueConverter HistoryTypeBadgeBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Rental"] = BlueBg,
            ["Purchase"] = PurpleBg,
            ["Return"] = OrangeBg,
            ["Payment"] = GreenBg
        });

    public static readonly IValueConverter HistoryTypeBadgeForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Rental"] = BlueFg,
            ["Purchase"] = PurpleFg,
            ["Return"] = OrangeFg,
            ["Payment"] = GreenFg
        }, GrayFg);

    #endregion

    #region History Status

    public static readonly IValueConverter HistoryStatusBadgeBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Completed"] = GreenBg,
            ["Pending"] = YellowBg,
            ["Overdue"] = RedBg,
            ["Refunded"] = IndigoBg
        });

    public static readonly IValueConverter HistoryStatusBadgeForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Completed"] = GreenFg,
            ["Pending"] = YellowFg,
            ["Overdue"] = RedFg,
            ["Refunded"] = IndigoFg
        }, GrayFg);

    #endregion

    #region Item Type

    public static readonly IValueConverter ItemTypeBadgeBackground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Product"] = BlueBg,
            ["Service"] = PurpleBg
        });

    public static readonly IValueConverter ItemTypeBadgeForeground = new StatusToBrushConverter(
        new Dictionary<string, string>
        {
            ["Product"] = BlueFg,
            ["Service"] = PurpleFg
        }, GrayFg);

    #endregion
}
