using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Data;

/// <summary>
/// Service for generating sequential IDs for entities.
/// IDs follow the pattern: PREFIX-SEQUENCE or PREFIX-YEAR-SEQUENCE
/// </summary>
public class IdGenerator(CompanyData companyData)
{
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
    /// Generates a new invoice ID (INV-2024-00001).
    /// </summary>
    public string NextInvoiceId()
    {
        companyData.IdCounters.Invoice++;
        return $"INV-{DateTime.UtcNow.Year}-{companyData.IdCounters.Invoice:D5}";
    }

    /// <summary>
    /// Generates a new invoice number for display (#INV-2024-001).
    /// Must be called after NextInvoiceId() which increments the counter.
    /// </summary>
    public string NextInvoiceNumber()
    {
        return $"#INV-{DateTime.UtcNow.Year}-{companyData.IdCounters.Invoice:D3}";
    }

    /// <summary>
    /// Peeks at what the next invoice ID and number would be without incrementing the counter.
    /// </summary>
    public (string Id, string Number) PeekNextInvoice()
    {
        var next = companyData.IdCounters.Invoice + 1;
        return (
            $"INV-{DateTime.UtcNow.Year}-{next:D5}",
            $"#INV-{DateTime.UtcNow.Year}-{next:D3}"
        );
    }

}
