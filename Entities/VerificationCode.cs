using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class VerificationCode : DeserializableMongoEntity<VerificationCode>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string id { get; set; }

        public string govId { get; set; }
        public string code { get; set; }
        public DateTime expiryDate { get; set; }

        public VerificationCode()
        {
            id = "";
            govId = "";
            code = "";
            expiryDate = DateTime.UtcNow;
        }

        public VerificationCode(string id, string govId, string code, DateTime expiryDate)
        {
            this.id = id;
            this.govId = govId;
            this.code = code;
            this.expiryDate = expiryDate;
        }

        public VerificationCode(string govId, string code, double validDurationInMinutes = 5)
            : this("", govId, code, DateTime.Now.AddMinutes(validDurationInMinutes))
        {
        }

        public override BsonDocument ToBsonDocument()
        {
            var document = new BsonDocument();

            if (id.IsObjectId())
            {
                document.Add("_id", ObjectId.Parse(id));
            }

            document.Add("govId", govId);
            document.Add("code", code);
            document.Add("expiryDate", expiryDate);

            return document;
        }

        public static new VerificationCode FromBsonDocument(BsonDocument document)
        {
            var result = new VerificationCode();

            result.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            result.govId = document.GetValueOrDefault<string>("govId") ?? "";
            result.code = document.GetValueOrDefault<string>("code") ?? "";
            result.expiryDate = document.GetValueOrDefault<DateTime>("expiryDate");

            return result;
        }
    }
}
