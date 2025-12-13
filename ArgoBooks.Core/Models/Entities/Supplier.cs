using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a supplier/vendor.
/// </summary>
public class Supplier
{
    /// <summary>
    /// Unique identifier (e.g., SUP-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Supplier/company name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact person name.
    /// </summary>
    [JsonPropertyName("contactPerson")]
    public string ContactPerson { get; set; } = string.Empty;

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
    /// Supplier address.
    /// </summary>
    [JsonPropertyName("address")]
    public Address Address { get; set; } = new();

    /// <summary>
    /// Supplier website URL.
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>
    /// Payment terms (e.g., Net 30).
    /// </summary>
    [JsonPropertyName("paymentTerms")]
    public string PaymentTerms { get; set; } = string.Empty;

    /// <summary>
    /// Notes about the supplier.
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

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
