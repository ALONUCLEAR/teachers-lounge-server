using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace teachers_lounge_server.Entities
{
    public class UserPreferences : MongoSerializable
    {
        public string[] popupAlerts { get; set; }

        public UserPreferences() { 
            popupAlerts = new string[]{ ImportanceLevel.Low, ImportanceLevel.Medium, ImportanceLevel.High, ImportanceLevel.Urgent };
        }

        public UserPreferences(string[] popupAlerts)
        {
            this.popupAlerts = popupAlerts;
        }

        public UserPreferences(UserPreferences toCopy)
        {
            this.popupAlerts = toCopy.popupAlerts.ShallowClone();
        }

        public BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();
            fullDocument.Add("popupAlerts", new BsonArray(popupAlerts));

            return fullDocument;
        }

        public static UserPreferences FromBsonDocument(BsonDocument document)
        {
            UserPreferences result = new UserPreferences();

            result.popupAlerts = document.GetValue("popupAlerts").AsBsonArray.Select(x => x.AsString).ToArray(); ;

            return result;
        }
    }

    [BsonNoId]
    public class User : MiniUser
    {
        public UserPreferences preferences { get; set; }

        public User(): base()
        {
            this.preferences = new UserPreferences();
        }

        public User(string id, string govId, string email, string activityStatus, string password, string role, UserInfo info, string[] associatedIds)
            : base(id, govId, email, activityStatus, password, role, info, associatedIds)
        {
            this.preferences = new UserPreferences();
        }

        public User(MiniUser miniUser) : base(miniUser)
        {
            this.preferences = new UserPreferences();
        }

        public User(User user) : base(user)
        {
            this.preferences = new UserPreferences();
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = base.ToBsonDocument();
            var objIdMapper = (string objId) => ObjectId.Parse(objId);

            fullDocument.Set("associatedSchools", new BsonArray(associatedSchools.Map(objIdMapper)));
            fullDocument.Add("preferences", preferences.ToBsonDocument());

            return fullDocument;
        }

        public static new User FromBsonDocument(BsonDocument document)
        {
            User result = new User(MiniUser.FromBsonDocument(document, true));

            result.preferences = UserPreferences.FromBsonDocument(document.GetValue("preferences").AsBsonDocument);

            return result;
        }
    }
}
