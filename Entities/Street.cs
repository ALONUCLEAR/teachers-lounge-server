namespace teachers_lounge_server.Entities
{
    public class Street : GovernmentData
    {
        public int municipalityFk;
        public Street(int id, string name, int fk, int municipalityFk) : base(id, name, fk)
        {
            this.municipalityFk = municipalityFk;
        }

        public Street(Street toCopy) : base(toCopy)
        {
            this.municipalityFk = toCopy.municipalityFk;
        }
    }
}