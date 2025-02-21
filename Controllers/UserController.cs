using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController: ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("active/{areActive}", Name = "Get all active/blocked users")]
        public async Task<ActionResult<List<User>>> GetAllUsersByStatus(bool areActive)
        {
            return await UserService.GetUsersByField("activityStatus", areActive ? ActivityStatus.Active : ActivityStatus.Blocked);
        }

        [HttpPost("from-request/{requestId}", Name = "Create user from request id")]
        public async Task<ActionResult<string>> UpsertUserRequest(string requestId)
        {
            try
            {
                int responseStatus = await UserService.CreateUserFromRequestId(requestId);

                switch (responseStatus)
                {
                    case StatusCodes.Status400BadRequest:
                        return BadRequest("Invalid user request");
                    case StatusCodes.Status206PartialContent:
                        return StatusCode(responseStatus, "Created user but failed to delete request");
                    default:
                        return Ok("User created successfully");
                }
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't create use from request", detail: e.Message);
            }
        }


        [HttpPost("restore/{userId}",  Name = "Reactivate a blocked user")]
        public async Task<ActionResult<string>> UnbanUser(string userId)
        {
            try
            {
                if (!userId.IsObjectId())
                {
                    return BadRequest($"Invalid ObjectId {userId}");
                }

                var updateResult = await UserService.RestoreUser(userId);

                return Ok($"User with id {userId} restored succesfully");
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't restore user", detail: e.Message);
            }
        }
    }
}
