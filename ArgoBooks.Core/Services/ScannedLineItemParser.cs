using System.Text.Json;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Parses a single <c>lineItems[]</c> element from a receipt scan response into a
/// <see cref="ScannedLineItem"/>. Shared by the Gemini and proxy scanners so the field
/// extraction (and product-name cleaning) stays consistent. Numeric fields require a JSON number
/// so a stray string value is ignored rather than throwing.
/// </summary>
public static class ScannedLineItemParser
{
    /// <summary>
    /// Parses one line-item JSON element. Returns false (with a default item) when the element
    /// carries no usable data (no description, unit price, or total price).
    /// </summary>
    public static bool TryParse(JsonElement item, out ScannedLineItem lineItem)
    {
        lineItem = new ScannedLineItem();
        var hasData = false;

        if (item.TryGetProperty("description", out var desc) && desc.ValueKind != JsonValueKind.Null)
        {
            lineItem.Description = ReceiptDescriptionCleaner.Clean(desc.GetString());
            hasData = true;
        }

        if (item.TryGetProperty("quantity", out var qty) && qty.ValueKind == JsonValueKind.Number)
            lineItem.Quantity = qty.GetDecimal();

        if (item.TryGetProperty("unitPrice", out var unitPrice) && unitPrice.ValueKind == JsonValueKind.Number)
        {
            lineItem.UnitPrice = unitPrice.GetDecimal();
            hasData = true;
        }

        if (item.TryGetProperty("totalPrice", out var totalPrice) && totalPrice.ValueKind == JsonValueKind.Number)
        {
            lineItem.TotalPrice = totalPrice.GetDecimal();
            hasData = true;
        }

        if (item.TryGetProperty("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number)
            lineItem.Confidence = confidence.GetDouble();

        return hasData;
    }
}
