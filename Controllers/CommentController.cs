using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("comments")]
    public class CommentController : ControllerBase
    {
        private readonly ILogger<CommentController> _logger;

        public CommentController(ILogger<CommentController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{parentCommentId}", Name = "Return the comment with the given id, expanded to the given depth")]
        [UserIdValidator]
        public async Task<ActionResult<ExpandedComment>> GetCommentsByParentPostId(
            string parentCommentId,
            [FromQuery(Name = "depth")] int depth = 1)
        {
            if (!parentCommentId.IsObjectId())
            {
                return BadRequest("Invalid or missing parentCommentId");
            }

            var commentId = ObjectId.Parse(parentCommentId);
            var comment = await CommentService.GetCommentById(commentId, depth);

            return comment == null ? NoContent() : Ok(comment);
        }

        [HttpPost(Name = "Upsert comment")]
        public async Task<ActionResult<ReplaceOneResult>> UpsertComment([FromForm] string commentJson, [FromForm] List<IFormFile>? mediaFiles)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            Comment comment;
            try
            {
                comment = JsonSerializer.Deserialize<Comment>(commentJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
            }
            catch (Exception e)
            {
                return BadRequest("Invalid JSON in comment data: " + e.Message);
            }

            if ((comment.id?.IsObjectId() ?? false) && !CommentService.CanUserEditComment(userId!, comment))
            {
                return Unauthorized($"User {userId} does not have permission to update the comment {comment.id}");
            }

            // Process media files if any
            if (mediaFiles is not null && mediaFiles.Count > 0)
            {
                List<MediaItem> mediaItems = new();

                foreach (var file in mediaFiles)
                {
                    var result = await MediaService.ProcessMediaAsync(file);
                    if (result.ErrorMessage != null || result.StatusCode >= StatusCodes.Status300MultipleChoices)
                        return StatusCode(result.StatusCode, result.ErrorMessage);

                    mediaItems.Add(result.Data!);
                }

                comment.media = mediaItems.ToArray();
            }

            return Ok((await CommentService.UpsertComment(userId!, comment)).Serialize());
        }

        [HttpDelete("{commentId}", Name = "Delete comment")]
        public async Task<ActionResult<bool>> DeleteComment(string commentId)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await CommentService.CanUserDeleteComment(userId!, commentId))
            {
                return Unauthorized($"User {userId} does not have permission to delete comment {commentId}");
            }

            return Ok(await CommentService.DeleteComment(commentId));
        }
    }
}
