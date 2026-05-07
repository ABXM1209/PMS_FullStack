namespace Api.Controllers;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

/// <summary>
/// Reads time-series data from a Thinger.io data bucket via the REST API.
/// </summary>
public class ThingerBucketController : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _username;

    /// <param name="accessToken">Bearer token from Thinger.io Access Tokens section.</param>
    /// <param name="username">Your Thinger.io username (e.g. "UserName").</param>
    /// <param name="baseUrl">Base URL of your Thinger.io instance. Defaults to the cloud server.</param>
    public ThingerBucketController(string accessToken, string username, string baseUrl = "https://api.thinger.io")
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));
        if (string.IsNullOrWhiteSpace(username))    throw new ArgumentNullException(nameof(username));

        _username = username;
        _baseUrl  = baseUrl.TrimEnd('/');

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retrieves bucket entries with optional filters.
    /// </summary>
    /// <param name="bucketId">The bucket ID (e.g. "moisture-data-bucket-id").</param>
    /// <param name="maxItems">Maximum number of rows to return.</param>
    /// <param name="from">Start of time range (UTC). Leave null for no lower bound.</param>
    /// <param name="to">End of time range (UTC). Leave null for no upper bound.</param>
    /// <returns>A list of bucket entries, newest first.</returns>
    public async Task<List<BucketEntry>> GetBucketDataAsync(
        string   bucketId,
        int      maxItems = 100,
        DateTime? from    = null,
        DateTime? to      = null)
    {
        var url = BuildUrl(bucketId, maxItems, from, to);

        Console.WriteLine("==============================================");
        Console.WriteLine($"[Thinger] GET {url}");
        Console.WriteLine($"[Thinger] Token length: {_httpClient.DefaultRequestHeaders.Authorization?.Parameter?.Length ?? 0}");
        Console.WriteLine($"[Thinger] Token prefix: {_httpClient.DefaultRequestHeaders.Authorization?.Parameter?[..20]}");
        Console.WriteLine("==============================================");
        
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new ThingerApiException(
                (int)response.StatusCode,
                $"Thinger.io API error {(int)response.StatusCode}: {body} | URL: {url}");
        }

        string json = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result  = JsonSerializer.Deserialize<List<BucketEntry>>(json, options);

        return result ?? new List<BucketEntry>();
    }

    /// <summary>
    /// Convenience overload — returns the most recent <paramref name="maxItems"/> entries.
    /// </summary>
    public Task<List<BucketEntry>> GetLatestAsync(string bucketId, int maxItems = 10)
        => GetBucketDataAsync(bucketId, maxItems);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string BuildUrl(string bucketId, int maxItems, DateTime? from, DateTime? to)
    {
        // Base path
        var url = $"{_baseUrl}/v3/users/{Uri.EscapeDataString(_username)}" +
                  $"/buckets/{Uri.EscapeDataString(bucketId)}/data" +
                  $"?items={maxItems}&sort=desc";

        if (from.HasValue)
            url += $"&min_ts={ToUnixMilliseconds(from.Value)}";

        if (to.HasValue)
            url += $"&max_ts={ToUnixMilliseconds(to.Value)}";

        return url;
    }

    private static long ToUnixMilliseconds(DateTime dt)
        => new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();

    public void Dispose() => _httpClient.Dispose();
}

// -------------------------------------------------------------------------
// Models
// -------------------------------------------------------------------------

/// <summary>
/// Represents a single row returned by the bucket data API.
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

// -------------------------------------------------------------------------
// Exception
// -------------------------------------------------------------------------

public class ThingerApiException : Exception
{
    public int StatusCode { get; }

    public ThingerApiException(int statusCode, string message)
        : base(message) => StatusCode = statusCode;
}

// -------------------------------------------------------------------------
// Usage example (remove or move to your Program.cs / controller action)
// -------------------------------------------------------------------------

// var controller = new ThingerBucketController(
//     accessToken: "YOUR_BEARER_TOKEN",
//     username:    "Asparrow"
// );
//
// // Get the 50 most recent entries
// var entries = await controller.GetLatestAsync("moisture-data-bucket-id", maxItems: 50);
//
// foreach (var entry in entries)
// {
//     double? moisture = entry.GetDouble("moisture");
//     Console.WriteLine($"{entry.Time:u}  moisture={moisture}");
// }
//
// // Get entries from the last 24 hours
// var recent = await controller.GetBucketDataAsync(
//     "moisture-data-bucket-id",
//     maxItems: 500,
//     from: DateTime.UtcNow.AddHours(-24)
// );