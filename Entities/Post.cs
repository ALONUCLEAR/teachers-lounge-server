using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class Post : DeserializableMongoEntity<Post>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string id { get; set; }

        public string title { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string subjectId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string authorId { get; set; }

        public string body { get; set; }

        public byte[][] media { get; set; }

        public DateTime publishedAt { get; set; }

        public DateTime? lastUpdatedAt { get; set; }

        public int totalChildrenCount { get; set; }

        public Post()
        {
            id = "";
            title = "";
            subjectId = "";
            authorId = "";
            body = "";
            media = new byte[0][];
            publishedAt = DateTime.UtcNow;
            lastUpdatedAt = null;
            totalChildrenCount = 0;
        }

        public Post(string id, string title, string subjectId, string authorId, string body, byte[][] media, DateTime publishedAt, DateTime? lastUpdatedAt, int totalChildrenCount)
        {
            this.id = id;
            this.title = title;
            this.subjectId = subjectId;
            this.authorId = authorId;
            this.body = body;
            this.media = media;
            this.publishedAt = publishedAt;
            this.lastUpdatedAt = lastUpdatedAt;
            this.totalChildrenCount = totalChildrenCount;
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument document = new BsonDocument();

            if (id.IsObjectId())
            {
                document.Add("_id", ObjectId.Parse(id));
            }

            document.Add("title", title);
            document.Add("subjectId", ObjectId.Parse(subjectId));
            document.Add("authorId", ObjectId.Parse(authorId));
            document.Add("body", body);

            document.Add("media", new BsonArray(media.Select(b => new BsonBinaryData(b))));

            document.Add("publishedAt", publishedAt);

            if (lastUpdatedAt.HasValue)
            {
                document.Add("lastUpdatedAt", lastUpdatedAt.Value);
            }
            else
            {
                document.Add("lastUpdatedAt", BsonNull.Value);
            }

            document.Add("totalChildrenCount", totalChildrenCount);

            return document;
        }

        public static new Post FromBsonDocument(BsonDocument document)
        {
            Post result = new Post();

            result.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            result.title = document.GetValueOrDefault<string>("title") ?? "";
            result.subjectId = document.GetValueOrDefault<ObjectId>("subjectId").ToString();
            result.authorId = document.GetValueOrDefault<ObjectId>("authorId").ToString();
            result.body = document.GetValueOrDefault<string>("body") ?? "";

            result.media = document.GetValue("media").AsBsonArray
                .Select(m => m.AsBsonBinaryData.Bytes)
                .ToArray();

            result.publishedAt = document.GetValueOrDefault<DateTime>("publishedAt");
            result.lastUpdatedAt = document.TryGetValue("lastUpdatedAt", out var lastUpdateVal) && lastUpdateVal.IsBsonDateTime
                ? lastUpdateVal.ToUniversalTime()
                : null;

            result.totalChildrenCount = document.GetValueOrDefault<int>("totalChildrenCount");

            return result;
        }
    }

}
