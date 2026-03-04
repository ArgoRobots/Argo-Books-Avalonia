namespace ArgoBooks.Core.Enums;

/// <summary>
/// Field types returned by Azure Form Recognizer for receipt scanning.
/// </summary>
public enum ReceiptFieldType
{
    MerchantName,
    TransactionDate,
    Subtotal,
    TotalTax,
    Tax,
    Total,
    Tip,
    Items,
    Unknown
}

/// <summary>
/// Field types for individual line items in a scanned receipt.
/// </summary>
public enum ReceiptLineItemFieldType
{
    Description,
    Quantity,
    Price,
    UnitPrice,
    TotalPrice,
    Amount,
    Unknown
}

/// <summary>
/// Extension methods for receipt field type enums.
/// </summary>
public static class ReceiptFieldTypeExtensions
{
    /// <summary>
    /// Parses an Azure Form Recognizer field name to a ReceiptFieldType.
    /// </summary>
    public static ReceiptFieldType ParseReceiptField(string fieldName)
    {
        return fieldName switch
        {
            "MerchantName" => ReceiptFieldType.MerchantName,
            "TransactionDate" => ReceiptFieldType.TransactionDate,
            "Subtotal" => ReceiptFieldType.Subtotal,
            "TotalTax" => ReceiptFieldType.TotalTax,
            "Tax" => ReceiptFieldType.Tax,
            "Total" => ReceiptFieldType.Total,
            "Tip" => ReceiptFieldType.Tip,
            "Items" => ReceiptFieldType.Items,
            _ => ReceiptFieldType.Unknown
        };
    }

    /// <summary>
    /// Parses an Azure Form Recognizer line item field name to a ReceiptLineItemFieldType.
    /// </summary>
    public static ReceiptLineItemFieldType ParseLineItemField(string fieldName)
    {
        return fieldName switch
        {
            "Description" => ReceiptLineItemFieldType.Description,
            "Quantity" => ReceiptLineItemFieldType.Quantity,
            "Price" => ReceiptLineItemFieldType.Price,
            "UnitPrice" => ReceiptLineItemFieldType.UnitPrice,
            "TotalPrice" => ReceiptLineItemFieldType.TotalPrice,
            "Amount" => ReceiptLineItemFieldType.Amount,
            _ => ReceiptLineItemFieldType.Unknown
        };
    }
}
