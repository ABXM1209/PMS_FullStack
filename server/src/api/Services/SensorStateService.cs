namespace Api.Services;

public sealed class SensorStateService
{
    private readonly object _sync = new();
    private bool _isRunning = true;
    private string _lastCommand = "initialized";
    private DateTime _lastCommandAtUtc = DateTime.UtcNow;

    public SensorStatusDto GetStatus()
    {
        lock (_sync)
        {
            return new SensorStatusDto
            {
                Running = _isRunning,
                LastCommand = _lastCommand,
                LastCommandAtUtc = _lastCommandAtUtc,
            };
        }
    }

    public SensorCommandResult SetRunning(bool running, string source)
    {
        lock (_sync)
        {
            _isRunning = running;
            _lastCommand = running ? "start" : "stop";
            _lastCommandAtUtc = DateTime.UtcNow;

            return new SensorCommandResult
            {
                Success = true,
                Message = $"Sensor {(_isRunning ? "started" : "stopped")} via {source}.",
                Running = _isRunning,
                LastCommand = _lastCommand,
                LastCommandAtUtc = _lastCommandAtUtc,
            };
        }
    }
}

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
