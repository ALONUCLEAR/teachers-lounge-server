using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("emails")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;

        public EmailController(ILogger<EmailController> logger)
        {
            _logger = logger;
        }


        [HttpPost("send-code/to/{emailAddress}", Name = "Send Code")]
        public async Task<ActionResult<string>> SendCode(string emailAddress)
        {
            return Ok(await EmailService.SendCodeToAddress(emailAddress));
        }
    }
}
