using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class Comment : ContentEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string parentId { get; set; } // Could be a post or another comment

        [BsonRepresentation(BsonType.ObjectId)]
        public string parentPostId { get; set; } // Always refers to the root post

        public Comment(): base()
        {
            parentId = "";
            parentPostId = "";
        }

        public Comment(string id, string authorId, string parentId, string parentPostId, string body, byte[][] media, DateTime publishedAt, DateTime? lastUpdatedAt, int totalChildrenCount)
            : base(id, authorId, body, media, publishedAt, lastUpdatedAt, totalChildrenCount)
        {
            this.parentId = parentId;
            this.parentPostId = parentPostId;
        }

        public Comment(ContentEntity content): base(content)
        {
            this.parentId = "";
            this.parentPostId = "";
        }

        public Comment(Comment comment): base(comment)
        {
            this.parentId = "";
            this.parentPostId = "";
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument doc = base.ToBsonDocument();
            
            doc.Add("parentId", ObjectId.Parse(parentId));
            doc.Add("parentPostId", ObjectId.Parse(parentPostId));

            return doc;
        }

        public static new Comment FromBsonDocument(BsonDocument document)
        {
            Comment comment = new Comment(ContentEntity.FromBsonDocument(document));

            comment.parentId = document.GetValueOrDefault<ObjectId>("parentId").ToString();
            comment.parentPostId = document.GetValueOrDefault<ObjectId>("parentPostId").ToString();

            return comment;
        }
    }
}
