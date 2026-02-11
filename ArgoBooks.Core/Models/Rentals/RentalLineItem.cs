using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Rentals;

/// <summary>
/// Represents a single line item in a rental record.
/// Each line item corresponds to one rental inventory item.
/// </summary>
public class RentalLineItem
{
    /// <summary>
    /// Rental item ID.
    /// </summary>
    [JsonPropertyName("rentalItemId")]
    public string RentalItemId { get; set; } = string.Empty;

    /// <summary>
    /// Quantity rented.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Rate type (Daily, Weekly, Monthly).
    /// </summary>
    [JsonPropertyName("rateType")]
    public RateType RateType { get; set; }

    /// <summary>
    /// Rate amount being charged.
    /// </summary>
    [JsonPropertyName("rateAmount")]
    public decimal RateAmount { get; set; }

    /// <summary>
    /// Security deposit for this item.
    /// </summary>
    [JsonPropertyName("securityDeposit")]
    public decimal SecurityDeposit { get; set; }
}
