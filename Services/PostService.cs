using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class PostService
    {
        private static PostRepository repo => new PostRepository();

        public static async Task<List<ExpandedPost>> GetPostsBySubjectIds(ObjectId[] subjectIds, int depth = 1)
        {
            var posts = await GetPostsByFieldIn("subjectId", subjectIds);
            return await Expand(posts, depth);
        }

        public static async Task<ExpandedPost?> GetPostById(ObjectId postId, int depth = 1)
        {
            var posts = await GetPostsByField("_id", postId);
            return posts.Count != 1 ? null : await Expand(posts[0], depth);
        }

        private static Task<List<Post>> GetPostsByField<TValue>(string fieldName, TValue value)
        {
            return repo.GetPostsByField(fieldName, value);
        }

        private static Task<List<Post>> GetPostsByFieldIn<TValue>(string fieldName, TValue[] values)
        {
            return repo.GetPostsByFieldIn(fieldName, values);
        }

        private static Task<List<Post>> GetPostsByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return repo.GetPostsByMultipleFilters(filterList);
        }

        public static async Task<bool> CanUserDeletePost(string? userId, string postId)
        {
            if (!postId.IsObjectId())
                return false;

            var post = await GetPostById(ObjectId.Parse(postId), 0);
            return post != null && await CanUserDeletePost(userId, post);
        }

        public static async Task<bool> CanUserDeletePost(string? userId, Post post)
        {
            var user = await UserService.GetUserById(userId);
            var subject = await AssociationService.GetAssociationById(ObjectId.Parse(post.subjectId));

            if (user == null || subject == null)
                return false;

            if (userId == post.authorId)
                return true;

            bool isAdmin = await UserService.HasPermissions(userId, Role.Admin);
            bool doShareSchool = user.associatedSchools.Some(schoolId => subject.associatedSchools.Contains(schoolId));

            return isAdmin && doShareSchool;
        }

        public static bool CanUserEditPost(string? userId, Post post)
        {
            return post.authorId == userId;
        }

        public static Task<ReplaceOneResult> UpsertPost(Post post)
        {
            return repo.UpsertPost(post);
        }

        public static Task<UpdateResult> IncrementTotalChildrenCount(ObjectId postId, int delta)
        {
            return repo.IncremeantTotalCommentsCount(postId, delta);
        }

        public static async Task<bool> DeletePost(string postId)
        {
            if (!postId.IsObjectId())
                return false;

            var parsedPostId = ObjectId.Parse(postId);
            var results = await Task.WhenAll([
                repo.DeletePost(postId),
                CommentService.DeleteAllCommentsInPost(parsedPostId)
            ]);

            return results?.Every(result => result) ?? false;
        }

        private static async Task<ExpandedPost> Expand(Post post, int depth = 1)
        {
            try
            {
                var comments = await CommentService.GetCommentsByParentId(ObjectId.Parse(post.id), depth - 1);
                return new ExpandedPost(post, comments.ToArray());
            }
            catch
            {
                return new ExpandedPost(post);
            }
        }

        private static async Task<List<ExpandedPost>> Expand(IEnumerable<Post> posts, int depth = 1)
        {
            if (depth <= 0)
                return posts.Map(p => new ExpandedPost(p));

            var tasks = posts.Map(post => Expand(post, depth));
            return (await Task.WhenAll(tasks)).ToList();
        }
    }
}
