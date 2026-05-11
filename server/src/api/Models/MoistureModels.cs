using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Models;

/// <summary>
/// Represents a single entry from a Thinger.io data bucket.
/// The <see cref="Val"/> dictionary holds your sensor fields
/// (e.g. "moisture", "temperature") keyed by field name.
/// </summary>
public class BucketEntry
{
    /// <summary>Server timestamp in milliseconds since Unix epoch.</summary>
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    /// <summary>Timestamp as a UTC DateTime for convenience.</summary>
    [JsonIgnore]
    public DateTime Time => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;

    /// <summary>The sensor payload — key/value pairs for each field in the bucket.</summary>
    [JsonPropertyName("val")]
    public Dictionary<string, JsonElement> Val { get; set; } = new();

    /// <summary>
    /// Helper: read a numeric field by name.
    /// Returns null if the field is missing or not a number.
    /// </summary>
    public double? GetDouble(string field)
        => Val.TryGetValue(field, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetDouble()
            : null;

    /// <summary>Helper: read a string field by name.</summary>
    public string? GetString(string field)
        => Val.TryGetValue(field, out var el) ? el.ToString() : null;
}

/// <summary>
/// Simplified moisture reading shape exposed to the frontend.
/// </summary>
public class MoistureReadingDto
{
    public DateTime Timestamp { get; set; }
    public double? Percent { get; set; }
    public string? Raw { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();

    public static MoistureReadingDto FromBucketEntry(BucketEntry entry)
    {
        return new MoistureReadingDto
        {
            Timestamp = entry.Time,
            Percent = entry.GetDouble("percent") ?? entry.GetDouble("moisture"),
            Raw = entry.GetString("raw"),
            Values = entry.Val.ToDictionary(kvp => kvp.Key, kvp => GetElementValue(kvp.Value)),
        };
    }

    private static object? GetElementValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString(),
        };
}