using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Converters;

/// <summary>
/// Ensures all DateTime values are serialized/deserialized as UTC (ISO 8601 format)
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeString = reader.GetString();
        if (string.IsNullOrEmpty(dateTimeString))
            return DateTime.MinValue;

        // Parse and ensure it's UTC
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            // If the date doesn't have timezone info, assume UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            // Convert to UTC if it's local
            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return dateTime;
        }

        throw new JsonException($"Unable to parse '{dateTimeString}' as DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always write as UTC in ISO 8601 format with 'Z' suffix
        var utcDateTime = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>
/// Nullable DateTime version
/// </summary>
public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeString = reader.GetString();
        if (string.IsNullOrEmpty(dateTimeString))
            return null;

        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return dateTime;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        var utcDateTime = value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();

        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
