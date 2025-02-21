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
        /// <summary>
        /// This id SHOULD be overriden, it's just here so we can implement MongoEntity but it seems like something in the inheritance messes up the mongo attributes
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public BsonDocument ToBsonDocument() => new BsonDocument();
        public static T FromBsonDocument(BsonDocument document) => new T();
    }
}