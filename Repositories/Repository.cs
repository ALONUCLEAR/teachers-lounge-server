using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public abstract class Repository
    {
        protected virtual string CollectionName => "";
        protected IMongoCollection<BsonDocument> Collection
        {
            get => MongoService.GetCollection<BsonDocument>(CollectionName);
        }
    }
}
