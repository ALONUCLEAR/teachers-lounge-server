namespace teachers_lounge_server.Entities
{
    public class School
    {
        public string id { get; set; }
        public string name { get; set; }
        public GovernmentData municipality { get; set; }
        public Address address { get; set; }

        public School(string id, string name, GovernmentData municipality, Address address)
        {
            this.id = id;
            this.name = name;
            this.municipality = new GovernmentData(municipality);
            this.address = new Address(address);
        }
    }

    public class Address
    {
        public Street street { get; set; }
        public int houseNumber { get; set; }

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
    }
}
