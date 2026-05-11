using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Api.Services;
using Api.Models;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoistureController : ControllerBase
{
    private readonly ThingerBucketService _thinger;
    private readonly SensorStateService _sensorState;

    public MoistureController(ThingerBucketService thinger, SensorStateService sensorState)
    {
        _thinger = thinger;
        _sensorState = sensorState;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var data = await _thinger.GetLatestAsync("moisture-data-bucket-id", maxItems: 50);
        var payload = data.Select(MoistureReadingDto.FromBucketEntry).ToList();
        return Ok(payload);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(_sensorState.GetStatus());
    }

    [HttpPost("start")]
    public IActionResult StartSensor()
    {
        return Ok(_sensorState.SetRunning(true, "api"));
    }

    [HttpPost("stop")]
    public IActionResult StopSensor()
    {
        return Ok(_sensorState.SetRunning(false, "api"));
    }

    [HttpGet("stream")]
    public async Task Stream()
    {
        HttpContext.Response.Headers["Cache-Control"] = "no-cache";
        HttpContext.Response.Headers["Content-Type"] = "text/event-stream";
        HttpContext.Response.Headers["X-Accel-Buffering"] = "no";

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        while (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            var data = await _thinger.GetLatestAsync("moisture-data-bucket-id", maxItems: 1);
            var latest = data.FirstOrDefault();

            if (latest is not null)
            {
                var payload = MoistureReadingDto.FromBucketEntry(latest);
                var json = JsonSerializer.Serialize(payload, options);
                await HttpContext.Response.WriteAsync($"data: {json}\n\n");
                await HttpContext.Response.Body.FlushAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(5), HttpContext.RequestAborted);
        }
    }
}
