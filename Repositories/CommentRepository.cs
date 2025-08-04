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

        // TODO: a lot of similar stuff here, we might want to put a function/var/calculated field for most  of the pipe
        public async Task<List<ObjectId>> GetAllAncestorCommentIds(ObjectId directParentId)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("_id", directParentId)),
                new BsonDocument("$graphLookup", new BsonDocument
                {
                    { "from", CollectionName },
                    { "startWith", "$parentId" },
                    { "connectFromField", "parentId" },
                    { "connectToField", "_id" },
                    { "as", "ancestors" }
                }),
                new BsonDocument("$project", new BsonDocument("ancestorIds",
                new BsonDocument("$map", new BsonDocument
                {
                    { "input", "$ancestors" },
                    { "as", "a" },
                    { "in", "$$a._id" }
                })
                ))
            };

            var result = await Collection
                .Aggregate<BsonDocument>(pipeline)
                .FirstOrDefaultAsync();

            var ancestorCommentsIds = result?.GetValue("ancestorIds")
                ?.AsBsonArray
                ?.Select(id => id.AsObjectId)
                ?.ToList()
                ?? new List<ObjectId>();
            ancestorCommentsIds.Add(directParentId);

            return ancestorCommentsIds;
        }

        public async Task<List<ObjectId>> GetAllNestedCommentIds(ObjectId rootCommentId)
        {
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument("_id", rootCommentId)),
                new BsonDocument("$graphLookup", new BsonDocument
                {
                    { "from", "comments" },
                    { "startWith", "$_id" },
                    { "connectFromField", "_id" },
                    { "connectToField", "parentId" },
                    { "as", "descendants" }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "allIds", new BsonDocument("$concatArrays", new BsonArray
                    {
                        new BsonArray { "$_id" },
                        new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$descendants" },
                            { "as", "desc" },
                            { "in", "$$desc._id" }
                        })
                    })
                    }
                })
            };

            var result = await Collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

            return result?.GetValue("allIds")?.AsBsonArray?.Map(id => id.AsObjectId)?.ToList() ?? new List<ObjectId>();
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
