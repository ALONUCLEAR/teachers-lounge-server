using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public interface MongoEntity : MongoSerializable
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
    }

    public abstract class DeserializableMongoEntity<T> : MongoEntity where T: new ()
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public BsonDocument ToBsonDocument() => new BsonDocument();
        public static T FromBsonDocument(BsonDocument document) => new T();
    }
}