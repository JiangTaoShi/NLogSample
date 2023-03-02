using Microsoft.AspNetCore.Mvc;
using NLogApp.References.App1API;

namespace NLogApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> logger;
        private readonly IApp1Manager app1Manager;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IApp1Manager app1Manager)
        {
            this.logger = logger;
            this.app1Manager = app1Manager;
        }

        [Route("test")]
        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            logger.LogInformation("call webapp1 test1 start");
            await app1Manager.GetData("test1");
            logger.LogInformation("call webapp1 test1 end");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}