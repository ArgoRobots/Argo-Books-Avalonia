namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Represents a purchase/expense transaction.
/// </summary>
public class Expense : Transaction
{
    /// <summary>
    /// Supplier ID.
    /// </summary>
    [JsonPropertyName("supplierId")]
    public string? SupplierId { get; set; }
}
