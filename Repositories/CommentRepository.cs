using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class CommentRepository : Repository
    {
        protected override string CollectionName => "comments";

        public Task<List<Comment>> GetAllComments()
        {
            return MongoService.GetEntireCollection<Comment>(Collection);
        }

        public Task<List<Comment>> GetCommentsByField<TValue>(string fieldName, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, fieldName, value, Comment.FromBsonDocument);
        }

        public Task<List<ObjectId>> GetAllAncestorCommentIds(ObjectId directParentId)
        {
            return MongoService.GraphTraverseIds(Collection, directParentId, "parentId", "parentId", "_id", "ancestorIds");
        }

        public Task<List<ObjectId>> GetAllNestedCommentIds(ObjectId rootCommentId)
        {
            return MongoService.GraphTraverseIds(Collection, rootCommentId, "_id", "_id", "parentId", "descendantIds");

        }


        public async Task<UpdateResult> IncremeantCountForAllAncestorComments(List<ObjectId> ancestorCommentsIds, int delta)
        {
            return await Collection.UpdateManyAsync(
                Builders<BsonDocument>.Filter.In("_id", ancestorCommentsIds),
                Builders<BsonDocument>.Update.Inc("totalChildrenCount", delta));
        }
        public Task<bool> DeleteComments(List<string> commentIds)
        {
            return DeleteComments(commentIds.Map(id => ObjectId.Parse(id)));
        }
        public async Task<bool> DeleteComments(List<ObjectId> commentIds)
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", commentIds);
            var result = await Collection.DeleteManyAsync(filter);
            return result.DeletedCount == commentIds.Count;
        }

        public async Task<bool> DeleteCommentsByParentPostId(ObjectId parentPostId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("parentPostId", parentPostId);
            var result = await Collection.DeleteManyAsync(filter);

            return result.IsAcknowledged;
        }

        public Task<List<Comment>> GetCommentsByFieldIn<TValue>(string fieldName, TValue[] values)
        {
            return MongoService.GetEntitiesByFieldValueIn(Collection, fieldName, values, Comment.FromBsonDocument);
        }

        public Task<List<Comment>> GetCommentsByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return MongoService.GetEntitiesByMultipleFilters(Collection, filterList, Comment.FromBsonDocument);
        }

        public Task<ReplaceOneResult> UpsertComment(Comment comment)
        {
            return MongoService.UpsertEntity(Collection, comment);
        }

        public Task<bool> DeleteComment(string commentId)
        {
            return MongoService.DeleteEntity(Collection, commentId);
        }
    }
}
