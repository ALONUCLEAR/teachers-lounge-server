using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class PostService
    {
        private static PostRepository repo => new PostRepository();
        // TODO: add a function that turns a post from DB into a Post that includes the optional mostPopularComment field (of type Comment)
        public static Task<List<Post>> GetPostsBySubjectIds(ObjectId[] subjectIds)
        {
            return GetPostsByFieldIn("subjectId", subjectIds);
        }

        public static async Task<Post?> GetPostById(ObjectId postId)
        {
            List<Post> posts = await GetPostsByField("_id", postId);

            return posts.Count != 1 ? null : posts[0];
        }

        public static Task<List<Post>> GetPostsByField<TValue>(string fieldName, TValue value)
        {
            return repo.GetPostsByField(fieldName, value);
        }

        public static Task<List<Post>> GetPostsByFieldIn<TValue>(string fieldName, TValue[] values)
        {
            return repo.GetPostsByFieldIn(fieldName, values);
        }

        public static Task<List<Post>> GetPostsByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return repo.GetPostsByMultipleFilters(filterList);
        }

        public static async Task<ReplaceOneResult> UpsertPost(string userId, Post post)
        {
            return await repo.UpsertPost(post);
        }

        public static async Task<bool> CanUserDeletePost(string? userId, string postId)
        {
            if (!postId.IsObjectId())
            {
                return false;
            }

            var post = await GetPostById(ObjectId.Parse(postId));

            return post != null && await CanUserDeletePost(userId, post);
        }

        public static async Task<bool> CanUserDeletePost(string? userId, Post post)
        {
            var user = await UserService.GetUserById(userId);
            var subject = await AssociationService.GetAssociationById(ObjectId.Parse(post.subjectId));

            if (user == null || subject == null)
            {
                return false;
            }

            if (userId == post.authorId)
            {
                return true;
            }

            bool isAdmin = await UserService.HasPermissions(userId, Role.Admin);

            bool doShareSchool = user.associatedSchools.Some(schoolId => subject.associatedSchools.Contains(schoolId));

            return isAdmin && doShareSchool;
        }

        public static bool CanUserEditPost(string? userId, Post post)
        {
            return post.authorId == userId;
        }

        // TODO: fix the delete method here so it deletes all the comments as well
        public static Task<UpdateResult> IncrementTotalChildrenCount(ObjectId postId, int delta)
        {
            return repo.IncremeantTotalCommentsCount(postId, delta);
        }


        public static Task<bool> DeletePost(string postId)
        {
            return repo.DeletePost(postId);
        }
    }
}
