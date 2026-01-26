using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// String converters for various UI elements.
/// </summary>
public static class StringConverters
{
    /// <summary>
    /// Converts item type ("Product" or "Service") to badge background color.
    /// </summary>
    public static readonly IValueConverter ToItemTypeBadgeBackground = StatusConverters.ItemTypeBadgeBackground;

    /// <summary>
    /// Converts item type ("Product" or "Service") to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToItemTypeBadgeForeground = StatusConverters.ItemTypeBadgeForeground;

    /// <summary>
    /// Converts payment status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToPaymentStatusBackground = StatusConverters.PaymentStatusBackground;

    /// <summary>
    /// Converts payment status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToPaymentStatusForeground = StatusConverters.PaymentStatusForeground;

    /// <summary>
    /// Converts history transaction type to badge background color.
    /// </summary>
    public static readonly IValueConverter ToHistoryTypeBadgeBackground = StatusConverters.HistoryTypeBadgeBackground;

    /// <summary>
    /// Converts history transaction type to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToHistoryTypeBadgeForeground = StatusConverters.HistoryTypeBadgeForeground;

    /// <summary>
    /// Converts history status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToHistoryStatusBadgeBackground = StatusConverters.HistoryStatusBadgeBackground;

    /// <summary>
    /// Converts history status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToHistoryStatusBadgeForeground = StatusConverters.HistoryStatusBadgeForeground;

    /// <summary>
    /// Converts rental record status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToRentalStatusBackground = StatusConverters.RentalStatusBackground;

    /// <summary>
    /// Converts rental record status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToRentalStatusForeground = StatusConverters.RentalStatusForeground;

    /// <summary>
    /// Converts rental item status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToRentalItemStatusBackground = StatusConverters.RentalItemStatusBackground;

    /// <summary>
    /// Converts rental item status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToRentalItemStatusForeground = StatusConverters.RentalItemStatusForeground;

    /// <summary>
    /// Converts payment transaction status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToPaymentTransactionStatusBackground = StatusConverters.PaymentTransactionStatusBackground;

    /// <summary>
    /// Converts payment transaction status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToPaymentTransactionStatusForeground = StatusConverters.PaymentTransactionStatusForeground;

    /// <summary>
    /// Converts invoice status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToInvoiceStatusBackground = StatusConverters.InvoiceStatusBackground;

    /// <summary>
    /// Converts invoice status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToInvoiceStatusForeground = StatusConverters.InvoiceStatusForeground;

    /// <summary>
    /// Returns true if the value equals the parameter.
    /// </summary>
    public new static readonly IValueConverter Equals =
        new FuncValueConverter<string, string, bool>((value, parameter) =>
            string.Equals(value, parameter, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Converts expense status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToExpenseStatusBackground = StatusConverters.TransactionStatusBackground;

    /// <summary>
    /// Converts expense status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToExpenseStatusForeground = StatusConverters.TransactionStatusForeground;

    /// <summary>
    /// Converts revenue status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToRevenueStatusBackground = StatusConverters.TransactionStatusBackground;

    /// <summary>
    /// Converts revenue status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToRevenueStatusForeground = StatusConverters.TransactionStatusForeground;
}
