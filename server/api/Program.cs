using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Protocol;

DotEnv.Load(Path.Combine(builderEnvironmentContentRoot(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ThingerOptions>(builder.Configuration.GetSection("Thinger"));
builder.Services.AddSingleton(ThingerOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddHttpClient<ThingerRestClient>();
builder.Services.AddSingleton<ThingerMqttClient>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ThingerMqttClient>());

var app = builder.Build();

app.UseCors("client");
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new { name = "Plant Moisture API", status = "running" }));

app.MapGet("/api/health", (ThingerOptions options, ThingerMqttClient mqtt) => Results.Ok(new
{
    status = "ok",
    thinger = new
    {
        options.Username,
        mqttClientId = options.MqttClientId,
        options.BaseUrl,
        mqttBroker = options.MqttBroker,
        mqttConnected = mqtt.IsConnected,
        devices = options.Devices
    }
}));

app.MapGet("/api/devices", async (string? search, ThingerOptions options, ThingerRestClient thinger, TelemetryStore store, CancellationToken cancellationToken) =>
{
    var devices = options.Devices.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        devices = devices.Where(device =>
            device.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            device.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    var summaries = new List<DeviceSummary>();
    foreach (var device in devices)
    {
        // Prefer an in-memory live reading. If none exists, fetch the latest from Thinger and add it.
        var latest = store.Latest(device.Id);
        if (latest is null)
        {
            latest = (await thinger.GetBucketHistoryAsync(device, 1, cancellationToken)).FirstOrDefault();
            if (latest is not null)
            {
                store.Add(device.Id, latest);
            }
        }

        // If we have a latest reading, derive an "online" flag from its age so devices show offline
        // when telemetry is stale (e.g., the ESP32 isn't running).
        TelemetryReading? displayLatest = latest;
        if (displayLatest is not null)
        {
            var age = DateTimeOffset.UtcNow - displayLatest.Timestamp;
            var online = displayLatest.Online ?? true; // assume online if payload didn't specify
            if (age > TimeSpan.FromSeconds(30))
            {
                online = false;
            }

            displayLatest = displayLatest with { Online = online };
        }

        summaries.Add(new DeviceSummary(
            device.Id,
            device.Name,
            device.BucketId,
            device.TelemetryTopic,
            device.CommandTopic,
            displayLatest));
    }

    return Results.Ok(summaries);
});

app.MapGet("/api/devices/{deviceId}/telemetry/history", async (string deviceId, int? items, ThingerOptions options, ThingerRestClient thinger, TelemetryStore store, CancellationToken cancellationToken) =>
{
    var device = options.GetDevice(deviceId);
    var count = Math.Clamp(items ?? 25, 1, 1000);
    var history = await thinger.GetBucketHistoryAsync(device, count, cancellationToken);

    foreach (var reading in history.OrderBy(reading => reading.Timestamp))
    {
        store.Add(device.Id, reading);
    }

    return Results.Ok(history);
});

app.MapGet("/api/devices/{deviceId}/telemetry/latest", async (string deviceId, ThingerOptions options, ThingerRestClient thinger, TelemetryStore store, CancellationToken cancellationToken) =>
{
    var device = options.GetDevice(deviceId);
    var live = store.Latest(device.Id);
    if (live is not null)
    {
        return Results.Ok(live);
    }

    var history = await thinger.GetBucketHistoryAsync(device, 1, cancellationToken);
    var latest = history.FirstOrDefault();

    if (latest is not null)
    {
        store.Add(device.Id, latest);
        return Results.Ok(latest);
    }

    return Results.NotFound(new { message = "No telemetry has been found in Thinger.io yet." });
});

app.MapPost("/api/devices/{deviceId}/pump", async (string deviceId, PumpCommand command, ThingerOptions options, ThingerMqttClient mqtt, TelemetryStore store, CancellationToken cancellationToken) =>
{
    var device = options.GetDevice(deviceId);
    var result = await mqtt.PublishPumpCommandAsync(device, command, cancellationToken);
    store.SetDeviceState(device.Id, command.Enabled, command.Auto);

    return Results.Accepted($"/api/devices/{device.Id}/pump", new
    {
        device.Id,
        command.Enabled,
        command.Auto,
        published = result,
        message = result
            ? "Pump command published to MQTT."
            : "Pump state stored locally, but MQTT is not connected yet."
    });
});

app.MapPost("/api/devices/{deviceId}/telemetry/simulate", (string deviceId, TelemetryInput input, ThingerOptions options, TelemetryStore store) =>
{
    var device = options.GetDevice(deviceId);
    var reading = new TelemetryReading(
        device.Id,
        DateTimeOffset.UtcNow,
        input.Percent,
        input.Raw,
        input.Data,
        input.PumpOn,
        input.Auto,
        input.Online,
        "simulated",
        input.Extra ?? new Dictionary<string, JsonElement>());

    store.Add(device.Id, reading);

    return Results.Created($"/api/devices/{device.Id}/telemetry/latest", reading);
});

app.MapGet("/api/telemetry/history", (int? items) =>
    Results.Redirect($"/api/devices/Billy/telemetry/history?items={items ?? 25}"));

app.MapGet("/api/telemetry/latest", () =>
    Results.Redirect("/api/devices/Billy/telemetry/latest"));

app.MapPost("/api/pump", async (PumpCommand command, ThingerOptions options, ThingerMqttClient mqtt, TelemetryStore store, CancellationToken cancellationToken) =>
{
    var device = options.Devices.First();
    var result = await mqtt.PublishPumpCommandAsync(device, command, cancellationToken);
    store.SetDeviceState(device.Id, command.Enabled, command.Auto);

    return Results.Accepted($"/api/devices/{device.Id}/pump", new { device.Id, command.Enabled, command.Auto, published = result });
});

app.Run();

static string builderEnvironmentContentRoot()
{
    return AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
        : Directory.GetCurrentDirectory();
}

sealed class DotEnv
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

sealed record ThingerOptions
{
    public required string AccessToken { get; init; }
    public required string Username { get; init; }
    public required string BaseUrl { get; init; }
    public required string MqttClientId { get; init; }
    public required string MqttBroker { get; init; }
    public required int MqttPort { get; init; }
    public required bool MqttUseTls { get; init; }
    public required string MqttPassword { get; init; }
    public required IReadOnlyList<DeviceConfig> Devices { get; init; }

    public DeviceConfig GetDevice(string deviceId)
    {
        return Devices.FirstOrDefault(device => string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new BadHttpRequestException($"Unknown device '{deviceId}'.");
    }

    public static ThingerOptions FromConfiguration(IConfiguration configuration)
    {
        var defaultDeviceId = configuration["THINGER_DEVICE_ID"] ?? "Billy";
        var defaultBucketId = configuration["THINGER_BUCKET_ID"] ?? "moisture-data-bucket-id";
        var defaultTelemetryTopic = configuration["THINGER_MQTT_TELEMETRY_TOPIC"] ?? "telemetry";
        var defaultCommandTopic = configuration["THINGER_MQTT_COMMAND_TOPIC"] ?? "pump/control";

        return new ThingerOptions
        {
            AccessToken = Required(configuration, "THINGER_ACCESS_TOKEN"),
            Username = Required(configuration, "THINGER_USERNAME"),
            BaseUrl = configuration["THINGER_BASE_URL"] ?? "https://eu-central.aws.thinger.io",
            MqttClientId = configuration["THINGER_MQTT_CLIENT_ID"] ?? defaultDeviceId,
            MqttBroker = configuration["THINGER_MQTT_BROKER"] ?? "backend.thinger.io",
            MqttPort = int.TryParse(configuration["THINGER_MQTT_PORT"], out var port) ? port : 1883,
            MqttUseTls = bool.TryParse(configuration["THINGER_MQTT_USE_TLS"], out var tls) && tls,
            MqttPassword = configuration["THINGER_MQTT_PASSWORD"] ?? string.Empty,
            Devices = ParseDevices(configuration["THINGER_DEVICES"], defaultDeviceId, defaultBucketId, defaultTelemetryTopic, defaultCommandTopic)
        };
    }

    static string Required(IConfiguration configuration, string key)
    {
        return configuration[key] ?? throw new InvalidOperationException($"{key} must be configured.");
    }

    static IReadOnlyList<DeviceConfig> ParseDevices(string? rawDevices, string defaultDeviceId, string defaultBucketId, string defaultTelemetryTopic, string defaultCommandTopic)
    {
        if (string.IsNullOrWhiteSpace(rawDevices))
        {
            return [new DeviceConfig(defaultDeviceId, defaultDeviceId, defaultBucketId, defaultTelemetryTopic, defaultCommandTopic)];
        }

        return rawDevices
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(raw =>
            {
                var parts = raw.Split('|', StringSplitOptions.TrimEntries);
                var id = parts.ElementAtOrDefault(0);
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException("Every THINGER_DEVICES entry must start with a device id.");
                }

                return new DeviceConfig(
                    id,
                    parts.ElementAtOrDefault(1) is { Length: > 0 } name ? name : id,
                    parts.ElementAtOrDefault(2) is { Length: > 0 } bucket ? bucket : defaultBucketId,
                    parts.ElementAtOrDefault(3) is { Length: > 0 } telemetry ? telemetry : defaultTelemetryTopic.Replace("{deviceId}", id, StringComparison.OrdinalIgnoreCase),
                    parts.ElementAtOrDefault(4) is { Length: > 0 } command ? command : defaultCommandTopic.Replace("{deviceId}", id, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
    }
}

sealed record DeviceConfig(string Id, string Name, string BucketId, string TelemetryTopic, string CommandTopic);

sealed record DeviceSummary(
    string Id,
    string Name,
    string BucketId,
    string TelemetryTopic,
    string CommandTopic,
    TelemetryReading? Latest);

sealed class ThingerRestClient(HttpClient httpClient, ThingerOptions options, ILogger<ThingerRestClient> logger)
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TelemetryReading>> GetBucketHistoryAsync(DeviceConfig device, int items, CancellationToken cancellationToken)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/v1/users/{Uri.EscapeDataString(options.Username)}/buckets/{Uri.EscapeDataString(device.BucketId)}/data?items={items}&sort=desc";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Thinger bucket request failed with {StatusCode}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Thinger bucket request failed with {(int)response.StatusCode}.");
        }

        var rows = JsonSerializer.Deserialize<List<ThingerBucketRow>>(body, JsonOptions) ?? [];
        return rows
            .Select(row => row.ToTelemetryReading(device.Id))
            .Where(reading => reading is not null)
            .Cast<TelemetryReading>()
            .ToList();
    }
}

sealed class ThingerMqttClient(
    ThingerOptions options,
    TelemetryStore store,
    ILogger<ThingerMqttClient> logger) : BackgroundService
{
    IMqttClient? _client;

    public bool IsConnected => _client?.IsConnected == true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(options.MqttPassword))
        {
            logger.LogWarning("THINGER_MQTT_PASSWORD is empty. MQTT connection will be skipped.");
            return;
        }

        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;
        _client.DisconnectedAsync += async args =>
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            logger.LogWarning("MQTT disconnected: {Reason}", args.ReasonString);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    var connectResult = await _client.ConnectAsync(BuildOptions(), stoppingToken);
                    if (!_client.IsConnected)
                    {
                        throw new InvalidOperationException($"MQTT connection was rejected: {connectResult.ResultCode} {connectResult.ReasonString}");
                    }

                    foreach (var device in options.Devices)
                    {
                        await _client.SubscribeAsync(device.TelemetryTopic, MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);
                    }

                    logger.LogInformation("MQTT connected to {Broker}:{Port} and subscribed to {Count} device topic(s).", options.MqttBroker, options.MqttPort, options.Devices.Count);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MQTT connection attempt failed. The backend will retry.");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    public async Task<bool> PublishPumpCommandAsync(DeviceConfig device, PumpCommand command, CancellationToken cancellationToken)
    {
        if (_client?.IsConnected != true)
        {
            return false;
        }

        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["deviceId"] = device.Id,
            ["pump"] = command.Enabled,
            ["enabled"] = command.Enabled,
            ["auto"] = command.Auto,
            ["source"] = "dashboard",
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(device.CommandTopic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await _client.PublishAsync(message, cancellationToken);
        return result.IsSuccess;
    }

    MqttClientOptions BuildOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(options.MqttClientId)
            .WithTcpServer(options.MqttBroker, options.MqttPort)
            .WithCredentials(options.Username, options.MqttPassword)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

        if (options.MqttUseTls)
        {
            builder.WithTlsOptions(new MqttClientTlsOptions { UseTls = true });
        }

        return builder.Build();
    }

    Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var payload = args.ApplicationMessage.ConvertPayloadToString();
        try
        {
            using var document = JsonDocument.Parse(payload);
            var device = options.Devices.FirstOrDefault(device => device.TelemetryTopic == args.ApplicationMessage.Topic)
                ?? options.Devices.First();
            var reading = TelemetryReading.FromPayload(device.Id, DateTimeOffset.UtcNow, document.RootElement, "mqtt");
            store.Add(device.Id, reading);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not parse MQTT payload from {Topic}: {Payload}", args.ApplicationMessage.Topic, payload);
        }

        return Task.CompletedTask;
    }
}

