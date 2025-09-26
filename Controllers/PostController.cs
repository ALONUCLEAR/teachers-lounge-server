using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("posts")]
    public class PostController : ControllerBase
    {
        private readonly ILogger<PostController> _logger;

        public PostController(ILogger<PostController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id}", Name = "Get post by ID")]
        [UserIdValidator]
        public async Task<ActionResult<ExpandedPost>> GetPostById(string id, [FromQuery(Name = "depth")] int depth = 1)
        {
            if (!id.IsObjectId())
            {
                return BadRequest($"Failed to get post by id. Invalid post id {id}");
            }

            var post = await PostService.GetPostById(ObjectId.Parse(id), depth);

            return post == null ? NoContent() : Ok(post);
        }

        [HttpGet("by-subjects", Name = "Get posts by subject IDs")]
        [UserIdValidator]
        public async Task<ActionResult<IEnumerable<ExpandedPost>>> GetPostsBySubjectIds([FromQuery(Name = "subjectIds[]")] string[]? subjectIds)
        {
            var objectIds = subjectIds?.FilterAndMap(Utils.IsObjectId, ObjectId.Parse) ?? [];

            if (objectIds.Length == 0)
            {
                return NoContent();
            }

            return Ok(await PostService.GetPostsBySubjectIds(objectIds));
        }

        [HttpPost(Name = "Upsert post")]
        public async Task<ActionResult<ReplaceOneResult>> UpsertPost([FromBody] Post post, [FromQuery(Name = "importantParticipants[]")] string[]? importantParticipants = null)
        {
            try
            {
                if (!Request.Headers.TryGetValue("userId", out var userId))
                {
                    return BadRequest("userId header is missing");
                }

                if ((post.id?.IsObjectId() ?? false) && !PostService.CanUserEditPost(userId, post))
                {
                    return Unauthorized($"User {userId} does not have permissions to update the post {post.id}");
                }

                var res = await PostService.UpsertPost(post);

                if (importantParticipants != null)
                {
                    Association? subject = await AssociationService.GetAssociationById(ObjectId.Parse(post.subjectId));
                    string content = $"לפוסט קוראים {post.title} והוא נמצא בתוך הנושא {subject!.name}.\nרוצים לקרוא אותו? הכנסו למערכת וקחו חלק בשיח!";
                    string pageLink = res.UpsertedId != null ? $"posts/{res.UpsertedId.AsObjectId.ToString()}" : "forum";

                    Alert newPostAlert = new Alert()
                    {
                        title = "פורסם פוסט חדש שנוגע לך",
                        body = content,
                        importanceLevel = ImportanceLevel.Medium,
                        link = $"{Utils.CLIENT_BASE_URL}/#/{pageLink}",
                        dateCreated = DateTime.Now,
                    };

                    newPostAlert = await AlertService.FillAlertByAssociations(newPostAlert, importantParticipants);
                    await AlertService.SendAlert(newPostAlert, true, userId);
                }

                return Ok(res.Serialize());
            } catch (Exception e)
            {
                this._logger.LogError(e, "An error occured while trying to create post");

                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: $"Couldn't create post {post.title}", detail: e.Message);
            }
        }

        [HttpDelete("{postId}", Name = "Delete post")]
        public async Task<ActionResult<bool>> DeletePost(string postId)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await PostService.CanUserDeletePost(userId!, postId))
            {
                return Unauthorized($"User {userId} do not have permissions to delete the post {postId}");
            }

            return Ok(await PostService.DeletePost(postId));
        }
    }
}
