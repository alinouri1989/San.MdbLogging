using Microsoft.AspNetCore.Mvc;
using MongoLogger;

namespace San.MdbLogging.WebApiTest.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase, ILoggable
{
    private readonly IMdbLogger<WeatherForecastController> _logger;

    public WeatherForecastController(IMdbLogger<WeatherForecastController> logger)
    {
        this._logger = logger;
    }

    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {

        var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        _logger.Log(546543213465, "Weather Hadler => ", result);
        return result;
    }
}