sealed class TelemetryStore
{
    readonly ConcurrentDictionary<string, ConcurrentQueue<TelemetryReading>> _readings = new(StringComparer.OrdinalIgnoreCase);
    readonly object _stateLock = new();
    readonly Dictionary<string, bool?> _pumpStates = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, bool?> _autoStates = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, bool?> _onlineStates = new(StringComparer.OrdinalIgnoreCase);

    public TelemetryReading? Latest(string deviceId)
    {
        return _readings.TryGetValue(deviceId, out var readings)
            ? readings.OrderByDescending(reading => reading.Timestamp).FirstOrDefault()
            : null;
    }

    public void Add(string deviceId, TelemetryReading reading)
    {
        TelemetryReading value = reading with { DeviceId = deviceId };

        lock (_stateLock)
        {
            _pumpStates.TryGetValue(deviceId, out var pumpOn);
            _autoStates.TryGetValue(deviceId, out var autoMode);
            _onlineStates.TryGetValue(deviceId, out var online);

            if (value.PumpOn is null && pumpOn is not null)
            {
                value = value with { PumpOn = pumpOn };
            }
            else if (value.PumpOn is not null)
            {
                _pumpStates[deviceId] = value.PumpOn;
            }

            if (value.AutoMode is null && autoMode is not null)
            {
                value = value with { AutoMode = autoMode };
            }
            else if (value.AutoMode is not null)
            {
                _autoStates[deviceId] = value.AutoMode;
            }

            if (value.Online is null && online is not null)
            {
                value = value with { Online = online };
            }
            else if (value.Online is not null)
            {
                _onlineStates[deviceId] = value.Online;
            }
        }

        var readings = _readings.GetOrAdd(deviceId, _ => new ConcurrentQueue<TelemetryReading>());
        readings.Enqueue(value);

        while (readings.Count > 200)
        {
            readings.TryDequeue(out _);
        }
    }

