using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("codes")]
    public class VerificationCodeController : ControllerBase
    {
        private readonly ILogger<VerificationCodeController> _logger;

        public VerificationCodeController(ILogger<VerificationCodeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("verify/{govId}", Name = "Verify code for a given govId")]
        public async Task<ActionResult<bool>> VerifyCode(string govId, [FromQuery] string? code)
        {
            if (code == null)
            {
                return BadRequest($"Required query parameter \"code\" was not provided");
            }

            try
            {
                return Ok(await VerificationCodeService.IsCodeVerified(govId, code));
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't verify code for user with govId {govId}", detail: exception.Message);
            }
        }
     }
}
