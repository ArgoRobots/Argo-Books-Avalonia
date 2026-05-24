using System.Text.Json;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class ScannedLineItemParserTests
{
    private static JsonElement Element(string json)
        => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void TryParse_full_item_populates_all_fields_and_cleans_description()
    {
        var ok = ScannedLineItemParser.TryParse(Element(
            """{"description":"6010-0272-0259-0062 Palm Refill","quantity":2,"unitPrice":5.50,"totalPrice":11.00,"confidence":0.9}"""),
            out var item);

        Assert.True(ok);
        Assert.Equal("Palm Refill", item.Description); // leading SKU code stripped
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(5.50m, item.UnitPrice);
        Assert.Equal(11.00m, item.TotalPrice);
        Assert.Equal(0.9, item.Confidence);
    }

    [Fact]
    public void TryParse_empty_object_returns_false()
    {
        Assert.False(ScannedLineItemParser.TryParse(Element("{}"), out _));
    }

    [Fact]
    public void TryParse_description_only_is_data()
    {
        Assert.True(ScannedLineItemParser.TryParse(Element("""{"description":"Coffee"}"""), out var item));
        Assert.Equal("Coffee", item.Description);
    }

    [Fact]
    public void TryParse_ignores_string_encoded_numbers_without_throwing()
    {
        // Numeric fields require a JSON number; a string must not throw or be parsed.
        var ok = ScannedLineItemParser.TryParse(Element(
            """{"description":"Milk","quantity":"2","unitPrice":"abc","totalPrice":3.00}"""),
            out var item);

        Assert.True(ok);
        Assert.Equal("Milk", item.Description);
        Assert.Equal(1m, item.Quantity);   // string "2" ignored, default quantity (1) kept
        Assert.Equal(0m, item.UnitPrice);  // string "abc" ignored
        Assert.Equal(3.00m, item.TotalPrice);
    }

    [Fact]
    public void TryParse_preserves_negative_total_for_caller_discount_handling()
    {
        Assert.True(ScannedLineItemParser.TryParse(Element(
            """{"description":"Coupon","totalPrice":-3.50}"""), out var item));
        Assert.Equal(-3.50m, item.TotalPrice);
    }
}
