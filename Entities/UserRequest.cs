using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class UserRequest: MiniUser
    {
        public string? message { get; set; }

        public UserRequest(): base() {}

        public UserRequest(string id, string govId, string email, string password, string role, UserInfo info, string[] associatedIds, string? message)
                        : base(id, govId, email, ActivityStatus.Pending, password, role, info, associatedIds)
        {
            this.message = message;
        }

        public UserRequest(MiniUser miniUser) : base(miniUser) {}

        public UserRequest(UserRequest userRequest): base(userRequest)
        {
            if (userRequest.message != null)
            {
                this.message = userRequest.message;
            }
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = base.ToBsonDocument();
            fullDocument.Remove("activityStatus");

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

            if (document.Contains("message"))
            {
                result.message = document.GetValueOrDefault<string>("message");
            }

            return result;
        }
    }
}
