using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class GovernmentData: MongoSerializable
    {
        public int id { get; set; }
        public string name { get; set; }
        public int fk { get; set; }
        public GovernmentData() { name = ""; }
        public GovernmentData(int id, string name, int fk)
        {
            this.id = id;
            this.name = name;
            this.fk = fk;
        }

        public GovernmentData(GovernmentData toCopy)
        {
            this.id = toCopy.id;
            this.name = toCopy.name;
            this.fk = toCopy.fk;
        }

        public virtual BsonDocument ToBsonDocument()
        {
            var fullDocument = new BsonDocument();
            fullDocument.Add("id", id);
            fullDocument.Add("name", name);
            fullDocument.Add("fk", fk);

            return fullDocument;
        }
    }
}
