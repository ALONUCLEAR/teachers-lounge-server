using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public struct ActivityStatus
    {
        public const string Active = "Active";
        public const string Blocked = "Blocked";
        public const string Pending = "Pending";

        public static bool isValid(string maybeStatus)
        {
            return typeof(ActivityStatus).GetFields().Some(field => field.Name.Equals(maybeStatus));
        }
    }

    [BsonNoId]
    public class MiniUser : DeserializableMongoEntity<MiniUser>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public new string id { get; set; }
        public string govId { get; set; }
        public string email { get; set; }
        public string activityStatus { get; set; }

        public MiniUser()
        {
            id = "";
            govId = "";
            email = "";
            activityStatus = ActivityStatus.Pending;
        }

        public MiniUser(string id, string govId, string email, string activityStatus)
        {
            this.id = id;
            this.govId = govId;
            this.email = email;
            this.activityStatus = activityStatus;
        }
        public MiniUser(MiniUser miniUser)
        {
            this.id = miniUser.id;
            this.govId = miniUser.govId;
            this.email = miniUser.email;
            this.activityStatus = miniUser.activityStatus;
        }

        public virtual new BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();

            if (id.IsObjectId())
            {
                fullDocument.Add("_id", ObjectId.Parse(id));
            }

            fullDocument.Add("govId", govId);
            fullDocument.Add("email", email);
            fullDocument.Add("activityStatus", activityStatus);

            return fullDocument;
        }

        public static new MiniUser FromBsonDocument(BsonDocument document)
        {
            MiniUser result = new MiniUser();

            result.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            result.govId = document.GetValueOrDefault<string>("govId") ?? "";
            result.email = document.GetValueOrDefault<string>("email") ?? "";
            result.activityStatus = document.GetValueOrDefault<string>("activityStatus") ?? "";

            return result;
        } 
    }
}
