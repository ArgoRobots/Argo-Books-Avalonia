using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Data;

/// <summary>
/// Service for generating sequential IDs for entities.
/// IDs follow the pattern: PREFIX-SEQUENCE or PREFIX-YEAR-SEQUENCE
/// </summary>
public class IdGenerator
{
    private readonly CompanyData _companyData;

    public IdGenerator(CompanyData companyData)
    {
        _companyData = companyData;
    }

    /// <summary>
    /// Generates a new customer ID (CUS-001).
    /// </summary>
    public string NextCustomerId()
    {
        _companyData.IdCounters.Customer++;
        return $"CUS-{_companyData.IdCounters.Customer:D3}";
    }

    /// <summary>
    /// Generates a new product ID (PRD-001).
    /// </summary>
    public string NextProductId()
    {
        _companyData.IdCounters.Product++;
        return $"PRD-{_companyData.IdCounters.Product:D3}";
    }

    /// <summary>
    /// Generates a new supplier ID (SUP-001).
    /// </summary>
    public string NextSupplierId()
    {
        _companyData.IdCounters.Supplier++;
        return $"SUP-{_companyData.IdCounters.Supplier:D3}";
    }

    /// <summary>
    /// Generates a new employee ID (EMP-001).
    /// </summary>
    public string NextEmployeeId()
    {
        _companyData.IdCounters.Employee++;
        return $"EMP-{_companyData.IdCounters.Employee:D3}";
    }

    /// <summary>
    /// Generates a new department ID (DEP-001).
    /// </summary>
    public string NextDepartmentId()
    {
        _companyData.IdCounters.Department++;
        return $"DEP-{_companyData.IdCounters.Department:D3}";
    }

    /// <summary>
    /// Generates a new category ID (CAT-SAL-001, CAT-PUR-001, CAT-RNT-001).
    /// </summary>
    public string NextCategoryId(CategoryType type)
    {
        _companyData.IdCounters.Category++;
        var typePrefix = type switch
        {
            CategoryType.Sales => "SAL",
            CategoryType.Purchase => "PUR",
            CategoryType.Rental => "RNT",
            _ => "GEN"
        };
        return $"CAT-{typePrefix}-{_companyData.IdCounters.Category:D3}";
    }

    /// <summary>
    /// Generates a new accountant ID (ACC-001).
    /// </summary>
    public string NextAccountantId()
    {
        _companyData.IdCounters.Accountant++;
        return $"ACC-{_companyData.IdCounters.Accountant:D3}";
    }

    /// <summary>
    /// Generates a new location ID (LOC-001).
    /// </summary>
    public string NextLocationId()
    {
        _companyData.IdCounters.Location++;
        return $"LOC-{_companyData.IdCounters.Location:D3}";
    }

    /// <summary>
    /// Generates a new sale ID (SAL-2024-00001).
    /// </summary>
    public string NextSaleId()
    {
        _companyData.IdCounters.Sale++;
        return $"SAL-{DateTime.UtcNow.Year}-{_companyData.IdCounters.Sale:D5}";
    }

    /// <summary>
    /// Generates a new purchase ID (PUR-2024-00001).
    /// </summary>
    public string NextPurchaseId()
    {
        _companyData.IdCounters.Purchase++;
        return $"PUR-{DateTime.UtcNow.Year}-{_companyData.IdCounters.Purchase:D5}";
    }

    /// <summary>
    /// Generates a new invoice ID (INV-2024-00001).
    /// </summary>
    public string NextInvoiceId()
    {
        _companyData.IdCounters.Invoice++;
        return $"INV-{DateTime.UtcNow.Year}-{_companyData.IdCounters.Invoice:D5}";
    }

    /// <summary>
    /// Generates a new invoice number for display (#INV-2024-001).
    /// </summary>
    public string NextInvoiceNumber()
    {
        return $"#INV-{DateTime.UtcNow.Year}-{_companyData.IdCounters.Invoice:D3}";
    }

