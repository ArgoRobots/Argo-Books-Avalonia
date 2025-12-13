using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a customer in the system.
/// </summary>
public class Customer
{
    /// <summary>
    /// Unique identifier (e.g., CUS-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Customer name (person or company).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

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
    /// Tags for categorization (e.g., VIP, Enterprise).
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

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

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the record was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