    public void SetDeviceState(string deviceId, bool? pumpOn, bool? autoMode)
    {
        lock (_stateLock)
        {
            if (pumpOn is not null)
            {
                _pumpStates[deviceId] = pumpOn;
            }

            if (autoMode is not null)
            {
                _autoStates[deviceId] = autoMode;
            }
        }
    }
}

sealed record TelemetryReading(
    string DeviceId,
    DateTimeOffset Timestamp,
    double? Percent,
    double? Raw,
    double? Data,
    bool? PumpOn,
    bool? AutoMode,
    bool? Online,
    string Source,
    IReadOnlyDictionary<string, JsonElement> Extra)
{
    public static TelemetryReading FromPayload(string deviceId, DateTimeOffset timestamp, JsonElement payload, string source)
    {
        var extra = new Dictionary<string, JsonElement>();

        foreach (var property in payload.EnumerateObject())
        {
            extra[property.Name] = property.Value.Clone();
        }

        return new TelemetryReading(
            deviceId,
            timestamp,
            ReadDouble(payload, "percent", "moisture", "moisturePercent"),
            ReadDouble(payload, "raw", "analog", "sensor"),
            ReadDouble(payload, "data"),
            ReadBool(payload, "pump", "pumpOn", "enabled", "active"),
            ReadBool(payload, "auto", "autoMode", "automatic"),
            ReadBool(payload, "online", "connected"),
            source,
            extra);
    }

    static double? ReadDouble(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (!payload.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return null;
    }

    static bool? ReadBool(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (!payload.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number != 0;
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

sealed record ThingerBucketRow(
    [property: JsonPropertyName("ts")] long Timestamp,
    [property: JsonPropertyName("val")] JsonElement Value)
{
    public TelemetryReading? ToTelemetryReading(string deviceId)
    {
        if (Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TelemetryReading.FromPayload(
            deviceId,
            DateTimeOffset.FromUnixTimeMilliseconds(Timestamp),
            Value,
            "thinger-bucket");
    }
}

sealed record PumpCommand(bool? Enabled, bool? Auto);

sealed record TelemetryInput(
    double? Percent,
    double? Raw,
    double? Data,
    bool? PumpOn,
    bool? Auto,
    bool? Online,
    Dictionary<string, JsonElement>? Extra);
