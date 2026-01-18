using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a customer in the system.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Customer name (person or company).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Company name (if different from name).
    /// </summary>
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Customer address.
    /// </summary>
    [JsonPropertyName("address")]
    public Address Address { get; set; } = new();

    /// <summary>
    /// Notes about the customer.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Customer status.
    /// </summary>
    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    /// <summary>
    /// Total value of all purchases.
    /// </summary>
    [JsonPropertyName("totalPurchases")]
    public decimal TotalPurchases { get; set; }

    /// <summary>
    /// Date of last transaction.
    /// </summary>
    [JsonPropertyName("lastTransactionDate")]
    public DateTime? LastTransactionDate { get; set; }
}
