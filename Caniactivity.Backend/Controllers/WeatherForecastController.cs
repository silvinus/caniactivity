using Caniactivity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace Caniactivity.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly CaniActivityContext _context;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, CaniActivityContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<RegisteredUser> Get()
        {
            return _context.RegisteredUsers;
        }
    }
}