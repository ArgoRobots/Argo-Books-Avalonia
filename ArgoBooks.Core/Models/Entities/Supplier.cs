using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a supplier/vendor.
/// </summary>
public class Supplier : BaseEntity
{
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
    /// Supplier address.
    /// </summary>
    [JsonPropertyName("address")]
    public Address Address { get; set; } = new();

    /// <summary>
    /// Supplier website URL.
    /// </summary>
    [JsonPropertyName("website")]
    public string Website { get; set; } = string.Empty;

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
}
