using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Tracking;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Receipt model.
/// </summary>
public class ReceiptTests
{
    #region Default Value Tests

    [Fact]
    public void Receipt_DefaultValues_AreCorrect()
    {
        var receipt = new Receipt();

        Assert.Equal(string.Empty, receipt.Id);
        Assert.Equal(string.Empty, receipt.TransactionId);
        Assert.Equal(string.Empty, receipt.TransactionType);
        Assert.Equal(string.Empty, receipt.FileName);
        Assert.Equal(string.Empty, receipt.FileType);
        Assert.Equal(0L, receipt.FileSize);
        Assert.Equal(0m, receipt.Amount);
        Assert.Equal(string.Empty, receipt.Supplier);
        Assert.Equal("Manual", receipt.Source);
        Assert.Null(receipt.FileData);
        Assert.Null(receipt.OriginalFilePath);
        Assert.Null(receipt.OcrData);
    }

    [Fact]
    public void Receipt_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var receipt = new Receipt();
        var after = DateTime.UtcNow;

        Assert.InRange(receipt.CreatedAt, before, after);
    }

    #endregion

    #region IsAiScanned Tests

    [Fact]
    public void IsAiScanned_ManualSource_ReturnsFalse()
    {
        var receipt = new Receipt
        {
            Source = "Manual",
            OcrData = new OcrData { Confidence = 0.95 }
        };

        Assert.False(receipt.IsAiScanned);
    }

    [Fact]
    public void IsAiScanned_AiScannedWithNullOcrData_ReturnsFalse()
    {
        var receipt = new Receipt
        {
            Source = "AI Scanned",
            OcrData = null
        };

        Assert.False(receipt.IsAiScanned);
    }

    [Fact]
    public void IsAiScanned_AiScannedWithOcrData_ReturnsTrue()
    {
        var receipt = new Receipt
        {
            Source = "AI Scanned",
            OcrData = new OcrData
            {
                ExtractedSupplier = "Test Supplier",
                ExtractedAmount = 100.00m,
                Confidence = 0.95
            }
        };

        Assert.True(receipt.IsAiScanned);
    }

    #endregion

    #region File Data Tests

    [Fact]
    public void Receipt_FileData_CanStoreBase64()
    {
        var originalData = "Hello, World!"u8.ToArray();
        var base64Data = Convert.ToBase64String(originalData);

        var receipt = new Receipt
        {
            FileData = base64Data
        };

        var decoded = Convert.FromBase64String(receipt.FileData);
        Assert.Equal(originalData, decoded);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("application/pdf", ".pdf")]
    public void Receipt_FileType_SupportsCommonTypes(string mimeType, string expectedExtension)
    {
        var receipt = new Receipt
        {
            FileType = mimeType,
            FileName = $"receipt{expectedExtension}"
        };

        Assert.Equal(mimeType, receipt.FileType);
        Assert.EndsWith(expectedExtension, receipt.FileName);
    }

    [Fact]
    public void Receipt_FileSize_TracksLargeFiles()
    {
        var receipt = new Receipt
        {
            FileSize = 10 * 1024 * 1024 // 10 MB
        };

        Assert.Equal(10 * 1024 * 1024, receipt.FileSize);
    }

    #endregion

    #region OCR Data Tests

    [Fact]
    public void OcrData_DefaultValues_AreCorrect()
    {
        var ocrData = new OcrData();

        Assert.Null(ocrData.ExtractedSupplier);
        Assert.Null(ocrData.ExtractedDate);
        Assert.Null(ocrData.ExtractedAmount);
        Assert.Null(ocrData.ExtractedSubtotal);
        Assert.Null(ocrData.ExtractedTaxAmount);
        Assert.Null(ocrData.ExtractedCurrency);
        Assert.Empty(ocrData.ExtractedItems);
        Assert.Empty(ocrData.LineItems);
        Assert.Equal(0.0, ocrData.Confidence);
        Assert.Null(ocrData.RawText);
    }

    [Fact]
    public void OcrData_WithExtractedValues_StoresCorrectly()
    {
        var ocrData = new OcrData
        {
            ExtractedSupplier = "Acme Corp",
            ExtractedDate = new DateTime(2024, 1, 15),
            ExtractedAmount = 125.50m,
            ExtractedSubtotal = 115.00m,
            ExtractedTaxAmount = 10.50m,
            ExtractedCurrency = "USD",
            Confidence = 0.92
        };

        Assert.Equal("Acme Corp", ocrData.ExtractedSupplier);
        Assert.Equal(new DateTime(2024, 1, 15), ocrData.ExtractedDate);
        Assert.Equal(125.50m, ocrData.ExtractedAmount);
        Assert.Equal(115.00m, ocrData.ExtractedSubtotal);
        Assert.Equal(10.50m, ocrData.ExtractedTaxAmount);
        Assert.Equal("USD", ocrData.ExtractedCurrency);
        Assert.Equal(0.92, ocrData.Confidence);
    }

    [Fact]
    public void OcrLineItem_DefaultValues_AreCorrect()
    {
        var lineItem = new OcrLineItem();

        Assert.Equal(string.Empty, lineItem.Description);
        Assert.Equal(1m, lineItem.Quantity);
        Assert.Equal(0m, lineItem.UnitPrice);
        Assert.Equal(0m, lineItem.TotalPrice);
        Assert.Equal(0.0, lineItem.Confidence);
    }

    [Fact]
    public void OcrLineItem_WithValues_CalculatesCorrectly()
    {
        var lineItem = new OcrLineItem
        {
            Description = "Widget",
            Quantity = 3,
            UnitPrice = 10.00m,
            TotalPrice = 30.00m,
            Confidence = 0.95
        };

        Assert.Equal("Widget", lineItem.Description);
        Assert.Equal(3m, lineItem.Quantity);
        Assert.Equal(10.00m, lineItem.UnitPrice);
        Assert.Equal(30.00m, lineItem.TotalPrice);
        Assert.Equal(lineItem.Quantity * lineItem.UnitPrice, lineItem.TotalPrice);
    }

    [Fact]
    public void OcrData_WithLineItems_StoresMultipleItems()
    {
        var ocrData = new OcrData
        {
            LineItems =
            [
                new OcrLineItem { Description = "Item 1", Quantity = 2, UnitPrice = 5.00m, TotalPrice = 10.00m },
                new OcrLineItem { Description = "Item 2", Quantity = 1, UnitPrice = 15.00m, TotalPrice = 15.00m },
                new OcrLineItem { Description = "Item 3", Quantity = 3, UnitPrice = 3.00m, TotalPrice = 9.00m }
            ]
        };

        Assert.Equal(3, ocrData.LineItems.Count);
        Assert.Equal(34.00m, ocrData.LineItems.Sum(li => li.TotalPrice));
    }

    #endregion

    #region Transaction Association Tests

    [Fact]
    public void Receipt_TransactionAssociation_WorksCorrectly()
    {
        var receipt = new Receipt
        {
            Id = "RCP-001",
            TransactionId = "TXN-2024-00001",
            TransactionType = "Expense"
        };

        Assert.Equal("RCP-001", receipt.Id);
        Assert.Equal("TXN-2024-00001", receipt.TransactionId);
        Assert.Equal("Expense", receipt.TransactionType);
    }

    [Theory]
    [InlineData("Expense")]
    [InlineData("Revenue")]
    public void Receipt_TransactionType_SupportsExpectedTypes(string transactionType)
    {
        var receipt = new Receipt
        {
            TransactionType = transactionType
        };

        Assert.Equal(transactionType, receipt.TransactionType);
    }

    #endregion
}
