using System.Globalization;
using Avalonia.Media;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the NotificationTypeConverters static class.
/// </summary>
public class NotificationTypeConvertersTests
{
    #region IsInfo Converter Tests

    [Fact]
    public void IsInfo_WithInfoType_ReturnsTrue()
    {
        var result = NotificationTypeConverters.IsInfo.Convert(
            NotificationType.Info, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsInfo_WithSuccessType_ReturnsFalse()
    {
        var result = NotificationTypeConverters.IsInfo.Convert(
            NotificationType.Success, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    [Fact]
    public void IsInfo_WithWarningType_ReturnsFalse()
    {
        var result = NotificationTypeConverters.IsInfo.Convert(
            NotificationType.Warning, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    #endregion

    #region IsSuccess Converter Tests

    [Fact]
    public void IsSuccess_WithSuccessType_ReturnsTrue()
    {
        var result = NotificationTypeConverters.IsSuccess.Convert(
            NotificationType.Success, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsSuccess_WithInfoType_ReturnsFalse()
    {
        var result = NotificationTypeConverters.IsSuccess.Convert(
            NotificationType.Info, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    #endregion

    #region IsWarning Converter Tests

    [Fact]
    public void IsWarning_WithWarningType_ReturnsTrue()
    {
        var result = NotificationTypeConverters.IsWarning.Convert(
            NotificationType.Warning, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsWarning_WithInfoType_ReturnsFalse()
    {
        var result = NotificationTypeConverters.IsWarning.Convert(
            NotificationType.Info, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    #endregion

    #region IsSystem Converter Tests

    [Fact]
    public void IsSystem_WithSystemType_ReturnsTrue()
    {
        var result = NotificationTypeConverters.IsSystem.Convert(
            NotificationType.System, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsSystem_WithInfoType_ReturnsFalse()
    {
        var result = NotificationTypeConverters.IsSystem.Convert(
            NotificationType.Info, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    #endregion

    #region ReadToFontWeight Converter Tests

    [Fact]
    public void ReadToFontWeight_IsRead_ReturnsNormal()
    {
        var result = NotificationTypeConverters.ReadToFontWeight.Convert(
            true, typeof(FontWeight), null, CultureInfo.InvariantCulture);

        Assert.Equal(FontWeight.Normal, result);
    }

    [Fact]
    public void ReadToFontWeight_NotRead_ReturnsSemiBold()
    {
        var result = NotificationTypeConverters.ReadToFontWeight.Convert(
            false, typeof(FontWeight), null, CultureInfo.InvariantCulture);

        Assert.Equal(FontWeight.SemiBold, result);
    }

    #endregion

    #region RelativeTime Converter Tests

    [Fact]
    public void RelativeTime_JustNow_ReturnsJustNow()
    {
        var result = NotificationTypeConverters.RelativeTime.Convert(
            DateTime.Now, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Just now", result);
    }

    [Fact]
    public void RelativeTime_MinutesAgo_ReturnsMinutesFormat()
    {
        var result = NotificationTypeConverters.RelativeTime.Convert(
            DateTime.Now.AddMinutes(-5), typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("5m ago", result);
    }

    [Fact]
    public void RelativeTime_HoursAgo_ReturnsHoursFormat()
    {
        var result = NotificationTypeConverters.RelativeTime.Convert(
            DateTime.Now.AddHours(-3), typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("3h ago", result);
    }

    [Fact]
    public void RelativeTime_DaysAgo_ReturnsDaysFormat()
    {
        var result = NotificationTypeConverters.RelativeTime.Convert(
            DateTime.Now.AddDays(-2), typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("2d ago", result);
    }

    [Fact]
    public void RelativeTime_WeeksAgo_ReturnsDateFormat()
    {
        var timestamp = DateTime.Now.AddDays(-10);
        var result = NotificationTypeConverters.RelativeTime.Convert(
            timestamp, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(timestamp.ToString("MMM d", CultureInfo.InvariantCulture), result);
    }

    #endregion

    #region Static Instance Tests

    [Fact]
    public void AllConverters_AreNotNull()
    {
        Assert.NotNull(NotificationTypeConverters.IsInfo);
        Assert.NotNull(NotificationTypeConverters.IsSuccess);
        Assert.NotNull(NotificationTypeConverters.IsWarning);
        Assert.NotNull(NotificationTypeConverters.IsSystem);
        Assert.NotNull(NotificationTypeConverters.ReadToFontWeight);
        Assert.NotNull(NotificationTypeConverters.RelativeTime);
    }

    #endregion
}
