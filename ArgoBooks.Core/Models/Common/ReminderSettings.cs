using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Settings for invoice payment reminders.
/// </summary>
public class ReminderSettings
{
    /// <summary>
    /// Send reminder 1 day before/after due date.
    /// </summary>
    [JsonPropertyName("day1")]
    public bool Day1 { get; set; } = true;

    /// <summary>
    /// Send reminder 7 days before/after due date.
    /// </summary>
    [JsonPropertyName("day7")]
    public bool Day7 { get; set; } = true;

    /// <summary>
    /// Send reminder 14 days after due date.
    /// </summary>
    [JsonPropertyName("day14")]
    public bool Day14 { get; set; }

    /// <summary>
    /// Send reminder 30 days after due date.
    /// </summary>
    [JsonPropertyName("day30")]
    public bool Day30 { get; set; }
}
