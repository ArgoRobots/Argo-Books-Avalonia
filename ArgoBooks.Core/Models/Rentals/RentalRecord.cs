using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Models.Rentals;

/// <summary>
/// Represents a rental record/transaction.
/// </summary>
public class RentalRecord
{
    /// <summary>
    /// Unique identifier (e.g., RNT-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Rental item ID.
    /// </summary>
    [JsonPropertyName("rentalItemId")]
    public string RentalItemId { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Accountant ID who processed the rental.
    /// </summary>
    [JsonPropertyName("accountantId")]
    public string? AccountantId { get; set; }

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
    /// Security deposit collected.
    /// </summary>
    [JsonPropertyName("securityDeposit")]
    public decimal SecurityDeposit { get; set; }

    /// <summary>
    /// Rental start date.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Due date for return.
    /// </summary>
    [JsonPropertyName("dueDate")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Actual return date.
    /// </summary>
    [JsonPropertyName("returnDate")]
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// Rental status.
    /// </summary>
    [JsonPropertyName("status")]
    public RentalStatus Status { get; set; } = RentalStatus.Active;

    /// <summary>
    /// Total cost (calculated at return).
    /// </summary>
    [JsonPropertyName("totalCost")]
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Amount of deposit refunded.
    /// </summary>
    [JsonPropertyName("depositRefunded")]
    public decimal? DepositRefunded { get; set; }

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
    /// Whether the rental is overdue.
    /// </summary>
    [JsonIgnore]
    public bool IsOverdue => Status == RentalStatus.Active && DateTime.Today > DueDate.Date;

    /// <summary>
    /// Number of days overdue (0 if not overdue).
    /// </summary>
    [JsonIgnore]
    public int DaysOverdue => IsOverdue ? (int)(DateTime.Today - DueDate.Date).TotalDays : 0;
}
