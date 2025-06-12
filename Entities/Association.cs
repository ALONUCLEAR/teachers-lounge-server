using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public struct AssociationType
    {
        public const string Normal = "Normal";
        public const string Subject = "Subject";

        public static bool isValid(string maybAssociationType)
        {
            return typeof(AssociationType).GetFields().Some(field => field.Name.Equals(maybAssociationType));
        }
    }

    [BsonNoId]
    public class Association : DeserializableMongoEntity<Association>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string id { get; set; }

        public string name { get; set; }

        public string type { get; set; }

        public string[] associatedSchools { get; set; }

        public Association()
        {
            id = "";
            name = "";
            type = AssociationType.Normal;
            associatedSchools = new string[0];
        }

        public Association(string id, string name, string type, string[] associatedSchools)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.associatedSchools = associatedSchools.ShallowClone();
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();

            if (id.IsObjectId())
            {
                fullDocument.Add("_id", ObjectId.Parse(id));
            }

            fullDocument.Add("name", name);
            fullDocument.Add("type", type);
            fullDocument.Add("associatedSchools", new BsonArray(associatedSchools.Map(ObjectId.Parse)));

            return fullDocument;
        }

        public static new Association FromBsonDocument(BsonDocument document)
        {
            Association result = new Association();

            result.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            result.name = document.GetValueOrDefault<string>("name") ?? "";
            result.type = document.GetValueOrDefault<string>("type") ?? "";
            result.associatedSchools = document.GetValue("associatedSchools").AsBsonArray.Select(x => x.AsObjectId.ToString()).ToArray();

            return result;
        }
    }
}
