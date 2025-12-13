
namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Emergency contact information for an employee.
/// </summary>
public class EmergencyContact
{
    /// <summary>
    /// Name of the emergency contact.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the emergency contact.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Relationship to the employee (e.g., Spouse, Parent, Sibling).
    /// </summary>
    [JsonPropertyName("relationship")]
    public string Relationship { get; set; } = string.Empty;
}
