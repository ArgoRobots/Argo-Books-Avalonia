using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a supplier.
/// </summary>
public class Supplier : BaseEntity, IAvatarOwner
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

    /// <summary>
    /// Relative path (within the company temp directory) to the supplier's avatar image,
    /// or null if no avatar is set. May be a user-uploaded file or a favicon auto-fetched
    /// from the supplier's website. When null, initials are displayed instead.
    /// </summary>
    [JsonPropertyName("avatarFileName")]
    public string? AvatarFileName { get; set; }
}
