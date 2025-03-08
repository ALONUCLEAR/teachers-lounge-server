using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class GovernmentData: MongoSerializable
    {
        public int id { get; set; }
        public string name { get; set; }
        public GovernmentData() { name = ""; }
        public GovernmentData(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public GovernmentData(GovernmentData toCopy)
        {
            this.id = toCopy.id;
            this.name = toCopy.name;
        }

        public virtual BsonDocument ToBsonDocument()
        {
            var fullDocument = new BsonDocument();
            fullDocument.Add("id", id);
            fullDocument.Add("name", name);

            return fullDocument;
        }

        public static GovernmentData FromBsonDocument(BsonDocument document)
        {
            GovernmentData result = new GovernmentData();

            result.id = document.GetValue("id").AsInt32;
            result.name = document.GetValue("name").AsString;

            return result;
        }
    }
}
