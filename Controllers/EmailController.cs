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
        [UserIdValidator]
        public async Task<ActionResult> SendMail(string emailAddress, [FromBody] MailInput mailInput)
        {
            try
            {
                await EmailService.SendMailToAddresses([emailAddress], mailInput);

                return Ok();
            } catch (Exception mailSendingException)
            {
                this._logger.LogError(mailSendingException.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Email wasn't sent", detail: mailSendingException.Message);

            }
        }

        [HttpPost("to/multiple", Name = "Send Mail to multiple email address")]
        [UserIdValidator]
        public async Task<ActionResult> SendMailToMultipleAddresses([FromQuery(Name = "addresses[]")] string[] emailAddresses, [FromBody] MailInput mailInput)
        {
            try
            {
                await EmailService.SendMailToAddresses(emailAddresses, mailInput);

                return Ok();
            } catch (Exception mailSendingException)
            {
                this._logger.LogError(mailSendingException.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Email wasn't sent", detail: mailSendingException.Message);

            }
        }

        [HttpPost("send-code/to", Name = "Send Code")]
        public async Task<ActionResult> SendCode([FromQuery(Name = "govId")] string? govId, [FromQuery(Name ="emailAddress")] string? emailAddress)
        {
            if (govId == null)
            {
                return BadRequest("Requeset to send code by email failed because required variable \"govId\" was not provided");
            }

            if (emailAddress == null)
            {
                return BadRequest("Requeset to send code by email failed because required variable \"emailAddress\" was not provided");
            }

            await EmailService.SendCodeToAddress(govId, emailAddress);
            return Ok();
        }

        [HttpPost("send-code/to-id/{govId}", Name = "Send Code To User With A Given Government ID")]
        public async Task<ActionResult> SendCodeByGovId(string govId)
        {
            await EmailService.SendCodeByGovId(govId);
            return Ok();
        }
    }
}
