using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Represents a physical address.
/// </summary>
public class Address
{
    /// <summary>
    /// Street address including number and apartment/suite.
    /// </summary>
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// City name.
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code.
    /// </summary>
    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name.
    /// </summary>
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Returns a formatted single-line address string.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Street))
            parts.Add(Street);
        if (!string.IsNullOrWhiteSpace(City))
            parts.Add(City);
        if (!string.IsNullOrWhiteSpace(State))
            parts.Add(State);
        if (!string.IsNullOrWhiteSpace(ZipCode))
            parts.Add(ZipCode);
        if (!string.IsNullOrWhiteSpace(Country))
            parts.Add(Country);

        return string.Join(", ", parts);
    }
}
