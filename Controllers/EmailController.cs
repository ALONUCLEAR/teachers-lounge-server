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

        [HttpPost("to/{emailAddress}", Name = "Send Mail to email address")]
        public async Task<ActionResult> SendMail(string emailAddress, [FromBody] MailInput mailInput)
        {
            try
            {
                await EmailService.SendMailToAddress(emailAddress, mailInput);

                return Ok();
            } catch (Exception mailSendingException)
            {
                this._logger.LogError(mailSendingException.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Email wasn't sent", detail: mailSendingException.Message);

            }
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
