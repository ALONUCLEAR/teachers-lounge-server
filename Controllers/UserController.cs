using System.Linq.Expressions;
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

                var updateResult = await UserService.ChangeUserStatus(userId, true);

                return Ok($"User with id {userId} restored succesfully");
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't restore user", detail: e.Message);
            }
        }

        [HttpPost("block/{userId}", Name = "Block an activer user")]
        public async Task<ActionResult<string>> BlockUser(string userId)
        {
            try
            {
                if (!userId.IsObjectId())
                {
                    return BadRequest($"Invalid ObjectId {userId}");
                }

                var updateResult = await UserService.ChangeUserStatus(userId, false);

                return Ok($"User with id {userId} blocked succesfully");
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't block user", detail: e.Message);
            }
        }

        [HttpPost("login", Name="Get user by credentials")]
        public async Task<ActionResult<User?>> GetUserByCredentials([FromBody] Dictionary<string, string> credentials)
        {
            try
            {
                string govId = credentials["govId"];
                string password = credentials["password"];

                return Ok(await UserService.GetUserByCredentials(govId, password));
            } catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't get user from credentials", detail: e.Message);
            }


        }

        [HttpPost("updatePassword/email", Name = "Send Mail To User To Update Password")]
        public async Task<ActionResult<string>> SendUpdatePasswordEmail([FromBody] Dictionary<string, string> userDetails)
        {
            try {
                await UserService.SendChangePasswordEmail(userDetails["email"], userDetails["userId"]);

                return Ok("Email Was Sent");
            } catch (Exception e) {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Email Was Not Sent :()", detail: e.Message);
            }
        }

        [HttpPost("updatePassword", Name = "Update Password")]
        public async Task<ActionResult<string>> UpdatePassword([FromBody] Dictionary<string, string> userDetails)
        {
            try {
                await UserService.ChangePassword(userDetails["userId"], userDetails["newPassword"]);

                return Ok("Password Was Updated");
            } catch (Exception e) {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "No Passwordo Changed", detail: e.Message);
            }
        }

        [HttpGet("{govId}", Name = "Get UserId By GovId")]
        public async Task<ActionResult<string>> getUserIdByGovId(string govId)
        {
            return await UserService.getUserIdByGovId(govId);
        }
    }
}
