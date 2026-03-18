using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Converters for notification type display.
/// </summary>
public static class NotificationTypeConverters
{
    /// <summary>
    /// Converter that returns true if the notification type is Info.
    /// </summary>
    public static readonly IValueConverter IsInfo =
        new FuncValueConverter<NotificationType, bool>(type => type == NotificationType.Info);

    /// <summary>
    /// Converter that returns true if the notification type is Success.
    /// </summary>
    public static readonly IValueConverter IsSuccess =
        new FuncValueConverter<NotificationType, bool>(type => type == NotificationType.Success);

    /// <summary>
    /// Converter that returns true if the notification type is Warning.
    /// </summary>
    public static readonly IValueConverter IsWarning =
        new FuncValueConverter<NotificationType, bool>(type => type == NotificationType.Warning);

    /// <summary>
    /// Converter that returns true if the notification type is System.
    /// </summary>
    public static readonly IValueConverter IsSystem =
        new FuncValueConverter<NotificationType, bool>(type => type == NotificationType.System);

    /// <summary>
    /// Converter that returns a brush color based on notification type.
    /// Info=Blue, Success=Green, Warning=Orange, System=Gray.
    /// </summary>
    public static readonly IValueConverter TypeToBrush =
        new FuncValueConverter<NotificationType, IBrush>(type => type switch
        {
            NotificationType.Info => new SolidColorBrush(Color.Parse("#3B82F6")),
            NotificationType.Success => new SolidColorBrush(Color.Parse("#22C55E")),
            NotificationType.Warning => new SolidColorBrush(Color.Parse("#F59E0B")),
            NotificationType.System => new SolidColorBrush(Color.Parse("#6B7280")),
            _ => new SolidColorBrush(Color.Parse("#3B82F6"))
        });

    /// <summary>
    /// Converter that returns a tinted background brush based on notification type.
    /// </summary>
    public static readonly IValueConverter TypeToBackgroundBrush =
        new FuncValueConverter<NotificationType, IBrush>(type => type switch
        {
            NotificationType.Info => new SolidColorBrush(Color.Parse("#1A3B82F6")),
            NotificationType.Success => new SolidColorBrush(Color.Parse("#1A22C55E")),
            NotificationType.Warning => new SolidColorBrush(Color.Parse("#1AF59E0B")),
            NotificationType.System => new SolidColorBrush(Color.Parse("#1A6B7280")),
            _ => new SolidColorBrush(Color.Parse("#1A3B82F6"))
        });

    /// <summary>
    /// Converter that returns font weight based on read status.
    /// </summary>
    public static readonly IValueConverter ReadToFontWeight =
        new FuncValueConverter<bool, FontWeight>(isRead => isRead ? FontWeight.Normal : FontWeight.SemiBold);

    /// <summary>
    /// Converter that returns a relative time string from a DateTime.
    /// </summary>
    public static readonly IValueConverter RelativeTime =
        new FuncValueConverter<DateTime, string>(timestamp =>
        {
            var span = DateTime.Now - timestamp;

            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}d ago";
            return timestamp.ToString("MMM d", CultureInfo.InvariantCulture);
        });
}
