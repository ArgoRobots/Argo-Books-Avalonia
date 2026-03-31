using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the OpenAiReceiptScannerService class.
/// </summary>
public class OpenAiReceiptScannerServiceTests
{
    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithoutLicenseService_ReturnsFalse()
    {
        var service = new OpenAiReceiptScannerService();

        Assert.False(service.IsConfigured);
    }

    #endregion

    #region ValidateConfiguration Tests

    [Fact]
    public async Task ValidateConfigurationAsync_WithoutLicenseService_ReturnsFalse()
    {
        var service = new OpenAiReceiptScannerService();

        var result = await service.ValidateConfigurationAsync();

        Assert.False(result);
    }

    #endregion

    #region ScanReceiptFromFile Tests

    [Fact]
    public async Task ScanReceiptFromFileAsync_FileNotFound_ReturnsFailedResult()
    {
        var service = new OpenAiReceiptScannerService();

        var result = await service.ScanReceiptFromFileAsync("/nonexistent/file.jpg");

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region ParseResponse Tests

    [Fact]
    public void ParseResponse_ValidJson_ReturnsCorrectResult()
    {
        var json = """
        {
          "supplierName": "Walmart",
          "transactionDate": "2026-03-15",
          "subtotal": 42.50,
          "taxes": [{"name": "State Tax", "amount": 2.10}, {"name": "County Tax", "amount": 1.30}],
          "discount": 5.00,
          "totalAmount": 40.90,
          "currencyCode": "USD",
          "paymentMethod": "Credit Card",
          "confidence": 0.95,
          "lineItems": [
            {"description": "Bread White", "quantity": 2, "unitPrice": 3.50, "totalPrice": 7.00, "confidence": 0.9},
            {"description": "Milk 2%", "quantity": 1, "unitPrice": 4.99, "totalPrice": 4.99, "confidence": 0.85}
          ]
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal("Walmart", result.SupplierName);
        Assert.Equal(new DateTime(2026, 3, 15), result.TransactionDate);
        Assert.Equal(42.50m, result.Subtotal);
        Assert.Equal(3.40m, result.TaxAmount);
        Assert.Equal(5.00m, result.Discount);
        Assert.Equal(40.90m, result.TotalAmount);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Equal("Credit Card", result.PaymentMethod);
        Assert.Equal(0.95, result.Confidence);
        Assert.Equal(2, result.LineItems.Count);
        Assert.Equal("Bread White", result.LineItems[0].Description);
        Assert.Equal(2, result.LineItems[0].Quantity);
        Assert.Equal(3.50m, result.LineItems[0].UnitPrice);
        Assert.Equal(7.00m, result.LineItems[0].TotalPrice);
        Assert.Equal("Milk 2%", result.LineItems[1].Description);
    }

    [Fact]
    public void ParseResponse_WithMarkdownCodeBlock_StripsAndParses()
    {
        var json = """
        ```json
        {
          "supplierName": "Target",
          "transactionDate": "2026-01-10",
          "subtotal": 15.00,
          "taxAmount": 1.20,
          "totalAmount": 16.20,
          "confidence": 0.88,
          "lineItems": []
        }
        ```
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal("Target", result.SupplierName);
        Assert.Equal(16.20m, result.TotalAmount);
    }

    [Fact]
    public void ParseResponse_ErrorField_ReturnsFailedResult()
    {
        var json = """
        {
          "error": "Not a valid receipt",
          "confidence": 0.0
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.False(result.IsSuccess);
        Assert.Equal("Not a valid receipt", result.ErrorMessage);
    }

    [Fact]
    public void ParseResponse_MalformedJson_ReturnsFailedResult()
    {
        var result = OpenAiReceiptScannerService.ParseResponse("this is not json at all");

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to parse", result.ErrorMessage);
    }

    [Fact]
    public void ParseResponse_WithDiscount_ExtractsDiscount()
    {
        var json = """
        {
          "supplierName": "CVS Pharmacy",
          "subtotal": 25.00,
          "taxAmount": 2.00,
          "discount": 3.50,
          "totalAmount": 23.50,
          "confidence": 0.92,
          "lineItems": [
            {"description": "Shampoo", "quantity": 1, "unitPrice": 12.50, "totalPrice": 12.50, "confidence": 0.9},
            {"description": "Coupon Discount", "quantity": 1, "unitPrice": -3.50, "totalPrice": -3.50, "confidence": 0.85}
          ]
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        // Discount sums the "discount" field (3.50) + negative line item (3.50)
        Assert.Equal(7.00m, result.Discount);
        Assert.Equal(23.50m, result.TotalAmount);
        // Negative line items are moved to discount, so only 1 product line item remains
        Assert.Single(result.LineItems);
    }

    [Fact]
    public void ParseResponse_NullFields_HandlesGracefully()
    {
        var json = """
        {
          "supplierName": null,
          "transactionDate": null,
          "subtotal": 10.00,
          "totalAmount": 10.00,
          "confidence": 0.5,
          "lineItems": []
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Null(result.SupplierName);
        Assert.Null(result.TransactionDate);
        Assert.Null(result.Discount);
        Assert.Equal(10.00m, result.TotalAmount);
    }

    [Fact]
    public void ParseResponse_EmptyLineItems_ReturnsEmptyList()
    {
        var json = """
        {
          "supplierName": "Gas Station",
          "totalAmount": 55.00,
          "confidence": 0.9,
          "lineItems": []
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.LineItems);
    }

    [Fact]
    public void ParseResponse_MultipleTaxLines_SumsCorrectly()
    {
        var json = """
        {
          "supplierName": "Independent",
          "subtotal": 178.68,
          "taxes": [
            {"name": "GST", "amount": 1.22},
            {"name": "PST", "amount": 1.47}
          ],
          "totalAmount": 181.37,
          "confidence": 0.9,
          "lineItems": []
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(2.69m, result.TaxAmount);
    }

    [Fact]
    public void ParseResponse_SingleTaxAmountFallback_StillWorks()
    {
        var json = """
        {
          "supplierName": "Store",
          "taxAmount": 5.50,
          "totalAmount": 55.50,
          "confidence": 0.9,
          "lineItems": []
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(5.50m, result.TaxAmount);
    }

    [Fact]
    public void ParseResponse_MultipleDiscountLines_SumsCorrectly()
    {
        var json = """
        {
          "supplierName": "Grocery Store",
          "subtotal": 50.00,
          "discounts": [
            {"name": "Member Discount", "amount": 2.49},
            {"name": "Coupon", "amount": 1.00},
            {"name": "Loyalty Points", "amount": 0.50}
          ],
          "totalAmount": 46.01,
          "confidence": 0.9,
          "lineItems": []
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(3.99m, result.Discount);
    }

    [Fact]
    public void ParseResponse_SingleDiscountFallback_StillWorks()
    {
        var json = """
        {
          "supplierName": "Store",
          "discount": 5.00,
          "totalAmount": 45.00,
          "confidence": 0.9,
          "lineItems": []
        }
        """;

        var result = OpenAiReceiptScannerService.ParseResponse(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(5.00m, result.Discount);
    }

    #endregion
}
