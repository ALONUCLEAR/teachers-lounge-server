using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class Post : ContentEntity
    {
        public string title { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string subjectId { get; set; }

        public Post(): base()
        {
            title = "";
            subjectId = "";
        }

        public Post(string id, string title, string subjectId, string authorId, string body, MediaItem[] media, DateTime publishedAt, DateTime? lastUpdatedAt, int totalChildrenCount)
            : base(id, authorId, body, media, publishedAt, lastUpdatedAt, totalChildrenCount)
        {
            this.title = title;
            this.subjectId = subjectId;
        }

        public Post(ContentEntity content): base(content)
        {
            this.title = "";
            this.subjectId = "";
        }

        public Post(Post post): base(post)
        {
            this.title = post.title;
            this.subjectId = post.subjectId;
        }

        public override BsonDocument ToBsonDocument()
        {
            BsonDocument document = base.ToBsonDocument();

            document.Add("title", title);
            document.Add("subjectId", ObjectId.Parse(subjectId));

            return document;
        }

        public static new Post FromBsonDocument(BsonDocument document)
        {
            Post result = new Post(ContentEntity.FromBsonDocument(document));

            result.title = document.GetValueOrDefault<string>("title") ?? "";
            result.subjectId = document.GetValueOrDefault<ObjectId>("subjectId").ToString();

            return result;
        }
    }

    public class ExpandedPost: Post
    {
        public ExpandedComment[] children { get; set; } = new ExpandedComment[0];

        public ExpandedPost(Post post) : base(post) { }

        public ExpandedPost(Post post, ExpandedComment[] children) : base(post)
        {
            this.children = children;
        }

        public ExpandedPost(Post post, Comment[] children) : base(post)
        {
            this.children = children.Map(comment => new ExpandedComment(comment));
        }
    }
}
