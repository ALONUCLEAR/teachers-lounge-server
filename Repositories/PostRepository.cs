using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class PostRepository : Repository
    {
        protected override string CollectionName => "posts";

        public Task<List<Post>> GetAllPosts()
        {
            return MongoService.GetEntireCollection<Post>(Collection);
        }

        public Task<List<Post>> GetPostsByField<TValue>(string fieldName, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, fieldName, value, Post.FromBsonDocument);
        }

        public Task<List<Post>> GetPostsByFieldIn<TValue>(string fieldName, TValue[] values)
        {
            return MongoService.GetEntitiesByFieldValueIn(Collection, fieldName, values, Post.FromBsonDocument);
        }

        public Task<List<Post>> GetPostsByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return MongoService.GetEntitiesByMultipleFilters(Collection, filterList, Post.FromBsonDocument);
        }

        public Task<ReplaceOneResult> UpsertPost(Post post)
        {
            return MongoService.UpsertEntity(Collection, post);
        }
        public async Task<UpdateResult> IncremeantTotalCommentsCount(ObjectId postId, int delta)
        {
            return await Collection.UpdateManyAsync(
                Builders<BsonDocument>.Filter.Eq("_id", postId),
                Builders<BsonDocument>.Update.Inc("totalChildrenCount", delta));
        }
        public Task<bool> DeletePost(string postId)
        {
            return MongoService.DeleteEntity(Collection, postId);
        }
    }
}
