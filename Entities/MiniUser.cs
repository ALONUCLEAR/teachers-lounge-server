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

    public struct Role
    {
        public const string Base = "Base";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
        public const string Support = "Support";

        public static bool isValid(string maybRole)
        {
            return typeof(Role).GetFields().Some(field => field.Name.Equals(maybRole));
        }

        public static string[] GetAllRoles()
        {
            return typeof(Role).GetFields().Map(field => field.Name);
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
        public string password { get; set; }
        public string role { get; set; }
        public UserInfo info { get; set; }
        public string[] associatedSchools { get; set; }

        public MiniUser()
        {
            id = "";
            govId = "";
            email = "";
            activityStatus = ActivityStatus.Pending;
            password = "";
            role = "";
            info = new UserInfo();
            associatedSchools = new string[0];
        }

        public MiniUser(string id, string govId, string email, string activityStatus, string password, string role, UserInfo info, string[] associatedIds)
        {
            this.id = id;
            this.govId = govId;
            this.email = email;
            this.activityStatus = activityStatus;
            this.password = password;
            this.role = role;
            this.info = info;
            this.associatedSchools = associatedIds;
        }

        public MiniUser(MiniUser miniUser)
        {
            this.id = miniUser.id;
            this.govId = miniUser.govId;
            this.email = miniUser.email;
            this.activityStatus = miniUser.activityStatus;
            this.password = miniUser.password;
            this.role = miniUser.role;
            this.info = new UserInfo(miniUser.info);
            this.associatedSchools = miniUser.associatedSchools.ToArray();
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();

            if (id.IsObjectId())
            {
                fullDocument.Add("_id", ObjectId.Parse(id));
            }

            fullDocument.Add("govId", govId);
            fullDocument.Add("email", email);
            fullDocument.Add("activityStatus", activityStatus);
            fullDocument.Add("password", password);
            fullDocument.Add("role", role);
            fullDocument.Add("info", info.ToBsonDocument());
            fullDocument.Add("associatedSchools", new BsonArray(associatedSchools));

            return fullDocument;
        }

        public static MiniUser FromBsonDocument(BsonDocument document, bool areObjectIdsEnforced = false)
        {
            MiniUser result = new MiniUser();

            result.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            result.govId = document.GetValueOrDefault<string>("govId") ?? "";
            result.email = document.GetValueOrDefault<string>("email") ?? "";
            result.activityStatus = document.GetValueOrDefault<string>("activityStatus") ?? "";
            result.password = document.GetValueOrDefault<string>("password") ?? "";
            result.role = document.GetValueOrDefault<string>("role") ?? "";
            result.info = UserInfo.FromBsonDocument(document.GetValue("info").AsBsonDocument);

            var schoolIdMapper = (BsonValue x) => areObjectIdsEnforced ?  x.AsObjectId.ToString() : x.AsString;

            result.associatedSchools = document.GetValue("associatedSchools").AsBsonArray.Select(schoolIdMapper).ToArray();

            return result;
        }
    }
    public class UserInfo : MongoSerializable
    {
        public string firstName { get; set; }
        public string lastName { get; set; }

        public string fullName => $"{firstName} {lastName}";

        public UserInfo()
        {
            firstName = "";
            lastName = "";
        }

        public UserInfo(string firstName, string lastName)
        {
            this.firstName = firstName;
            this.lastName = lastName;
        }

        public UserInfo(UserInfo toCopy)
        {
            this.firstName = toCopy.firstName;
            this.lastName = toCopy.lastName;
        }

        public BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();
            fullDocument.Add("firstName", firstName);
            fullDocument.Add("lastName", lastName);

            return fullDocument;
        }
        public static UserInfo FromBsonDocument(BsonDocument document)
        {
            UserInfo result = new UserInfo();

            result.firstName = document.GetValueOrDefault<string>("firstName") ?? "";
            result.lastName = document.GetValueOrDefault<string>("lastName") ?? "";

            return result;
        }
    }
}
