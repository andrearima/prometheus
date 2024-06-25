using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet("Country/{country}/State/{state}/City/{city}")]
    public async Task<IActionResult> Get(string country, string state, string city)
    {
        // Do stuff
        return Ok();
    }
}
