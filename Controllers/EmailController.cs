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

        [HttpPost("send-code/to-id/{govId}", Name = "Send Code To User With A Given Government ID")]
        public async Task<ActionResult<string>> SendCodeByGovId(string govId)
        {
            return Ok(await EmailService.SendCodeByGovId(govId));
        }
    }
}
