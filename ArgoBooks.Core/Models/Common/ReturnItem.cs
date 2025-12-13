
namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Represents an item being returned.
/// </summary>
public class ReturnItem
{
    /// <summary>
    /// Product ID of the returned item.
    /// </summary>
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Quantity being returned.
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Reason for the return.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
