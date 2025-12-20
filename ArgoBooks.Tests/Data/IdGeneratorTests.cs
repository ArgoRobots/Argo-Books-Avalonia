using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using Xunit;

namespace ArgoBooks.Tests.Data;

/// <summary>
/// Tests for the IdGenerator class.
/// </summary>
public class IdGeneratorTests
{
    private CompanyData CreateCompanyData()
    {
        return new CompanyData();
    }

    #region Entity ID Generation Tests

    [Fact]
    public void NextCustomerId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextCustomerId();
        var id2 = generator.NextCustomerId();
        var id3 = generator.NextCustomerId();

        Assert.Equal("CUS-001", id1);
        Assert.Equal("CUS-002", id2);
        Assert.Equal("CUS-003", id3);
    }

    [Fact]
    public void NextCustomerId_FollowsCorrectFormat()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id = generator.NextCustomerId();

        Assert.Matches(@"^CUS-\d{3}$", id);
    }

    [Fact]
    public void NextProductId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextProductId();
        var id2 = generator.NextProductId();

        Assert.Equal("PRD-001", id1);
        Assert.Equal("PRD-002", id2);
    }

    [Fact]
    public void NextSupplierId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextSupplierId();
        var id2 = generator.NextSupplierId();

        Assert.Equal("SUP-001", id1);
        Assert.Equal("SUP-002", id2);
    }

    [Fact]
    public void NextEmployeeId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextEmployeeId();
        var id2 = generator.NextEmployeeId();

        Assert.Equal("EMP-001", id1);
        Assert.Equal("EMP-002", id2);
    }

    [Fact]
    public void NextDepartmentId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextDepartmentId();
        var id2 = generator.NextDepartmentId();

        Assert.Equal("DEP-001", id1);
        Assert.Equal("DEP-002", id2);
    }

    [Fact]
    public void NextLocationId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextLocationId();
        var id2 = generator.NextLocationId();

        Assert.Equal("LOC-001", id1);
        Assert.Equal("LOC-002", id2);
    }

    [Fact]
    public void NextAccountantId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextAccountantId();
        var id2 = generator.NextAccountantId();

        Assert.Equal("ACC-001", id1);
        Assert.Equal("ACC-002", id2);
    }

    #endregion

    #region Category ID Generation Tests

    [Theory]
    [InlineData(CategoryType.Sales, "CAT-SAL-001")]
    [InlineData(CategoryType.Purchase, "CAT-PUR-001")]
    [InlineData(CategoryType.Rental, "CAT-RNT-001")]
    public void NextCategoryId_GeneratesCorrectPrefixForType(CategoryType type, string expectedId)
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id = generator.NextCategoryId(type);

        Assert.Equal(expectedId, id);
    }

    [Fact]
    public void NextCategoryId_SharesCounterAcrossTypes()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var salesId = generator.NextCategoryId(CategoryType.Sales);
        var purchaseId = generator.NextCategoryId(CategoryType.Purchase);
        var rentalId = generator.NextCategoryId(CategoryType.Rental);

        Assert.Equal("CAT-SAL-001", salesId);
        Assert.Equal("CAT-PUR-002", purchaseId);
        Assert.Equal("CAT-RNT-003", rentalId);
    }

    #endregion

    #region Transaction ID Generation Tests

    [Fact]
    public void NextSaleId_IncludesYear()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id = generator.NextSaleId();

        Assert.Contains(DateTime.UtcNow.Year.ToString(), id);
        Assert.Matches(@"^SAL-\d{4}-\d{5}$", id);
    }

    [Fact]
    public void NextPurchaseId_IncludesYear()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id = generator.NextPurchaseId();

        Assert.Contains(DateTime.UtcNow.Year.ToString(), id);
        Assert.Matches(@"^PUR-\d{4}-\d{5}$", id);
    }

    [Fact]
    public void NextInvoiceId_IncludesYear()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id = generator.NextInvoiceId();

        Assert.Contains(DateTime.UtcNow.Year.ToString(), id);
        Assert.Matches(@"^INV-\d{4}-\d{5}$", id);
    }

    [Fact]
    public void NextInvoiceNumber_ReturnsDisplayFormat()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        // First generate an invoice ID to increment counter
        generator.NextInvoiceId();

        var number = generator.NextInvoiceNumber();

        Assert.StartsWith("#INV-", number);
        Assert.Contains(DateTime.UtcNow.Year.ToString(), number);
    }

    [Fact]
    public void NextPaymentId_IncludesYear()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id = generator.NextPaymentId();

        Assert.Contains(DateTime.UtcNow.Year.ToString(), id);
        Assert.Matches(@"^PAY-\d{4}-\d{5}$", id);
    }

    [Fact]
    public void NextRecurringInvoiceId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextRecurringInvoiceId();
        var id2 = generator.NextRecurringInvoiceId();

        Assert.Equal("REC-INV-001", id1);
        Assert.Equal("REC-INV-002", id2);
    }

    #endregion

    #region Inventory ID Generation Tests

    [Fact]
    public void NextInventoryItemId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextInventoryItemId();
        var id2 = generator.NextInventoryItemId();

        Assert.Equal("INV-ITM-001", id1);
        Assert.Equal("INV-ITM-002", id2);
    }

    [Fact]
    public void NextStockAdjustmentId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextStockAdjustmentId();
        var id2 = generator.NextStockAdjustmentId();

        Assert.Equal("ADJ-001", id1);
        Assert.Equal("ADJ-002", id2);
    }

    [Fact]
    public void NextStockTransferId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextStockTransferId();
        var id2 = generator.NextStockTransferId();

        Assert.Equal("TRF-001", id1);
        Assert.Equal("TRF-002", id2);
    }

    [Fact]
    public void NextPurchaseOrderId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextPurchaseOrderId();
        var id2 = generator.NextPurchaseOrderId();

        Assert.Equal("PO-001", id1);
        Assert.Equal("PO-002", id2);
    }

    [Fact]
    public void NextPurchaseOrderNumber_ReturnsDisplayFormat()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        // First generate a PO ID to increment counter
        generator.NextPurchaseOrderId();

        var number = generator.NextPurchaseOrderNumber();

        Assert.StartsWith("#PO-", number);
        Assert.Contains(DateTime.UtcNow.Year.ToString(), number);
    }

    #endregion

    #region Rental ID Generation Tests

    [Fact]
    public void NextRentalItemId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextRentalItemId();
        var id2 = generator.NextRentalItemId();

        Assert.Equal("RNT-ITM-001", id1);
        Assert.Equal("RNT-ITM-002", id2);
    }

    [Fact]
    public void NextRentalId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextRentalId();
        var id2 = generator.NextRentalId();

        Assert.Equal("RNT-001", id1);
        Assert.Equal("RNT-002", id2);
    }

    [Fact]
    public void NextReturnId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextReturnId();
        var id2 = generator.NextReturnId();

        Assert.Equal("RET-001", id1);
        Assert.Equal("RET-002", id2);
    }

    [Fact]
    public void NextLostDamagedId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextLostDamagedId();
        var id2 = generator.NextLostDamagedId();

        Assert.Equal("LOST-001", id1);
        Assert.Equal("LOST-002", id2);
    }

    [Fact]
    public void NextReceiptId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextReceiptId();
        var id2 = generator.NextReceiptId();

        Assert.Equal("RCP-001", id1);
        Assert.Equal("RCP-002", id2);
    }

    [Fact]
    public void NextReportTemplateId_GeneratesSequentialIds()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        var id1 = generator.NextReportTemplateId();
        var id2 = generator.NextReportTemplateId();

        Assert.Equal("TPL-001", id1);
        Assert.Equal("TPL-002", id2);
    }

    #endregion

    #region SKU Generation Tests

    [Fact]
    public void GenerateSku_CreatesSkuFromProductName()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        // First call NextProductId to set counter
        generator.NextProductId();

        var sku = generator.GenerateSku("Widget Pro Max");

        Assert.StartsWith("WIDPROMA", sku);
        Assert.Matches(@"^[A-Z]+-\d{3}$", sku);
    }

    [Fact]
    public void GenerateSku_HandlesShortWords()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextProductId();

        var sku = generator.GenerateSku("A B C");

        Assert.StartsWith("ABC", sku);
    }

    [Fact]
    public void GenerateSku_HandlesSpecialCharacters()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextProductId();

        var sku = generator.GenerateSku("Product-123 (Special)");

        // Should filter out special characters
        Assert.DoesNotContain("-", sku.Substring(0, sku.LastIndexOf('-')));
        Assert.DoesNotContain("(", sku);
        Assert.DoesNotContain(")", sku);
    }

    [Fact]
    public void GenerateSku_LimitsToThreeWords()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextProductId();

        var sku = generator.GenerateSku("One Two Three Four Five");

        // Should only use first 3 words
        var prefix = sku.Substring(0, sku.LastIndexOf('-'));
        Assert.True(prefix.Length <= 9); // 3 chars * 3 words max
    }

    [Fact]
    public void GenerateSku_DefaultsToSKU_ForEmptyName()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextProductId();

        var sku = generator.GenerateSku("   ");

        Assert.StartsWith("SKU-", sku);
    }

    [Fact]
    public void GenerateSku_IsUppercase()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextProductId();

        var sku = generator.GenerateSku("lowercase product name");

        var prefix = sku.Substring(0, sku.LastIndexOf('-'));
        Assert.Equal(prefix.ToUpperInvariant(), prefix);
    }

    #endregion

    #region Counter Persistence Tests

    [Fact]
    public void Counters_ArePersistedInCompanyData()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextCustomerId();
        generator.NextCustomerId();

        Assert.Equal(2, companyData.IdCounters.Customer);
    }

    [Fact]
    public void NewGenerator_UsesExistingCounters()
    {
        var companyData = CreateCompanyData();
        companyData.IdCounters.Customer = 50;

        var generator = new IdGenerator(companyData);
        var id = generator.NextCustomerId();

        Assert.Equal("CUS-051", id);
    }

    [Fact]
    public void DifferentEntityTypes_HaveIndependentCounters()
    {
        var companyData = CreateCompanyData();
        var generator = new IdGenerator(companyData);

        generator.NextCustomerId();
        generator.NextCustomerId();
        generator.NextCustomerId();

        var productId = generator.NextProductId();

        Assert.Equal("PRD-001", productId);
        Assert.Equal(3, companyData.IdCounters.Customer);
        Assert.Equal(1, companyData.IdCounters.Product);
    }

    #endregion
}
