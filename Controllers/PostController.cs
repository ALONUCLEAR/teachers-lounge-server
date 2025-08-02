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

        [HttpGet("by-subjects", Name = "Get posts by subject IDs")]
        [UserIdValidator]
        public async Task<ActionResult<IEnumerable<Post>>> GetPostsBySubjectIds([FromQuery(Name = "subjectIds[]")] string[]? subjectIds)
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
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if ((post.id?.IsObjectId() ?? false) && !PostService.CanUserEditPost(userId, post))
            {
                return Unauthorized($"User {userId} does not have permissions to update the post {post.id}");
            }



            if (importantParticipants != null)
            {
                Association? subject = await AssociationService.GetAssociationById(ObjectId.Parse(post.subjectId));
                string content = $"לפוסט קוראים {post.title} והוא נמצא בתוך הנושא {subject!.name}.\nרוצים לקרוא אותו? הכנסו למערכת וקחו חלק בשיח!";
                await EmailService.SendMailByAssociations(importantParticipants, "פורסם פוסט חדש שנוגע לך", content);
            }

            return Ok(await PostService.UpsertPost(userId!, post));
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
