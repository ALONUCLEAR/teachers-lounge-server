using Microsoft.AspNetCore.Mvc;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : Controller
    {
        [HttpGet(Name="Health Check")]
        public ActionResult<string> HealthCheck()
        {
            return Ok("Service is healthy");
        }
    }
}
