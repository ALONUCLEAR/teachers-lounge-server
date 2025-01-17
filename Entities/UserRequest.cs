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
    public class UserRequest: MongoEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public string govId { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public UserInfo info { get; set; }
        public string[] associatedSchools { get; set; }
        public string? message { get; set; }

        public UserRequest()
        {
            id = "";
            govId = "";
            email = "";
            password = "";
            role = Role.Base;
            info = new UserInfo();
            associatedSchools = new string[0];
        }

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
        }
        public UserRequest(UserRequest userRequest)
        {
            this.id = userRequest.id;
            this.govId = userRequest.govId;
            this.email = userRequest.email;
            this.password = userRequest.password;
            this.role = userRequest.role;
            this.info = new UserInfo(userRequest.info);
            this.associatedSchools = userRequest.associatedSchools.ToArray();

            if (userRequest.message != null)
            {
                this.message = userRequest.message;
            }
        }


        public BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();

            if (id.IsObjectId())
            {
                fullDocument.Add("_id", ObjectId.Parse(id));
            }

            fullDocument.Add("govId", govId);
            fullDocument.Add("email", email);
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
    }

    public class UserInfo: MongoSerializable
    {
        public string firstName { get; set; }
        public string lastName { get; set; }

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
    }
}
