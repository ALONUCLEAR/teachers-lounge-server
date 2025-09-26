using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public struct ImportanceLevel
    {
        public const string Low = "Low";
        public const string Medium= "Medium";
        public const string High = "High";
        public const string Urgent = "Urgent";

        public static bool isValid(string maybeImportanceLevel)
        {
            return typeof(ImportanceLevel).GetFields().Some(field => field.Name.Equals(maybeImportanceLevel));
        }
    }

    [BsonNoId]
    public class Alert : DeserializableMongoEntity<Alert>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string id { get; set; }
        public string title { get; set; }
        public string importanceLevel { get; set; }
        public string body { get; set; }
        public string? link { get; set; }
        public List<string> targetRecipients { get; set; }
        public List<string> remainingRecipients { get; set; }
        public DateTime dateCreated { get; set; }

        public Alert()
        {
            id = "";
            title = "";
            importanceLevel = ImportanceLevel.Low;
            body = "";
            link = null;
            targetRecipients = new List<string>();
            remainingRecipients = new List<string>();
            dateCreated = DateTime.UtcNow;
        }

        public Alert(string id, string title, string importanceLevel, string body, string? link,
                     List<string> targetRecipients, List<string> remainingRecipients, DateTime dateCreated)
        {
            this.id = id;
            this.title = title;
            this.importanceLevel = importanceLevel;
            this.body = body;
            this.link = link;
            this.targetRecipients = new List<string>(targetRecipients);
            this.remainingRecipients = new List<string>(remainingRecipients);
            this.dateCreated = dateCreated;
        }

        public Alert(Alert toCopy)
        {
            this.id = toCopy.id;
            this.title = toCopy.title;
            this.importanceLevel = toCopy.importanceLevel;
            this.body = toCopy.body;
            this.link = toCopy.link;
            this.targetRecipients = new List<string>(toCopy.targetRecipients);
            this.remainingRecipients = new List<string>(toCopy.remainingRecipients);
            this.dateCreated = toCopy.dateCreated;
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument doc = new BsonDocument();

            if (id.IsObjectId())
            {
                doc.Add("_id", ObjectId.Parse(id));
            }

            doc.Add("title", title);
            doc.Add("importanceLevel", importanceLevel);
            doc.Add("body", body);

            if (!string.IsNullOrWhiteSpace(link))
            {
                doc.Add("link", link);
            }

            doc.Add("targetRecipients", new BsonArray(targetRecipients.Map(ObjectId.Parse)));
            doc.Add("remainingRecipients", new BsonArray(remainingRecipients.Map(ObjectId.Parse)));
            doc.Add("dateCreated", dateCreated);

            return doc;
        }

        public static new Alert FromBsonDocument(BsonDocument document)
        {
            Alert result = new Alert();

            result.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            result.title = document.GetValueOrDefault<string>("title") ?? "";
            result.importanceLevel = document.GetValueOrDefault<string>("importanceLevel") ?? ImportanceLevel.Low;
            result.body = document.GetValueOrDefault<string>("body") ?? "";
            result.link = document.TryGetValue("link", out var linkValue) ? linkValue.AsString : null;

            result.targetRecipients = document.GetValueOrDefault<BsonArray>("targetRecipients")?.Map(v => v.AsObjectId.ToString())
                ?? new List<string>();

            result.remainingRecipients = document.GetValueOrDefault<BsonArray>("remainingRecipients")?.Map(v => v.AsObjectId.ToString())
                ?? new List<string>();

            result.dateCreated = document.GetValueOrDefault<DateTime>("dateCreated");

            return result;
        }
    }
}
