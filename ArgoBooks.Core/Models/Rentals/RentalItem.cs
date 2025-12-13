using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Rentals;

/// <summary>
/// Represents an item available for rental.
/// </summary>
public class RentalItem
{
    /// <summary>
    /// Unique identifier (e.g., RNT-ITM-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Item name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Supplier ID.
    /// </summary>
    [JsonPropertyName("supplierId")]
    public string? SupplierId { get; set; }

    /// <summary>
    /// Total quantity owned.
    /// </summary>
    [JsonPropertyName("totalQuantity")]
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Quantity currently available.
    /// </summary>
    [JsonPropertyName("availableQuantity")]
    public int AvailableQuantity { get; set; }

    /// <summary>
    /// Quantity currently rented out.
    /// </summary>
    [JsonPropertyName("rentedQuantity")]
    public int RentedQuantity { get; set; }

    /// <summary>
    /// Daily rental rate.
    /// </summary>
    [JsonPropertyName("dailyRate")]
    public decimal DailyRate { get; set; }

    /// <summary>
    /// Weekly rental rate.
    /// </summary>
    [JsonPropertyName("weeklyRate")]
    public decimal WeeklyRate { get; set; }

    /// <summary>
    /// Monthly rental rate.
    /// </summary>
    [JsonPropertyName("monthlyRate")]
    public decimal MonthlyRate { get; set; }

    /// <summary>
    /// Security deposit amount.
    /// </summary>
    [JsonPropertyName("securityDeposit")]
    public decimal SecurityDeposit { get; set; }

    /// <summary>
    /// Item status.
    /// </summary>
    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    /// <summary>
    /// Additional notes.
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

    /// <summary>
    /// Whether any quantity is available for rental.
    /// </summary>
    [JsonIgnore]
    public bool IsAvailable => AvailableQuantity > 0 && Status == EntityStatus.Active;
}
