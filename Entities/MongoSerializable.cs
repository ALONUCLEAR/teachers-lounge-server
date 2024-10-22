using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public interface MongoSerializable
    {
        public BsonDocument ToBsonDocument();
    }
}