    /// <summary>
    /// Generates a new payment ID (PAY-2024-00001).
    /// </summary>
    public string NextPaymentId()
    {
        _companyData.IdCounters.Payment++;
        return $"PAY-{DateTime.UtcNow.Year}-{_companyData.IdCounters.Payment:D5}";
    }

    /// <summary>
    /// Generates a new recurring invoice ID (REC-INV-001).
    /// </summary>
    public string NextRecurringInvoiceId()
    {
        _companyData.IdCounters.RecurringInvoice++;
        return $"REC-INV-{_companyData.IdCounters.RecurringInvoice:D3}";
    }

    /// <summary>
    /// Generates a new inventory item ID (INV-ITM-001).
    /// </summary>
    public string NextInventoryItemId()
    {
        _companyData.IdCounters.InventoryItem++;
        return $"INV-ITM-{_companyData.IdCounters.InventoryItem:D3}";
    }

    /// <summary>
    /// Generates a new stock adjustment ID (ADJ-001).
    /// </summary>
    public string NextStockAdjustmentId()
    {
        _companyData.IdCounters.StockAdjustment++;
        return $"ADJ-{_companyData.IdCounters.StockAdjustment:D3}";
    }

    /// <summary>
    /// Generates a new stock transfer ID (TRF-001).
    /// </summary>
    public string NextStockTransferId()
    {
        _companyData.IdCounters.StockTransfer++;
        return $"TRF-{_companyData.IdCounters.StockTransfer:D3}";
    }

    /// <summary>
    /// Generates a new purchase order ID (PO-001).
    /// </summary>
    public string NextPurchaseOrderId()
    {
        _companyData.IdCounters.PurchaseOrder++;
        return $"PO-{_companyData.IdCounters.PurchaseOrder:D3}";
    }

    /// <summary>
    /// Generates a new purchase order number for display (#PO-2024-001).
    /// </summary>
    public string NextPurchaseOrderNumber()
    {
        return $"#PO-{DateTime.UtcNow.Year}-{_companyData.IdCounters.PurchaseOrder:D3}";
    }

    /// <summary>
    /// Generates a new rental item ID (RNT-ITM-001).
    /// </summary>
    public string NextRentalItemId()
    {
        _companyData.IdCounters.RentalItem++;
        return $"RNT-ITM-{_companyData.IdCounters.RentalItem:D3}";
    }

    /// <summary>
    /// Generates a new rental record ID (RNT-001).
    /// </summary>
    public string NextRentalId()
    {
        _companyData.IdCounters.Rental++;
        return $"RNT-{_companyData.IdCounters.Rental:D3}";
    }

    /// <summary>
    /// Generates a new return ID (RET-001).
    /// </summary>
    public string NextReturnId()
    {
        _companyData.IdCounters.Return++;
        return $"RET-{_companyData.IdCounters.Return:D3}";
    }

    /// <summary>
    /// Generates a new lost/damaged ID (LOST-001).
    /// </summary>
    public string NextLostDamagedId()
    {
        _companyData.IdCounters.LostDamaged++;
        return $"LOST-{_companyData.IdCounters.LostDamaged:D3}";
    }

    /// <summary>
    /// Generates a new receipt ID (RCP-001).
    /// </summary>
    public string NextReceiptId()
    {
        _companyData.IdCounters.Receipt++;
        return $"RCP-{_companyData.IdCounters.Receipt:D3}";
    }

    /// <summary>
    /// Generates a new report template ID (TPL-001).
    /// </summary>
    public string NextReportTemplateId()
    {
        _companyData.IdCounters.ReportTemplate++;
        return $"TPL-{_companyData.IdCounters.ReportTemplate:D3}";
    }

    /// <summary>
    /// Generates a new SKU based on product name.
    /// </summary>
    public string GenerateSku(string productName)
    {
        // Take first 3 chars of each word, uppercase, max 3 words
        var words = productName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(3)
            .Select(w => new string(w.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant());

        var prefix = string.Join("", words);
        if (string.IsNullOrEmpty(prefix))
            prefix = "SKU";

        return $"{prefix}-{_companyData.IdCounters.Product:D3}";
    }
}
