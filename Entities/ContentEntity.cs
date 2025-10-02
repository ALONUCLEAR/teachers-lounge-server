using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public class MediaItem
    {
        public byte[] Data { get; set; }
        public string Type { get; set; } = "image/jpeg";

        public MediaItem(byte[] data, string mimeType)
        {
            Data = data;
            Type = mimeType;
        }
    }

    public class ContentEntity: DeserializableMongoEntity<ContentEntity>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string authorId { get; set; }

        public string body { get; set; }

        public MediaItem[] media { get; set; }

        public DateTime publishedAt { get; set; }

        [BsonIgnoreIfNull]
        public DateTime? lastUpdatedAt { get; set; }

        public int totalChildrenCount { get; set; } = 0;

        public ContentEntity()
        {
            id = "";
            authorId = "";
            body = "";
            media = new MediaItem[0];
            publishedAt = DateTime.UtcNow;
            lastUpdatedAt = null;
        }

        public ContentEntity(string id, string authorId, string body, MediaItem[] media, DateTime publishedAt, DateTime? lastUpdatedAt, int totalChildrenCount)
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
            doc.Add("media", new BsonArray(media.Map(item =>
                new BsonDocument {
                    { "data", new BsonBinaryData(item.Data) },
                    { "type", item.Type }
                }
            )));
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

            if (document.TryGetValue("media", out var mediaDocument) && mediaDocument is BsonArray mediaArray)
            {
                content.media = mediaArray.Map(file =>
                {
                    var doc = file as BsonDocument;
                    var data = doc!.GetValue("data").AsBsonBinaryData.Bytes;
                    var mimeType = doc.GetValueOrDefault<string>("type") ?? "image/jpeg";
                    return new MediaItem(data, mimeType);
                })
                .ToArray() ?? Array.Empty<MediaItem>();
            }

            content.publishedAt = document.GetValueOrDefault<DateTime>("publishedAt");

            if (document.TryGetValue("lastUpdatedAt", out var bsonValue) && bsonValue.IsBsonDateTime)
            {
                content.lastUpdatedAt = bsonValue.ToUniversalTime();
            }
            else
            {
                content.lastUpdatedAt = null;
            }

            content.totalChildrenCount = document.GetValueOrDefault<int>("totalChildrenCount");

            return content;
        }
    }
}
