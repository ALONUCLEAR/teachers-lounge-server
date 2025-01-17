using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace teachers_lounge_server.Entities
{
    [BsonNoId]
    public class School : MongoEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public string name { get; set; }
        public GovernmentData municipality { get; set; }
        public Address address { get; set; }
        public School()
        {
            id = "";
            name = "";
            municipality = new GovernmentData();
            address = new Address();
        }
        public School(string id, string name, GovernmentData municipality, Address address)
        {
            this.id = id;
            this.name = name;
            this.municipality = new GovernmentData(municipality);
            this.address = new Address(address);
        }
        public BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();

            if (id.IsObjectId())
            {
                fullDocument.Add("_id", ObjectId.Parse(id));
            }

            fullDocument.Add("name", name);
            fullDocument.Add("municipality", municipality.ToBsonDocument());
            fullDocument.Add("address", address.ToBsonDocument());

            return fullDocument;
        }
    }

    public class Address : MongoSerializable
    {
        public Street street { get; set; }
        public int houseNumber { get; set; }
        public Address() { street = new Street(); }
        public Address(Street street, int houseNumber)
        {
            this.street = street;
            this.houseNumber = houseNumber;
        }

        public Address(Address toCopy)
        {
            this.street = toCopy.street;
            this.houseNumber = toCopy.houseNumber;
        }

        public BsonDocument ToBsonDocument()
        {
            BsonDocument fullDocument = new BsonDocument();
            fullDocument.Add("street", street.ToBsonDocument());
            fullDocument.Add("houseNumber", houseNumber);

            return fullDocument;
        }
    }
}
