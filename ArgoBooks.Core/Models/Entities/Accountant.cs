
namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents an accountant who can be assigned to transactions.
/// </summary>
public class Accountant
{
    /// <summary>
    /// Unique identifier (e.g., ACC-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Accountant name.
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
    /// Number of transactions assigned to this accountant.
    /// </summary>
    [JsonPropertyName("assignedTransactions")]
    public int AssignedTransactions { get; set; }

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
