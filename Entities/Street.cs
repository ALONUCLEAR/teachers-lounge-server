using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public class Street : GovernmentData
    {
        public int municipalityId;
        public Street() : base() { }
        public Street(int id, string name, int municipalityId) : base(id, name)
        {
            this.municipalityId = municipalityId;
        }
        public Street(GovernmentData toCopy): base(toCopy) { }
        public Street(Street toCopy) : base(toCopy)
        {
            this.municipalityId = toCopy.municipalityId;
        }

        public override BsonDocument ToBsonDocument()
        {
            var fullDocument = base.ToBsonDocument();
            fullDocument.Add("municipalityId", municipalityId);

            return fullDocument;
        }

        public static new Street FromBsonDocument(BsonDocument document)
        {
            Street result = new Street(GovernmentData.FromBsonDocument(document));
            result.municipalityId = document.GetValueOrDefault<int>("municipalityId");

            return result;
        }
    }
}