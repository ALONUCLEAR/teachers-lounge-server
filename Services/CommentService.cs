using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class CommentService
    {
        private static CommentRepository repo => new CommentRepository();

        public static async Task<List<ExpandedComment>> GetCommentsByParentId(ObjectId parentId, int depth = 1)
        {
            var comments = await GetCommentsByField("parentId", parentId);
            return await Expand(comments.ToArray(), depth);
        }

        public static async Task<List<ExpandedComment>> GetCommentsByParentPostId(ObjectId parentPostId, int depth = 1)
        {
            var comments = await GetCommentsByField("parentPostId", parentPostId);
            return await Expand(comments, depth);
        }

        public static async Task<ExpandedComment?> GetCommentById(ObjectId commentId, int depth = 1)
        {
            var comments = await GetCommentsByField("_id", commentId);
            return comments.Count != 1 ? null : await Expand(comments[0], depth);
        }

        public static Task<List<Comment>> GetCommentsByField<TValue>(string fieldName, TValue value)
        {
            return repo.GetCommentsByField(fieldName, value);
        }

        public static Task<List<Comment>> GetCommentsByFieldIn<TValue>(string fieldName, TValue[] values)
        {
            return repo.GetCommentsByFieldIn(fieldName, values);
        }

        public static Task<List<Comment>> GetCommentsByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return repo.GetCommentsByMultipleFilters(filterList);
        }

        public static async Task<ReplaceOneResult> UpsertComment(string userId, Comment comment)
        {
            var result = await repo.UpsertComment(comment);

            if (!comment.id.IsObjectId())
            {
                // New comment -> increment totalChildrenCount for parents
                await UpdateParentChildCount(ObjectId.Parse(comment.parentId), ObjectId.Parse(comment.parentPostId), 1);
            }

            return result;
        }

        public static async Task<bool> DeleteComment(string commentId)
        {
            if (!commentId.IsObjectId()) return false;

            var rootId = ObjectId.Parse(commentId);
            var root = await GetCommentById(rootId, 0);

            if (root == null) return false;

            var idsToDelete = await repo.GetAllNestedCommentIds(rootId);
            idsToDelete.Add(rootId);

            await repo.DeleteComments(idsToDelete);

            int totalRemoved = idsToDelete.Count;
            await UpdateParentChildCount(ObjectId.Parse(root.parentId), ObjectId.Parse(root.parentPostId), -totalRemoved);

            return true;
        }

        public static Task<bool> DeleteAllCommentsInPost(ObjectId postId)
        {
            return repo.DeleteCommentsByParentPostId(postId);
        }

        public static async Task<bool> CanUserDeleteComment(string? userId, string commentId)
        {
            if (!commentId.IsObjectId())
                return false;

            var comment = await GetCommentById(ObjectId.Parse(commentId));
            return comment != null && await CanUserDeleteComment(userId, comment);
        }

        public static async Task<bool> CanUserDeleteComment(string? userId, Comment comment)
        {
            var user = await UserService.GetUserById(userId);
            var parentPost = await PostService.GetPostById(ObjectId.Parse(comment.parentPostId));
            if (user == null || parentPost == null) return false;

            var subject = await AssociationService.GetAssociationById(ObjectId.Parse(parentPost.subjectId));
            if (subject == null) return false;

            if (userId == comment.authorId)
                return true;

            bool isAdmin = await UserService.HasPermissions(userId, Role.Admin);
            bool doShareSchool = user.associatedSchools.Some(s => subject.associatedSchools.Contains(s));
            return isAdmin && doShareSchool;
        }

        public static bool CanUserEditComment(string? userId, Comment comment)
        {
            return comment.authorId == userId;
        }

        private static async Task<ExpandedComment> Expand(Comment comment, int depth = 1)
        {
            if (depth <= 0) return new ExpandedComment(comment);

            try
            {
                var children = await GetCommentsByParentId(ObjectId.Parse(comment.id), depth - 1);
                return new ExpandedComment(comment, children.ToArray());
            }
            catch
            {
                return new ExpandedComment(comment);
            }
        }

        private static Task<List<ExpandedComment>> Expand(Comment[] comments, int depth = 1)
        {
            return Expand(comments.ToList(), depth);
        }

        private static async Task<List<ExpandedComment>> Expand(IEnumerable<Comment> comments, int depth = 1)
        {
            if (depth <= 0) return comments.Map(c => new ExpandedComment(c));

            var tasks = comments.Map(comment => Expand(comment, depth));
            return (await Task.WhenAll(tasks)).ToList();
        }

        private static async Task<bool> UpdateParentChildCount(ObjectId parentId, ObjectId parentPostId, int delta)
        {
            var ancestorCommentsIds = await repo.GetAllAncestorCommentIds(parentId);
            ancestorCommentsIds.Add(parentId);

            await repo.IncremeantCountForAllAncestorComments(ancestorCommentsIds, delta);

            await PostService.IncrementTotalChildrenCount(parentPostId, delta);

            return true;
        }

    }
}
