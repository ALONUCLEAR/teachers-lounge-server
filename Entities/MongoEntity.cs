using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public interface MongoEntity: MongoSerializable
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
    }
}