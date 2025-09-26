using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("alerts")]
    public class AlertController : ControllerBase
    {
        private readonly ILogger<AlertController> _logger;

        public AlertController(ILogger<AlertController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "All alerts")]
        public async Task<ActionResult<IEnumerable<Alert>>> GetAllAlerts()
        {
            return Ok(await AlertService.GetAllAlerts());
        }

        [HttpGet("{id}", Name = "Get Alert By Id")]
        public async Task<ActionResult<Alert>> GetAlertById(string id)
        {
            if (!id.IsObjectId())
            {
                return BadRequest($"Id {id} doesn't fit the object Id format");
            }

            Alert? alert = await AlertService.GetAlertById(ObjectId.Parse(id));
            return alert == null ? NoContent() : Ok(alert);
        }

        [HttpGet("for-user", Name = "All alerts for the user with ID userId")]
        public async Task<ActionResult<List<Alert>>> GetAlertsForUser([FromQuery(Name = "onlyNew")] bool onlyNew = false)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!userId.ToString().IsObjectId())
            {
                return BadRequest("The value given for the \"userId\" header is not an object id");
            }

            try
            {
                return Ok(await AlertService.GetAlertsByUserId(ObjectId.Parse(userId)));
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't get alerts for user {userId}", detail: exception.Message);
            }
        }

        [HttpPost("send", Name = "Send alert(and email too if needed)")]
        public async Task<ActionResult> SendAlert([FromBody] Alert alert, [FromQuery(Name = "associationIds[]")] string[] associationIds, [FromQuery(Name = "shouldMail")] bool shouldMail = false)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            try
            {
                if (associationIds != null && associationIds.Length > 0)
                {
                    alert = await AlertService.FillAlertByAssociations(alert, associationIds);
                }

                await AlertService.SendAlert(alert, shouldMail, userId!);
                return Ok();
            }
            catch (Exception alertSendingException)
            {
                this._logger.LogError(alertSendingException.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Alert wasn't sent", detail: alertSendingException.Message);
            }
        }

        [HttpPatch("view/{alertId}")]
        public async Task<ActionResult<UpdateResult>> MarkAsViewed(string alertId)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!userId.ToString().IsObjectId())
            {
                return BadRequest("The value given for the \"userId\" header is not an object id");
            }

            
            if (!alertId.IsObjectId())
            {
                return BadRequest("alertId is not an object id");
            }

            try
            {
                return Ok(await AlertService.MarkAsViewed(alertId, userId!));
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Alert wasn't marked as viewed for user {userId}", detail: exception.Message);
            }
        }

        [HttpDelete("{alertId}", Name = "Delete alert")]
        public async Task<ActionResult<bool>> DeleteAlert(string alertId)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await UserService.HasPermissions(userId, Role.SuperAdmin))
            {
                return Unauthorized($"You do not have permissions to delete the alert {alertId}");
            }

            return Ok(await AlertService.DeleteAlert(alertId));
        }
    }

}
