using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents a department within the company.
/// </summary>
public class Department
{
    /// <summary>
    /// Unique identifier (e.g., DEP-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Department name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the department head.
    /// </summary>
    [JsonPropertyName("headEmployeeId")]
    public string? HeadEmployeeId { get; set; }

    /// <summary>
    /// Department budget.
    /// </summary>
    [JsonPropertyName("budget")]
    public decimal Budget { get; set; }

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
