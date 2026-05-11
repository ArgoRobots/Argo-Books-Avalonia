namespace ArgoBooks.Core.Enums;

/// <summary>
/// Where a Payment row originated. Distinguishes manual entries from
/// portal-sync rows so the UI can show provider info and the refund flow
/// knows which payments are eligible for portal-side refund.
/// </summary>
[JsonConverter(typeof(PaymentSourceJsonConverter))]
public enum PaymentSource
{
    /// <summary>Entered manually in the Argo Books desktop app.</summary>
    Manual = 0,

    /// <summary>Received via the payment portal (Stripe / PayPal / Square).</summary>
    Online = 1
}

/// <summary>
/// Permissive converter so legacy .argo files (which stored "Manual" or
/// "Online" as strings) continue to load. Unknown values fall back to
/// Manual — the safe default that won't accidentally enable portal-only
/// flows on a row that isn't actually a portal payment.
/// </summary>
public sealed class PaymentSourceJsonConverter : JsonConverter<PaymentSource>
{
    public override PaymentSource Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return PaymentSource.Manual;
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt32(out var n) && Enum.IsDefined(typeof(PaymentSource), n)
                ? (PaymentSource)n
                : PaymentSource.Manual;
        }
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return PaymentSource.Manual;
        return Enum.TryParse<PaymentSource>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : PaymentSource.Manual;
    }

    public override void Write(
        Utf8JsonWriter writer, PaymentSource value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
