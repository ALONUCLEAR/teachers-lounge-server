using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("requests")]
    public class UserRequestController: ControllerBase
    {
        private readonly ILogger<UserRequestController> _logger;

        public UserRequestController(ILogger<UserRequestController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "All user requests")]
        public async Task<ActionResult<IEnumerable<UserRequest>>> GetAllUserRequests()
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            return Ok(await UserRequestService.GetAllRelevantUserRequests(userId));
        }

        [HttpPost(Name = "Create user Request")]
        public async Task<ActionResult<string>> UpsertUserRequest([FromBody] UserRequest UserRequest)
        {
            try
            {
                int responseStatus = await UserRequestService.CreateUserRequest(UserRequest);

                switch (responseStatus)
                {
                    case StatusCodes.Status400BadRequest:
                        return BadRequest("Invalid user request input");
                    default:
                        return Ok("User creation request sent successfully");
                }
            } catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "User creation request wasn't sent", detail: e.Message);
            }
        }

        [HttpPost("recovery/{govId}", Name = "User Recovery Request")]
        public async Task<ActionResult> SendUserRecoveryRequest(string govId)
        {
            await UserRequestService.SendUserRecoveryRequest(govId);

            return Ok();
        }

        [HttpDelete("{requestId}", Name = "Delete UserRequest")]
        public async Task<ActionResult<bool>> DeleteUserRequest(string requestId)
        {
            if (!Request.Headers.TryGetValue("userId", out var requestingUserId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await UserService.CanRequestAffectUser(requestingUserId, requestId, ActivityStatus.Pending))
            {
                return Unauthorized($"You do not have permissions to deny the request {requestId}");
            }

            return Ok(await UserRequestService.DeleteUserRequest(requestId));
        }
    }
}