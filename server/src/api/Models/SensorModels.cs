namespace Api.Models;

public sealed class SensorStatusDto
{
    public bool Running { get; init; }
    public string LastCommand { get; init; } = string.Empty;
    public DateTime LastCommandAtUtc { get; init; }
}

public sealed class SensorCommandResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Running { get; init; }
    public string LastCommand { get; init; } = string.Empty;
    public DateTime LastCommandAtUtc { get; init; }
}