using System.Globalization;
using ArgoBooks.Core;
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
        new FuncValueConverter<NotificationType, IBrush>(type => BrushFor(type));

    /// <summary>
    /// Converter that returns a tinted background brush based on notification type.
    /// Uses 60% opacity of the accent color.
    /// </summary>
    public static readonly IValueConverter TypeToBackgroundBrush =
        new FuncValueConverter<NotificationType, IBrush>(type =>
        {
            var color = ColorFor(type);
            return new SolidColorBrush(new Color(0x99, color.R, color.G, color.B));
        });

    private static Color ColorFor(NotificationType type) => type switch
    {
        NotificationType.Info => Color.Parse(AppColors.Primary),
        NotificationType.Success => Color.Parse(AppColors.Success),
        NotificationType.Warning => Color.Parse(AppColors.Warning),
        NotificationType.System => Color.Parse(AppColors.GrayMedium),
        _ => Color.Parse(AppColors.Primary)
    };

    /// <summary>
    /// Returns the accent brush for a notification type. Public so view models can
    /// expose pre-converted brushes and avoid binding errors when the source notification is null.
    /// </summary>
    public static SolidColorBrush BrushFor(NotificationType type) =>
        new(ColorFor(type));

    /// <summary>
    /// Returns the tinted background brush (60% opacity) for a notification type.
    /// </summary>
    public static SolidColorBrush BackgroundBrushFor(NotificationType type)
    {
        var color = ColorFor(type);
        return new SolidColorBrush(new Color(0x99, color.R, color.G, color.B));
    }

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
