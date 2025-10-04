using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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

        /// <summary>
        /// <param name="areActive">areActive: true -> active users, false -> blocked users</param> <br></br>
        /// <param name="affectedOnly">affectedOnly: whether or not to filter for users the requesting user can affect, eg. change their associatedSchools</param>
        /// </summary>
        /// <returns></returns>
        [HttpGet("active/{areActive}", Name = "Get all active/blocked users")]
        public async Task<ActionResult<List<User>>> GetAllUsersByStatus(bool areActive, [FromQuery] bool affectedOnly = false)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            return await UserService.GetUsersByStatus(userId, areActive ? ActivityStatus.Active : ActivityStatus.Blocked, affectedOnly);
        }

        [HttpPost("from-request/{requestId}", Name = "Create user from request id")]
        public async Task<ActionResult<string>> UpsertUserRequest(string requestId)
        {
            try
            {
                if (!Request.Headers.TryGetValue("userId", out var requestingUserId))
                {
                    return BadRequest("userId header is missing");
                }

                if (!await UserService.CanRequestAffectUser(requestingUserId, requestId, ActivityStatus.Pending))
                {
                    return Unauthorized($"You do not have permissions to accept the request {requestId}");
                }

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

        [HttpPost("restore/{userId}", Name = "Reactivate a blocked user")]
        public async Task<ActionResult<string>> UnbanUser(string userId)
        {
            try
            {
                if (!Request.Headers.TryGetValue("userId", out var requestingUserId))
                {
                    return BadRequest("userId header is missing");
                }

                if (!userId.IsObjectId())
                {
                    return BadRequest($"Invalid ObjectId {userId}");
                }

                if (!await UserService.CanRequestAffectUser(requestingUserId, userId, ActivityStatus.Blocked))
                {
                    return Unauthorized($"You do not have permissions to unban user {userId}");
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
                if (!Request.Headers.TryGetValue("userId", out var requestingUserId))
                {
                    return BadRequest("userId header is missing");
                }

                if (!userId.IsObjectId())
                {
                    return BadRequest($"Invalid ObjectId {userId}");
                }

                if (!await UserService.CanRequestAffectUser(requestingUserId, userId))
                {
                    return Unauthorized($"You do not have permissions to ban user {userId}");
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

        [HttpPost("login", Name = "Get user by credentials")]
        public async Task<ActionResult<User?>> GetUserByCredentials([FromBody] Dictionary<string, string> credentials)
        {
            try
            {
                if (!credentials.ContainsKey("govId"))
                {
                    return BadRequest("Failed to login the given user. Required field \"govId\" was not provided.");
                }

                if (!credentials.ContainsKey("password"))
                {
                    return BadRequest("Failed to login the given user. Required field \"password\" was not provided.");
                }

                string govId = credentials["govId"];
                string password = credentials["password"];

                return Ok(await UserService.GetUserByCredentials(govId, password));
            } catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't get user from credentials", detail: e.Message);
            }
        }

        [HttpGet("from-school/{schoolId}", Name = "All active users from said school")]
        [UserIdValidator]
        public async Task<ActionResult<IEnumerable<User>>> GetAllAssociationsOfType(string schoolId, [FromQuery] bool includePending = false)
        {
            try
            {
                if (!schoolId.IsObjectId())
                {
                    return BadRequest($"Invalid schoolId {schoolId}. Did not fit the ObjectId format");
                }
                IEnumerable<User> users = await UserService.GetUsersBySchool(ObjectId.Parse(schoolId));

                if (includePending)
                {
                    // no need to check because we already have the UserIdValidator
                    Request.Headers.TryGetValue("userId", out var userId);
                    var userRequestsForSchool = await UserRequestService.GetAllRequestsForSchool(userId!, schoolId);
                    users = users.Concat(userRequestsForSchool);
                }

                return Ok(users);
            } catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't get users from schoolId {schoolId}", detail: e.Message);
            }
        }

        [HttpPost("{targetUserId}/unlink-school/{schoolId}", Name = "Remove the school from the associatedSchools array")]
        [UserIdValidator]
        public async Task<ActionResult<UpdateResult>> UnlinkSchool(string targetUserId, string schoolId)
        {
            try
            {
                if (!targetUserId.IsObjectId())
                {
                    return BadRequest($"Invalid targetUserId {targetUserId}. Did not fit the ObjectId format");
                }

                if (!schoolId.IsObjectId())
                {
                    return BadRequest($"Invalid schoolId {schoolId}. Did not fit the ObjectId format");
                }

                Request.Headers.TryGetValue("userId", out var userId);

                if (!await UserService.CanRequestAffectUser(userId, targetUserId))
                {
                    return Unauthorized($"You do not have permissions to unlink the user {targetUserId}");
                }

                return Ok(await UserService.UnlinkSchool(targetUserId, schoolId));
            } catch (Exception e) {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't unlink school {schoolId} from user {targetUserId}", detail: e.Message);
            }
        }

        [HttpPost("link-school/{schoolId}", Name = "Add the school from the associatedSchools array for all target users")]
        [UserIdValidator]
        public async Task<ActionResult<UpdateResult>> LinkSchool(string schoolId, [FromBody] string[] targetUserIds)
        {
            try
            {
                if (targetUserIds.Some(id => !id.IsObjectId()))
                {
                    return BadRequest($"Invalid targetUserId. One of these values [{targetUserIds.Join(", ")}] did not fit the ObjectId format");
                }

                if (!schoolId.IsObjectId())
                {
                    return BadRequest($"Invalid schoolId {schoolId}. Did not fit the ObjectId format");
                }

                return Ok(await UserService.LinkSchool(targetUserIds, schoolId));
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't link users {targetUserIds.Join(", ")} to school {schoolId}", detail: e.Message);
            }
        }

        [HttpPatch("{targetUserId}/set-schools", Name = "Set the schools as the associatedSchools array for the target user")]
        public async Task<ActionResult<UpdateResult>> SetSchools(string targetUserId, [FromBody] string[] schoolIds)
        {
            try
            {
                if (!Request.Headers.TryGetValue("userId", out var requestingUserId))
                {
                    return BadRequest($"Required \"userId\" header is missing");
                }

                if (!Utils.IsObjectId(requestingUserId!))
                {
                    return BadRequest($"Invalid \"userId\" header - {requestingUserId} is not a valid ObjectId");
                }

                if (schoolIds.Some(id => !id.IsObjectId()))
                {
                    return BadRequest($"Invalid schoolIds. One of these values [{schoolIds.Join(", ")}] did not fit the ObjectId format");
                }

                if (!targetUserId.IsObjectId())
                {
                    return BadRequest($"Invalid targetId {targetUserId}. Did not fit the ObjectId format");
                }

                if (!await UserService.CanRequestAffectUser(requestingUserId!, targetUserId))
                {
                    return Unauthorized($"User {requestingUserId} cannot set the schools associated with {targetUserId}");
                }

                return Ok(await UserService.SetSchools(targetUserId, schoolIds));
            } catch (Exception e) {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't link user {targetUserId} to schools {schoolIds.Join(", ")}", detail: e.Message);
            }
        }

        [HttpPost("updatePassword", Name = "Update Password")]
        public async Task<ActionResult<string>> UpdatePassword([FromBody] Dictionary<string, string> userDetails)
        {
            try
            {
                await UserService.ChangePassword(userDetails["userId"], userDetails["newPassword"]);

                return Ok("Password Was Updated");
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "No Passwordo Changed", detail: e.Message);                
            }
        }

        [HttpPatch("promote/{targetUserId}/to/{targetRole}", Name = "Promote userId to role (if the permissions fit)")]
        [UserIdValidator]
        public async Task<ActionResult<bool>> Promote(string targetUserId, string targetRole)
        {
            try
            {
                Request.Headers.TryGetValue("userId", out var userId);
                var validityResponse = await CanChangeUserRole(userId, targetUserId, targetRole);

                if (validityResponse.Result == null || validityResponse.Result is not OkObjectResult okResult
                    || okResult.Value is not bool isValid || !isValid)
                {
                    return validityResponse;
                }

                int targetUserRoleComparison = await UserService.CompareUserToTargetRole(targetUserId, targetRole);

                if (targetUserRoleComparison >= 0)
                {
                    return BadRequest($"The target role {targetRole} is not a promotion for {targetUserId}.\nIf you meant to demote them, use /demote instead of /promote");
                }

                var res = await UserService.ChangeUserRole(targetUserId, targetRole);

                return Ok(res.IsAcknowledged);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "No Passwordo Changed", detail: e.Message);
            }
        }

        [HttpPatch("demote/{targetUserId}/to/{targetRole}", Name = "Demote userId to role (if the permissions fit)")]
        [UserIdValidator]
        public async Task<ActionResult<bool>> Demote(string targetUserId, string targetRole)
        {
            try
            {
                Request.Headers.TryGetValue("userId", out var userId);
                var validityResponse = await CanChangeUserRole(userId, targetUserId, targetRole);

                if (validityResponse.Result == null || validityResponse.Result is not OkObjectResult okResult
                    || okResult.Value is not bool isValid || !isValid)
                {
                    return validityResponse;
                }

                int targetUserRoleComparison = await UserService.CompareUserToTargetRole(targetUserId, targetRole);

                if (targetUserRoleComparison <= 0)
                {
                    return BadRequest($"The target role {targetRole} is not a demotion for {targetUserId}.\nIf you meant to promote them, use /demote instead of /promote");
                }

                var res = await UserService.ChangeUserRole(targetUserId, targetRole);

                return Ok(res.IsAcknowledged);
            }
            catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "No Passwordo Changed", detail: e.Message);
            }
        }

        private async Task<ActionResult<bool>> CanChangeUserRole(string? requestingUserId, string targetUserId, string role)
        {
            try
            {
                if (requestingUserId == null)
                {
                    return BadRequest("Required userId header was not provided");
                }

                if (!requestingUserId.IsObjectId())
                {
                    return BadRequest($"The requesting user id {requestingUserId} is not a valid ObjectId");
                }

                if (!targetUserId.IsObjectId())
                {
                    return BadRequest($"The given target user id {targetUserId} is not a valid ObjectId");
                }

                if (!Role.isValid(role))
                {
                    return BadRequest($"The given role({role}) is invalid");
                }

                if (!await UserService.CanRequestAffectUser(requestingUserId!, targetUserId))
                {
                    return Unauthorized($"The requesting user {requestingUserId} cannot change the role of the target user {targetUserId}");
                }

                if (!await UserService.HasPermissions(requestingUserId, role, false))
                {
                    return Unauthorized($"The requesting user {requestingUserId} cacnnot grant the role {role}");
                }

                return Ok(true);
            } catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Failed to validate users before role change", detail: e.Message);
            }
        }


            [HttpGet("{govId}", Name = "Get User By GovId")]
        public async Task<ActionResult<User?>> GetUserByGovId(string govId)
        {
            try
            {
                return Ok(await UserService.GetUserByGovId(govId));
            } catch (Exception e)
            {
                this._logger.LogError(e.Message);

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't get user with govId {govId}", detail: e.Message);
            }

        }
    }
}
