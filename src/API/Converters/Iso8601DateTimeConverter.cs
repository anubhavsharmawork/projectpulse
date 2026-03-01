using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Converters;

/// <summary>
/// Ensures all DateTime values are serialized as ISO 8601 UTC strings
/// with the "yyyy-MM-ddTHH:mm:ss.fffZ" format.
/// Deserialization accepts ISO 8601 strings with or without timezone,
/// date-only strings (yyyy-MM-dd), and converts to UTC.
/// </summary>
public sealed class Iso8601DateTimeConverter : JsonConverter<DateTime>
{
    private const string Iso8601Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return default;

        // Accept standard ISO 8601 variants
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        // Fallback: exact date-only format
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateOnly))
        {
            return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
        }

        throw new JsonException($"Unable to parse '{raw}' as ISO 8601 DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always write UTC with Z suffix
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        writer.WriteStringValue(utc.ToString(Iso8601Format, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Handles nullable DateTime with the same ISO 8601 format.
/// Writes null as JSON null.
/// </summary>
public sealed class Iso8601NullableDateTimeConverter : JsonConverter<DateTime?>
{
    private const string Iso8601Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateOnly))
        {
            return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
        }

        throw new JsonException($"Unable to parse '{raw}' as ISO 8601 DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        var utc = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : value.Value.ToUniversalTime();

        writer.WriteStringValue(utc.ToString(Iso8601Format, CultureInfo.InvariantCulture));
    }
}
