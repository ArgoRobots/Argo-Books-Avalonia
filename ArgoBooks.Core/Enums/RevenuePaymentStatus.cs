namespace ArgoBooks.Core.Enums;

/// <summary>
/// Collection state of a Revenue row, used by cash-basis dashboard /
/// analytics aggregations. Replaces the historical free-form string field.
///
/// <see cref="Paid"/> and <see cref="Complete"/> both mean "fully
/// collected"; <see cref="Complete"/> is a legacy alias preserved so
/// older imports continue to deserialize. New code should write
/// <see cref="Paid"/>.
///
/// See docs/Calculations.md §7 for the full table.
/// </summary>
[JsonConverter(typeof(RevenuePaymentStatusJsonConverter))]
public enum RevenuePaymentStatus
{
    /// <summary>Fully collected. Default for new rows.</summary>
    Paid = 0,

    /// <summary>Legacy alias for Paid, kept for older imports.</summary>
    Complete = 1,

    /// <summary>Some payment received but invoice not closed.</summary>
    Partial = 2,

    /// <summary>Awaiting payment; not yet collected.</summary>
    Pending = 3,

    /// <summary>No payment received.</summary>
    Unpaid = 4,

    /// <summary>Past due date and not paid.</summary>
    Overdue = 5
}

/// <summary>
/// Permissive JSON converter for <see cref="RevenuePaymentStatus"/>.
///
/// Falls back to <see cref="RevenuePaymentStatus.Paid"/> for null, empty,
/// or unknown values so legacy .argo files with blank or typo'd payment
/// statuses continue to load without crashing. Writes the enum name as a
/// string (matching the previous on-disk format).
/// </summary>
public sealed class RevenuePaymentStatusJsonConverter : JsonConverter<RevenuePaymentStatus>
{
    public override RevenuePaymentStatus Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return RevenuePaymentStatus.Paid;
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt32(out var n) && Enum.IsDefined(typeof(RevenuePaymentStatus), n)
                ? (RevenuePaymentStatus)n
                : RevenuePaymentStatus.Paid;
        }
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return RevenuePaymentStatus.Paid;
        return Enum.TryParse<RevenuePaymentStatus>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : RevenuePaymentStatus.Paid;
    }

    public override void Write(
        Utf8JsonWriter writer, RevenuePaymentStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
