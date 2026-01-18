using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Data;

/// <summary>
/// Service for generating sequential IDs for entities.
/// IDs follow the pattern: PREFIX-SEQUENCE or PREFIX-YEAR-SEQUENCE
/// </summary>
public class IdGenerator(CompanyData companyData)
{
    /// <summary>
    /// Generates a new customer ID (CUS-001).
    /// </summary>
    public string NextCustomerId()
    {
        companyData.IdCounters.Customer++;
        return $"CUS-{companyData.IdCounters.Customer:D3}";
    }

    /// <summary>
    /// Generates a new product ID (PRD-001).
    /// </summary>
    public string NextProductId()
    {
        companyData.IdCounters.Product++;
        return $"PRD-{companyData.IdCounters.Product:D3}";
    }

    /// <summary>
    /// Generates a new supplier ID (SUP-001).
    /// </summary>
    public string NextSupplierId()
    {
        companyData.IdCounters.Supplier++;
        return $"SUP-{companyData.IdCounters.Supplier:D3}";
    }

    /// <summary>
    /// Generates a new employee ID (EMP-001).
    /// </summary>
    public string NextEmployeeId()
    {
        companyData.IdCounters.Employee++;
        return $"EMP-{companyData.IdCounters.Employee:D3}";
    }

    /// <summary>
    /// Generates a new department ID (DEP-001).
    /// </summary>
    public string NextDepartmentId()
    {
        companyData.IdCounters.Department++;
        return $"DEP-{companyData.IdCounters.Department:D3}";
    }

    /// <summary>
    /// Generates a new category ID (CAT-SAL-001, CAT-PUR-001, CAT-RNT-001).
    /// </summary>
    public string NextCategoryId(CategoryType type)
    {
        companyData.IdCounters.Category++;
        var typePrefix = type switch
        {
            CategoryType.Revenue => "REV",
            CategoryType.Expense => "EXP",
            CategoryType.Rental => "RNT",
            _ => "GEN"
        };
        return $"CAT-{typePrefix}-{companyData.IdCounters.Category:D3}";
    }

    /// <summary>
    /// Generates a new accountant ID (ACC-001).
    /// </summary>
    public string NextAccountantId()
    {
        companyData.IdCounters.Accountant++;
        return $"ACC-{companyData.IdCounters.Accountant:D3}";
    }

    /// <summary>
    /// Generates a new location ID (LOC-001).
    /// </summary>
    public string NextLocationId()
    {
        companyData.IdCounters.Location++;
        return $"LOC-{companyData.IdCounters.Location:D3}";
    }

    /// <summary>
    /// Generates a new revenue ID (REV-2024-00001).
    /// </summary>
    public string NextRevenueId()
    {
        companyData.IdCounters.Revenue++;
        return $"REV-{DateTime.UtcNow.Year}-{companyData.IdCounters.Revenue:D5}";
    }

    /// <summary>
    /// Generates a new expense ID (EXP-2024-00001).
    /// </summary>
    public string NextExpenseId()
    {
        companyData.IdCounters.Expense++;
        return $"EXP-{DateTime.UtcNow.Year}-{companyData.IdCounters.Expense:D5}";
    }

    /// <summary>
    /// Generates a new invoice ID (INV-2024-00001).
    /// </summary>
    public string NextInvoiceId()
    {
        companyData.IdCounters.Invoice++;
        return $"INV-{DateTime.UtcNow.Year}-{companyData.IdCounters.Invoice:D5}";
    }

    /// <summary>
    /// Generates a new invoice number for display (#INV-2024-001).
    /// </summary>
    public string NextInvoiceNumber()
    {
        return $"#INV-{DateTime.UtcNow.Year}-{companyData.IdCounters.Invoice:D3}";
    }

    /// <summary>
    /// Generates a new payment ID (PAY-2024-00001).
    /// </summary>
    public string NextPaymentId()
    {
        companyData.IdCounters.Payment++;
        return $"PAY-{DateTime.UtcNow.Year}-{companyData.IdCounters.Payment:D5}";
    }

    /// <summary>
    /// Generates a new recurring invoice ID (REC-INV-001).
    /// </summary>
    public string NextRecurringInvoiceId()
    {
        companyData.IdCounters.RecurringInvoice++;
        return $"REC-INV-{companyData.IdCounters.RecurringInvoice:D3}";
    }

    /// <summary>
    /// Generates a new inventory item ID (INV-ITM-001).
    /// </summary>
    public string NextInventoryItemId()
    {
        companyData.IdCounters.InventoryItem++;
        return $"INV-ITM-{companyData.IdCounters.InventoryItem:D3}";
    }

    /// <summary>
    /// Generates a new stock adjustment ID (ADJ-001).
    /// </summary>
    public string NextStockAdjustmentId()
    {
        companyData.IdCounters.StockAdjustment++;
        return $"ADJ-{companyData.IdCounters.StockAdjustment:D3}";
    }

    /// <summary>
    /// Generates a new stock transfer ID (TRF-001).
    /// </summary>
    public string NextStockTransferId()
    {
        companyData.IdCounters.StockTransfer++;
        return $"TRF-{companyData.IdCounters.StockTransfer:D3}";
    }

    /// <summary>
    /// Generates a new purchase order ID (PO-001).
    /// </summary>
    public string NextPurchaseOrderId()
    {
        companyData.IdCounters.ExpenseOrder++;
        return $"PO-{companyData.IdCounters.ExpenseOrder:D3}";
    }

    /// <summary>
    /// Generates a new purchase order number for display (#PO-2024-001).
    /// </summary>
    public string NextPurchaseOrderNumber()
    {
        return $"#PO-{DateTime.UtcNow.Year}-{companyData.IdCounters.ExpenseOrder:D3}";
    }

    /// <summary>
    /// Generates a new rental item ID (RNT-ITM-001).
    /// </summary>
    public string NextRentalItemId()
    {
        companyData.IdCounters.RentalItem++;
        return $"RNT-ITM-{companyData.IdCounters.RentalItem:D3}";
    }

    /// <summary>
    /// Generates a new rental record ID (RNT-001).
    /// </summary>
    public string NextRentalId()
    {
        companyData.IdCounters.Rental++;
        return $"RNT-{companyData.IdCounters.Rental:D3}";
    }

    /// <summary>
    /// Generates a new return ID (RET-001).
    /// </summary>
    public string NextReturnId()
    {
        companyData.IdCounters.Return++;
        return $"RET-{companyData.IdCounters.Return:D3}";
    }

    /// <summary>
    /// Generates a new lost/damaged ID (LOST-001).
    /// </summary>
    public string NextLostDamagedId()
    {
        companyData.IdCounters.LostDamaged++;
        return $"LOST-{companyData.IdCounters.LostDamaged:D3}";
    }

    /// <summary>
    /// Generates a new receipt ID (RCP-001).
    /// </summary>
    public string NextReceiptId()
    {
        companyData.IdCounters.Receipt++;
        return $"RCP-{companyData.IdCounters.Receipt:D3}";
    }

    /// <summary>
    /// Generates a new report template ID (TPL-001).
    /// </summary>
    public string NextReportTemplateId()
    {
        companyData.IdCounters.ReportTemplate++;
        return $"TPL-{companyData.IdCounters.ReportTemplate:D3}";
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

        return $"{prefix}-{companyData.IdCounters.Product:D3}";
    }
}
