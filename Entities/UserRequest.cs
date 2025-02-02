using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Net.Mail;

namespace teachers_lounge_server.Entities
{
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
    }

    [BsonNoId]
    public class UserRequest: MiniUser
    {
        public string password { get; set; }
        public string role { get; set; }
        public UserInfo info { get; set; }
        public string[] associatedSchools { get; set; }
        public string? message { get; set; }
        private void Init()
        {
            password = "";
            role = Role.Base;
            info = new UserInfo();
            associatedSchools = new string[0];
        }

        public UserRequest(): base() { Init(); }

        public UserRequest(string id, string govId, string email, string password, string role, UserInfo info, string[] associatedIds, string? message)
        {
            this.id = id;
            this.govId = govId;
            this.email = email;
            this.password = password;
            this.role = role;
            this.info = info;
            this.associatedSchools = associatedIds;
            this.message = message;
            this.activityStatus = ActivityStatus.Pending;
        }

        public UserRequest(MiniUser miniUser) : base(miniUser) { Init(); }

        public UserRequest(UserRequest userRequest): base(userRequest)
        {
            this.password = userRequest.password;
            this.role = userRequest.role;
            this.info = new UserInfo(userRequest.info);
            this.associatedSchools = userRequest.associatedSchools.ToArray();

            if (userRequest.message != null)
            {
                this.message = userRequest.message;
            }
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = base.ToBsonDocument();

            fullDocument.Add("password", password);
            fullDocument.Add("role", role);
            fullDocument.Add("info", info.ToBsonDocument());
            fullDocument.Add("associatedSchools", new BsonArray(associatedSchools));

            if (message != null)
            {
                fullDocument.Add("message", message);
            }

            return fullDocument;
        }

        public static new UserRequest FromBsonDocument(BsonDocument document)
        {
            UserRequest result = new UserRequest(MiniUser.FromBsonDocument(document));

            result.activityStatus = ActivityStatus.Pending;
            result.password = document.GetValueOrDefault<string>("password") ?? "";
            result.role = document.GetValueOrDefault<string>("role") ?? "";
            result.info = UserInfo.FromBsonDocument(document.GetValue("info").AsBsonDocument);
            result.associatedSchools = document.GetValue("associatedSchools").AsBsonArray.Select(x => x.AsString).ToArray();

            if (document.Contains("message"))
            {
                result.message = document.GetValueOrDefault<string>("message");
            }

            return result;
        }

    }

    public class UserInfo: MongoSerializable
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
