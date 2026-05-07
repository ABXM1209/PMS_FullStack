using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealTimeController : ControllerBase
{
    private readonly ILogger<RealTimeController> _logger;
    private readonly IConfiguration _configuration;
    private const string WebhookSecretKey = "ThingerIo:WebhookSecret";

    public RealTimeController(ILogger<RealTimeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "Realtime webhook receiver is online" });

    [HttpPost("webhook")]
    [Consumes("application/json")]
    public IActionResult ReceiveWebhook([FromBody] JsonElement payload)
    {
        var configuredSecret = _configuration[WebhookSecretKey];
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            if (!Request.Headers.TryGetValue("X-ThingIo-Webhook-Secret", out var headerSecret) ||
                !string.Equals(headerSecret, configuredSecret, StringComparison.Ordinal))
            {
                _logger.LogWarning("Rejected webhook call because the Thinger.io webhook secret was invalid or missing.");
                return Unauthorized(new { error = "Invalid webhook secret" });
            }
        }

        _logger.LogInformation("Received Thinger.io webhook payload: {Payload}", payload.ToString());

        // TODO: Add your own processing, persistence, or event forwarding here.
        return Ok(new
        {
            receivedAtUtc = DateTime.UtcNow,
            payload
        });
    }
}
