using MongoDB.Bson;

namespace teachers_lounge_server.Entities
{
    public class Street : GovernmentData
    {
        public int municipalityFk;
        public Street() : base() { }
        public Street(int id, string name, int fk, int municipalityFk) : base(id, name, fk)
        {
            this.municipalityFk = municipalityFk;
        }
        public Street(GovernmentData toCopy): base(toCopy) { }
        public Street(Street toCopy) : base(toCopy)
        {
            this.municipalityFk = toCopy.municipalityFk;
        }

        public override BsonDocument ToBsonDocument()
        {
            var fullDocument = base.ToBsonDocument();
            fullDocument.Add("municipalityFk", municipalityFk);

            return fullDocument;
        }

        public static new Street FromBsonDocument(BsonDocument document)
        {
            Street result = new Street(GovernmentData.FromBsonDocument(document));
            result.municipalityFk = document.GetValueOrDefault<int>("municipalityFk");

            return result;
        }
    }
}