using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public class ContentEntity: DeserializableMongoEntity<ContentEntity>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string authorId { get; set; }

        public string body { get; set; }

        public byte[][] media { get; set; }

        public DateTime publishedAt { get; set; }

        [BsonIgnoreIfNull]
        public DateTime? lastUpdatedAt { get; set; }

        public int totalChildrenCount { get; set; } = 0;

        public ContentEntity()
        {
            id = "";
            authorId = "";
            body = "";
            media = new byte[0][];
            publishedAt = DateTime.UtcNow;
            lastUpdatedAt = null;
        }

        public ContentEntity(string id, string authorId, string body, byte[][] media, DateTime publishedAt, DateTime? lastUpdatedAt, int totalChildrenCount)
        {
            this.id = id;
            this.authorId = authorId;
            this.body = body;
            this.media = media;
            this.publishedAt = publishedAt;
            this.lastUpdatedAt = lastUpdatedAt;
            this.totalChildrenCount = totalChildrenCount;
        }

        public ContentEntity(ContentEntity content)
        {
            this.id = content.id;
            this.authorId = content.authorId;
            this.body = content.body;
            this.media = content.media;
            this.publishedAt = content.publishedAt;
            this.lastUpdatedAt = content.lastUpdatedAt;
            this.totalChildrenCount = content.totalChildrenCount;
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument doc = new BsonDocument();

            if (id.IsObjectId())
                doc.Add("_id", ObjectId.Parse(id));

            doc.Add("authorId", ObjectId.Parse(authorId));
            doc.Add("body", body);
            doc.Add("media", new BsonArray(media.Map(x => new BsonBinaryData(x))));
            doc.Add("publishedAt", publishedAt);
            doc.Add("lastUpdatedAt", lastUpdatedAt == null ? BsonNull.Value : (BsonValue)lastUpdatedAt);
            doc.Add("totalChildrenCount", totalChildrenCount);

            return doc;
        }

        public static new ContentEntity FromBsonDocument(BsonDocument document)
        {
            ContentEntity content = new ContentEntity();

            content.id = document.GetValueOrDefault<ObjectId>("_id").ToString();
            content.authorId = document.GetValueOrDefault<ObjectId>("authorId").ToString();
            content.body = document.GetValueOrDefault<string>("body") ?? "";
            content.media = document.GetValueOrDefault<BsonArray>("media")?
                .Map(x => ((BsonBinaryData)x).Bytes)
                .ToArray() ?? Array.Empty<byte[]>();
            content.publishedAt = document.GetValueOrDefault<DateTime>("publishedAt");
            content.lastUpdatedAt = document.Contains("lastUpdatedAt")
                ? document.GetValueOrDefault<DateTime>("lastUpdatedAt").ToUniversalTime()
                : null;
            content.totalChildrenCount = document.GetValueOrDefault<int>("totalChildrenCount");

            return content;
        }
    }
}
