using Avalonia.Media;
using ArgoBooks.Converters;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the StatusToBrushConverter and pre-configured StatusConverters.
/// </summary>
public class StatusToBrushConverterTests
{
    #region Custom StatusToBrushConverter Tests

    [Fact]
    public void Convert_KnownStatus_ReturnsConfiguredColor()
    {
        var colors = new Dictionary<string, string>
        {
            { "Active", "#00FF00" },
            { "Inactive", "#FF0000" }
        };
        var converter = new StatusToBrushConverter(colors);

        var result = converter.Convert("Active", typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse("#00FF00"), result.Color);
    }

    [Fact]
    public void Convert_UnknownStatus_ReturnsDefaultColor()
    {
        var colors = new Dictionary<string, string>
        {
            { "Active", "#00FF00" }
        };
        var converter = new StatusToBrushConverter(colors, "#CCCCCC");

        var result = converter.Convert("Unknown", typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse("#CCCCCC"), result.Color);
    }

    [Fact]
    public void Convert_NullValue_ReturnsDefaultColor()
    {
        var colors = new Dictionary<string, string>
        {
            { "Active", "#00FF00" }
        };
        var converter = new StatusToBrushConverter(colors, "#AABBCC");

        var result = converter.Convert(null, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse("#AABBCC"), result.Color);
    }

    #endregion

    #region Pre-configured Payment Status Tests

    [Theory]
    [InlineData("Current", "#DCFCE7")]
    [InlineData("Overdue", "#FEF3C7")]
    [InlineData("Delinquent", "#FEE2E2")]
    public void PaymentStatusBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.PaymentStatusBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    [Theory]
    [InlineData("Current", "#166534")]
    [InlineData("Overdue", "#92400E")]
    [InlineData("Delinquent", "#DC2626")]
    public void PaymentStatusForeground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.PaymentStatusForeground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion

    #region Pre-configured Transaction Status Tests

    [Theory]
    [InlineData("Completed", "#DCFCE7")]
    [InlineData("Pending", "#FEF3C7")]
    [InlineData("Unpaid", "#FFEDD5")]
    [InlineData("Returned", "#DBEAFE")]
    [InlineData("Partial Return", "#F3E8FF")]
    [InlineData("Lost / Damaged", "#FEE2E2")]
    [InlineData("Cancelled", "#F3F4F6")]
    public void TransactionStatusBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.TransactionStatusBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion

    #region Pre-configured Invoice Status Tests

    [Theory]
    [InlineData("Paid", "#DCFCE7")]
    [InlineData("Pending", "#FEF3C7")]
    [InlineData("Overdue", "#FEE2E2")]
    [InlineData("Draft", "#F3F4F6")]
    [InlineData("Sent", "#DBEAFE")]
    [InlineData("Viewed", "#DBEAFE")]
    [InlineData("Partial", "#F3E8FF")]
    public void InvoiceStatusBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.InvoiceStatusBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion

    #region Pre-configured Rental Status Tests

    [Theory]
    [InlineData("Active", "#DCFCE7")]
    [InlineData("Returned", "#DBEAFE")]
    [InlineData("Overdue", "#FEE2E2")]
    [InlineData("Cancelled", "#F3F4F6")]
    public void RentalStatusBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.RentalStatusBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion

    #region Pre-configured Rental Item Status Tests

    [Theory]
    [InlineData("Available", "#DCFCE7")]
    [InlineData("In Maintenance", "#FEF3C7")]
    [InlineData("All Rented", "#F3E8FF")]
    public void RentalItemStatusBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.RentalItemStatusBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion

    #region Pre-configured Item Type Tests

    [Theory]
    [InlineData("Product", "#DBEAFE")]
    [InlineData("Service", "#F3E8FF")]
    public void ItemTypeBadgeBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.ItemTypeBadgeBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion

    #region Pre-configured History Type Tests

    [Theory]
    [InlineData("Rental", "#DBEAFE")]
    [InlineData("Purchase", "#F3E8FF")]
    [InlineData("Return", "#FFEDD5")]
    [InlineData("Payment", "#DCFCE7")]
    public void HistoryTypeBadgeBackground_ReturnsCorrectColor(string status, string expectedHex)
    {
        var converter = StatusConverters.HistoryTypeBadgeBackground;
        var result = converter.Convert(status, typeof(IBrush), null, null!) as ISolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(Color.Parse(expectedHex), result.Color);
    }

    #endregion
}
