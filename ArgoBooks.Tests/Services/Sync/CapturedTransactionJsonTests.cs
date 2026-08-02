using System.Text.Json;
using ArgoBooks.Core.Services.Sync;
using Xunit;

namespace ArgoBooks.Tests.Services.Sync;

/// <summary>
/// Locks down the wire format of <see cref="CapturedTransaction"/> (and its line-item type) to the
/// explicit lowerCamelCase <c>JsonPropertyName</c> values, since the phone and desktop must agree on
/// this shape independent of any future C# member-casing change.
/// </summary>
public class CapturedTransactionJsonTests
{
    private static CapturedTransaction NewTransaction() => new()
    {
        Type = CapturedTransactionType.Expense,
        SupplierOrCustomer = "Office Depot",
        Date = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Total = 54.00m,
        Tax = 4.00m,
        LineItems =
        [
            new CapturedLineItem
            {
                Description = "Printer paper",
                Quantity = 2,
                UnitPrice = 25.00m,
                Total = 50.00m,
                ProductName = "Printer paper"
            }
        ],
        ImageBase64 = Convert.ToBase64String([1, 2, 3, 4]),
        ScanUid = "11111111-1111-1111-1111-111111111111"
    };

    [Fact]
    public void Serialize_UsesExplicitLowerCamelCasePropertyNames()
    {
        var json = JsonSerializer.Serialize(NewTransaction());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("type", out _));
        Assert.True(root.TryGetProperty("supplierOrCustomer", out _));
        Assert.True(root.TryGetProperty("date", out _));
        Assert.True(root.TryGetProperty("total", out _));
        Assert.True(root.TryGetProperty("tax", out _));
        Assert.True(root.TryGetProperty("lineItems", out var lineItems));
        Assert.True(root.TryGetProperty("imageBase64", out _));
        Assert.True(root.TryGetProperty("scanUid", out var scanUid));
        Assert.Equal("11111111-1111-1111-1111-111111111111", scanUid.GetString());

        var line = lineItems[0];
        Assert.True(line.TryGetProperty("description", out _));
        Assert.True(line.TryGetProperty("quantity", out _));
        Assert.True(line.TryGetProperty("unitPrice", out _));
        Assert.True(line.TryGetProperty("total", out _));
        Assert.True(line.TryGetProperty("productName", out _));
    }

    [Fact]
    public void RoundTrips_ThroughJson_CaseInsensitive()
    {
        var original = NewTransaction();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var json = JsonSerializer.Serialize(original, options);
        var back = JsonSerializer.Deserialize<CapturedTransaction>(json, options);

        Assert.NotNull(back);
        Assert.Equal(original.Type, back!.Type);
        Assert.Equal(original.SupplierOrCustomer, back.SupplierOrCustomer);
        Assert.Equal(original.Date, back.Date);
        Assert.Equal(original.Total, back.Total);
        Assert.Equal(original.Tax, back.Tax);
        Assert.Equal(original.ImageBase64, back.ImageBase64);
        Assert.Equal(original.ScanUid, back.ScanUid);

        Assert.Single(back.LineItems);
        var line = back.LineItems[0];
        var originalLine = original.LineItems[0];
        Assert.Equal(originalLine.Description, line.Description);
        Assert.Equal(originalLine.Quantity, line.Quantity);
        Assert.Equal(originalLine.UnitPrice, line.UnitPrice);
        Assert.Equal(originalLine.Total, line.Total);
        Assert.Equal(originalLine.ProductName, line.ProductName);
    }

    [Fact]
    public void RoundTrips_WithUppercasePropertyNames_CaseInsensitive()
    {
        // Simulates a wire payload written with PascalCase keys (e.g. an older client); the
        // explicit JsonPropertyName + case-insensitive options must still bind correctly.
        const string json = """
        {
            "Type": 1,
            "SupplierOrCustomer": "Acme Corp",
            "Date": "2026-06-02T00:00:00Z",
            "Total": 220.00,
            "Tax": 20.00,
            "LineItems": [ { "Description": "Consulting", "Quantity": 1, "UnitPrice": 200.00, "Total": 200.00 } ],
            "ScanUid": "22222222-2222-2222-2222-222222222222"
        }
        """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var tx = JsonSerializer.Deserialize<CapturedTransaction>(json, options);

        Assert.NotNull(tx);
        Assert.Equal(CapturedTransactionType.Revenue, tx!.Type);
        Assert.Equal("Acme Corp", tx.SupplierOrCustomer);
        Assert.Equal(220.00m, tx.Total);
        Assert.Equal("22222222-2222-2222-2222-222222222222", tx.ScanUid);
        Assert.Single(tx.LineItems);
        Assert.Equal("Consulting", tx.LineItems[0].Description);
    }
}
