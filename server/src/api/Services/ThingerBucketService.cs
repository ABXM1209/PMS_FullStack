using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Api.Models;

namespace Api.Services;

/// <summary>
/// Reads time-series data from a Thinger.io data bucket via the REST API.
/// </summary>
public class ThingerBucketService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _username;

    /// <param name="accessToken">Bearer token from Thinger.io Access Tokens section.</param>
    /// <param name="username">Your Thinger.io username (e.g. "UserName").</param>
    /// <param name="baseUrl">Base URL of your Thinger.io instance. Defaults to the cloud server.</param>
    public ThingerBucketService(string accessToken, string username, string baseUrl = "https://eu-central.aws.thinger.io")
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentNullException(nameof(accessToken));
        if (string.IsNullOrWhiteSpace(username))    throw new ArgumentNullException(nameof(username));

        _username = username;
        _baseUrl  = baseUrl.TrimEnd('/');

        _httpClient = new HttpClient();  // create ONCE with default handler
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);  // then set headers
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

    public async Task<string> DebugAsync()
    {
        var tests = new[]
        {
            $"{_baseUrl}/v3/users/{_username}/buckets",
            $"{_baseUrl}/v1/users/{_username}/buckets",
            $"{_baseUrl}/v2/users/{_username}/buckets",
            $"{_baseUrl}/",
        };

        var sb = new System.Text.StringBuilder();
        foreach (var url in tests)
        {
            Console.WriteLine($"[Thinger Debug] GET {url}");
            var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Thinger Debug] {url} => {(int)response.StatusCode}: {body}");
            sb.AppendLine($"{url} => {(int)response.StatusCode}: {body}");
        }
        return sb.ToString();
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
        var url = $"{_baseUrl}/v1/users/{Uri.EscapeDataString(_username)}" +
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
// Exception
// -------------------------------------------------------------------------

public class ThingerApiException : Exception
{
    public int StatusCode { get; }

    public ThingerApiException(int statusCode, string message)
        : base(message) => StatusCode = statusCode;
}