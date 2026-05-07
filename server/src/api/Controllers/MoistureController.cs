using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoistureController : ControllerBase
{
    private readonly ThingerBucketController _thinger;

    public MoistureController(ThingerBucketController thinger)
    {
        _thinger = thinger;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var data = await _thinger.GetLatestAsync("moisture-data-bucket-id", maxItems: 50);
        return Ok(data);
    }
    
    [HttpGet("debug")]
    public async Task<IActionResult> Debug()
    {
        var result = await _thinger.DebugAsync();
        return Ok(result);
    }
    
    
}