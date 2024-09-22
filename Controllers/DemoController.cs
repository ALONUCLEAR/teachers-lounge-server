using Microsoft.AspNetCore.Mvc;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetNumbers")]
        public IEnumerable<int> Get()
        {
            return Enumerable.Range(1, 5);
        }
    }
}
