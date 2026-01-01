namespace ArgoBooks.Core.Models.Transactions;

/// <summary>
/// Represents a sale/revenue transaction.
/// </summary>
public class Sale : Transaction
{
    /// <summary>
    /// Customer ID.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Subtotal before tax.
    /// </summary>
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Payment status (e.g., Paid, Pending).
    /// </summary>
    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = "Paid";
}
